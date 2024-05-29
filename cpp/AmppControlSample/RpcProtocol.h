//
// Copyright Grass Valley
//

#ifndef RPC_PROTOCOL_H_
#define RPC_PROTOCOL_H_

#include <string>
#include <nlohmann/json.hpp>

// IMPORTANT NOTE:
//    - This is a very minimal implementation of the RPC protocol used by the
//      Push Notification Server. Although we could have implemented all the
//      proper setter/getter for all the objects, for simplicity's sake,
//      we only implemented what was necessary for this sample application
//      to work.

using json = nlohmann::json;

// RPC Packet
/*
{
    "packetType": "", // Will be either "RpcRequest" or "RpcResponse".
    "payload": {}     // An RpcRequest type or and RpcResponse type.
}
*/

class RpcPacket
{
public:
    RpcPacket()
        : mPacketType( RpcPacket::PacketType::RPC_REQUEST )
    {
    }

    enum PacketType
    {
        RPC_REQUEST,
        RPC_RESPONSE
    };

    bool setFromJsonString( const std::string& js )
    {
        json j = json::parse( js );

        if ( j.find( "packetType" ) != j.end() )
        {
            std::string packetType = j[ "packetType" ];
            setPacketType( packetType );
            return true;
        }

        return false;
    }

    json toJson( const json& request )
    {
        json j;

        if ( mPacketType == RpcPacket::PacketType::RPC_REQUEST )
        {
            j[ "packetType" ] = "RpcRequest";
        }
        else
        {
            j[ "packetType" ] = "RpcResponse";
        }

        j[ "payload" ] = request;

        return j;
    }

    void setPacketType( const RpcPacket::PacketType packetType )
    {
        mPacketType = packetType;
    }

    void setPacketType( const std::string& packetTypeString )
    {
        if ( packetTypeString == "RpcRequest" )
        {
            mPacketType = RpcPacket::PacketType::RPC_REQUEST;
        }
        else
        {
            mPacketType = RpcPacket::PacketType::RPC_RESPONSE;
        }
    }


    RpcPacket::PacketType setPacketType() const
    {
        return mPacketType;
    }

    std::string getPacketTypeString() const
    {
        if ( mPacketType == RpcPacket::PacketType::RPC_REQUEST )
        {
            return "RpcRequest";
        }
        else
        {
            return "RpcResponse";
        }
    }

private:

    RpcPacket::PacketType mPacketType;
};



// RPC Request
/*
{
    "requestId": "", // The guid identifying the request.
    "hubName": "",   // The name of the hub.
    "hubMethod": "", // The name of method to invoke.
    "arguments": []  // An array of method arguments.
}
*/
class RpcRequest
{
public:
    RpcRequest()
        : mRequestId( "" )
        , mHubName( "" )
        , mHubMethod( RpcRequest::HubMethod::PUBLISH_NOTIFICATION )
    {
    }

    enum HubMethod
    {
        PUBLISH_NOTIFICATION,
        SUBSCRIBE,
        UNSUBSCRIBE,
        RECEIVE_NOTIFICATION
    };

    json toJson( const json& arguments )
    {
        json j;

        j[ "requestId" ] = mRequestId;

        if ( !mHubName.empty() )
        {
            j[ "hubName" ] = mHubName;
        }
        else
        {
            j[ "hubName" ] = nullptr;
        }

        switch ( mHubMethod )
        {
        case RpcRequest::HubMethod::PUBLISH_NOTIFICATION:
            j[ "hubMethod" ] = "PublishNotification";
            break;
        case RpcRequest::HubMethod::SUBSCRIBE:
            j[ "hubMethod" ] = "Subscribe";
            break;
        case RpcRequest::HubMethod::UNSUBSCRIBE:
            j[ "hubMethod" ] = "Unsubscribe";
            break;
        case RpcRequest::HubMethod::RECEIVE_NOTIFICATION:
            j[ "hubMethod" ] = "ReceivedNotification";
            break;
        default:
            break;
        }

        j[ "arguments" ][ 0 ] = arguments;

        return j;
    }

    void setRequestId( std::string requestId )
    {
        mRequestId = requestId;
    }

    void setHubName( std::string hubName )
    {
        mHubName = hubName;
    }

    void setHubMethod( RpcRequest::HubMethod hubMethod )
    {
        mHubMethod = hubMethod;
    }

private:
    std::string mRequestId;
    std::string mHubName;
    RpcRequest::HubMethod mHubMethod;
    // std::string mArguments;
};


// RPC Response
/*
{
    "requesId": "", // The guid identifying the request this response is for.
    "returnValue": {}, // The return value.
    "exception": "", // If the call resulted in an exception, this will contain the type.
    "exceptionMessage": "" // If the call resulted in an exception, this will contain the message.
}
*/
class RpcResponse
{
public:
    RpcResponse()
        : mRequestId( "" )
        , mException( "" )
        , mExceptionMessage( "" )
    {
    }

private:
    std::string mRequestId;
    // std::string returnValue;
    std::string mException;
    std::string mExceptionMessage;

};

// RPC Service Result
/*
{
    "serviceStatus": "", // The service status.
    "isSuccess": <true/false>, // A value indicating whether or not the call was successful.
    "errorResult": { // An error object if "isSuccess" is false.
    "message": "", // An error message
    "retry": <true/false> // A value indicating whether or not to retry the call.
}
*/

class ServiceResult
{
public:
    ServiceResult()
        : mServiceStatus( "" )
        , mIsSuccess( true )
        , mErrorResultMessage( "" )
        , mRetry( false )
    {
    }

private:
    std::string mServiceStatus;
    bool mIsSuccess;
    std::string mErrorResultMessage;
    bool mRetry;

};

// Subscribe Model
/*
{
    "subscriptions": [
    // An array of subscriptions to remove.
    "subscription"
    ],
    "context": {
    "correlationId": "" // A correlation id, usually a <guid>
    }
}
*/

class SubscribeModel
{
public:
    SubscribeModel()
        : mCorrelationId( "" )
    {
    }

    json toJson( void )
    {
        json j;

        if ( mSubcription.size() > 0 )
        {
            for ( int i = 0; i < mSubcription.size(); ++i )
            {
                j[ "subscriptions" ][ i ] = mSubcription[ i ];
            }
        }

        if ( !mCorrelationId.empty() )
        {
            j[ "context" ][ "correlationId" ] = mCorrelationId;
        }

        return j;
    }

    void setCorrelationId( const std::string& correlationId )
    {
        mCorrelationId = correlationId;
    }

    void addSubscription( const std::string& subscription )
    {
        mSubcription.push_back( subscription );
    }

    void clearAllSubscriptions()
    {
        mSubcription.clear();
    }

private:
    std::vector<std::string> mSubcription;
    std::string mCorrelationId;
};

// Unsubscribe Model
/*
{
    "subscriptions": [
    // An array of subscriptions to remove.
    "subscription"
    ],
    "context": {
    "correlationId": "" // A correlation id, usually a <guid>
    }
}
*/

class UnsubscribeModel
{
public:
public:
    UnsubscribeModel()
        : mCorrelationId( "" )
    {
    }

    json toJson( void )
    {
        json j;

        if ( mSubcription.size() > 0 )
        {
            for ( int i = 0; i < mSubcription.size(); ++i )
            {
                j[ "subscriptions" ][ i ] = mSubcription[ i ];
            }
        }

        if ( !mCorrelationId.empty() )
        {
            j[ "context" ][ "correlationId" ] = mCorrelationId;
        }

        return j;
    }

    void setCorrelationId( const std::string& correlationId )
    {
        mCorrelationId = correlationId;
    }

    void addSubscription( const std::string& subscription )
    {
        mSubcription.push_back( subscription );
    }

    void clearAllSubscriptions()
    {
        mSubcription.clear();
    }

private:
    std::vector<std::string> mSubcription;
    std::string mCorrelationId;
};


// PublishNotificationModel
/*
{
    "id": "", // The id of the notification, typically a <guid>.
    "time": "", // The date-time in iso 8601 format.
    "topic": "", // A period separated word list denoting the notification topic.
    "source" : "", // The source of the notification.
    "ttl": 0, // The number of milliseconds the message should live for.
    "contentType": "", // The content type e.g. application/json
    "contentLength": 0, // The length of the content.
    "binaryContent": [], // An array of bytes containing the data.
    "context": {
                  "correlationId": // A correlation id, usually a <guid>
               }
}
*/

class PublishNotificationModel
{
public:
    PublishNotificationModel()
        : mId( "" )
        , mTime( "" )
        , mTopic( "" )
        , mSource( "" )
        , mTtl( 0 )
        , mContentType( "" )
    {
    }

    json toJson( void )
    {
        json j;

        j[ "id" ] = mId;
        j[ "time" ] = mTime;
        j[ "topic" ] = mTopic;
        j[ "source" ] = mSource;
        j[ "ttl" ] = mTtl;
        j[ "content" ] = mContent;

        if ( !mContentType.empty() )
        {
            j[ "contentType" ] = mContentType;
        }

        if ( mContentLength > 0 )
        {
            j[ "contentLength" ] = mContentLength;
        }

        if ( !mCorrelationId.empty() )
        {
            j[ "context" ][ "correlationId" ] = mCorrelationId;
        }


        return j;
    }


    void setId( const std::string& id )
    {
        mId = id;
    }

    void setTime( const std::string& time )
    {
        mTime = time;
    }

    void setTopic( const std::string& topic )
    {
        mTopic = topic;
    }

    void setSource( const std::string& source )
    {
        mSource = source;
    }

    void setTtl( uint16_t ttl )
    {
        mTtl = ttl;
    }

    void setContentType( const std::string& contentType )
    {
        mContentType = contentType;
    }

    void setContentLength( uint16_t contentLength )
    {
        mContentLength = contentLength;
    }

    void setContent( const std::string& content )
    {
        mContent = content;
    }

    void setCorrelationId( const std::string& correlationId )
    {
        mCorrelationId = correlationId;
    }


private:
    std::string mId;
    std::string mTime;
    std::string mTopic;
    std::string mSource;
    uint16_t mTtl;
    std::string mContentType;
    uint16_t mContentLength;
    std::string mContent;
    std::string mCorrelationId;
};


// PublishNotificationModel
/*
{
    "account": "", // The notifications account.
    "correlationId": "", // The correlation id that was included in the publish request.
    "id": "", // The id of the notification, typically a <guid>.
    "time": "", // The date-time in iso 8601 format.
    "topic": "", // A period separated word list denoting the notification topic.
    "source" : "", // The source of the notification.
    "ttl": 0, // The number of milliseconds the message should live for.
    "content": "", // The notification content if the publish was a simple one.
    "contentType": "", // The content type if there is binary content.
    "contentLength": 0, // The content length if there is binary content.
    "binaryContent": [], // An array of bytes containing the data.
}
*/

class ReceivedNotificationModel
{
public:
    ReceivedNotificationModel()
        : mAccount( "" )
        , mId( "" )
        , mTime( "" )
        , mTopic( "" )
        , mSource( "" )
        , mTtl( 0 )
        , mContent( "" )
        , mContentType( "" )
    {
    }

    std::string getAccount()
    {
        return mAccount;
    }
    std::string getCorrelationId()
    {
        return mCorrelationId;
    }
    std::string getId()
    {
        return mId;
    }
    std::string getTime()
    {
        return mTime;
    }
    std::string getTopic()
    {
        return mTopic;
    }
    std::string getSource()
    {
        return mSource;
    }
    uint16_t getTtl()
    {
        return mTtl;
    }
    std::string getContent()
    {
        return mContent;
    }
    std::string getContentType()
    {
        return mContentType;
    }
    uint16_t getContentLength()
    {
        return mContentLength;
    }

private:
    std::string mAccount;
    std::string mCorrelationId;
    std::string mId;
    std::string mTime;
    std::string mTopic;
    std::string mSource;
    uint16_t mTtl;
    std::string mContent;
    std::string mContentType;
    uint16_t mContentLength;
    std::vector<uint8_t> mBinaryContent;
};



class PublishNotification : public RpcRequest, public RpcPacket, public PublishNotificationModel
{
public:
    PublishNotification()
    {
        RpcPacket::setPacketType( RpcPacket::PacketType::RPC_REQUEST );
    }

    json toJson( void )
    {
        return RpcPacket::toJson( RpcRequest::toJson( PublishNotificationModel::toJson() ) );
    }
};

class SubscriptionRequest : public RpcRequest, public RpcPacket, public SubscribeModel
{
public:
    json toJson( void )
    {
        return RpcPacket::toJson( RpcRequest::toJson( SubscribeModel::toJson() ) );
    }
};

class UnsubscriptionRequest : public RpcRequest, public RpcPacket, public UnsubscribeModel
{
public:
    json toJson( void )
    {
        return RpcPacket::toJson( RpcRequest::toJson( UnsubscribeModel::toJson() ) );
    }
};


#endif /* RPC_PROTOCOL_H_ */
