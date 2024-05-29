import { EventEmitter } from 'stream';
import { IPlatformNotification, Producer } from './Model';
import { GVPlatform } from './GVPlatform';
import { randomUUID } from 'crypto';

export interface ProbeNotification {
  id: string;
  time: number;
  rms: number[];
  peak: number[];
}

export class SoundProbeClient extends EventEmitter {
  private gvPlatform: GVPlatform | null;
  private subscriptions: Record<string, Producer> = {};
  private subscriptionInterval: NodeJS.Timer;
  private notificationsTopicBase = "gv.ampp.audiometer";

  private readonly SUBSCRIPTION_RENEW_TIME_SECONDS = 60;

  constructor(baseURL: string, apiKey: string) {
    super();
    this.gvPlatform = new GVPlatform(baseURL, apiKey);
  }

  public async login(): Promise<boolean> {
    return this.gvPlatform.login();
  }

  public async getProducerAsync(fabricId: string, producerName: string): Promise<Producer> {

    const name = encodeURIComponent(producerName);
    const url = `/cluster/matrix/api/v1/producer/${fabricId}/${name}`;

    const result = await this.gvPlatform.get(url);
    if (result.status === 200) {
      const messages: Producer = { producer: result.data };
      return messages;
    }
    return null;
  }

  public async startNotificationListener(): Promise<boolean> {
    this.gvPlatform.on('notification', this.onNotification);
    return this.gvPlatform.startNotificationListener();
  }

  public addAudioMeterSubscription(producer : Producer): string {

    const probeId = randomUUID();
    this.subscriptions[probeId] = producer;
    return probeId;
  }

  public async startAudioMeterSubscriptionAsync() {

    const keys = Object.keys(this.subscriptions);
    for (const key of keys) {

      const topic = `${this.notificationsTopicBase}.${key}`;
      await this.gvPlatform.subscribeToNotification(topic);
      console.log('Subscribing to', topic);
    }
    
    await this.renewAudioMeterSubscriptions();
    this.subscriptionInterval = setInterval(
      this.renewAudioMeterSubscriptions.bind(this),
      this.SUBSCRIPTION_RENEW_TIME_SECONDS * 1000,
    );
  }

  public async stopAudioMeterSubscriptionAsync() {
    const keys = Object.keys(this.subscriptions);
    for (const key of keys) {
      const topic = `${this.notificationsTopicBase}.${key}`;
      await this.gvPlatform.unsubscribeNotification(topic);
      console.log('Unsubscribing to', topic);
    }
    clearInterval(this.subscriptionInterval);
  }

  private async renewAudioMeterSubscriptions() {
    try {
      const keys = Object.keys(this.subscriptions);
      for (const key of keys) {
        await this.registerSoundProbe(key, this.subscriptions[key]);
      }
    } catch (err) {
      console.log('err', err);
      throw new Error('renewFrameCacheSubscriptions() error');
    }
  }

  private async registerSoundProbe(probeId : string, producer: Producer) {
    if (!producer.producer) {
      return;
  }

  const nodeId = producer.producer.nodeId;

  const flows = producer.producer.stream?.flows;

  if (!flows) {
      return;
  }

  const soundFlows = flows.filter((flow) => flow.dataType == "Snd");

  if (soundFlows.length === 0) {
      return;
  }
 
  const probeSampleRate = 250;
  const probeRefeshRate = 1000;

  soundFlows.forEach(async (flow) => {
      const mochaFlow = {
          id: flow.flowId,
          dataType: flow.dataType,
          descriptor: flow.descriptor
      };

      const soundProbe = {
          id: probeId,
          flow: mochaFlow,
          peak: true,
          rms: true,
          type: "sound",
          rmsWindowPeriodMs: probeSampleRate,
          updatePeriodMs: probeRefeshRate,
      };

      const probeSubscription = {
          clientId: 'SDKDemoClient',
          flowId: flow.flowId,
          probeId: soundProbe.id,
          probeObject: JSON.stringify(soundProbe),
          probeType: "sound"
      }

      console.log('Registering soundprobe', probeSubscription);
    await this.gvPlatform.publishNotification(`gv.ampp.audiometerprobe.${nodeId}`, probeSubscription);

  });

}

private onNotification = (notification: IPlatformNotification) => {

  const id = notification.topic.split('.').pop();

  const probeData = JSON.parse(notification.content);

  const probe : ProbeNotification = {
      id: id,
      time: probeData.updateTimeMs,
      rms: probeData.rms,
      peak: probeData.peak
  }
  this.emit('audiometer', probe);
  
}
}
