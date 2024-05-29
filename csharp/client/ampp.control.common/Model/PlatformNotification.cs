using System;

namespace ampp.control.common.Model
{
    /// <summary>
    /// DataModel for a PlatformNotification
    /// </summary>
    internal class PlatformNotification
    {
        public string Account { get; set; }
        public DateTime Time { get; set; }
        public string Topic { get; set; }
        public dynamic Content { get; set; }
        public string Source { get; set; }
        public string CorrelationId { get; set; }
        public int Ttl { get; set; }
    }
}
