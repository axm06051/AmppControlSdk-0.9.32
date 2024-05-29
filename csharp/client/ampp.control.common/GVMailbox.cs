using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ampp.control.common.Model;
using Newtonsoft.Json;

namespace ampp.control.common
{
    /// <summary>
    /// Class for getting Notifications from GVPlatform
    /// </summary>
    internal class GVMailbox
    {
        // The connection to GVPlatform
        private readonly GVPlatform gVPlatform;

        // The secret that is used to delete mailbox
        private string secret;

        // The MailboxId
        private string mailboxId;

        /// <summary>
        /// Event fired when a notification is received
        /// </summary>
        public event EventHandler<PlatformNotification> OnPlatformNotification;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gVPlatform"></param>
        public GVMailbox(GVPlatform gVPlatform)
        {
            this.gVPlatform = gVPlatform;
            this.secret = null;
            this.mailboxId = null;
        }

        /// <summary>
        /// Subscribe to a notification topic
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> Subscribe(string topic)
        {
            if (string.IsNullOrEmpty(this.mailboxId))
            {
                await this.CreateMailbox();
            }

            if (string.IsNullOrEmpty(this.mailboxId))
            {
                throw new Exception("Error creating mailbox");
            }

            string subscription = System.Uri.EscapeDataString(topic);
            Console.WriteLine("Subscribing to: " + subscription);

            string url = $"/notifications/api/v1/mailbox/{this.mailboxId}/subscribe/{subscription}";

            var response = await this.gVPlatform.Post(url, null);
            bool ok = response.IsSuccessStatusCode;
            Console.WriteLine("subscription result: " + ok);

            return ok;
        }

        /// <summary>
        /// Create a new mailbox
        /// </summary>
        /// <returns></returns>
        private async Task<string> CreateMailbox()
        {
            // If we already have a mailbox, delete id
            if (!string.IsNullOrEmpty(this.secret))
            {
                await this.DeleteMailbox();
            }

            string mailboxId = $"cs-ampp-sdk-sampl--{System.Guid.NewGuid().ToString()}";
            string url = "/notifications/api/v1/mailbox";

            MailboxRequest mailboxRequest = new MailboxRequest
            {
                Durable = false,
                Id = mailboxId,
                MailboxTTL = 60 * 2 * 1000,
                MaximumLength = 1000,
                Subscription = "gv"
            };

            var response = await this.gVPlatform.Post(url, mailboxRequest);
            if (response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().Result;
                var mailboxResponse = JsonConvert.DeserializeObject<MailboxResponse>(body);
                this.secret = mailboxResponse.Secret;
                this.mailboxId = mailboxResponse.Id;
                return mailboxResponse.Id;
            }

            return null;
        }

        /// <summary>
        /// Delete a mailbox
        /// </summary>
        /// <returns></returns>
        private async Task<bool> DeleteMailbox()
        {
            Console.WriteLine("Deleting Mailbox: " + this.mailboxId);
            string url = $"/notifications/api/v1/mailbox/{this.mailboxId}/{this.secret}";

            var result = await this.gVPlatform.Delete(url);

            this.mailboxId = null;
            this.secret = null;

            Console.WriteLine((int)result.StatusCode);

            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Start the thread that listens for notifications
        /// </summary>
        /// <returns></returns>
        public bool StartNotificationsListener()
        {
            if (string.IsNullOrEmpty(mailboxId))
            {
                // Mailbox hasn't been created
                return false;
            }

            Task.Run(async () =>
            {
                while (mailboxId != null)
                {
                    await Task.Delay(1000);
                    await MailboxPoll();
                }
            });

            return true;
        }

        /// <summary>
        /// Stop the thread that listens for notifications and Delete the mailbox
        /// </summary>
        /// <returns></returns>
        public async Task StopNotificationsListener()
        {
            await this.DeleteMailbox();
            this.mailboxId = null;
        }


        /// <summary>
        /// Poll the Mailbox for new messages
        /// </summary>
        /// <returns></returns>
        private async Task MailboxPoll()
        {
            if (string.IsNullOrEmpty(this.mailboxId))
            {
                // Mailbox hasn't been created or has been deleted
                return;
            }

            var messages = await this.GetMailboxMessages();

            foreach (var m in messages)
            {
                this.OnPlatformNotification.Invoke(this, new PlatformNotification
                {
                    Account = m.UserOrClientId,
                    Content = m.Content,
                    CorrelationId = m.CorrelationId,
                    Source = m.Source,
                    Time = DateTime.Parse(m.Time),
                    Topic = m.Topic,
                    Ttl = 1000
                });
            }

        }

        /// <summary>
        /// Retreive messages from mailbox
        /// </summary>
        /// <returns></returns>
        private async Task<List<MailboxMessage>> GetMailboxMessages()
        {
            string url = "/notifications/api/v1/notifications/" + this.mailboxId + "?count=100&timeout=1000";
            HttpResponseMessage result = await this.gVPlatform.Get(url);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                string response = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<MailboxMessage>>(response);
            }
            return new List<MailboxMessage>();
        }
    }

    /// <summary>
    /// Definition of MailboxRequest
    /// </summary>
    internal class MailboxRequest
    {
        public string Id { get; set; }
        public string Subscription { get; set; }
        public bool Durable { get; set; }
        public int MaximumLength { get; set; }
        public int MailboxTTL { get; set; }
    }

    /// <summary>
    /// Definition of MailboxResponse
    /// </summary>
    internal class MailboxResponse
    {
        public string Id { get; set; }
        public string Subscription { get; set; }
        public bool Durable { get; set; }
        public int MaximumLength { get; set; }
        public int MailboxTTL { get; set; }
        public string Secret { get; set; }
    }

    /// <summary>
    /// Definition of MailboxMessage
    /// </summary>
    internal class MailboxMessage
    {
        public string Id { get; set; }
        public string Time { get; set; }
        public string Topic { get; set; }
        public string Source { get; set; }
        public string CorrelationId { get; set; }
        public string UserOrClientId { get; set; }
        public string Content { get; set; }
    }

}
