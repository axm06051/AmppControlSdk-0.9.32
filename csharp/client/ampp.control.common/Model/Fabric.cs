using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ampp.control.common.Model
{
    /// <summary>
    /// DataModel for FabricsResponse
    /// </summary>
    public class FabricsResponse
    {
        public FabricData[] Fabrics { get; set; }
    }

    /// <summary>
    /// DataModel for FabricData
    /// </summary>
    public class FabricData
    {
        public Fabric Fabric { get; set; }
    }

    /// <summary>
    /// DataModel for Fabric
    /// </summary>
    public class Fabric
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Watermarking { get; set; }
        public string Type { get; set; }
        public Node[] Nodes { get; set; }
    }

    /// <summary>
    /// DataMode for Node
    /// </summary>
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
