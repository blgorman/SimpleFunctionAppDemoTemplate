// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SimpleFunctionAppDemo
{
    //adapted from from serverless MCW
    public static class ParseFileEventGridTrigger
    {
        private static HttpClient _client;

        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var cloudBlob = new CloudBlob(uri);
            return cloudBlob.Name;
        }

        [FunctionName("ParseFileEventGridTrigger")]
        public static void Run([EventGridTrigger]EventGridEvent eventGridEvent, 
                [Blob(blobPath: "{data.url}", access: FileAccess.Read, Connection = "defaultStorageConnection")] Stream incomingFile, 
                ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());

            // Reuse the HttpClient across calls as much as possible so as not to exhaust all available sockets on the server on which it runs.
            _client = _client ?? new HttpClient();

            try
            {
                if (incomingFile != null)
                {
                    var createdEvent = ((JObject)eventGridEvent.Data).ToObject<StorageBlobCreatedEventData>();
                    var name = GetBlobNameFromUrl(createdEvent.Url);

                    log.LogInformation($"Processing {name}");

                    //process the file here:
                    var data = ParseExcelFileData.GetFileData(incomingFile, log);

                    //write to cosmos:
                    var parsedDataJson = JsonConvert.SerializeObject(data);
                    log.LogInformation($"parsedData:{Environment.NewLine}{parsedDataJson}");
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex.Message);
                throw;
            }

            log.LogInformation($"Finished processing.");
        }
    }
}
