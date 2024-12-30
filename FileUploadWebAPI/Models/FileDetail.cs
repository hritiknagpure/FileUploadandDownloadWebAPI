namespace FileUploadWebAPI.Models
{
    public class FileDetail
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; } // MIME type, e.g., "image/jpeg"
        public byte[] Data { get; set; } // Image data
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
    }

}
