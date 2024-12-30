using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FileUploadWebAPI.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FileUploadWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FileUploadController> _logger;

        // Injecting AppDbContext and ILogger to log events
        public FileUploadController(AppDbContext context, ILogger<FileUploadController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Upload a file and store its metadata in the database.
        /// </summary>
        /// <param name="file">The file to be uploaded</param>
        /// <returns>Result with the file's metadata</returns>
        [HttpPost("Upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // Validate that the file is not null or empty
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File not selected.");
                return BadRequest("No file selected.");
            }

            // Validate file size (e.g., max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File size exceeds the limit.");
                return BadRequest("File size exceeds the maximum allowed limit of 10MB.");
            }

            // Validate file type (e.g., images only)
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                _logger.LogWarning($"Invalid file type: {file.ContentType}");
                return BadRequest("Invalid file type. Only JPEG, PNG, and GIF images are allowed.");
            }

            // Process the file asynchronously
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                var fileDetail = new FileDetail
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Data = memoryStream.ToArray(),
                    FileSize = file.Length,
                    UploadedDate = DateTime.Now
                };

                try
                {
                    _context.FileDetails.Add(fileDetail);
                    await _context.SaveChangesAsync();

                    // Log successful upload
                    _logger.LogInformation($"File uploaded successfully: {fileDetail.FileName}, Size: {fileDetail.FileSize} bytes.");

                    return Ok(new { fileDetail.Id, fileDetail.FileName, fileDetail.FileSize });
                }
                catch (Exception ex)
                {
                    // Log exception
                    _logger.LogError(ex, "Error saving file details to database.");
                    return StatusCode(500, "An error occurred while saving the file data.");
                }
            }
        }

        /// <summary>
        /// Retrieve a file from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the file</param>
        /// <returns>File content or NotFound</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            // Retrieve file metadata and data from the database
            var fileDetail = await _context.FileDetails.FindAsync(id);

            if (fileDetail == null)
            {
                _logger.LogWarning($"File with ID {id} not found.");
                return NotFound("File not found.");
            }

            // Log file retrieval
            _logger.LogInformation($"File retrieved successfully: {fileDetail.FileName}, ID: {id}");

            // Return the file with the appropriate content type
            return File(fileDetail.Data, fileDetail.ContentType, fileDetail.FileName);
        }
    }
}
