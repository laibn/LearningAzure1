using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhotoProcessFunction.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Advanced;
using System.Linq;

namespace PhotoProcessFunction
{
    public class Function1
    {
        private readonly string blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureBlobStorage");

        [FunctionName("Function1")]
        public async Task RunAsync([ServiceBusTrigger("laiqueue", Connection = "AzureServiceBus")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            try
            {
                var imageMessage = JsonConvert.DeserializeObject<ImageMessage>(myQueueItem);
                var processedBlobUri = await ProcessImageAsync(imageMessage.BlobUri);

                log.LogInformation($"Image processed and stored at: {processedBlobUri}");
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<string> ProcessImageAsync(string blobUri)
        {
            var blobServiceClient = new BlobServiceClient(blobStorageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("uploads");

            var blobName = Uri.UnescapeDataString(new Uri(blobUri).Segments.Last());
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var inputStream = new MemoryStream())
            {

                await blobClient.DownloadToAsync(inputStream);
                inputStream.Position = 0;

                using (var image = await Image.LoadAsync(inputStream))
                {

                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new Size(100, 100)
                    }));

                    using (var outputStream = new MemoryStream())
                    {
 
                        await image.SaveAsJpegAsync(outputStream);
                        outputStream.Position = 0;

                         containerClient = blobServiceClient.GetBlobContainerClient("resizes");
                        var processedBlobClient = containerClient.GetBlobClient(blobClient.Name);
                        await processedBlobClient.UploadAsync(outputStream, overwrite: true);
                        return processedBlobClient.Uri.ToString();
                    }
                }
            }
        }
    }
}
