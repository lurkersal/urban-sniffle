using Microsoft.AspNetCore.Mvc;

namespace TheArchive.Controllers;

public class ImageController : Controller
{
    [HttpGet("/image")]
    public IActionResult GetImage(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return NotFound();
        }

        // If path is already absolute, use it directly
        // Otherwise, get the root directory from the environment variable
        string fullPath;
        if (Path.IsPathRooted(path))
        {
            fullPath = path;
        }
        else
        {
            var root = Environment.GetEnvironmentVariable("MAGAZINE_IMAGE_ROOT");
            if (string.IsNullOrEmpty(root))
            {
                return StatusCode(500, "Image root environment variable is not set.");
            }
            fullPath = Path.Combine(root, path);
        }

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

