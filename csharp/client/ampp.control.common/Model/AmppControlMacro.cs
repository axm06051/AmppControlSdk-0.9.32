using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmppControl.Model
{
    /// <summary>
    /// DataModel for accessing a Macro over RESTApi
    /// </summary>
    public class AmppControlMacro
    {
        /// <summary>
        /// Unique ID OF Macro
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Name of Macro
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of Macro
        /// </summary>
        public string Description { get; set; }
    }
}
