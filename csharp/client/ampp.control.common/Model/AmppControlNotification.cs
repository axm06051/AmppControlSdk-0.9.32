using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// DataModel for AmppControl Notification
    /// </summary>
    public class AmppControlNotification
    {
        /// <summary>
        /// The message payload
        /// </summary>
        public JToken Payload { get; set; }

        /// <summary>
        /// The reconKey
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the error details.
        /// </summary>
        public string Details { get; set; }
    }
}
