using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Example Config object for storing in the Config Service
    /// </summary>
    internal class DemoConfiguration
    {
        /// <summary>
        /// Gets or Sets the ConnectionString
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Port
        /// </summary>
        public int Port { get; set; }
    }
}
