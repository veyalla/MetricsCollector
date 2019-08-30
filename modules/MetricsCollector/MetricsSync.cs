namespace MetricsCollector
{
    using Microsoft.Azure.Devices.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MetricsSync
    {
        readonly MessageFormatter messageFormatter;
        readonly Scraper scraper;
        readonly ModuleClient moduleClient;

        readonly string wId, wKey, clName;
        readonly SyncTarget syncMethod;
        readonly AzureLogAnalytics logAnalytics;

        public MetricsSync(MessageFormatter messageFormatter, Scraper scraper, ModuleClient moduleClient)
        {
            this.messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            this.scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
        }

        public MetricsSync(MessageFormatter messageFormatter, SyncTarget syncMethod, Scraper scraper, ModuleClient moduleClient, String wId, String wKey, String clName)
        {
            this.messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            this.scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
            this.syncMethod = syncMethod;
            this.wId = wId;
            this.wKey = wKey;
            this.moduleClient = moduleClient; 
            this.clName = clName;

            if (this.wId != "" && this.wKey != "")
            {
                this.logAnalytics = new AzureLogAnalytics(wId, wKey, clName);
            }
        }

        public async Task ScrapeAndSync()
        {
            try
            {
                IEnumerable<string> scrapedMetrics = await this.scraper.Scrape();

                if (this.syncMethod == SyncTarget.RestAPI) 
                {
                    foreach (var scrape in scrapedMetrics) 
                    {
                        logAnalytics.Post(this.messageFormatter.BuildJSON(scrape));
                    }
                } 
                else 
                {
                    IList<Message> messages =
                        scrapedMetrics.SelectMany(prometheusMetrics => this.messageFormatter.Build(prometheusMetrics)).ToList();
                    await this.moduleClient.SendEventBatchAsync(messages);
                    Console.WriteLine($"Sent metrics as {messages.Count} messages");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error scraping and syncing metrics - {e}");
            }
        }
    }    
}