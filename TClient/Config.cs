using System.Xml.Serialization;

namespace ClientConsole
{
    [XmlRoot("Config")]
    public class AppConfig
    {
        public required MulticastConfig Multicast { get; set; }
    }

    public class MulticastConfig
    {
        public required string Address { get; set; }
        public int Port { get; set; }
    }
}
