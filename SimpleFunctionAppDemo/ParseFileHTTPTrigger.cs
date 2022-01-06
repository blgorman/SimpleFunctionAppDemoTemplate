using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Azure.Storage.Blobs;
using System.Text;

namespace SimpleFunctionAppDemo
{
    public static class ParseFileHTTPTrigger
    {
        private static HttpClient _client;

        private static string _storageAccountConnectionString;
        private static BlobServiceClient _blobServiceClient;
        private static BlobContainerClient _blobContainerClient;

        //note: defaultStorageConnection must be set in the environment variables for the function app as `AzureWebJobsdefaultStorageConnection`
        //note: ParsedDataLogicAppHttpEndpoint must be set in order to post back to a logic app to write to cosmos (or whatever you want to do with it).
        [FunctionName("ParseFileHTTPTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Reuse the HttpClient across calls as much as possible so as not to exhaust all available sockets on the server on which it runs.
            _client = _client ?? new HttpClient();

            var payload = await req.Content.ReadAsAsync<ParseFilePayload>();

            //connect to azure storage using the SDK, not bindings
            _storageAccountConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsdefaultStorageConnection");
            _blobServiceClient = new BlobServiceClient(_storageAccountConnectionString);

            //get the container
            var container = _blobServiceClient.GetBlobContainerClient(payload.containerName);

            //set the container permission to public blob, get the container client reference
            _blobContainerClient = new BlobContainerClient(_storageAccountConnectionString, container.Name);

            var blob = _blobContainerClient.GetBlobClient(payload.blobName);

            MemoryStream stream = new MemoryStream();
            blob.DownloadTo(stream);
            stream.Position = 0;

            var data = ParseExcelFileData.GetFileData(stream, log).Result;

            //use this payload to post back to a logic app
            var parsedDataJson = JsonConvert.SerializeObject(data);
            log.LogInformation(new string('*', 80));
            log.LogInformation($"parsedData:{Environment.NewLine}{parsedDataJson}{Environment.NewLine}{Environment.NewLine}");
            log.LogInformation(new string('*', 80));
            log.LogInformation($"{Environment.NewLine}");

            var url = Environment.GetEnvironmentVariable("ParsedDataLogicAppHttpEndpoint");

            if (url == null)
            {
                log.LogInformation("No URL for logic app to post result. Exiting");
                return new OkObjectResult("Success without post to callback logic app");
            }

            var dataType = "application/json";
            var result = await _client.PostAsync(url, new StringContent(parsedDataJson, Encoding.UTF8, dataType));

            //get the response back from the logic app post
            var responseString = await result.Content.ReadAsStringAsync();
            log.LogInformation("Logic app response: {responseString}");

            return new OkObjectResult("Success");
        }
    }
}
