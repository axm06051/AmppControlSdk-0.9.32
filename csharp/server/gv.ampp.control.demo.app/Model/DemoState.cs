using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gv.ampp.control.demo.app.Model
{
    internal class DemoState
    {
        /// <summary>
        /// Channel Index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// An example of setting an Integer
        /// </summary>
        public int? Volume { get; set; }

        /// <summary>
        ///  An example of setting a label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// An example of setting a boolean
        /// </summary>
        public bool? Active { get; set; }
    }
}
