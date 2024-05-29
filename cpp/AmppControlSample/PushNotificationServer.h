//
// Copyright Grass Valley
//

#ifndef PUSH_NOTIFICATION_SERVER_H_
#define PUSH_NOTIFICATION_SERVER_H_

#include "Sockets.h"

// Send a "subscribe" command to the Push Notification Server
void pushNotificationServerSubscribe( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic );

void pushNotificationServerUnsubscribe( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic );

void pushNotificationServerSendNotification( websocket_endpoint& in_endpoint,
    const int in_connectionId, const std::string& in_requestId,
    const std::string& in_topic, const std::string& in_message );


#endif /* PUSH_NOTIFICATION_SERVER_H_ */
