using Microsoft.Azure.Cosmos;
using PhotoProcessor.Models;

namespace PhotoProcessor.Data
{
    public class CosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task AddPhotoMetadataAsync(PhotoMetadata metadata)
        {
            await _container.CreateItemAsync(metadata, new PartitionKey(metadata.id.ToString()));
        }
    }
}
