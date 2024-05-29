using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ampp.control.common.Model
{
    /// <summary>
    /// DataModel for ProducerResponse
    /// </summary>
    public class ProducerResponse
    {
        public ProducerData[] Producers { get; set; }
    }

    /// <summary>
    /// DataModel for ProducerData
    /// </summary>
    public class ProducerData
    {
        public Producer Producer { get; set; }
    }

    /// <summary>
    /// DataModel for a Producer
    /// </summary>
    public class Producer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string WorkloadId { get; set; }
        public string NodeId { get; set; }
        public string FabricId { get; set; }
        public string GroupName { get; set; }
        public string Type { get; set; }
        public Stream Stream { get; set; }
    }

    /// <summary>
    /// DataModel for a Stream
    /// </summary>
    public class Stream
    {
        public string StreamId { get; set; }
        public List<Flow> Flows { get; set; }

        /// <summary>
        /// DataModel for a Flow
        /// </summary>
        public class Flow
        {
            public int FrameAge { get; set; }
            public int MaxFrameAge { get; set; }
            public int MinFrameAge { get; set; }
            public string FlowId { get; set; }
            public FlowDataType DataType { get; set; }
            public Descriptor Descriptor { get; set; }
        }
    }

    /// <summary>
    /// DataModel for a Descriptor
    /// </summary>
    public class Descriptor
    {
        public AspectRatio AspectRatio { get; set; }
        public string ColorSpace { get; set; }
        public int Height { get; set; }
        public string PixelLayout { get; set; }
        public bool Progressive { get; set; }
        public Rate Rate { get; set; }
        public string TransferCharacteristic { get; set; }
        public int Width { get; set; }

    }

    /// <summary>
    /// DataModel for AspectRatio
    /// </summary>
    public class AspectRatio
    {
        public int Den { get; set; }

        public int Num { get; set; }
    }

    /// <summary>
    /// DataModel for Rate
    /// </summary>
    public class Rate
    {
        public int Den { get; set; }
        public int Num { get; set; }
    }
}
