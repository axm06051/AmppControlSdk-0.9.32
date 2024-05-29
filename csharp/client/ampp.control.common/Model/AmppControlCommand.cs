using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Data Model for Retreiving Ampp Control Commands over RESTApi
    /// </summary>
    public class AmppControlCommand
    {
        /// <summary>
        /// Name of Command
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Command Version
        /// </summary>
        public string Version {  get ; set; }

        /// <summary>
        /// A JSON7 Schema defining the parameters used in Command
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// A Markdown file describing how to use the command
        /// </summary>
        public string Markdown { get; set; }
    }
}
