using ampp.control.common.Model;

namespace AmppControl.Model
{
    /// <summary>
    /// DataModel for receiving PushNotification from SignalR client
    /// </summary>
    public class PushNotification
    {
        /// <summary>
        /// Account where PushNotification originated
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Id of notification
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Time notification sent
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Notification Topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// JSON payload
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// where notification originated from
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// CorrelationId
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Binary contents type.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Binary contents length.
        /// </summary>
        public long ContentLength { get; set; }

        /// <summary>
        /// Binary content of the notification.
        /// </summary>
        public byte[] BinaryContent { get; set; }
    }
}
