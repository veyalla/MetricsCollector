using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Azure.Devices.Client;

namespace MetricsCollector
{
    public class MetricsSync
    {
        readonly MessageFormatter messageFormatter;
        readonly Scraper scraper;
        readonly ModuleClient moduleClient;

        readonly string wId, wKey, clName;
        readonly SyncMethod syncMethod;
        readonly AzureLogAnalytics logAnalytics;

        public MetricsSync(MessageFormatter messageFormatter, Scraper scraper, ModuleClient moduleClient)
        {
            this.messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            this.scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
        }

        public MetricsSync(MessageFormatter messageFormatter, SyncMethod syncMethod, Scraper scraper, ModuleClient moduleClient, String wId, String wKey, String clName)
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

                if (this.syncMethod == SyncMethod.RestAPI) 
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

    // Sample code from: 
    // https://dejanstojanovic.net/aspnet/2018/february/send-data-to-azure-log-analytics-from-c-code/

    public class AzureLogAnalytics
    {
        public String WorkspaceId { get; set; }
        public String SharedKey { get; set; }
        public String ApiVersion { get; set; }
        public String LogType { get; set; }
        public AzureLogAnalytics(String workspaceId, String sharedKey, String logType, String apiVersion = "2016-04-01")
        {
            this.WorkspaceId = workspaceId;
            this.SharedKey = sharedKey;
            this.LogType = logType;
            this.ApiVersion = apiVersion;
        }
        public void Post(byte[] content)
        {
            string requestUriString = $"https://{WorkspaceId}.ods.opinsights.azure.com/api/logs?api-version={ApiVersion}";
            DateTime dateTime = DateTime.UtcNow;
            string dateString = dateTime.ToString("r");
            string signature = GetSignature("POST", content.Length, "application/json", dateString, "/api/logs");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers["Log-Type"] = LogType;
            request.Headers["x-ms-date"] = dateString;
            request.Headers["Authorization"] = signature;
            using (Stream requestStreamAsync = request.GetRequestStream())
            {
                requestStreamAsync.Write(content, 0, content.Length);
            }
            using (HttpWebResponse responseAsync = (HttpWebResponse)request.GetResponse())
            {
                if (responseAsync.StatusCode != HttpStatusCode.OK && responseAsync.StatusCode != HttpStatusCode.Accepted)
                {
                    Stream responseStream = responseAsync.GetResponseStream();
                    if (responseStream != null)
                    {
                        using (StreamReader streamReader = new StreamReader(responseStream))
                        {
                        throw new Exception(streamReader.ReadToEnd());
                        }
                    }
                }
            }
        }

        private string GetSignature(string method, int contentLength, string contentType, string date, string resource)
        {
            string message = $"{method}\n{contentLength}\n{contentType}\nx-ms-date:{date}\n{resource}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            using (HMACSHA256 encryptor = new HMACSHA256(Convert.FromBase64String(SharedKey)))
            {
                return $"SharedKey {WorkspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
            }
        }
    }
}