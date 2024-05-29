namespace ampp.control.common.Model
{
    /// <summary>
    /// DataModel for ConsumerResponse
    /// </summary>
    public class ConsumerResponse
    {
        public ConsumerData[] Consumers { get; set; }
    }

    /// <summary>
    /// DataModel for ConsumerData
    /// </summary>
    public class ConsumerData
    {
        public Consumer Consumer { get; set; }
    }

    /// <summary>
    /// DataModel for Consumer
    /// </summary>
    public class Consumer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string WorkloadId { get; set; }
        public string NodeId { get; set; }
        public string FabricId { get; set; }
        public string GroupName { get; set; }
        public string Type { get; set; }
        public string TallyState { get; set; }
        public bool Locked { get; set; }
        public string Flags { get; set; }
        public bool Enabled { get; set; }
    }
}
