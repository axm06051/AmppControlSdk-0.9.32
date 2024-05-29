using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Data Model for Sending an AMPP Control Request over REST interface
    /// </summary>
    public class AmppControlRequest
    {
        /// <summary>
        /// The workload to Control
        /// </summary>
        public string Workload {  get; set; }

        /// <summary>
        /// The Application Type to Control
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// The Command to send
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// A JSON stringified payload for data to send to command
        /// </summary>
        public string FormData { get; set; }

        /// <summary>
        /// A key that will be sent back in any notify or status responses
        /// </summary>
        public string ReconKey { get; set; }

    }
}
