using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// Data Model for retreiving ControlGroups over the RESTApi
    /// </summary>
    public class ControlGroup
    {
        /// <summary>
        /// Id if Group
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Application type associated with group
        /// </summary>
        public string ApplicationType { get; set; }

        /// <summary>
        /// List of workloads withing the group
        /// </summary>
        public IList<string> Workloads { get; set; }
    }
}
