# DemoApp

This sample gives an example of an AMPP Control Server APP written in C#.

This is intended as a standalone application that can be run on any server, however if you wish to build a package that can be deployed within AMPP (via Resource Manager) and run under node agent then please use the scripts within the scripts subfolder.

The Application

- Connects/Authenticates with AMPP
- Registers itself as an AMPP Workload
- Listens for AMPP Control Notifications

## Authentication

In order to connect to AMPP you will need:

- The PlatformURL for your AMPP tennancy
- An API Key that supports platform & cluster.readonly scopes.

You must also provide:

- WorkloadId - A GUID That uniquely describes this instance
- WorkloadName - A unique friendly name for this application.

*If running under node agent these values will be provided via environment variables.

You can also set a ProductCode for your application.
If this is set then the application will check for a license and report its usage to the billing portal

To connect to platform the code is as follows:

```
 AmppControlService amppControl = new AmppControlService(platformUrl, apiKey, workloadName, productCode);
 await amppControl.ConnectToGVPlatform()
            
```

## Registering with AMPP

To register an application with AMPP you simply need to call the method ```InitializeWorkloadAsync()``` and pass in the service that you want to register.

In this example all the work is done in **DemoApplication.cs**

This sample shows a service that has an array of 10 channels.

Each channel has an active state, volume and label.

Each of these properties can be set by ampp control.



```
   // Create our Application and Initialise it
   // This sets up all the method handlers and listens to the SignalR notifications
   // As well as writing details of our app to the AMPP Control Registry
   var demoApplication = new DemoApplication(demoConfig, amppControl);
  
   await amppControl.InitializeWorkloadAsync("DemoService", "3rd Party", demoApplication);
   
```



The name **DemoService** is the application type. 
Change this to your application type.

The next parameter is the **VendorName** Change this from "3rd Party" to the name of your company.

The final parameter is a reference to the service that contains the AMPP Control methods.
This method uses reflection to find all methods that are decorated with the ```AMPPCommand``` attribute and register these methods within AMPP Control.

Example
```
[AMPPCommand("config", Schema = ConfigSchema, Version = "1.0", Markdown ="Markdown.config.md")]
```

This method will register a method called ```config```.

- ```Schema``` is a JSON7 Forms schema that describes the format of the data expected.
- ```Version``` is a Verion number for the schema. If you change the schema change the version
- ```Markdown``` is a Markdown help file for this command. This should be an embedded resource.

By calling ```InitializeWorkloadAsync()``` The app registers all commands with Ampp Control and starts listening for messages.

You should be able to see your workload in the AMPP Control UI and start testing the commands.


## Responding to messages

Once you have received an AMPP Control message and acted on it, you should inform the client application of your new state by sending a notify message

Example: After setting the state of a channel in the ```SetChannelState``` method the service responds with:

```
 // Inform all clients channelstate has changed
// Pass the reconKey back so clients can see where the update originated from
await amppControlService.PushAmppControlMessageAsync("channelstate", JObject.FromObject(this.applicationState[demoState.Index - 1]), reconKey);
```


Note: The **reconKey** is a string token that is sent by the client app which made the request
All responses should contain the same key.

The response message data should be valid against the schema registered for the command.


## Ping

The libraries within the sample app automatically create a ```ping``` command.

## Getstate

All apps should provide a ```getstate``` command that when called returns the current state of the application.

This is used by client applications when they start to get the current state of the application.

## Metrics

The Server application can be queried for performance metrics which can be visualised in the health app or on the System Dashboard.

If you wish your application to provide metrics then you must:

- Support the metrics command

Example:

```
[AMPPCommand("metrics", Schema = "{}", Version = "1.0", Markdown = "metrics.md")]
public async Task GetMetrics(string key)
{
    // Build the metrics payload
    ...
    await this.amppControlService.PushAmppControlMessageAsync("metrics", payload, key);
}
```

- Provide a schema for the the metrics by overriding the ``GetMetricsSchema()`` method.
This schema should be a JSON7 Forms schema






