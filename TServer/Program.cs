using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace TServer
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var configPath = "server_config.xml";
            if (args.Length > 0)
                configPath = args[0];

            var config = ConfigLoader.Load(configPath);
            Console.WriteLine($"Config loaded: Range {config.Range.Min}–{config.Range.Max}, Multicast {config.Multicast.Address}:{config.Multicast.Port}");

            var generator = new QuoteGenerator(config.Range.Min, config.Range.Max);

            using var broadcaster = new UdpBroadcaster(config.Multicast.Address, config.Multicast.Port);

            Console.WriteLine("Start sending quotes. Press Ctrl+C to exit.");

            while (true)
            {
                var (id, value) = generator.Next();
                await broadcaster.SendAsync(id, value);
               //await Task.Delay(1); 
            }
        }

        static AppConfig LoadConfig(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
            using var stream = File.OpenRead(path);
            return (AppConfig)serializer.Deserialize(stream);
        }
    }
}

