using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace MetricsCollector
{
    public class MetricsSync
    {
        readonly MessageFormatter messageFormatter;
        readonly Scraper scraper;
        readonly ModuleClient moduleClient;

        public MetricsSync(MessageFormatter messageFormatter, Scraper scraper, ModuleClient moduleClient)
        {
            this.messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            this.scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
            this.moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
        }

        public async Task ScrapeAndSync()
        {
            try
            {
                IEnumerable<string> scrapedMetrics = await this.scraper.Scrape();
                IList<Message> messages =
                    scrapedMetrics.SelectMany(prometheusMetrics => this.messageFormatter.Build(prometheusMetrics)).ToList();
                await this.moduleClient.SendEventBatchAsync(messages);
                Console.WriteLine($"Sent metrics as {messages.Count} messages");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error scraping and syncing metrics to IoTHub - {e}");
            }
        }
    }
}