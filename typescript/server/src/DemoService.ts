import { amppCommand, AmppControlService, LogProperty } from "@gv/amppsdk";

/**
*  An Example Schema for setting the application configuration
*/
const configSchema = `{
"title": "Updates Configuration",
"type": "object",
"properties": {
    "connectionString": {
    "type": "string",
    "title": "Connection String"
    },
    "port": {
    "type": "integer",
    "title": "Port Number",
    "minimum" : 1000,
    "maximum" : 65535
    }
}
}`;

/**
*  An Exampple Schema for setting the application state
* Use a simple structure here.
* If using an array used either "index" (integer) or "id" (string) to define the position in array
* You can update multiple properties at once but only a single entry
*/
const StateSchema  = `
{
    'title': 'Updates State',
    'type': 'object',
    'properties': {
    'index': {
        'type': 'integer',
        'title': 'Channel Index',
        'minimum' : 1,
        'maximum' : 10
    },
    'volume': {
        'type': 'integer',
        'title': 'Volume',
        'minimum' : 0,
        'maximum' : 100
    },
    'active': {
        'type': 'boolean',
        'title': 'Active'
    },
    'label': {
        'type': 'string',
        'title': 'Label'
    }
    }
}`;

 /// <summary>
 /// Schema for metrics
 /// </summary>
 const MetricsSchema = `
 {
     "type": "object",
     "properties": {
         "offset": {
          "type": "integer",
          "description": "Offset from master",
          "unit": "us",
          "error_criteria": "> 15",
          "warn_criteria": "> 10",
          "minimum": 0,
          "maximum": 100
         },
        "gmid": {
            "type": "string",
            "description": "Grand Master ID"
          },
         "source": {
             "type": "string",
             "enum": [
               "GPS",
               "Boundary Clock",
               "Unlocked"
             ],
             "description": "Time Source",
             "error_criteria": "== Unlocked"
           },
          "locked": {
             "type": "boolean",
             "description": "PTP Locked",
             "error_criteria": "== false"
           },
           "kernel_driver": {
             "type": "boolean",
             "description": "Kernel Driver Mode",
             "error_criteria": "== false"
           }
         }
  }`;
 
          

class DemoServiceConfig {
    connectionString? : string = 'foo://demoservice';
    port? : number = 80085;
}


/**
 * DemoService
 * This is an example of a class that supports receiving AMPP Control commands
 * All methods decorated with @amppCommand will be registered with AMPP Control
 * When a notification is received (on the correct topic) then the method will be automatically called
 * If you want to respond to a message, then use the amppControl.sendNotifyResponse() passing back the same command name and key
 * This allows clients to tie up requests/responses
 * You must always support a getstate method so clients can get the existing state of the application when they first connect
 */
export class DemoService  {

    // Referece to the AMPP Control Service
    amppControl: AmppControlService;
    
    configuration : DemoServiceConfig = new DemoServiceConfig();

    /** Log Category */
    logCategory : string = 'SDKDemoService';

    /**
     * Constructor
     * @param amppControl reference to AMPP Control
     */
    constructor(amppControl : AmppControlService) {
        this.amppControl = amppControl;
    }

  
   /**
   * GetState - All Apps Must support getstate
   * This returns the complete state of the application so that any client can sync its display
   * @payload - The message payload (empty for getstate)
   * @key - A reconKey that is passed back in any response
   */
    @amppCommand({Name : "getstate", Schema : "{}", Version : "1.0", Markdown : "getstate.md" })
    GetState = ( payload : any, key : string) => {

        let lp : LogProperty = {
            key : "reconKey",
            value : key
        };

        this.amppControl?.logInfo(this.logCategory, "SetConfig", [ lp] );

        
        this.amppControl?.logInfo(this.logCategory, "GetState", [lp] );

        // Respond with the current config
        this.amppControl?.sendNotifyResponse('config', this.configuration, key);

       // Respond with the current channelstate
        this.amppControl?.sendNotifyResponse('channelstate', this.channelState, key);
    }


    /**
   * SetConfig - Any example message that sets the application config
   * respond back with the existing config, or an error
   * the payload should match the schema defined above
   * the Markdown is used to describe how to use the command
   * @payload - The message payload (empty for getstate)
   * @key - A reconKey that is passed back in any response
   */
    @amppCommand({Name : "config", Schema : configSchema, Version : "1.0", Markdown : "config.md" })
    SetConfig = (payload : any, key: string) => {
        
        this.amppControl?.logInfo(this.logCategory, "SetConfig", [ { key : "reconKey",value : key}, { key : "payload", value : payload }] );


        const config : DemoServiceConfig = payload as DemoServiceConfig;

        if(config.connectionString !== undefined) {
            this.configuration.connectionString = config.connectionString;
        }

        if(config.port !== undefined) {

            if(config.port < 1000 || config.port > 65535) {
                this.amppControl?.logError(this.logCategory, "SetConfig Error: The Port is out of range", [ { key : "reconKey",value : key}, { key : "payload", value : payload }] );
                // You can send error responses like this
                // or throw errors which will be caught and sent back as notifications
                this.amppControl?.sendErrorResponse('config', "Invalid Input", "The Port is out of range", key);
                return;
            }

            this.configuration.port = config.port;
        }

        this.amppControl?.sendNotifyResponse('config', this.configuration, key);

    }

    private channelState: { index: number; active: boolean, volume : number, label : string }[] = [
        { index: 1, active: false, volume  : 0, label : 'Channel1'},
        { index: 2, active: false , volume  : 0, label : 'Channel2'},
        { index: 3, active: false , volume  : 0, label : 'Channel3'},
        { index: 4, active: false, volume  : 0, label : 'Channel4' }
      ];

    /**
   * SetChannelState - Any example message that sets a property in the application state
   * We allow partial updates so don't overwrite data that isn't present in the message
   * If the schema changes, up the version number so that older instances apps will still show the old schema
   * respond back with the existing state, or an error
   * the payload should match the schema defined above
   * the Markdown is used to describe how to use the command
   * @payload - The message payload (empty for getstate)
   * @key - A reconKey that is passed back in any response
   */
    @amppCommand({Name : "channelstate", Schema : StateSchema, Version : "1.0", Markdown : "channelstate.md" })
    SetChannelState = (payload : any, key : string) => {

        this.amppControl?.logInfo(this.logCategory, "SetChannelState", [ { key : "reconKey",value : key}, { key : "payload", value : payload }] );

        const index: number = payload.index || -1;

        if(index < 1 || index > 4) {
            this.amppControl?.logError(this.logCategory, "SetChannelState Invalid Input. Index must be between 0-4", [ { key : "reconKey",value : key}, { key : "payload", value : payload }] );
            this.amppControl?.sendErrorResponse('channelstate', "Invalid Input", "Index must be between 0-4", key);
            return;
        }

        
        const active : boolean | undefined = payload.active;

        if(active !== undefined) {
            this.channelState[index-1].active = active;
        }

        const volume : number  = payload.volume || -1;

        if(volume > 500 || volume < 0) {
            this.amppControl?.logError(this.logCategory, "SetChannelState Error: Volume out of bounds", [ { key : "reconKey",value : key}, { key : "payload", value : payload }] );
            // You can also throw errors here and the will be caught in the library and error notifications sent
            throw new  RangeError("Volume out of bounds")
        }

        this.channelState[index-1].volume = volume;

        const label : string | undefined = payload.label;

        if(label !== undefined) {
            this.channelState[index-1].label = label;
        }

        // Respond with the new state
        this.amppControl?.sendNotifyResponse('channelstate', this.channelState, key);

    }

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

    public GetMetricsSchema = () : string => {
        return MetricsSchema;
    }
}