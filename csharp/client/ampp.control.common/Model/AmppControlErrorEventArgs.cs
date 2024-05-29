using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    public class AmppControlErrorEventArgs
    {
        /// <summary>
        /// The workload that raised the error
        /// </summary>
        public string Workload { get; set; }

        /// <summary>
        /// The Command that was called to raise the error
        /// </summary>
        public string Command { get; set; }

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

        /// <summary>
        /// Key indicating the source of the update
        /// </summary>
        public string Key { get; set; }

    }
}
