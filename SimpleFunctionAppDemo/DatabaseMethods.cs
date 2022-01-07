using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFunctionAppDemo
{
    public class DatabaseMethods
    {
        //ported from the MCW-Serverless-Architecture
        private readonly string _endpointUrl = Environment.GetEnvironmentVariable("cosmosDBEndpointUrl");
        private readonly string _authorizationKey = Environment.GetEnvironmentVariable("cosmosDBAuthorizationKey");
        private readonly string _databaseId = Environment.GetEnvironmentVariable("cosmosDBDatabaseId");
        private readonly string _collectionId = Environment.GetEnvironmentVariable("cosmosDBCollectionId");
        private readonly ILogger _log;
        
        // Reusable instance of DocumentClient which represents the connection to a Cosmos DB endpoint.
        private DocumentClient _client;

        public DatabaseMethods(ILogger log)
        {
            _log = log;
        }
        /// <summary>
        /// Upserts data from import
        /// </summary>
        /// <param name="data">The data to upsert</param>
        /// <returns></returns>
        public async Task UpsertSampleData(IEnumerable<FileDataPayload> data)
        {
            _log.LogInformation("Upserting Sample Data in the database");
            var collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

            using (_client = new DocumentClient(new Uri(_endpointUrl), _authorizationKey))
            {
                foreach (var sampleData in data)
                {
                    var response = await _client.UpsertDocumentAsync(UriFactory.CreateCollectionUri(_databaseId, _collectionId), sampleData);
                    _log.LogInformation($"Upsert complete for document: {sampleData.UniqueId}");
                }
            }
        }
    }
}
