using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MetricsCollector
{
    public class Configuration
    {
        [JsonConstructor]
        public Configuration(string schemaVersion, IDictionary<string, string> endpoints, int scrapeFrequencySecs, MetricsFormat metricsFormat, SyncMethod syncMethod)
        {
            SchemaVersion = schemaVersion;
            Endpoints = endpoints;
            ScrapeFrequencySecs = scrapeFrequencySecs;
            MetricsFormat = metricsFormat;
            SyncMethod = syncMethod;
        }

        public string SchemaVersion { get; }
        public IDictionary<string, string> Endpoints { get; }
        public int ScrapeFrequencySecs { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricsFormat MetricsFormat { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SyncMethod SyncMethod { get; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    public enum MetricsFormat
    {
        Prometheus,
        Json
    }

    public enum SyncMethod
    {
        IoTHub,
        RestAPI
    }
}