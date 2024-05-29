using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Platform settings from appsettings.json
    /// </summary>
    public class PlatformSettings
    {
        /// <summary>
        /// The URI of the platform
        /// </summary>
        public string PlatformUrl { get; set; }

        /// <summary>
        /// The APIKey
        /// </summary>
        public string ApiKey { get; set; }
    }
}
