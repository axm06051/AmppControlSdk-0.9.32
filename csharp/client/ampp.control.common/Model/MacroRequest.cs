using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// DataModel for executing a Macro over the RESTApi
    /// </summary>
    public class MacroRequest
    {
        /// <summary>
        /// Id of the Macro
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Key that will be sentback in all notify/status messages from workloads
        /// </summary>
        public string ReconKey { get; set; }

    }
}
