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
        private readonly ApplicationDbContext _context;
        private readonly ServiceBusClient _serviceBusClient;

        public PhotoController(BlobServiceClient blobServiceClient, ApplicationDbContext context, ServiceBusClient serviceBusClient)
        {
            _blobServiceClient = blobServiceClient;
            _context = context;
            _serviceBusClient = serviceBusClient;
        }
        public IActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Upload(IFormFile file)
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
                FileName = file.FileName,
                BlobUri = blobClient.Uri.ToString(),
                UploadDate = DateTime.UtcNow,
                ProcessedBlobUri = " "
            };

            _context.Photos.Add(metadata);
            _context.SaveChanges();

            var message = new
            {
                BlobUri = metadata.BlobUri,
                ImageId = metadata.Id
            };

            var messageJson = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageJson);
            var sender = _serviceBusClient.CreateSender("laiqueue");
            sender.SendMessageAsync(serviceBusMessage).Wait();

            ViewBag.Message = "Image uploaded successfully!";
            return View();
        }
    }
}
