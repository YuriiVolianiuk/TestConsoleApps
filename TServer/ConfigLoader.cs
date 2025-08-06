using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TServer
{
    public static class ConfigLoader
    {
        public static AppConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}");

            XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
            using var stream = File.OpenRead(path);
            var config = (AppConfig)serializer.Deserialize(stream);

            Validate(config);

            return config;
        }
        private static void Validate(AppConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Config is null");

            if (config.Multicast == null)
                throw new ArgumentException("Multicast config missing");

            if (string.IsNullOrWhiteSpace(config.Multicast.Address))
                throw new ArgumentException("Multicast address missing");

            if (config.Multicast.Port <= 0 || config.Multicast.Port > 65535)
                throw new ArgumentException("Multicast port is invalid");

            if (config.Range == null)
                throw new ArgumentException("Range config missing");

            if (config.Range.Min >= config.Range.Max)
                throw new ArgumentException("Range min should be less than max");
        }
    }
}