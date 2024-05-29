# DemoService

This sample gives an example of an AMPP Control Service written in Typescript

This is intended as a standalone application that can be run on any server, however if you wish to build a package that can be deployed within AMPP (via Resource Manager) and run under node agent then please use the scripts within the scripts subfolder.

The Application

- Connects/Authenticates with AMPP
- Registers itself as an AMPP Workload
- Listens for AMPP Control Notifications

# Build

Requires nodejs

to build run

```npm run build```


to execute

```npm run start```


## Authentication

In order to connect to AMPP you will need:

- The PlatformURL for your AMPP tennancy
- An API Key that supports platform & cluster.readonly scopes.

You must also provide:

- WorkloadId - A GUID That uniquely describes this instance
- WorkloadName - A unique friendly name for this application.

*If running under node agent these values will be provided via environment variables.

To connect to platform the code is as follows:

```
 const platform = new GVPlatform(workloadName, platformUrl, apiKey);
            
```

## Registering with AMPP

To register an application with AMPP you simply need to:

- Create an AMPPControlService object
- Register your application.



In this example all the work is done in **DemoService.ts**

This sample shows a service that has an array of 4 channels.

Each channel has an active state, volume and label.

Each of these properties can be set by ampp control.


### Create an AMPP Control Service
```
/**
     * This service will register your app with the AMPP Control Registry as well as handling all the AMPP Control Messaging over the signalR connection
     * @platform - The platform you created above
     * @workloadId - A unique Id for this service, don't create a new one every time, but If you have multiple instances then each should have a unique Id
     * @workloadName - A unique name of this instance
     * @ApplicationType - Describes the type of App
     * @vendor - The vendor name
     * @
     */
    const amppControl = new AmppControlService(platform, workloadId, workloadName, applicationType, vendor);
```


### Register Your Service code with AMPP Control

```

    /**
     * Your code in this class
     * Add methods to this class that you want to be called when an Ampp Control Message is recieved.
     */
    const demoService = new DemoService(amppControl);

    // This is where you register your service
    // Method are registered (via reflection) with AMPP control
    // decorate methods with @amppCommand
    // This will do all the plumbing to map notifications to methods
    const registered = await amppControl.RegisterWorkloadAsync(demoService)

   
```




This method uses reflection to find all methods that are decorated with the ```@amppCommand``` attribute and register these methods within AMPP Control.

Example
```
 @amppCommand({Name : "channelstate", Schema : StateSchema, Version : "1.0", Markdown : "channelstate.md" })
```

This method will register a method called ```channelstate```.

- ```Schema``` is a JSON7 Forms schema that describes the format of the data expected.
- ```Version``` is a Verion number for the schema. If you change the schema change the version
- ```Markdown``` is a Markdown help file for this command. 

By calling ```RegisterWorkloadAsync()``` The app registers all commands with Ampp Control and starts listening for messages.

You should be able to see your workload in the AMPP Control UI and start testing the commands.



## Responding to messages

Once you have received an AMPP Control message and acted on it, you should inform the client application of your new state by sending a notify message

Example: After setting the state of a channel in the ```SetChannelState``` method the service responds with:

```
// Respond with the new state
this.amppControl?.sendNotifyResponse('channelstate', this.channelstate, key);
```


Note: The **key** is a string token that is sent by the client app which made the request
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
 /**
   * GetMetrics - Any example message that returns the applications metrics
   * If this isn't present then the default metrics will be sent
   * @payload - The message payload (empty for metrics)
   * @key - A reconKey that is passed back in any response
   */
    @amppCommand({Name : "metrics", Schema : "{}", Version : "1.0", Markdown : "metrics.md" })
    GetMetrics = (payload : any, key: string) => {
        // If you want to extemnd the existing metrics then call
        // let metrics = this.amppControl.GetMetrics()
        const metrics: Record<string, any> = {};
        metrics["offset"] = Math.floor(Math.random() * 101);
        metrics["gmid"] = this.amppControl.workloadId;
        metrics["source"] = "Unlocked";
        metrics["locked"] = true;
        metrics["kernel_driver"] = false;

        this.amppControl.sendNotifyResponse("metrics", metrics, key)

    }
```

- Provide a schema for the the metrics by overriding the ``GetMetricsSchema()`` method.
This schema should be a JSON7 Forms schema




## Licensing

If you wish that your product is licensed you can provide a product code and make a call to the license server like so:

```

const productCode = "MY_PRODUCT_CODE";
const licensing : GVLicensing  = new GVLicensing(platform, productCode);

licensing.on('LicenseChangedEvent', ((productCode: string, licensed: boolean, error: string) => {

        
   console.log('************************************');
   console.log(`License Status Changed: ${licensed}, ${error}` );
   console.log('*************************************');

    if(!licensed) {
         exit(1);
    }
        
}));

```

This will report usage to the billing portal and cause the application to exit if no longer licensed