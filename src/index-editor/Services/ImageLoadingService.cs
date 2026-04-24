using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using IndexEditor.Shared;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for loading and managing image files.
    /// Extracted from PageControllerView to improve separation of concerns.
    /// </summary>
    public interface IImageLoadingService
    {
        /// <summary>
        /// Loads an image bitmap for the specified page number from the given folder.
        /// </summary>
        /// <returns>Bitmap if found, null otherwise</returns>
        Bitmap? LoadPageImage(string folder, int pageNumber);

        /// <summary>
        /// Finds the full path to an image file for the given page number.
        /// </summary>
        string? FindImagePath(string folder, int pageNumber);
    }

    public class ImageLoadingService : IImageLoadingService
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };

        public Bitmap? LoadPageImage(string folder, int pageNumber)
        {
            try
            {
                var imagePath = FindImagePath(folder, pageNumber);
                if (imagePath == null)
                {
                    DebugLogger.Log($"ImageLoadingService: No image file found for page {pageNumber}");
                    return null;
                }

                DebugLogger.Log($"ImageLoadingService: Loading image from: {imagePath}");
                
                // Load the bitmap with high quality interpolation
                var bitmap = new Bitmap(imagePath);
                return bitmap;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ImageLoadingService.LoadPageImage: page {pageNumber}", ex);
                return null;
            }
        }

        public string? FindImagePath(string folder, int pageNumber)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return null;
            }

            try
            {
                // Look for files with the page number as the base name
                var pageStr = pageNumber.ToString();
                
                foreach (var ext in ImageExtensions)
                {
                    // Try exact match first
                    var exactPath = Path.Combine(folder, pageStr + ext);
                    if (File.Exists(exactPath))
                    {
                        return exactPath;
                    }

                    // Try with leading zeros (e.g., "001.jpg", "0001.jpg")
                    for (int zeros = 1; zeros <= 4; zeros++)
                    {
                        var paddedPath = Path.Combine(folder, pageStr.PadLeft(zeros + pageStr.Length, '0') + ext);
                        if (File.Exists(paddedPath))
                        {
                            return paddedPath;
                        }
                    }
                }

                // Fallback: search all image files in the folder
                var files = Directory.GetFiles(folder)
                    .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                foreach (var file in files)
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    if (int.TryParse(nameWithoutExt, out var num) && num == pageNumber)
                    {
                        return file;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException($"ImageLoadingService.FindImagePath: page {pageNumber}", ex);
            }

            return null;
        }
    }
}

