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

namespace SimpleFunctionAppDemo
{
    public static class ParseFileHTTPTrigger
    {
        private static HttpClient _client;

        private static string _storageAccountConnectionString;
        private static BlobServiceClient _blobServiceClient;
        private static BlobContainerClient _blobContainerClient;

        [FunctionName("ParseFileHTTPTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Reuse the HttpClient across calls as much as possible so as not to exhaust all available sockets on the server on which it runs.
            _client = _client ?? new HttpClient();

            var payload = await req.Content.ReadAsAsync<ParseFilePayload>();

            //connect to azure storage
            _storageAccountConnectionString = Environment.GetEnvironmentVariable("defaultStorageAccountString");
            _blobServiceClient = new BlobServiceClient(_storageAccountConnectionString);

            //get the container
            var container = _blobServiceClient.GetBlobContainerClient(payload.containerName);

            //set the container permission to public blob, get the container client reference
            _blobContainerClient = new BlobContainerClient(_storageAccountConnectionString, container.Name);

            var blob = _blobContainerClient.GetBlobClient(payload.blobName);

            MemoryStream stream = new MemoryStream();
            blob.DownloadTo(stream);
            stream.Position = 0;

            var data = ParseExcelFileData.GetFileData(stream, log);

            var parsedDataJson = JsonConvert.SerializeObject(data);
            log.LogInformation($"parsedData:{Environment.NewLine}{parsedDataJson}");


            return new OkObjectResult("Success");
        }
    }
}
