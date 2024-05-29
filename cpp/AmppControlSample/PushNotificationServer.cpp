//
// Copyright Grass Valley
//

#include "PushNotificationServer.h"
#include "RpcProtocol.h"
#include "Util.h"

void pushNotificationServerSubscribe( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic )
{
    SubscriptionRequest subReq;
    subReq.setRequestId( in_requestId );
    subReq.setHubName( "" );
    subReq.setHubMethod( RpcRequest::HubMethod::SUBSCRIBE );
    subReq.addSubscription( in_topic );
    subReq.setCorrelationId( in_requestId );

    std::string jsonString = subReq.toJson().dump();

    in_endpoint.send( in_connectionId, jsonString );
}

void pushNotificationServerUnsubscribe( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic )
{
    UnsubscriptionRequest unsubReq;
    unsubReq.setRequestId( in_requestId );
    unsubReq.setHubName( "" );
    unsubReq.setHubMethod( RpcRequest::HubMethod::UNSUBSCRIBE );
    unsubReq.addSubscription( in_topic );
    unsubReq.setCorrelationId( in_requestId );

    std::string jsonString = unsubReq.toJson().dump();

    in_endpoint.send( in_connectionId, jsonString );
}

void pushNotificationServerSendNotification( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic, const std::string& in_message )
{
    PublishNotification notif;

    notif.setRequestId( in_requestId );
    notif.setHubName( "" );
    notif.setHubMethod( RpcRequest::HubMethod::PUBLISH_NOTIFICATION );
    notif.setId( in_requestId );
    notif.setTime( getCurrentTimeString() );
    notif.setTopic( in_topic );
    notif.setSource( "TestApplication" );
    notif.setTtl( 30000 );
    notif.setContent( in_message );
    notif.setContentType( "application/json" );
    notif.setContentLength( static_cast<uint16_t>(in_message.size()) );
    notif.setCorrelationId( in_requestId );
    std::string jsonString = notif.toJson().dump();

    in_endpoint.send( in_connectionId, jsonString );
}

