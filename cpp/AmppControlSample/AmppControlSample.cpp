//
// Copyright Grass Valley
//

#include <iostream>
#include <stdlib.h>

#include "AmppControlUtil.h"
#include "BearerToken.h"
#include "PushNotificationServer.h"
#include "RpcProtocol.h"
#include "Sockets.h"
#include "Util.h"

/*
 * ================================================================================================
DESCRIPTION:

This sample application will do the following actions:

    1) Request a bearer token through a REST API call using the provided API_KEY.
    2) Request a list of all applications currently registered to Ampp Control.
    3) For a specific application, request the list of all its workload IDs (instances), running or not.
    4) Using the bearer token, open a secure websocket.
    5) Using a specific workload ID, subscribe to the appropriate notification and status topics.
    6) Request notifications for any state changes (.getstate command) for this workload.
    7) Send a command to change the state of the application (here, request a slider change on an audiomixer).

IMPORTANT NOTES:

    - This application expects to have the user's base site name (not a URL) and the API_KEY passed as parameters
      on the command line. Without them, we won't be able to get a bearer token and the secure websocket will fail
      to open.

    - Every subscription or command will have a response of type RpcResponse, stating if the transaction was
      successful or not. This doesn't however indicates if the workload is running, this is just the response
      from the Push Notification Server.

    - Notifications will be received through unsolicited RpcRequest of type "ReceiveNotification".

    - All websocket received messages are handle by the on_message() method in "Sockets.h". This sample application
      doesn't process or try to associate messages to their originating commands; it just prints them to the screen.

    - At the top of the "Sockets.h" file, the sub-protocol used can be changed between "json-rpc" and "bson-rpc".
      We recommend leaving it at "bson-rpc" since it is the most efficient.

    - This sample application assumes that the targeted workload is running. There isn't currently any way to tell
      if a workload is running beside not receiving any notification after the .getstate command.

    - In a real world application, it is assumed that the user will already know what the targeted workload is so
      steps 2) and 3) are optionals.

 * ================================================================================================
*/


int main( int argc, char* argv[] )
{
    client c;
    websocket_endpoint endpoint;
    websocketpp::lib::error_code ec;

    std::string baseSite;
    std::string baseUrl;
    std::string credentials;
    if ( argc >= 3 )
    {
        baseSite = std::string( argv[ 1 ] );
        baseUrl = "https://" + baseSite;
        credentials = argv[ 2 ];
    }
    else
    {
        std::cout << "Usage: AmppControlSample <baseSite> <api_key>" << std::endl;
        std::cout << "Ex: ./AmppControlSample \"xxx.yyy.grassvalley.com\" " <<
            "\"NWVkYjE4ZjM3OTA3NDUzYzgzZjY0MmYzOWU5MTMwZDA6bU...\"" << std::endl;
        return -1;
    }

    //********************************************************************************
    //********************************************************************************
    // 1) Request a bearer token through a REST API call using the provided API_KEY.
    //********************************************************************************
    //********************************************************************************
    bool tokenResult = false;
    unsigned int expiresIn = 0;
    std::string bearer_token;
    std::string baseNotificationServerUri = "wss://" + baseSite + "/pushnotifications-ws";

    tokenResult = getToken( baseUrl, credentials, bearer_token, expiresIn );

    std::cout << "*******************************************" << std::endl;
    std::cout << "Current time is: " << getCurrentTimeString() << std::endl;
    std::cout << "*******************************************" << std::endl;
    std::cout << "tokenResult = " << tokenResult << std::endl;
    std::cout << "bearer_token = " << bearer_token << std::endl;
    std::cout << "expiresIn = " << expiresIn << std::endl;
    std::cout << "*******************************************" << std::endl;


    //********************************************************************************
    //********************************************************************************
    // 2) Request a list of all applications currently registered to Ampp Control.
    //********************************************************************************
    //********************************************************************************
    std::string targetApp = "AudioMixer";
    std::string targetAppWorkload;


    bool applicationsResult = false;
    std::string applicationInfo;
    applicationsResult = getAmppControlApplications( baseUrl, credentials, applicationInfo );
    json applicationsJson = json::parse( applicationInfo );
    std::cout << "applicationsResult = " << applicationsResult << std::endl;
    std::cout << "applications size = " << applicationsJson.size() << std::endl;
    // Cycle through the array of application object to see if our app is found
    bool foundApp = false;
    for ( auto it = applicationsJson.begin(); it != applicationsJson.end() && !foundApp; ++it )
    {
        if ( it.value()[ "name" ] == targetApp )
        {
            foundApp = true;
            break;
        }
    }

    std::cout << targetApp << ( foundApp ? " was found." : " was not found." ) << std::endl;
    std::cout << "*******************************************" << std::endl;


    //********************************************************************************
    //********************************************************************************
    // 3) For a specific application, request the list of all its workload IDs (instances), running or not.
    //********************************************************************************
    //********************************************************************************
    json workloadsJson;
    if ( foundApp )
    {
        bool workloadsResult = false;
        std::string workloadsInfo;
        // The following call will return an array of workload IDs for the specified app.
        workloadsResult = getAmppControlWorkloads( baseUrl, credentials, targetApp, workloadsInfo );

        // workloadsInfo is a string of format: "[ "xxx-yyy-zzz", "aaa-bbb-ccc", ... ]".
        // In order for json::parse() to accept this string, we must inclose it in curly brackets {}
        // and add a key to make it a legitimate JSON string
        workloadsJson = json::parse( "{ \"workloads\" : " + workloadsInfo + "}" );
        std::cout << "workloadsResult = " << workloadsResult << std::endl;
        std::cout << "workloads array size = " << workloadsJson[ "workloads" ].size() << std::endl;

        // For our example, we will simply take the first one. In real life, it is most probable that
        // the user will already know the workload id for his/her target application.
        targetAppWorkload = workloadsJson[ "workloads" ][ 0 ];
        std::cout << "Workload for app \"" << targetApp << "\" is " << targetAppWorkload << std::endl;
        std::cout << "*******************************************" << std::endl;

    }

    // Force a workload that we know is running.
    targetAppWorkload = "620a89fc-ace8-441b-a423-733b54aec299"; // AudioMixer
    // Check if it is in the list we just got (optional)
    for ( int i = 0; i < workloadsJson[ "workloads" ].size(); ++i )
    {
        if ( targetAppWorkload == workloadsJson[ "workloads" ][ i ] )
        {
            std::cout << "---- FOUND RUNNING WORKLOAD IN LIST ----" << std::endl;
        }
    }
    std::cout << "*******************************************" << std::endl;

    try
    {
        //********************************************************************************
        //********************************************************************************
        // 4) Using the bearer token, open a secure websocket.
        //********************************************************************************
        //********************************************************************************
        // Notes:
        //    - Endpoints are objects that handle multiple websocket connections.
        //    - Any number of connections can be created, used and closed independently.
        //    - The endpoint keeps a list of all its connections and refers to them by their
        //      id (simple index int) returned by the .connect() method.
        std::string uri = baseNotificationServerUri + "?access_token=" + bearer_token;
        int id = endpoint.connect( uri );
        if ( id != -1 )
        {
            std::cout << "> Created connection with id " << id << std::endl;
        }

#ifdef _WIN32
        Sleep( 5000 ); // Milliseconds
#else
        sleep( 5 ); // Seconds
#endif

        //********************************************************************************
        //********************************************************************************
        // 5) Using a specific workload ID, subscribe to the appropriate notification and status topics.
        //********************************************************************************
        //********************************************************************************
        // Notes:
        //    - Each command (subcsribe, unsubscribe or notification) need to have a UUID.
        //    - Here, we use getUuid() without storing it anywhere. If a new application needs to
        //      to do a better response management, this UUID may be stored and used by the receiver
        //      function to match a RpcResponse to its associated command.
        //
        std::string notifySubscribeTopic = "gv.ampp.control." + targetAppWorkload + ".*.notify";
        std::cout << ">>>>>>>>>>>>> Subscribing to \"" << notifySubscribeTopic << "\"" << std::endl;
        pushNotificationServerSubscribe( endpoint, id, getUuid(), notifySubscribeTopic );

        std::string statusSubscribeTopic = "gv.ampp.control." + targetAppWorkload + ".*.status";
        std::cout << ">>>>>>>>>>>>> Subscribing to \"" << statusSubscribeTopic << "\"" << std::endl;
        pushNotificationServerSubscribe( endpoint, id, getUuid(), statusSubscribeTopic );



        //********************************************************************************
        //********************************************************************************
        // 6) Request notifications for any state changes (.getstate command) for this workload.
        //********************************************************************************
        //********************************************************************************
        std::string getStatePayload = "{ \"Key\" : \"TestApplication\", \"Payload\" : {} }";

        std::string getStateCommand = "gv.ampp.control." + targetAppWorkload + ".getstate";
        std::cout << ">>>>>>>>>>>>> Sending command \"" << getStateCommand << "\"" << std::endl;
        pushNotificationServerSendNotification( endpoint, id, getUuid(), getStateCommand, getStatePayload );

#ifdef _WIN32
        Sleep( 5000 ); // Milliseconds
#else
        sleep( 5 ); // Seconds
#endif


        //********************************************************************************
        //********************************************************************************
        // 7) Send a command to change the state of the application (here, request a slider change on an audiomixer).
        //********************************************************************************
        //********************************************************************************
        // Notes:
        // - Since we have previously sent the .getstate command, we will receive a RpcRequest ("ReceiveNotification")
        //   for this state change.
        //
        std::string channelStatePayload = "{ \"Key\" : \"TestApplication\", \"Payload\" : {\"Index\": 1,\"Level\": 33} }";

        std::string channelStateCommand = "gv.ampp.control." + targetAppWorkload + ".channelstate";
        std::cout << std::endl << ">>>>>>>>>>>>> Sending command \"" << channelStateCommand << "\" with payload \""
            << channelStatePayload << "\"" << std::endl << std::endl;
        pushNotificationServerSendNotification( endpoint, id, getUuid(), channelStateCommand, channelStatePayload );

        // Wait a little, maybe try to modify a control in the online app itself and see if we get a notification...
#ifdef _WIN32
        Sleep( 30000 ); // Milliseconds
#else
        sleep( 30 ); // Seconds
#endif

        // Note:
        //    No need to call close as the destructor will close
        //    the connection for us. If we call the .close() first,
        //    the destructor will attempt to close it a second time
        //    and it will print a warning that the connection is no
        //    longer valid.
        // int close_code = websocketpp::close::status::normal;
        // std::string reason;
        // endpoint.close(id, close_code, reason);

    }
    catch ( websocketpp::exception const& e )
    {
        std::cout << e.what() << std::endl;
    }
}
