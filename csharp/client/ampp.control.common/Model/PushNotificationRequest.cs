using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// DataModel for Sending PushNotification Request
    /// </summary>
    public class PushNotificationRequest
    {
        /// <summary>
        /// Unique Id of messaage
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Time message was sent
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Topic of message
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Source of Messsage
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Time To Live (in ms)
        /// </summary>
        public int Ttl { get; set; }

        /// <summary>
        /// JSON Content of message
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Notification Context
        /// </summary>
        public PushNotificationContext Context { get; set; }
    }

    /// <summary>
    /// Notification Context
    /// </summary>
    public class PushNotificationContext
    {
        /// <summary>
        /// CorrelationId
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
