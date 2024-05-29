import {
  AxiosResponse
} from "axios";


import events from "events";
import { GVPlatform } from "./GVPlatform";
import { IAmppControlError, IAmppControlMacro, IAmppControlNotification, IPlatformNotification } from "./Model";



/**
 * Class for Accessing AMPP Control Functionality 
 */
export class AmppControl extends events.EventEmitter {

   private gvPlatform : GVPlatform | null;

  /**
   * constructor
   * @param baseURL url for accessing GV Platform
   * @param apiKey The API Key (must have platform and cluster.readonly scopes) 
   */
  constructor(baseURL: string, apiKey: string) {
    super();
    this.gvPlatform = new GVPlatform(baseURL, apiKey);
    };


 

  /**
   * login
   * connects and authenticates with GVPlatform
   */
  async login(): Promise < boolean > {
    return this.gvPlatform.login()
  }

  /**
   * listApplicationTypes
   * uses AMPP Control API to get a list of applicationTypes
   */
  async listApplicationTypes(): Promise < string[] > {

    let res: AxiosResponse;

    try {
      res = await this.gvPlatform.get('/ampp/control/api/v1/control/application/references');
    }
    catch (err) {
      throw new Error('listApplicationTypes() error' + err);
    }

    const apps: [] = res.data
    return apps.map(({
      name
    }) => name)
  }

  /**
   * listWorkloadsForApplicationType
   * uses AMPP Control API to get a list all workloads for a specific application
   */
  async listWorkloadsForApplicationType(application: string): Promise < string[] > {

    let res: AxiosResponse;

    const url = `ampp/control/api/v1/control/application/${application}/workloads`

    try {
      res = await this.gvPlatform.get(url);
    } catch (err) {
      throw new Error('listWorkloadsForApplicationType() error' + err);
    }

    return res.data
  }


  /**
   * getControlSchemasForApplication
   * uses AMPP Control API to get a list all schema versions for a specific application
   */
  async getControlSchemasForApplication(application: string): Promise < [] > {

    let res: AxiosResponse;

    const url = `ampp/control/api/v1/control/application/${application}/schemaversions`

    try {
      res = await this.gvPlatform.get(url);
    } catch (err) {
      throw new Error('getControlSchemasForApplication() error' + err);
    }

    return res.data
  }

  /**
   * listMacros
   * uses AMPP Control API to get a list all Macros
   */
   async listMacros(): Promise < IAmppControlMacro[] > {

    let res: AxiosResponse;

    const url = `ampp/control/api/v1/macro`

    try {
      res = await this.gvPlatform.get(url);
    } catch (err) {
      throw new Error('listMacros() error' + err);
    }

    const macros : IAmppControlMacro[] = res.data;
    return macros
  }

    /**
   * executeMacro
   * uses AMPP Control API to execute a Macro
    * @param uuid - Unique identifier of Macro
    * @param reconKey - Key to indicate source of request. Returned in AMPP Control commands
    * @returns a value indicating success
   */
     async executeMacro( uuid : string, reconKey : string ): Promise < boolean > {

      let res: AxiosResponse;
  
      const url = `ampp/control/api/v1/macro/execute`
  
      try {
        res = await this.gvPlatform.post(url, {uuid, reconKey});
      } catch (err) {
        throw new Error('executeMacro() error' + err);
      }
  
     
      return res.status == 204
    }


  /**
   * sendAmppControlMessage
   * uses AMPP Control API to send and AMPP Control message
   * @param workload - The workloadId to send message to
   * @param application - The Application type (not actually needed, can pass any) 
   * @param command - The AMPP control command to execute
   * @param payload - an object containing the payload for the command
   * @param reconKey - a string that will be passed back in any notify or status response
   * @returns a value indicating whether command has been sent
   */
  async sendAmppControlMessage(
    workload: string,
    application: string,
    command: string,
    payload: any,
    reconKey: string
  ): Promise < boolean > {

    let res: AxiosResponse;

    let url : string = `ampp/control/api/v1/control/commit`

    const data = {
      application,
      command,
      workload,
      reconKey,
      FormData: JSON.stringify(payload)

    }

    try {
      res = await this.gvPlatform.post(url, data);
    } catch (err) {
      throw new Error('sendAmppControlMessage() error' + err);
    }

    return res.status == 204
  }

  /**
   * pushAmppControlMessage
   * uses SignalR connection to send an Ampp Control message
   * @param workload - The workloadId to send message to
   * @param application - The Application type (not actually needed, can pass any) 
   * @param command - The AMPP control command to execute
   * @param payload - an object containing the payload for the command
   * @param reconKey - a string that will be passed back in any notify or status response
   * @returns a value indicating whether command has been sent
   */
   async pushAmppControlMessage(
    workload: string,
    application: string,
    command: string,
    payload: any,
    reconKey: string
  ): Promise < boolean > {

   const topic : string = `gv.ampp.control.${workload}.${command}`

   const content = {
     Key : reconKey,
     Payload : payload,
   }


   return await this.gvPlatform.publishNotification(topic, content);
  }

  /**
   * getState
   * A wrapper for the sendAmppControlMessage() function that sends the getstate command
   * @param workload - The workloadId to send message to
   * @param reconKey - a string that will be passed back in any notify or status response
   * @returns a value indicating that the notification has been raised successfully
   */
  async getState(
    workload: string,
    reconKey: string
  ): Promise < boolean > {

    return this.sendAmppControlMessage(
      workload,
      'any',
      'getstate', {},
      reconKey
    );
  }

  /**
   * startNotificationListener
   * Opens the Connection to the SignalR connection on the PushNotifications Service
   * And starts listening for push notifications
   * You must subscribe to topics to get events. see subscribeToNotification
   * @returns a value indicating whether the connection has been established correctly
   */
  async startNotificationListener(): Promise < boolean > {
    this.gvPlatform.on('notification', this.onNotification)
    return this.gvPlatform.startNotificationListener();
    
  }

  /**
   * subscribeToNotification
   * @param topic the topic to listen to notifications on
   * For AMPP Control these are of the format `gv.ampp.control.{workloadId}.{command}.notify
   */
  async subscribeToNotification(topic: string) {
    await this.gvPlatform.subscribeToNotification(topic);
  }



  private onNotification = (notification: IPlatformNotification) => {

    let content = JSON.parse(notification.content);
    
    if (notification.topic.endsWith('notify')) {

      let amppControlResponse: IAmppControlNotification = {
        topic: notification.topic,
        payload: content.payload,
        reconKey: content.key
      };

      this.emit('notify', amppControlResponse)
    } 
    else
    if (notification.topic.endsWith('status')) {

      let errorNotification : IAmppControlError = {
        topic : notification.topic,
        reconKey : content.key,
        status : content.status,
        error : content.error,
        details : content.details
      }
      this.emit('status', errorNotification)
    }
  }


}