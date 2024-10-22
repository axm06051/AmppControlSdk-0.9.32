﻿using Microsoft.Extensions.Configuration;
using ampp.control.common;
using ampp.control.common.Model;
using AmppControl.Model;
using KeyframesSdkClient;

/// This program allows you to subscribe to keyframes notifications and save them as images in the local folder
/// Keyframes are images representing preview of the stream that is generated by producer applications in AMPP
/// Provide details of the producer and the envrionment in the appsettings.json file

Console.WriteLine("Hello, World!");

IConfigurationRoot? config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();
PlatformSettings platformSettings = config.GetSection(nameof(PlatformSettings)).Get<PlatformSettings>();
KeyframesSettings keyframesSettings = config.GetSection(nameof(KeyframesSettings)).Get<KeyframesSettings>();


KeyframesClient keyframesClient = new KeyframesClient(
    platformSettings.PlatformUrl, platformSettings.ApiKey, keyframesSettings.FolderPath);


bool connectedToAMPP = await keyframesClient.LoginAsync();
if (!connectedToAMPP)
{
    Exit("Error Connecting to AMPP");
}

Producer producer = await keyframesClient.GetProducerAsync(keyframesSettings.FabricId, keyframesSettings.ProducerName);
string flowId = producer.Stream?.Flows?.FirstOrDefault(f => f.DataType == FlowDataType.Pic)?.FlowId;
if (string.IsNullOrEmpty(flowId))
{
    Exit("Error getting flowId");
}

keyframesClient.AddKeyframesSubscription(keyframesSettings.NodeId.ToString(), flowId);

keyframesClient.StartKeyframesSubscriptionAsync();

Console.WriteLine("Press Enter to stop subscribing");
Console.WriteLine("*******************************\n");
Console.ReadLine();
await keyframesClient.StopKeyframesSubscriptionAsync();

Console.WriteLine("Press Enter to Exit...");
Console.ReadLine();



void Exit(string exitMessage)
{
    Console.WriteLine(exitMessage);
    Console.WriteLine("Press Any Key to Continue...");
    Environment.Exit(0);
}