import { AmppControl } from './AmppControl';
import 'dotenv/config';
import { IAmppControlError, IAmppControlNotification, Producer } from './Model';
import { Router } from './Router';
import { KeyframesClient } from './KeyframesClient';
import { SoundProbeClient, ProbeNotification } from './SoundProbeClient';
const clc = require('cli-color');

const apiKey = process.env.API_KEY;
const platformUrl = process.env.PLATFORM_URL;
const fabricId = process.env.FABRIC_ID;
const nodeId = process.env.NODE_ID;

// Pass this Key in all our AMPP Control Commands
// Any notifications we receive that have originated from us will contain this same key
// If they originated from another source the key will be different
const reconKey: string = 'AMPP Control SDK Sample';

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function main() {
  // Test that the API key has been provided.
  if (apiKey == null || apiKey === '') {
    console.error('ERROR : Please provide an apiKey in the .env file');
    return;
  }

  // Test that the platformUrl has been provided
  if (platformUrl == null || platformUrl === '') {
    console.error('ERROR : Please provide a platformUrl in the .env file');
    return;
  }

  // Example AMPP Control implementation in here
  await amppControlSdk();

  // Example Routing SDK commands here
  await routingSDK();

  // Example Keyframes Subscription
  await subscribeToKeyframes();

  // Example Audio Levels Subscription
  await subscribeToAudioLevels();
}

///////////////////////////////////////////////////////////////////////
// Keyframes Client
///////////////////////////////////////////////////////////////////////
async function subscribeToKeyframes() {
  /// This method allows you to subscribe to keyframes notifications and save them as images in the local folder
  /// Keyframes are images representing preview of the stream that is generated by producer applications in AMPP

  console.log('Hello Keyframes Client');

  // Path to the local folder where kayframe images will be saved
  const FOLDER_PATH = '';

  const keyframesClient = new KeyframesClient(platformUrl, apiKey, FOLDER_PATH);
  await keyframesClient.login();
  console.log('Login Ok');

  // The name of keyframes Producer you want to subscribe to
  const PRODUCER_NAME = '';

  console.log('**********Keyframes*************');
  await keyframesClient.startNotificationListener();

  const producer: Producer = await keyframesClient.getProducerAsync(fabricId, PRODUCER_NAME);
  const flowId = producer.producer.stream?.flows?.find((f) => f.dataType === 'Pic')?.flowId;
  console.log('FlowId:', flowId);

  const subscriptionTopic = keyframesClient.addKeyframesSubscription(nodeId, flowId);
  console.log('SubscriptionTopic:', subscriptionTopic);

  await keyframesClient.startKeyframesSubscriptionAsync();
  console.log('Subscription started');

  await sleep(5 * 60 * 1000);
  await keyframesClient.stopKeyframesSubscriptionAsync();
  console.log('Subscription stopped');
}

///////////////////////////////////////////////////////////////////////
// The AMPP Control SDK
///////////////////////////////////////////////////////////////////////
async function amppControlSdk() {
  // Create the ampp control object and log in
  console.log('Hello AMPP Control');
  const ampp = new AmppControl(platformUrl, apiKey);
  await ampp.login();
  console.log('Login Ok');

  console.log('**********Macros*************');
  // Get a List of Macros
  var macros = await ampp.listMacros();
  const macroNames = macros.map((m) => m.name);
  console.log(macroNames);
  console.log('****************************************');
  console.log();
  console.log();

  console.log('**********Application Types*************');
  // Get a List of application types
  var apps = await ampp.listApplicationTypes();
  console.log(apps);
  console.log('****************************************');
  console.log();
  console.log();

  console.log('**********Workloads for MiniMixer*************');
  // Get a List of workloads that are MiniMixers
  var workloads = await ampp.listWorkloadsForApplicationType('MiniMixer');
  console.log(workloads);
  console.log('***********************************************');
  console.log();
  console.log();

  console.log('**********SchemaVersions*************');
  // Determine which workloads support what schemas (using MiniMixer as an example)
  var schemaversions = await ampp.getControlSchemasForApplication('MiniMixer');

  const miniMixerWorkloadId = process.env.WORKLOAD_ID;
  const schemaVersionsForWorkload = schemaversions.filter(({ id }) => id == miniMixerWorkloadId);

  console.log(JSON.stringify(schemaVersionsForWorkload));
  console.log('***********************************************');
  console.log();
  console.log();

  // Start listening for AMPP Control Notifications
  var listening = await ampp.startNotificationListener();

  if (!listening) {
    console.log('Error starting Notifications Listener');
    return;
  }

  console.log('**********Commit an AMPP Control Message*************');

  // Send an AMPP Control Message to the MiniMixer using HTTP Client
  // The format of these messages is documented (for each application) in the AMPP Control Service UI
  // https://{platformURL}/ampp/control/
  var commitResult = await ampp.sendAmppControlMessage(
    miniMixerWorkloadId,
    'MiniMixer',
    'controlstate',
    // Set Preview 1

    { Index: 1, Preview: true },
    reconKey,
  );

  console.log('commit result : ' + commitResult);

  console.log('*******************************************************');

  console.log('**********Push an AMPP Control Message*************');

  // Send an AMPP Control Message to the MiniMixer using SignalR Client (Faster)
  // The format of these messages is documented (for each application) in the AMPP Control Service UI
  // https://{platformURL}/ampp/control/
  var pushResult = await ampp.pushAmppControlMessage(
    miniMixerWorkloadId,
    'MiniMixer',
    'controlstate',
    // Set Program 4
    { Index: 4, Program: true },
    reconKey,
  );

  console.log('pushResult result : ' + pushResult);

  console.log('*******************************************************');

  // Subscribe to notifications from our MiniMixer
  // Notitifications come back on topics ending .notify
  // Errors come back on topics ending .status
  const topic = `gv.ampp.control.${miniMixerWorkloadId}.*.*`;
  ampp.subscribeToNotification(topic);

  // Notifications handler
  ampp.addListener('notify', (notification: IAmppControlNotification) => {
    // If the reconKey matches our reconKey, then the message originated from ourselves
    if (notification.reconKey == reconKey) {
      console.log(clc.green('********Notification Received From self*********'));
      console.log(clc.green(`topic : ${notification.topic}`));
      console.log(clc.green(`reconKey : ${notification.reconKey}`));
      console.log(clc.green(`payload : ${JSON.stringify(notification.payload)}`));
      console.log(clc.green('**************************************'));
    }
    // Else the notification was triggered from somewhere else
    else {
      console.log(clc.yellow('********Notification Received From other*********'));
      console.log(clc.yellow(`topic : ${notification.topic}`));
      console.log(clc.yellow(`reconKey : ${notification.reconKey}`));
      console.log(clc.yellow(`payload : ${JSON.stringify(notification.payload)}`));
      console.log(clc.yellow('**************************************'));
    }
  });

  // Error handler
  ampp.addListener('status', (notification: IAmppControlError) => {
    // If the reconKey matches our reconKey, then the error originated from ourselves
    if (notification.reconKey == reconKey) {
      console.log(clc.red('********Error Notification Received From self*********'));
      console.error(clc.red(`topic : ${notification.topic}`));
      console.error(clc.red(`reconKey : ${notification.reconKey}`));
      console.error(clc.red(`status : ${JSON.stringify(notification.status)}`));
      console.error(clc.red(`error : ${JSON.stringify(notification.error)}`));
      console.error(clc.red(`details : ${JSON.stringify(notification.details)}`));
      console.error(clc.red('**************************************'));
    }
    // Else the error was triggered from somewhere else
    else {
      console.log(clc.yellow('********Error Notification Received From other*********'));
    }
  });

  // Send a getstate command to our MiniMixer forcing it to send a response
  ampp.getState(miniMixerWorkloadId, reconKey);

  // Send an AMPP Control command with an Invalid Payload so we get an error response
  console.log('**********Push an AMPP Control Message*************');

  // Send an AMPP Control Message to the MiniMixer
  await ampp.pushAmppControlMessage(
    miniMixerWorkloadId,
    'MiniMixer',
    'controlstate',
    // Invalid Index
    { Index: 0, Program: true },
    reconKey,
  );

  console.log('*******************************************************');

  // If there is a MacroId configured, and it exists in the list of Macros returned, then execute it
  const macroId = process.env.MACRO_ID;

  if (macroId != null) {
    const macroToExecute = macros.filter(({ uuid }) => uuid == macroId)[0];

    if (macroToExecute != null) {
      console.log(`**********Excuting Macro ${macroToExecute.name}*************`);
      ampp.executeMacro(macroId, reconKey);
      console.log('**************************************');
    }
  }
}

///////////////////////////////////////////////////////////////////////
// The Routing SDK
///////////////////////////////////////////////////////////////////////
async function routingSDK() {
  console.log('Hello Routing SDK');

  const router: Router = new Router(platformUrl, apiKey);
  const connected = await router.login();

  if (!connected) {
    console.error('Failed to connected to RouterSDK');
    return;
  }

  // Get a list of Fabrics
  const fabrics = await router.GetFabrics();

  console.log(`**********Fabrics*************`);
  fabrics.forEach((f) => {
    console.log(`${f.fabric.id}\t\t${f.fabric.name}`);
  });

  // Start listening for any RouteChanged Events
  router.StartListeningForRouteEvents();

  router.addListener('RouteChangedEvent', (fabric, src, dst) => {
    console.log('**********Route Made***************');
    console.log(`Fabric: ${fabric}\tSrc: ${src}\t-->\t${dst}`);
    console.log('***********************************');
  });

  if (fabricId) {
    // Get all Producers on a Fabric
    const producers = await router.GetProducers(fabricId);

    console.log(`**********Producers*************`);
    producers.forEach((p) => {
      console.log(`${p.producer.id}\t\t${p.producer.name}(${p.producer.alias})`);
    });

    // Get all Consumers on a Fabric
    const consumers = await router.GetConsumers(fabricId);

    console.log(`**********Consumers*************`);
    consumers.forEach((c) => {
      console.log(`${c.consumer.id}\t\t${c.consumer.name}(${c.consumer.alias})`);
    });

    // Request a Route on a specific Fabric
    console.log(`**********Making Route*************`);
    const routeRequest = await router.MakeRoute(fabricId, 'ColinF4:ClipPlayer', 'ColinF4:Flow');

    // Check RouteStatus
    await sleep(2000);

    // Check the route has been made
    const routeStatus = await router.CheckRouteRequest(routeRequest);

    console.log('Route Status');
    console.log(routeStatus);

    // Set a Router Alias for a Producer
    console.log(`**********Set Producer Alias*************`);
    const setAlias = await router.SetProducerAlias(fabricId, 'ColinF4:ClipPlayer', 'CF4:C1');

    console.log('Success');
    console.log(setAlias);

    // Set a Router Alias for a consumer
    const setConsumerAlias = await router.SetConsumerAlias(fabricId, 'ColinF4:Flow', 'CF4:Flow');

    console.log('Success');
    console.log(setConsumerAlias);
  }

  // Get a list of all router salvos
  const salvos = await router.GetSalvos();

  console.log(`**********Salvos*************`);
  salvos.forEach((s) => {
    console.log(`${s.id}\t\t${s.name}`);
  });

  // Execute a Salvo
  console.log(`**********Execute Salvo*************`);
  const salvo = await router.ExecuteSalvo('ColinSalvo1');

  console.log('Success');
  console.log(salvo);

  await sleep(2000);

  // Cnacel a Salvo
  console.log(`**********Cancel Salvo*************`);
  const salvoUndo = await router.CancelSalvo('ColinSalvo1');

  console.log('Success');
  console.log(salvoUndo);

  // Remember to stop listening
  // This deletes the mailbox
  router.StopListeningForRouteEvents();
}

///////////////////////////////////////////////////////////////////////
// Sound Probe Client
///////////////////////////////////////////////////////////////////////
async function subscribeToAudioLevels() {
  /// This method allows you to subscribe to keyframes notifications and save them as images in the local folder
  /// Keyframes are images representing preview of the stream that is generated by producer applications in AMPP

  console.log('Hello Audio Levels Client');


  const audioMeterClient = new SoundProbeClient(platformUrl, apiKey);
  await audioMeterClient.login();
  console.log('Login Ok');

  // The name of Producer you want to subscribe to Audio Levels from
  const PRODUCER_NAME = 'ColinF2:AudioClip2';

  console.log('**********Audio Meters*************');
  await audioMeterClient.startNotificationListener();

  const producer: Producer = await audioMeterClient.getProducerAsync(fabricId, PRODUCER_NAME);
  console.log('Producer:', producer.producer.id);

  const probeId = audioMeterClient.addAudioMeterSubscription(producer);
  console.log('probeId:', probeId);

  audioMeterClient.on('audiometer', (notification: ProbeNotification) => {

    console.log('**********Audio Meter Notification*************');
    console.log(`id: ${notification.id}, time: ${notification.time}, rms: ${notification.rms}, peak: ${notification.peak}`);

  });

  await audioMeterClient.startAudioMeterSubscriptionAsync();
  console.log('Subscription started');

  await sleep(1 * 60 * 1000);

  await audioMeterClient.stopAudioMeterSubscriptionAsync();
  console.log('Subscription stopped');
}


// Run main
main();
