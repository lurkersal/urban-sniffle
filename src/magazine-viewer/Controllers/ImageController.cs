using Microsoft.AspNetCore.Mvc;

namespace MagazineViewer.Controllers
{
    public class ImageController : Controller
    {
        [HttpGet("/image")]
        public IActionResult GetImage(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }

            // Get the root directory from the environment variable
            var root = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
            if (string.IsNullOrEmpty(root))
            {
                return StatusCode(500, "Image root environment variable is not set.");
            }

            // Combine the root with the relative path from the database
            var fullPath = Path.Combine(root, path);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            var fileStream = System.IO.File.OpenRead(fullPath);
            return File(fileStream, contentType);
        }
    }
}
