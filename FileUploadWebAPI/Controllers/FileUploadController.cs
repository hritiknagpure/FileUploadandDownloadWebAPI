using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FileUploadWebAPI.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;
using System.Collections.Generic;
using FileUploadWebAPI.DTO.FileUploadWebAPI.Models;

namespace FileUploadWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(AppDbContext context, ILogger<FileUploadController> logger)
        {
            _context = context;
            _logger = logger;
        }

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

            var allowedContentTypes = new[] {
    "image/jpeg",
    "image/png",
    "image/gif",
    "image/jpg",
    "application/pdf",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
};

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

                    // Return only the FileName and ContentType to the client
                    var fileMetadata = new FileMetadataDTO
                    {
                        FileName = fileDetail.FileName,
                        ContentType = fileDetail.ContentType
                    };

                    return Ok(fileMetadata);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving file details to database.");
                    return StatusCode(500, "An error occurred while saving the file data.");
                }
            }
        }

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

            var fileMetadata = new FileMetadataDTO
            {
                FileName = fileDetail.FileName,
                ContentType = fileDetail.ContentType
            };

            return Ok(fileMetadata);
        }

        [HttpGet("All")]
        public IActionResult GetAllFiles()
        {
            var files = _context.FileDetails.ToList();

            if (!files.Any())
            {
                return NotFound("No files found.");
            }

            var fileMetadataList = files.Select(f => new FileMetadataDTO
            {
                FileName = f.FileName,
                ContentType = f.ContentType
            }).ToList();

            return Ok(fileMetadataList);
        }

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

                    var fileMetadata = new FileMetadataDTO
                    {
                        FileName = fileDetail.FileName,
                        ContentType = fileDetail.ContentType
                    };

                    return Ok(fileMetadata);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating file details in database.");
                    return StatusCode(500, "An error occurred while updating the file data.");
                }
            }
        }

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

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from database.");
                return StatusCode(500, "An error occurred while deleting the file.");
            }
        }
    }
}
