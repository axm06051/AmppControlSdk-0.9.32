import axios, { AxiosInstance, AxiosResponse } from 'axios';
import { randomUUID } from 'crypto';
import { DateTime } from 'luxon';
import events from 'events';
import * as signalR from '@microsoft/signalr';
import { IPlatformNotification, NotificationEvent } from './Model';

/**
 * Class for Accessing AMPP Control Functionality
 */
export class GVPlatform extends events.EventEmitter implements NotificationEvent {
  platformUri: string;
  instance: AxiosInstance;
  apiKey: string;
  scopes: string[];
  bearerToken: string;
  notificationConnection: any;
  signal: signalR.HubConnection | any;
  correlationId: string;
  tokenRefreshTimer: any;

  /**
   * constructor
   * @param baseURL url for accessing GV Platform
   * @param apiKey The API Key (must have platform and cluster.readonly scopes)
   */
  constructor(baseURL: string, apiKey: string) {
    super();
    this.platformUri = baseURL;
    this.apiKey = apiKey;
    this.instance = axios.create({
      baseURL,
    });

    // If using AMMPP Control Only then you only need platform and cluster.readonly scopes
    // this.scopes = ['platform', 'cluster.readonly']
    // However, if you are using the Routing API then you will need the cluster scope
    this.scopes = ['platform', 'platform.readonly', 'cluster', 'cluster.readonly'];

    this.bearerToken = '';
    this.notificationConnection = null;
    this.correlationId = randomUUID();
  }

  public OnNotification(notification: IPlatformNotification) {
    this.emit('notification', notification);
  }

  /**
   * Schedule a token refresh based on its expiration time
   * The refresh will occur at 75% of expiry time
   * @param token The base64 JWT token
   */
  scheduleTokenRefresh(token: string) {
    const payloadBase64 = token.split('.')[1];
    const decodedJson = Buffer.from(payloadBase64, 'base64').toString();
    const decoded = JSON.parse(decodedJson);

    // Calculate the remaining time until expiration
    const remainingTime = decoded.exp * 1000 - Date.now();

    // Schedule a refresh at 75% of the remaining time
    const sleepTime = 0.75 * remainingTime;

    // Clear the previous timer if it exists
    clearTimeout(this.tokenRefreshTimer);

    // Set up the refresh timer.
    this.tokenRefreshTimer = setTimeout(async () => {
      await this.getToken();
    }, sleepTime);
  }

  /**
   * Obtains a JWT token from the identity service
   * Will schedule a timer to refresh the token before its expiration
   * @returns true if token obtained.
   */
  async getToken(): Promise<boolean> {
    let res: AxiosResponse;

    try {
      res = await this.instance.request({
        data: 'grant_type=client_credentials&scope=' + this.scopes.join(' '),
        headers: {
          Authorization: 'Basic ' + this.apiKey,
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        method: 'POST',
        url: '/identity/connect/token',
      });

      this.bearerToken = res.data.access_token;
      this.scheduleTokenRefresh(this.bearerToken);
    } catch (err) {
      throw new Error('login error' + err);
    }

    return true;
  }

  /**
   * login
   * connects and authenticates with GVPlatform
   */
  async login(): Promise<boolean> {
    return this.getToken();
  }

  /**
   * startNotificationListener
   * Opens the Connection to the SignalR connection on the PushNotifications Service
   * And starts listening for push notifications
   * You must subscribe to topics to get events. see subscribeToNotification
   * @returns a value indicating whether the connection has been established correctly
   */
  async startNotificationListener(): Promise<boolean> {
    try {
      let hubUrl: URL = new URL('/pushnotificationshub', this.platformUri);
      let connectionBuilder = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl.href, {
          accessTokenFactory: () => {
            return this.bearerToken;
          },
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Debug)
        .withHubProtocol(new signalR.JsonHubProtocol());

      this.signal = connectionBuilder.build();

      await this.signal.start();

      // Add event handler.
      // You must subscribe to topics using subscribeToNotification() to receive notifications
      this.signal.on('ReceiveNotification', this.onNotification);

      // Test to make sure we are reciving notifications from the Hub
      this.signal.on('Pong', this.onPong);
      this.signal.invoke('Ping');
      return true;
    } catch (err) {
      console.log('error starting SignalR Connection: ', err);
    }

    return false;
  }

  /**
   * subscribeToNotification
   * @param topic the topic to listen to notifications on
   * For AMPP Control these are of the format `gv.ampp.control.{workloadId}.{command}.notify
   */
  async subscribeToNotification(topic: string) {
    const subscriptionRequest = {
      Subscriptions: [topic],
      Context: {
        CorrelationId: this.correlationId,
      },
    };
    await this.signal.invoke('Subscribe', subscriptionRequest);
  }

  async unsubscribeNotification(topic: string) {
    const subscriptionRequest = {
      Subscriptions: [topic],
      Context: {
        CorrelationId: this.correlationId,
      },
    };
    await this.signal.invoke('Unsubscribe', subscriptionRequest);
  }

  private onPong = (account: string) => {
    console.log('pong', account);
  };

  private onNotification = (notification: IPlatformNotification) => {
    this.OnNotification(notification);
  };

  public async get(url: string): Promise<AxiosResponse> {
    let res: AxiosResponse;

    res = await this.instance.request({
      headers: {
        Authorization: 'Bearer ' + this.bearerToken,
      },
      method: 'GET',
      url: url,
    });

    return res;
  }

  public async post(url: string, data: any): Promise<AxiosResponse> {
    let res: AxiosResponse;

    res = await this.instance.request({
      headers: {
        Authorization: 'Bearer ' + this.bearerToken,
      },
      method: 'POST',
      url: url,
      data,
    });

    return res;
  }

  public async put(url: string, data: any): Promise<AxiosResponse> {
    let res: AxiosResponse;

    res = await this.instance.put(url, data, {
      headers: {
        Authorization: 'Bearer ' + this.bearerToken,
        'Content-Type': 'application/json-patch+json',
        'if-match': '"*"',
      },
    });
    return res;
  }

  public async delete(url: string): Promise<AxiosResponse> {
    let res: AxiosResponse;

    res = await this.instance.delete(url, {
      headers: {
        Authorization: 'Bearer ' + this.bearerToken,
      },
    });
    return res;
  }

  /**
   * publishNotification
   * uses SignalR connection to send a notification
   * @returns a value indicating whether command has been sent
   */
  async publishNotification(topic: string, content: any): Promise<boolean> {
    var publishNotification = {
      id: randomUUID(),
      time: DateTime.utc().toISO(),
      topic,
      source: 'AMPP SDK Sample',
      ttl: 30000,
      content: JSON.stringify(content),
      contentType: null,
      contentLength: 0,
      context: {
        correlationId: this.correlationId,
      },
    };

    const response = await this.signal.invoke('PublishNotification', publishNotification);
    return response.isSuccess;
  }

  /**
   * Gets notifications for a given mailbox
   * @param mailboxId The mailbox ID to get
   * @returns A result object containing a list of notifications
   */
  public getNotifications = async (mailboxId: string) => {
    const result = await this.instance.request({
      method: 'get',
      headers: {
        'x-correlation-id': this.correlationId,
      },
      params: {
        count: 100,
        timeout: 10000,
      },
      url: `/notifications/${mailboxId}`,
    });

    return result;
  };
}
