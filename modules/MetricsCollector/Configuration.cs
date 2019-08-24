using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MetricsCollector
{
    public class Configuration
    {
        [JsonConstructor]
        public Configuration(string schemaVersion, IDictionary<string, string> endpoints, int scrapeFrequencySecs, MetricsFormat metricsFormat)
        {
            SchemaVersion = schemaVersion;
            Endpoints = endpoints;
            ScrapeFrequencySecs = scrapeFrequencySecs;
            MetricsFormat = metricsFormat;
        }

        public string SchemaVersion { get; }
        public IDictionary<string, string> Endpoints { get; }
        public int ScrapeFrequencySecs { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricsFormat MetricsFormat { get; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public enum MetricsFormat
    {
        Prometheus,
        Json
    }
}