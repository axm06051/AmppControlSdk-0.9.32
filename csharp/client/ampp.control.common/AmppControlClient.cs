using ampp.control.common;
using AmppControl.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmppControl
{
    /// <summary>
    /// Class for interacting with AMPP Control using HTTP Client and SignalR Client
    /// </summary>
    public class AmppControlClient
    {
        private GVPlatform platform;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="platformURL">URL for GV Platform</param>
        /// <param name="apiKey">The JWT API Key</param>
        public AmppControlClient(string platformURL, string apiKey)
        {
            platform = new GVPlatform(platformURL, apiKey);
        }

        /// <summary>
        /// Event Fired when an AMPP Control Notification Event is received
        /// </summary>
        public event EventHandler<AmppControlNotificationEventArgs> OnAmppControlNotifyEvent;

        /// <summary>
        /// Event fired when an AMPP Control Error notification is received
        /// </summary>
        public event EventHandler<AmppControlErrorEventArgs> OnAmppControlErrorEvent;

        /// <summary>
        /// Event fired when signalr hubconnection recovered from a connection loss.
        /// </summary>
        public event EventHandler<string> OnSignalRReconnected;


        /// <summary>
        /// Execute a Macro
        /// </summary>
        /// <param name="macroId">Id of Macro</param>
        /// <param name="reconKey">Key that will be sent in all AMPP Control Messages</param>
        /// <returns></returns>
        public async Task<bool> ExecuteMacroAsync(string macroId, string reconKey)
        {
            var url = "/ampp/control/api/v1/macro/execute";

            var macroRequest = new MacroRequest
            {
                ReconKey = reconKey,
                Uuid = macroId,
            };

            var response =  await this.platform.Post(url, macroRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Request that an Application Sends all information about its state
        /// </summary>
        /// <param name="workloadId">The workload</param>
        /// <param name="reconKey">the key to be sent back in all response messages.</param>
        /// <returns></returns>
        public Task<bool> GetStateAsync(string workloadId, string reconKey)
        {
            return this.PushAmppControlMessageAsync(workloadId, "any", "getstate", new JObject(), reconKey);
        }

        /// <summary>
        /// Gets a List all application types
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AmppControlApplication>> GetApplicationTypesAsync()
        {
            var url = "/ampp/control/api/v1/control/application/references";

            var result = await this.platform.Get(url);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var body = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<IList<AmppControlApplication>>(body);
                return data;
            }

            return null;
        }

        /// <summary>
        /// List all Macros
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AmppControlMacro>> GetMacrosAsync()
        {
            var url = "/ampp/control/api/v1/macro";
            var result = await this.platform.Get(url);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var body = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<IList<AmppControlMacro>>(body);
                return data;
            }

            return null;
        }

        /// <summary>
        /// List all workloads for an application type
        /// </summary>
        /// <param name="applicationType">The application type (I.e. MiniMixer, AudioMixer, Clip Player etc)</param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetWorkloadsForApplicationTypeAsync(string applicationType)
        {
            var url = $"/ampp/control/api/v1/control/application/{applicationType}/workloads";
            var result = await this.platform.Get(url);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var body = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<IList<string>>(body);
                return data;
            }

            return null;
        }

        /// <summary>
        /// Connect to GV Platform
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoginAsync()
        {
            var result = await this.platform.LoginAsync();

            if (result)
            {
                this.platform.OnPushNotifyEvent += Platform_OnPushNotifyEvent;

                return await this.platform.StartNotificationsListenerAsync();

            }

            return result;
        }

        private void Platform_OnPushNotifyEvent(object sender, PushNotification e)
        {
            this.OnNotification(e);
        }

        /// <summary>
        /// Ping an application and wait for a response.
        /// </summary>
        /// <param name="workloadId">The Id of the workload to ping</param>
        /// <param name="timeout">How long to block for response in ms</param>
        /// <returns></returns>
        public async Task<bool> PingAsync(string workloadId, int timeout)
        {
            string pingGuid = System.Guid.NewGuid().ToString();
            ManualResetEvent pingResponse = new ManualResetEvent(false);

            EventHandler<AmppControlNotificationEventArgs> pingHandler = (sender, e) =>
            {
                if (e.Notification.Key == pingGuid)
                {
                    pingResponse.Set();
                }
            };

            OnAmppControlNotifyEvent += pingHandler;

            await PushAmppControlMessageAsync(workloadId, "any", "ping", new JObject(), pingGuid);
            
            bool pingOkay = pingResponse.WaitOne(timeout);

            OnAmppControlNotifyEvent -= pingHandler;

            return pingOkay;
        }

        /// <summary>
        /// Send an AMPP Control message directly using the PushNotifications SignalR connection
        /// </summary>
        /// <param name="workloadId">Workload to send to</param>
        /// <param name="applicationType">Application Type (I.e. MiniMixer)</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="payload">JSON Payload</param>
        /// <param name="reconKey">A key that will be returned in any notify response.</param>
        /// <returns></returns>
        public async Task<bool> PushAmppControlMessageAsync(string workloadId, string applicationType, string command, JObject payload, string reconKey)
        {
            var topic = $"gv.ampp.control.{workloadId}.{command}";

            var content = JObject.FromObject(new
            {
                Key = reconKey,
                Payload = payload,
            });

            return await this.platform.PushNotificationAsync(topic, content);
        }

        /// <summary>
        /// Send an AMPP Control message via an HTTP PUT request to the AMPP Control service
        /// </summary>
        /// <param name="workloadId">Workload to send to</param>
        /// <param name="applicationType">Application Type (I.e. MiniMixer)</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="payload">JSON Payload</param>
        /// <param name="reconKey">A key that will be returned in any notify response.</param>
        /// <returns></returns>
        public async Task<bool> SendAmppControlMessageAsync(string workloadId, string applicationType, string command, JObject payload, string reconKey)
        {
            var url = "/ampp/control/api/v1/control/commit";

            AmppControlRequest controlRequest = new AmppControlRequest
            {
                Application = applicationType,
                Command = command,
                FormData = JsonConvert.SerializeObject(payload),
                Workload = workloadId,
                ReconKey = reconKey,
            };

            var response = await this.platform.Post(url, controlRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// List all ControlGroups for an application type
        /// </summary>
        /// <param name="applicationType">The application type (I.e. MiniMixer)</param>
        /// <returns></returns>
        public async Task<IEnumerable<ControlGroup>> GetControlGroupsForApplicationTypeAsync(string applicationType)
        {
            var url = $"/ampp/control/api/v1/group/application/{applicationType}/groups";
            var result = await this.platform.Get(url);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var body = result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<IList<ControlGroup>>(body);
                return data;
            }

            return null;
        }

        /// <summary>
        /// Subscribe to Notifications for a given workload
        /// </summary>
        /// <param name="workloadId">Id of workload to subscribe to.</param>
        public Task SubscribeToWorkload(string workloadId)
        {
            // Notification topics are of the form:
            // gv.ampp.control.{workload}.{command}.{type}
            string topic = $"gv.ampp.control.{workloadId}.*.*";
            return this.platform.SubscribeToNotification(topic);
        }


        /// <summary>
        /// PushNotification Received from SignalR Hub
        /// </summary>
        /// <param name="notification"></param>

        private void OnNotification(PushNotification notification)
        {
            // Notification topics are of the form:
            // gv.ampp.control.{workload}.{command}.{type}
            var topicParts = notification.Topic.Split(".");
            var type = topicParts[5];
            var command = topicParts[4];
            var workload = topicParts[3];

            var amppPayload = JObject.Parse(notification.Content).ToObject<AmppControlNotification>();

            // Sometimes AMPP Control Messages are bundled up
            // if the Key is multi message then unbundle them.
            if (amppPayload.Key == "multimessage")
            {
                foreach(var message in amppPayload.Payload)
                {
                    var packagedNotification = message.ToObject<AmppControlNotification>();
                    this.ProcessNotification(type, workload, command, packagedNotification);
                }
                return;
            }

            this.ProcessNotification(type, workload, command, amppPayload); 

        }

        private void ProcessNotification(string type, string workload, string command, AmppControlNotification payload)
        {

            if (type == "notify")
            {
                this.OnAmppControlNotifyEvent?.Invoke(this, new AmppControlNotificationEventArgs()
                {
                    Workload = workload,
                    Command = command,
                    Notification = payload,
                });
            }
            else if (type == "status")
            {
                this.OnAmppControlErrorEvent?.Invoke(this, new AmppControlErrorEventArgs()
                {
                    Workload = workload,
                    Command = command,
                    Status = payload.Status,
                    Details = payload.Details,
                    Error = payload.Error,
                    Key = payload.Key,
                });
            }
        }
    }
}
