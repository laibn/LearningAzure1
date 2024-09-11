using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using PhotoProcessor.Data;
using PhotoProcessor.Models;
using System.Text.Json;

namespace PhotoProcessor.Controllers
{
    public class PhotoController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ServiceBusClient _serviceBusClient;

        public PhotoController(BlobServiceClient blobServiceClient, CosmosDbService cosmosDbService, ServiceBusClient serviceBusClient)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosDbService = cosmosDbService;
            _serviceBusClient = serviceBusClient;
        }

        public IActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Please upload a file.";
                return View();
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient("uploads");
            var blobClient = containerClient.GetBlobClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                blobClient.Upload(stream, true);
            }

            var metadata = new PhotoMetadata
            {
                id = Guid.NewGuid().ToString(),
                FileName = file.FileName,
                BlobUri = blobClient.Uri.ToString(),
                UploadDate = DateTime.UtcNow,
                ProcessedBlobUri = " "
            };

            await _cosmosDbService.AddPhotoMetadataAsync(metadata);

            var message = new
            {
                BlobUri = metadata.BlobUri,
                ImageId = metadata.id
            };

            var messageJson = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageJson);
            var sender = _serviceBusClient.CreateSender("laiqueue");
            await sender.SendMessageAsync(serviceBusMessage);

            ViewBag.Message = "Image uploaded successfully!";
            return View();
        }

    }
}
