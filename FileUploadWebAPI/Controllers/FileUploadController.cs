using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FileUploadWebAPI.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;

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
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File not selected.");
                return BadRequest("No file selected.");
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File size exceeds the limit.");
                return BadRequest("File size exceeds the maximum allowed limit of 10MB.");
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                _logger.LogWarning($"Invalid file type: {file.ContentType}");
                return BadRequest("Invalid file type. Only JPEG, PNG, and GIF images are allowed.");
            }

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

                    _logger.LogInformation($"File uploaded successfully: {fileDetail.FileName}, Size: {fileDetail.FileSize} bytes.");

                    return Ok(new { fileDetail.Id, fileDetail.FileName, fileDetail.FileSize });
                }
                catch (Exception ex)
                {
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
            var fileDetail = await _context.FileDetails.FindAsync(id);

            if (fileDetail == null)
            {
                _logger.LogWarning($"File with ID {id} not found.");
                return NotFound("File not found.");
            }

            _logger.LogInformation($"File retrieved successfully: {fileDetail.FileName}, ID: {id}");

            return File(fileDetail.Data, fileDetail.ContentType, fileDetail.FileName);
        }

        /// <summary>
        /// Retrieve all files from the database.
        /// </summary>
        /// <returns>List of file metadata</returns>
        [HttpGet("All")]
        public IActionResult GetAllFiles()
        {
            var files = _context.FileDetails.ToList();

            if (!files.Any())
            {
                return NotFound("No files found.");
            }

            return Ok(files.Select(f => new { f.Id, f.FileName, f.FileSize, f.UploadedDate }));
        }

        /// <summary>
        /// Update file metadata in the database.
        /// </summary>
        /// <param name="id">The ID of the file to update</param>
        /// <param name="file">The new file to upload</param>
        /// <returns>Updated file metadata</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFile(int id, IFormFile file)
        {
            var fileDetail = await _context.FileDetails.FindAsync(id);

            if (fileDetail == null)
            {
                _logger.LogWarning($"File with ID {id} not found.");
                return NotFound("File not found.");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected.");
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File size exceeds the limit.");
                return BadRequest("File size exceeds the maximum allowed limit of 10MB.");
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };
            if (!allowedContentTypes.Contains(file.ContentType))
            {
                _logger.LogWarning($"Invalid file type: {file.ContentType}");
                return BadRequest("Invalid file type. Only JPEG, PNG, and GIF images are allowed.");
            }

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                fileDetail.FileName = file.FileName;
                fileDetail.ContentType = file.ContentType;
                fileDetail.Data = memoryStream.ToArray();
                fileDetail.FileSize = file.Length;
                fileDetail.UploadedDate = DateTime.Now;

                try
                {
                    _context.FileDetails.Update(fileDetail);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"File updated successfully: {fileDetail.FileName}, Size: {fileDetail.FileSize} bytes.");

                    return Ok(new { fileDetail.Id, fileDetail.FileName, fileDetail.FileSize });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating file details in database.");
                    return StatusCode(500, "An error occurred while updating the file data.");
                }
            }
        }

        /// <summary>
        /// Delete a file from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the file to delete</param>
        /// <returns>Action result</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var fileDetail = await _context.FileDetails.FindAsync(id);

            if (fileDetail == null)
            {
                _logger.LogWarning($"File with ID {id} not found.");
                return NotFound("File not found.");
            }

            try
            {
                _context.FileDetails.Remove(fileDetail);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"File deleted successfully: {fileDetail.FileName}, ID: {id}");

                return NoContent(); // No content to return after successful deletion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from database.");
                return StatusCode(500, "An error occurred while deleting the file.");
            }
        }
    }
}
