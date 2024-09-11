namespace PhotoProcessor.Models
{
    public class PhotoMetadata
    {
        public string id { get; set; }
        public string FileName { get; set; }
        public string BlobUri { get; set; }
        public DateTime UploadDate { get; set; }
        public string ProcessedBlobUri { get; set; }
    }
}
