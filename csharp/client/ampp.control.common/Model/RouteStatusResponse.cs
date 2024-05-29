using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ampp.control.common.Model
{
    /// <summary>
    /// Definition of RouteStatusResponse
    /// </summary>
    public class RouteStatusResponse
    {
        public string RequestId { get; set; }

        public RouteStatusData Status { get; set; }

    }

    /// <summary>
    /// Definition of RouteStatusData
    /// </summary>
    public class RouteStatusData
    {
        public string RouteStatus { get; set; }
        public string RouteErrorMessage { get; set; }

        public override string ToString()
        {
            return $"{RouteStatus} {RouteErrorMessage}";   
        }
    }
}
