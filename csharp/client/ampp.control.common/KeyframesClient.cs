using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using AmppControl.Model;
using ampp.control.common.Model;

namespace ampp.control.common
{
    public class KeyframesClient
    {
        private GVPlatform gvPlatform;

        private List<string> subscriptions = new List<string>();
        private readonly string folderPath;
        private readonly CancellationTokenSource frameCacheSubscriptionCancellationToken = new CancellationTokenSource();

        private const int SUBSCRIPTION_RENEW_TIME_SECONDS = 60;

        public KeyframesClient(string baseURL, string apiKey, string folderPath)
        {
            gvPlatform = new GVPlatform(baseURL, apiKey);
            this.folderPath = folderPath;
        }

        public async Task<bool> LoginAsync()
        {
            bool connected = await this.gvPlatform.LoginAsync();
            if (connected)
            {
                gvPlatform.OnPushNotifyEvent += OnNotification;
                return await this.gvPlatform.StartNotificationsListenerAsync();
            }
            return connected;
        }


        public async Task<Producer> GetProducerAsync(Guid fabricId, string producerName)
        {
            string requestUri = $"/cluster/matrix/api/v1/producer/{fabricId}/{producerName}";
            HttpResponseMessage response = await this.gvPlatform.Get(requestUri);

            if (response.IsSuccessStatusCode)
            {
                string body = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<Producer>(body);
            }
            return null;
        }

        public void AddKeyframesSubscription(string nodeId, string flowId)
        {
            string subscription = $"gv.ampp.keyframe.{nodeId}.{flowId}.{PreviewSize.Small.ToString().ToLower()}";
            subscriptions.Add(subscription);
        }

        public async Task StartKeyframesSubscriptionAsync()
        {
            foreach (string subscription in subscriptions)
            {
                await gvPlatform.SubscribeToNotification(subscription);
                Console.WriteLine($"Subscribing {subscription}");
            }
            await Task.Run(async () => 
            { 
                await RenewFrameCacheSubscriptions(); 
            }, 
            frameCacheSubscriptionCancellationToken.Token);
        }

        private async Task RenewFrameCacheSubscriptions()
        {
            try
            {
                while (!frameCacheSubscriptionCancellationToken.Token.IsCancellationRequested)
                {
                    foreach (string subscription in subscriptions)
                    {
                        Console.WriteLine($"Renewing subscription: {subscription}");
                        string[] parts = subscription.Split('.');
                        string topic = $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}";
                        string flowId = parts[4];
                        await SendFlowSubscriptionRequest(topic, flowId);
                    }
                    await Task.Delay(SUBSCRIPTION_RENEW_TIME_SECONDS * 1000);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while subscribing to keyframes!", ex);
                throw;
            }
        }

        public async Task StopKeyframesSubscriptionAsync()
        {
            frameCacheSubscriptionCancellationToken.Cancel();
            foreach (string subscription in subscriptions)
            {
                await gvPlatform.UnsubscribeFromNotification(subscription);
                Console.WriteLine($"Unsubscribing {subscription}");
            }
        }

        // This method tells the framecache to generate keyframes
        private Task SendFlowSubscriptionRequest(string topic, string flowId)
        {
            KeyframesSubscriptionNotification notification = new KeyframesSubscriptionNotification()
            {
                PreviewSize = PreviewSize.Small,
                FlowId = flowId
            };
            return gvPlatform.PushNotificationAsync(topic, JObject.FromObject(notification));
        }

        private void OnNotification(object sender, PushNotification e)
        {
            Console.WriteLine("Received keyframes notification");
            if(e.Content == MediaTypeNames.Image.Jpeg && e.BinaryContent is not null)
            {
                string filePath = $"{folderPath}\\keyframe.jpg";
                File.WriteAllBytes(filePath, e.BinaryContent);
                Console.WriteLine($"Saved keyframe as {filePath}");
                Console.WriteLine(DateTime.Now.ToString());
            }
            else if (e.Content == "Removed")
            {
                Console.WriteLine("Notification is removed");
            }
            else
            {
                throw new Exception("Error handling keyframes notification");
            }
        }
    }
}
