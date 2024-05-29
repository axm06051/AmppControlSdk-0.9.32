using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Data Model for getting Applications over AMPPControl RESTApi
    /// </summary>
    public class AmppControlApplication
    {
        /// <summary>
        /// Name of Application
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of Supported Commands and their Schemas
        /// </summary>
        public IList<AmppControlCommand> Commands { get; set; }
    }
}
