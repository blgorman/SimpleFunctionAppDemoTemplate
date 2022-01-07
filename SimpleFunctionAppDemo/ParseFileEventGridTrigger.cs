// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Text;

namespace SimpleFunctionAppDemo
{
    //adapted from from serverless MCW
    //Parse out the excel file using an event grid trigger input integration that fires from the event grid subscription on the storage account
    /// <summary>
    /// This method parses the document that is uploaded to storage
    ///     must have event grid subscription on create of document at azure storage
    ///     must have the integrations set for input bindings on the Function app
    /// Once the document is parsed, the data is then upserted to cosmos
    ///     must have the output bindings set for the function app to connect to cosmos
    ///     must have the environment variables set to write to cosmos
    /// </summary>
    public static class ParseFileEventGridTrigger
    {
        private static HttpClient _client;

        //parse out the blog name from the url:
        private static string GetBlobNameFromUrl(string bloblUrl)
        {
            var uri = new Uri(bloblUrl);
            var cloudBlob = new CloudBlob(uri);
            return cloudBlob.Name;
        }

        //note: defaultStorageConnection must be set in the environment variables for the function app as `AzureWebJobsdefaultStorageConnection`
        [FunctionName("ParseFileEventGridTrigger")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, 
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
                    var data = ParseExcelFileData.GetFileData(incomingFile, log).Result;

                    //write to cosmos [function bindings present, environment variable set at azure]
                    var databaseMethods = new DatabaseMethods(log);
                    await databaseMethods.UpsertSampleData(data);

                    //could do this as well to the logic app and not have to write the cosmos code and not need the cosmos variables
                    //in the function app
                    //get data serialized and log it
                    /*
                    var parsedDataJson = JsonConvert.SerializeObject(data);
                    log.LogInformation(new string('*', 80));
                    log.LogInformation($"parsedData:{Environment.NewLine}{parsedDataJson}{Environment.NewLine}{Environment.NewLine}");
                    log.LogInformation(new string('*', 80));
                    log.LogInformation($"{Environment.NewLine}");

                    //post to a logic app via HTTP endpoint and body
                    var url = Environment.GetEnvironmentVariable("ParsedDataLogicAppHttpEndpoint");

                    if (url == null)
                    {
                        throw new Exception("Url variable not set...please add logic app url to the environment variables");
                    }

                    var dataType = "application/json";
                    var result = await _client.PostAsync(url, new StringContent(parsedDataJson, Encoding.UTF8, dataType));

                    //get the response back from the logic app post
                    var responseString = await result.Content.ReadAsStringAsync();
                    log.LogInformation($"Logic app response: {responseString}");
                    */
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
