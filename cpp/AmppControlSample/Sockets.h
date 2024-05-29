//
// Copyright Grass Valley
//

#ifndef SOCKETS_H_
#define SOCKETS_H_

#ifdef _WIN32
// Necessary to build in Windows
#define ASIO_ERROR_CATEGORY_NOEXCEPT noexcept( true )

#endif

// NOTE:
//    - This is a minimal implementation of websocket functionality strongly
//      inspired on the websocketpp library samples.

#include <iostream>
#include <websocketpp/config/asio_client.hpp>
#include <websocketpp/client.hpp>

typedef websocketpp::client<websocketpp::config::asio_tls_client> client;
typedef websocketpp::lib::shared_ptr<websocketpp::lib::asio::ssl::context> context_ptr;

using websocketpp::lib::placeholders::_1;
using websocketpp::lib::placeholders::_2;
using websocketpp::lib::bind;

#include <nlohmann/json.hpp>

namespace
{
    const std::string SUB_PROTOCOL = "bson-rpc";
    // const std::string SUB_PROTOCOL = "json-rpc";
}

// for convenience
using json = nlohmann::json;

class connection_metadata
{
public:
    typedef websocketpp::lib::shared_ptr<connection_metadata> ptr;

    connection_metadata( int id, websocketpp::connection_hdl hdl, std::string uri )
        : m_id( id )
        , m_hdl( hdl )
        , m_status( "Connecting" )
        , m_uri( uri )
        , m_server( "N/A" )
    {
    }

    void on_open( client* c, websocketpp::connection_hdl hdl )
    {
        m_status = "Open";

        client::connection_ptr con = c->get_con_from_hdl( hdl );
        m_server = con->get_response_header( "Server" );
    }

    void on_fail( client* c, websocketpp::connection_hdl hdl )
    {
        m_status = "Failed";

        client::connection_ptr con = c->get_con_from_hdl( hdl );
        m_server = con->get_response_header( "Server" );
        m_error_reason = con->get_ec().message();
    }

    void on_close( client* c, websocketpp::connection_hdl hdl )
    {
        m_status = "Closed";
        client::connection_ptr con = c->get_con_from_hdl( hdl );
        std::stringstream s;
        s << "close code: " << con->get_remote_close_code() << " ("
            << websocketpp::close::status::get_string( con->get_remote_close_code() )
            << "), close reason: " << con->get_remote_close_reason();
        m_error_reason = s.str();
    }

    void on_message( websocketpp::connection_hdl, client::message_ptr msg )
    {
        if ( msg->get_opcode() == websocketpp::frame::opcode::text )
        {
            std::cout << "Receiving TEXT message:" << std::endl;
            std::cout << msg->get_payload() << std::endl;

            m_messages.push_back( "<< " + msg->get_payload() );
        }
        else
        {
            std::cout << "Receiving BINARY message." << std::endl;
            m_messages.push_back( "<< " + websocketpp::utility::to_hex( msg->get_payload() ) );
            std::string rawString = msg->get_raw_payload();
            // Transfer to a std::vector:
            std::vector<uint8_t> binaryBuffer( rawString.begin(), rawString.end() );
            json j_from_bson = json::from_bson( binaryBuffer );

            std::cout << "*** From BSON to JSON: ***" << std::endl;
            std::cout << j_from_bson.dump() << std::endl;
            std::cout << "***************************" << std::endl;

        }
    }

    websocketpp::connection_hdl get_hdl() const
    {
        return m_hdl;
    }

    int get_id() const
    {
        return m_id;
    }

    std::string get_status() const
    {
        return m_status;
    }

    void record_sent_message( std::string message )
    {
        m_messages.push_back( ">> " + message );
    }

private:
    int m_id;
    websocketpp::connection_hdl m_hdl;
    std::string m_status;
    std::string m_uri;
    std::string m_server;
    std::string m_error_reason;
    std::vector<std::string> m_messages;
};


class websocket_endpoint
{
public:
    websocket_endpoint() : m_next_id( 0 )
    {
        // Set logging to be pretty verbose (everything except message payloads)
        m_endpoint.set_access_channels( websocketpp::log::alevel::all );
        m_endpoint.clear_access_channels( websocketpp::log::alevel::frame_payload );
        m_endpoint.set_error_channels( websocketpp::log::elevel::all );

        m_endpoint.set_tls_init_handler( websocketpp::lib::bind(
            &websocket_endpoint::on_tls_init,
            this,
            websocketpp::lib::placeholders::_1
        ) );

        // Initialize ASIO
        m_endpoint.init_asio();
        m_endpoint.start_perpetual();

        m_thread = websocketpp::lib::make_shared<websocketpp::lib::thread>( &client::run, &m_endpoint );
    }

    ~websocket_endpoint()
    {
        m_endpoint.stop_perpetual();

        for ( con_list::const_iterator it = m_connection_list.begin(); it != m_connection_list.end(); ++it )
        {
            if ( it->second->get_status() != "Open" )
            {
                // Only close open connections
                continue;
            }

            std::cout << "> Closing connection " << it->second->get_id() << std::endl;

            websocketpp::lib::error_code ec;
            m_endpoint.close( it->second->get_hdl(), websocketpp::close::status::going_away, "", ec );
            if ( ec )
            {
                std::cout << "> Error closing connection " << it->second->get_id() << ": "
                    << ec.message() << std::endl;
            }
        }

        m_thread->join();
    }


    /// TLS Initialization handler
    /**
     * WebSocket++ core and the Asio Transport do not handle TLS context creation
     * and setup. This callback is provided so that the end user can set up their
     * TLS context using whatever settings make sense for their application.
     *
     * As Asio and OpenSSL do not provide great documentation for the very common
     * case of connect and actually perform basic verification of server certs this
     * example includes a basic implementation (using Asio and OpenSSL) of the
     * following reasonable default settings and verification steps:
     *
     * - Disable SSLv2 and SSLv3
     * - Load trusted CA certificates and verify the server cert is trusted.
     * - Verify that the hostname matches either the common name or one of the
     *   subject alternative names on the certificate.
     *
     * This is not meant to be an exhaustive reference implimentation of a perfect
     * TLS client, but rather a reasonable starting point for building a secure
     * TLS encrypted WebSocket client.
     *
     * If any TLS, Asio, or OpenSSL experts feel that these settings are poor
     * defaults or there are critically missing steps please open a GitHub issue
     * or drop a line on the project mailing list.
     *
     * Note the bundled CA cert ca-chain.cert.pem is the CA cert that signed the
     * cert bundled with echo_server_tls. You can use print_client_tls with this
     * CA cert to connect to echo_server_tls as long as you use /etc/hosts or
     * something equivilent to spoof one of the names on that cert
     * (websocketpp.org, for example).
     */
    context_ptr on_tls_init( websocketpp::connection_hdl )
    {
        context_ptr ctx = websocketpp::lib::make_shared<websocketpp::lib::asio::ssl::context>( websocketpp::lib::asio::ssl::context::sslv23 );

        try
        {
            ctx->set_options( websocketpp::lib::asio::ssl::context::default_workarounds |
                websocketpp::lib::asio::ssl::context::no_sslv2 |
                websocketpp::lib::asio::ssl::context::no_sslv3 |
                websocketpp::lib::asio::ssl::context::single_dh_use );


        }
        catch ( std::exception& e )
        {
            std::cout << e.what() << std::endl;
        }
        return ctx;
    }



    int connect( std::string const& uri )
    {
        websocketpp::lib::error_code ec;

        client::connection_ptr con = m_endpoint.get_connection( uri, ec );

        if ( ec )
        {
            std::cout << "> Connect initialization error: " << ec.message() << std::endl;
            return -1;
        }

        int new_id = m_next_id++;
        connection_metadata::ptr metadata_ptr = websocketpp::lib::make_shared<connection_metadata>( new_id, con->get_handle(), uri );
        m_connection_list[ new_id ] = metadata_ptr;

        con->set_open_handler( websocketpp::lib::bind(
            &connection_metadata::on_open,
            metadata_ptr,
            &m_endpoint,
            websocketpp::lib::placeholders::_1
        ) );
        con->set_fail_handler( websocketpp::lib::bind(
            &connection_metadata::on_fail,
            metadata_ptr,
            &m_endpoint,
            websocketpp::lib::placeholders::_1
        ) );
        con->set_close_handler( websocketpp::lib::bind(
            &connection_metadata::on_close,
            metadata_ptr,
            &m_endpoint,
            websocketpp::lib::placeholders::_1
        ) );
        con->set_message_handler( websocketpp::lib::bind(
            &connection_metadata::on_message,
            metadata_ptr,
            websocketpp::lib::placeholders::_1,
            websocketpp::lib::placeholders::_2
        ) );

        con->add_subprotocol( SUB_PROTOCOL );

        m_endpoint.connect( con );

        return new_id;
    }

    void close( int id, websocketpp::close::status::value code, std::string reason )
    {
        websocketpp::lib::error_code ec;

        con_list::iterator metadata_it = m_connection_list.find( id );
        if ( metadata_it == m_connection_list.end() )
        {
            std::cout << "> No connection found with id " << id << std::endl;
            return;
        }

        m_endpoint.close( metadata_it->second->get_hdl(), code, reason, ec );
        if ( ec )
        {
            std::cout << "> Error initiating close: " << ec.message() << std::endl;
        }
    }

    void send( int id, std::string message )
    {
        websocketpp::lib::error_code ec;

        con_list::iterator metadata_it = m_connection_list.find( id );
        if ( metadata_it == m_connection_list.end() )
        {
            std::cout << "> No connection found with id " << id << std::endl;
            return;
        }



        if ( SUB_PROTOCOL == "bson-rpc" )
        {
            // serialize to BSON
            std::vector<uint8_t> binaryBuffer( message.begin(), message.end() );

            json messageJson = json::parse( message );
            std::vector<std::uint8_t> v_bson = json::to_bson( messageJson );
            m_endpoint.send( metadata_it->second->get_hdl(),
                static_cast< void const* >( v_bson.data() ), v_bson.size(),
                websocketpp::frame::opcode::binary, ec );

        }
        else // "json-rpc"
        {
            // Use the following instead if in "json" sub-protocol
            m_endpoint.send( metadata_it->second->get_hdl(), message, websocketpp::frame::opcode::text, ec );
        }
        if ( ec )
        {
            std::cout << "> Error sending message: " << ec.message() << std::endl;
            return;
        }

        metadata_it->second->record_sent_message( message );
    }


    connection_metadata::ptr get_metadata( int id ) const
    {
        con_list::const_iterator metadata_it = m_connection_list.find( id );
        if ( metadata_it == m_connection_list.end() )
        {
            return connection_metadata::ptr();
        }
        else
        {
            return metadata_it->second;
        }
    }
private:
    typedef std::map<int, connection_metadata::ptr> con_list;

    client m_endpoint;
    websocketpp::lib::shared_ptr<websocketpp::lib::thread> m_thread;

    con_list m_connection_list;
    int m_next_id;
};


#endif /* SOCKETS_H_ */
