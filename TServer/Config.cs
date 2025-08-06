using System.Xml.Serialization;

namespace TServer
{
    [XmlRoot("Config")]
    public class AppConfig
    {
        public RangeConfig Range { get; set; }
        public MulticastConfig Multicast { get; set; }
    }

    public class RangeConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class MulticastConfig
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }
}
