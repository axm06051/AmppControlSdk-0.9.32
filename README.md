# AMPP Control Demo code

This code is being provided as documentation demonstrating how to connect to AMPP, send and receive commands through AMPP Control.

## Introduction to AMPP Control

AMPP Control is a protocol that allows AMPP applications to be remotely controlled (or configured) using, for example, hardware panels. Controllable applications register their control schema in the AMPP config service. Communication between the AMPP apps and the controlling clients is achieved through the AMPP High Speed notification service (SignalR/Websocket based). For example the Events from the hardware panel can be mapped to AMPP Control protocol messages that are then consumed by the AMPP application. Status responses are sent back using the same mechanism. The hardware panel can then react to the status response messages to update the state of the panel (I.e. by lighting relevant buttons or setting the sliders or control knobs to the correct value).

## AMPP Control Service

The AMPP Control Service provides a number of different functions.
It hosts the AMPP Control REST API that can be used to query the application registry and execute AMPP Control commands and Macros
It provides an enginneering UI, that allows you to test AMPP Control Commands, Build Macros and Control Groups and perform other housekeeping functions.

The AMPP Control Service can be accessed at the following URL:

`https://{platformURL}}/ampp/control/`

The **Overview** tab allows you to see all the application types that support AMPP Control and the commands that can be executed. From this screen you can also execute an AMPP Control command and find documentation for the particalar command.

The **Macros** tab allows you to build an AMPP Control Macro. That is block of AMPP Control Commands (possibly with delays in between) that can be sent to 1 or more workloads.

the **Control Groups** tab allows you to configure up groups of workloads in order that multiple instances of the same application type can be executed simultaneously.

### AMPP Control Protocol

This SDK provides examples of

- Authentication/Identity Management using JWT token
- Querying the AMPP Control registry using the REST api
- Sending and receiving AMPP Control messages using SignalR/websockets
- Error handling of AMPP Control messages

## Implementations


### Client Applications

Client Applications are apps designed for controling native services within AMPP
Every AMPP App can be controlled via the AMPP Control protocol.

The SDK provides 3 client applications demonstrating how to use AMPP Control from different programming languages.

- [C++ SDK](cpp/AMPPControlSample/README.md)
- [C# SDK](csharp/client/test.client/README.md)
- [TypeScript SDK](typescript/client/README.md)


### Server Applications

AMPP Control Server applications are for people who want to write applications that can exist withing the AMPP ecosystem and be controlled by our existing client interfaces. (System Dashboard, StreamDeck, MIDI, Maverick, etc)
Server applications can be standalone apps or can be deployed to run under Node Agent

The SDK provides 2 example server side apss in different languages

- [C# SDK](csharp/server/gv.ampp.control.demo.app/README.md)
- [Tyoescript SDK](typescript/server/gv.ampp.control.demo.app/README.md)

---

## AMPP Control Registry

When an AMPP application starts up it registers itself with the AMPP Control registry.
The Registry stores the commands supported by each application well as a schema describing the command and markdown documentation for the command.
It also stores a list of workloads for each application type.
This registry can be queried to discover the schema(s) associated with the application and which workloads are available.

The registry can be accessed via the AMPP Control REST API

```
https://{platformURL}/ampp/control/swagger/index.html
```

### Registry Structure

**Applications**

A list of all supported applications and their commands can be obtained by querying the REST API at the following URL:

```
GET /application/references
```

The response to this message contains the schemas for the commands, as well as the markdown documentation for the commands.

**Workloads**

A list of all workloads associated with a particular application type can be obtained by querying the API at the following URL:

```
GET /control/application/{application}/workloads
```

E.g.

```
GET /control/application/MiniMixer/workloads
```

**Schemas Versions**

A list of which workloads support which particular commands (and versions) can be obtained by by querying the API at the following URL:

```
GET /control/application/{application}/schemaversions
```

E.g.

```
GET /control/application/MiniMixer/schemaversions
```

**Control Groups**

Workloads can be added to _control groups_ to allow multiple workloads to be controlled simultaneously. A list of control groups (for an application type) and the workloads associated with them can be obtained by querying the API at the following URL:

```
GET /control/application/{application}/groups
```

E.g.

```
GET /control/application/MiniMixer/groups
```

### Schemas

Each Command in the registry is described by a JSON7 Schema

A schema can contain multiple parameters and each parameter can be mapped to a control on a hardware panel.

The panels are stateless, so the application will send the state of all parameters when 1 change occurs, or in response to a _getstate_ message

Example Schema - (MiniMixer - controlstate)

```
{
  "description": "Set the preview/program state",
  "properties": {
    "Index": {
      "type": "number",
      "minimum": 1,
      "maximum": 12
    },
    "Preview": {
      "type": "boolean"
    },
    "Program": {
      "type": "boolean"
    }
  }
}
```

## Sending AMPP Control Messages

AMPP Control Messages are messages sent by client applications to control AMPP applications

These consist of a message topic and a message body

The topic is of the form:

`gv.ampp.control.{workload}.{command} `

- workload is the ID/GUID of the application instance to control.

- command is the command to invoke (as defined in the Registry)

The message body consists of a Key and a Playload.

The Key is a string used to determine the source of the message an can be used to to allow client applications to filter messages that originated from themselves. If the AMPP Control application changes state due to the AMPP Control message, then the same key will be returned in the subsequent .notify or .status message.

The payload is a a JSON object that adheres to the schema (as described above) and can be a partial object.

**Partial Objects**

Where a schema contains multiple paramaters, a client application can send a partial object structure to set just the individual parameter required.

**Example AMPP Control Message Payload (MiniMixer - controlstate)**

Set Channel 1 on Preview

```
{
  "Index" : 1,
  "Preview" : true
}
```

AMPP Control Messages can be sent in 1 of 2 ways:

**HTTP POST - AMPP Control**

AMPP Control Messages can be POSTed via the AMPP Control REST api at the following URL:

```

POST /control/commit

{
    "workload": "workloadId",
    "application": "MiniMixer",
    "command": "controlstate",
    "formData": "{\n  \"Index\" : 1,\n  \"Preview\" : true\n}"
}
```

- workload - is the GUID of the workload to control
- application - is the name of the application type
- command - is the AMPP Control command to execute
- formData - is a JSON stringified payload for the command.

**SignalR PushNotification**

AMPP Control Messages can be sent direct via the PushNotifications SignalR Hub.
An example of how to connect to the SignalR Hub is included in this Sample SDK
The format of the message is a follows:

```
{
  id: '{guid}',
  time: '{iso datetime}',
  topic: 'gv.ampp.control.{workload}.{command}',
  source: 'AMPP SDK Sample',
  ttl: 30000,
  content: '{"Key":"{reconKey}","Payload":{"Index":1,"Preview":true}}',
  context: { correlationId: '{guid}' }
}
```

## AMPP Control Notifications

AMPP Control Notify messages are sent by the AMPP applications to indicate to all client applications (such as hardware panels) that the application state has change and they need to update their status.

These messages consist of a message topic and a message body.

The topic is of the form:

gv.ampp.control.{workload}.{command}.notify

- workload is the ID/GUID of the application where the message originated

- command is the command that has been invoked.

The body contains a _key_ and a _payload_

The Key is passed through from the AMPP Control message that triggered change.

The payload is a a JSON object that adheres to the schema that contains the entire object described in the schema.

E.g. If - in the example above - a client sends a partial object containing a request to set Set Channel 1 on Preview of a MiniMixer with the reconKey = "AMPP SDK Sample"

Then, the AMPP application then responds with a notify message containing the entire controlstate object and a Key = "AMPP SDK Sample"

**Request**

```
gv.ampp.control.{workload}.controlstate
{
  "Key" : "AMPP SDK Sample",
  "Payload" : {
    "Index" : 1,
    "Preview" : true
  }
}
```

**Response**

```
gv.ampp.control.{workload}.controlstate.notify
{
  "Key" : "AMPP SDK Sample",
  "Payload" :[
  {
    "Index": 1,
    "Program": false,
    "Preview": true
  },
  {
    "Index": 2,
    "Program": false,
    "Preview": false
  },
  {
    "Index": 3,
    "Program": false,
    "Preview": false
  },
  {
    "Index": 4,
    "Program": true,
    "Preview": false
  }
]
}
```

## Error Handling - AMPP Control Status Messages

AMPP Control Status messages are messages sent by the AMPP applications to indicate that an error occured in response to an AMPP Control request.

These messages consist of a message topic and a message body.

The topic is of the form:

gv.ampp.control.{workload}.{command}.status

- workload is the ID/GUID of the application where the message originated

- command is the command that has been invoked.

The body contains a _key_, a _status_, an _error_ and _details_

- Key is passed through from the AMPP Control message that triggered change.
- status is an error code
- error is a short error description
- details is a longer description of the error

3 types of error status can be expected:

- 4 (BadRequest) Payload does not match schema The data sent with the AMPP control message does not comply with the schema
- 7 (ResourceNotFound) Unknown method A message has been sent for a command/method that does not exist on the service
- 16 (Error) The service experienced an error processing the command. The message will give more details of the fault.

### Example

A client sends a _controlstate_ message to an invalid channel on a MiniMixer

```
gv.ampp.control.{workload}.controlstate
{
  "Key": "AMPP Control SDK Sample",
  "Payload": {
    "Index": 0,
    "Program": true
  }
}
```

MiniMixer responds with an error message

```
gv.ampp.control.{workload}.controlstate.status
{
  "key": "AMPP Control SDK Sample",
  "payload": null,
  "status": 4,
  "error": "Invalid Payload",
  "details": "Integer 0 is less than minimum value of 1. Path 'Index'."
}
```

## Receiving AMPP Control Notifications

AMPP Control Notifications can be received by connecting to the PushNotifications service SignalR HUB.
An example of how to connect to the SignalR Hub is included in this Sample SDK.
In order to receive AMPP Control notifications and status responses from a specific application you will need to subscribe to the following topics:

`gv.ampp.control.{workload}.*.notify`

`gv.ampp.control.{workload}.*.status`

## GetState

All applications support the standard AMPP Control command **getstate**

This command takes no parameters and instructs the application to send back full details about its current state. This allows client applications to get full details of the current state of the application when they start.

## Ping

All (newer) applications support the standard ping AMPP Control command **ping**

This command takes no parameters and instructs the applcation to send an immediate _.ping.notify_ message response. This command allows client apps to easily check if a workload is running and accepting AMPP Control messages. Older applications (that do not yet support _ping_) should still send a _.ping.status_ response indicating that the command is not supported. This error response can also be used to determine whether the workload is running/available.
