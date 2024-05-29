using AmppControl.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ampp.control.common
{
    internal class GVPlatform
    {
        private readonly HttpClient client = new HttpClient();
        private readonly string platformURL;
        private readonly string apiKey;
        private string bearerToken = null;
        private readonly string correlationId = null;
        private HubConnection hubConnection = null;


        /// <summary>
        /// Event Fired when an AMPP Control Notification Event is received
        /// </summary>
        public event EventHandler<PushNotification> OnPushNotifyEvent;

        /// <summary>
        /// Event fired when signalr hubconnection recovered from a connection loss.
        /// </summary>
        private event EventHandler<string> OnSignalRReconnected;


        public GVPlatform(string platformURL, string apiKey)
        {
            this.platformURL = platformURL;
            this.apiKey = apiKey;
            client.BaseAddress = new Uri(platformURL);
            correlationId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Connect to GV Platform
        /// </summary>
        /// <returns></returns>
        public Task<bool> LoginAsync()
        {
            return this.GetAccessTokenAsync();
        }

        /// <summary>
        /// Starts a Connection to the SignalR Hub on the PushNotificationService to receive Notifications
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartNotificationsListenerAsync()
        {
            try
            {
                var hubConnectionUrl = $"{this.platformURL}/pushnotificationshub";

                this.hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubConnectionUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(this.bearerToken);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                hubConnection.Reconnecting += HubConnection_Reconnecting;
                hubConnection.Reconnected += HubConnection_Reconnected;
                hubConnection.Closed += HubConnection_Closed;

                await hubConnection.StartAsync();

                hubConnection.On<string>("Pong", (s) => {
                    Log.Debug("Pong: " + s);
                }
                );
                await hubConnection.InvokeAsync("Ping");

                hubConnection.On<PushNotification>("ReceiveNotification", OnNotification);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start notification listener {ex}");
                return false;
            }
        }

        private Task HubConnection_Closed(Exception arg)
        {
            Log.Error($"SignalR hub connection closed.  Error : {arg?.Message}");
            return Task.CompletedTask;
        }

        private async Task HubConnection_Reconnected(string arg)
        {
            Log.Information($"SignalR hub reconnected.");
            OnSignalRReconnected?.Invoke(this, arg);
            await hubConnection.InvokeAsync("Ping");
        }

        /// <summary>
        /// Reconnection event handler for signalr hubconnections.
        /// </summary>
        /// <param name="arg">The cause of the disconnection</param>
        /// <returns></returns>
        private Task HubConnection_Reconnecting(Exception arg)
        {
            Log.Error($"SignalR hub connection lost. Reconnecting.  Error : {arg?.Message}");
            return Task.CompletedTask;
        }


        /// <summary>
        /// PushNotification Received from SignalR Hub
        /// </summary>
        /// <param name="notification"></param>

        private void OnNotification(PushNotification notification)
        {
            this.OnPushNotifyEvent?.Invoke(this, notification);
        }

        /// <summary>
        /// Gets an access token to be used in all REST calls using your API key
        /// Needs to be called every 30 mins to make sure the token is refreshed as it will expire after an hour
        /// </summary>
        /// <returns></returns>
        private async Task<bool> GetAccessTokenAsync()
        {
            try
            {
                var request = new HttpRequestMessage();
                request.Method = HttpMethod.Post;
                request.Headers.Add("Authorization", $"Basic {this.apiKey}");
                request.RequestUri = new Uri(this.platformURL + "/identity/connect/token");
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    // For AMPP Control you need only: platform cluster.readonly
                    // For Routing you need: platform cluster.readonly cluster
                    // new KeyValuePair<string, string>("scope", "platform cluster.readonly")
                    new KeyValuePair<string, string>("scope", "platform cluster.readonly cluster")
                });

                var result = await client.SendAsync(request);
                if (result != null && result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // we have a valid token
                    var body = result.Content.ReadAsStringAsync().Result;
                    var data = JsonConvert.DeserializeObject<JObject>(body);
                    this.bearerToken = data.GetValue("access_token").ToString();
                    int expiryTime = data.Value<int>("expires_in");
                    ScheduleTokenRefresh(expiryTime*1000);
                    return true;
                }
                else
                {
                    Log.Error($"Failed to get access token {result?.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error obtaining access token {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Refresh the AccessToken at 75% of the remaining time until expiry or every 1 hour, whichever comes first.
        /// </summary>
        private void ScheduleTokenRefresh(int expiryTimeMs)
        {
            // Calculate 75% of the remaining time until expiry
            double remainingTime = expiryTimeMs - DateTime.Now.Millisecond;
            double interval = Math.Min(0.75 * remainingTime, TimeSpan.FromHours(1).TotalMilliseconds);

            // Create and configure the timer
            var timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.Elapsed += async (s, e) => { await GetAccessTokenAsync(); };
            timer.AutoReset = false;
            timer.Start();
        }

        /// <summary>
        /// Send Request to SignalR to Subscribe to a specific topic
        /// </summary>
        /// <param name="topic"></param>
        public Task SubscribeToNotification(string topic)
        {
            var subscriptionRequest = new
            {
                subscriptions = new string[] { topic },
                context = new { correlationId }
            };
            return this.hubConnection.InvokeAsync("Subscribe", subscriptionRequest);
        }

        public Task UnsubscribeFromNotification(string topic)
        {
            var unsubscriptionRequest = new
            {
                subscriptions = new string[] { topic },
                context = new { correlationId }
            };
            return this.hubConnection.InvokeAsync("Unsubscribe", unsubscriptionRequest);
        }

        public async Task<bool> PushNotificationAsync(string topic, JObject content)
        {
            PushNotificationRequest request = new PushNotificationRequest
            {
                Content = JsonConvert.SerializeObject(content),
                Context = new PushNotificationContext
                {
                    CorrelationId = correlationId,
                },
                Id = Guid.NewGuid().ToString(),
                Source = "AMPP SDK Sample",
                Time = DateTime.UtcNow.ToString("s"),
                Topic = topic,
                Ttl = 3000,
            };

            await hubConnection.InvokeAsync("PublishNotification", request);

            return true;
        }

        public async Task<HttpResponseMessage> Post(string url, object data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {this.bearerToken}");


            var json = JsonConvert.SerializeObject(data);
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            request.Content = content;

            var result = await client.SendAsync(request);
            return result;
        }

        public async Task<HttpResponseMessage> Get(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {this.bearerToken}");
            var result = await client.SendAsync(request);
            return result;
        }

        public async Task<HttpResponseMessage> Put(string url, object data)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Add("Authorization", $"Bearer {this.bearerToken}");
            request.Headers.Add("if-match", "\"*\"");


            var json = JsonConvert.SerializeObject(data);
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json-patch+json");
            request.Content = content;

            var result = await client.SendAsync(request);
            return result;
        }

        public async Task<HttpResponseMessage> Delete(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", $"Bearer {this.bearerToken}");
            var result = await client.SendAsync(request);
            return result;
        }

    }
}
