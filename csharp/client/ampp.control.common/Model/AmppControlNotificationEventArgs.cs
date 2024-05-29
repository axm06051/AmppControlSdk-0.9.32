using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// EventArgs for an AMPP Control Notify message
    /// </summary>
    public class AmppControlNotificationEventArgs
    {
        /// <summary>
        /// The notification response
        /// </summary>
        public AmppControlNotification Notification { get; set; }

        /// <summary>
        /// The workload that raised the notification
        /// </summary>
        public string Workload { get; set; }

        /// <summary>
        /// The Command that was executed
        /// </summary>
        public string Command { get; set; }
    }
}
