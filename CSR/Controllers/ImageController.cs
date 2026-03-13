using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration; // Add this line

namespace CSR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("imageUploadPolicy")]
    public class ImageController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration; // Add this line

        public ImageController(IWebHostEnvironment hostingEnvironment, IConfiguration configuration) // Modify constructor
        {
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration; // Assign configuration
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm(Name = "dx-htmleditor-image")] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded." });
            }

            // 2. Add File Size Limitation
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = $"File size cannot exceed {maxFileSize / 1024 / 1024} MB." });
            }

            // Allowed image MIME types for security
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(new { error = "Invalid file type." });
            }

            try
            {
                var imageUploadPath = _configuration["ImageUploadPath"]; // Get path from configuration
                if (string.IsNullOrEmpty(imageUploadPath))
                {
                    // Fallback or throw an error if configuration is missing
                    imageUploadPath = "EditorImage"; 
                }

                var uploadsFolder = Path.Combine(imageUploadPath == "ITSM" ? "D:\\" : _hostingEnvironment.ContentRootPath, imageUploadPath);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Create a unique file name to prevent overwrites
                var uniqueFileName = $"{Guid.NewGuid().ToString()}_{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // The editor expects a JSON response with the URL of the uploaded image.
                var imageUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{imageUploadPath}/{uniqueFileName}";
                
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                // In a real application, you should log this exception.
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }
    }
}