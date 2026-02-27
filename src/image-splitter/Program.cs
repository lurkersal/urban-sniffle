using System.Diagnostics;

namespace ImageSplitter;

class Program
{
    static void Main(string[] args)
    {
        bool force = false;
        string? directory = null;
        string? singleFile = null;

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--force")
            {
                force = true;
            }
            else if (args[i] == "-f" && i + 1 < args.Length)
            {
                singleFile = args[i + 1];
                i++;
            }
            else if (args[i] == "-d" || args[i] == "--folder")
            {
                if (i + 1 < args.Length)
                {
                    directory = args[i + 1];
                    i++;
                }
            }
            else if (!args[i].StartsWith("-"))
            {
                // If no option specified and it's not a flag, treat as directory (for backward compatibility)
                if (directory == null)
                {
                    directory = args[i];
                }
            }
        }

        // Validate mutually exclusive options
        if (singleFile != null && directory != null)
        {
            Console.WriteLine("Error: -f and -d options are mutually exclusive.");
            PrintUsage();
            return;
        }

        if (singleFile != null)
        {
            if (!File.Exists(singleFile))
            {
                Console.WriteLine($"Error: File '{singleFile}' does not exist.");
                return;
            }
            if (!TrySplitSingleFile(singleFile, force))
            {
                Console.WriteLine("File was not split (see above for reason).");
            }
            return;
        }

        // Default to current directory if none specified
        if (directory == null)
        {
            directory = ".";
        }

        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Error: Directory '{directory}' does not exist.");
            return;
        }

        ProcessDirectory(directory, force);
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: image-splitter [options]");
        Console.WriteLine("Splits double-page spread images into separate left and right pages.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -d, --folder <directory>  Directory to process (default: current directory)");
        Console.WriteLine("  -f <file>                 Split a single image file");
        Console.WriteLine("  --force                   Skip creating backup files");
        Console.WriteLine();
        Console.WriteLine("Note: -f and -d are mutually exclusive.");
    }

    static bool TrySplitSingleFile(string imagePath, bool force)
    {
        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var extension = Path.GetExtension(imagePath);
        var dir = Path.GetDirectoryName(imagePath) ?? ".";

        // Try to parse the filename as a page number
        if (!int.TryParse(fileName, out int pageNum))
        {
            Console.WriteLine($"Error: Filename '{fileName}' is not a number.");
            return false;
        }

        // Check if this is an even-numbered page
        if (pageNum % 2 != 0)
        {
            Console.WriteLine($"Error: Filename '{fileName}' is not even. Only even-numbered files can be split.");
            return false;
        }

        // Check if the next odd page already exists
        int nextPage = pageNum + 1;
        var nextPagePath = Path.Combine(dir, $"{nextPage}{extension}");
        if (File.Exists(nextPagePath))
        {
            Console.WriteLine($"Error: Right-hand file '{nextPagePath}' already exists.");
            return false;
        }

        // Get image dimensions using identify
        try
        {
            var (width, height) = GetImageDimensions(imagePath);
            if (width <= height)
            {
                Console.WriteLine("Error: Image is not a double-page spread (width <= height).");
                return false;
            }

            int halfWidth = width / 2;

            // In force mode, we need a temp copy since we'll overwrite the original
            string sourceForRight;
            if (force)
            {
                sourceForRight = Path.Combine(Path.GetTempPath(), $"orig_{Guid.NewGuid()}{extension}");
                File.Copy(imagePath, sourceForRight, true);
            }
            else
            {
                // Create backup
                sourceForRight = imagePath + ".backup";
                File.Copy(imagePath, sourceForRight, true);
            }

            // Create left half (keep original filename)
            string tempLeft = Path.Combine(Path.GetTempPath(), $"left_{Guid.NewGuid()}{extension}");
            RunConvert(imagePath, "-crop", $"{halfWidth}x{height}+0+0", "+repage", tempLeft);
            File.Move(tempLeft, imagePath, true);

            // Create right half (save as next odd page)
            RunConvert(sourceForRight, "-crop", $"{halfWidth}x{height}+{halfWidth}+0", "+repage", nextPagePath);

            if (force)
            {
                File.Delete(sourceForRight);
            }

            Console.WriteLine($"Split complete: {imagePath} (left), {nextPagePath} (right)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {imagePath}: {ex.Message}");
            return false;
        }
    }

    static void ProcessDirectory(string directory, bool force)
    {
        Console.WriteLine($"Processing directory: {directory}\n");

        var imageFiles = Directory.GetFiles(directory, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        int processedCount = 0;
        int skippedCount = 0;

        foreach (var imagePath in imageFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var extension = Path.GetExtension(imagePath);
            
            // Try to parse the filename as a page number
            if (!int.TryParse(fileName, out int pageNum))
            {
                continue; // Skip non-numeric filenames
            }

            // Check if this is an even-numbered page
            if (pageNum % 2 != 0)
            {
                continue; // Skip odd-numbered pages
            }

            // Check if the next odd page already exists
            int nextPage = pageNum + 1;
            var nextPagePath = Path.Combine(directory, $"{nextPage}{extension}");
            
            if (File.Exists(nextPagePath))
            {
                skippedCount++;
                continue; // Skip if odd page already exists
            }

            // Get image dimensions using identify
            try
            {
                var (width, height) = GetImageDimensions(imagePath);
                
                if (width <= height)
                {
                    continue; // Not a double-page spread
                }

                Console.WriteLine($"Splitting page {pageNum} ({width}x{height})...");

                int halfWidth = width / 2;
                
                // In force mode, we need a temp copy since we'll overwrite the original
                string sourceForRight;
                if (force)
                {
                    sourceForRight = Path.Combine(Path.GetTempPath(), $"orig_{Guid.NewGuid()}{extension}");
                    File.Copy(imagePath, sourceForRight, true);
                }
                else
                {
                    // Create backup
                    sourceForRight = imagePath + ".backup";
                    File.Copy(imagePath, sourceForRight, true);
                }
                
                // Create left half (keep original filename)
                string tempLeft = Path.Combine(Path.GetTempPath(), $"left_{Guid.NewGuid()}{extension}");
                RunConvert(imagePath, "-crop", $"{halfWidth}x{height}+0+0", "+repage", tempLeft);
                File.Move(tempLeft, imagePath, true);

                // Create right half (save as next odd page)
                RunConvert(sourceForRight, "-crop", $"{halfWidth}x{height}+{halfWidth}+0", "+repage", nextPagePath);
                
                // Clean up temp file if force mode
                if (force)
                {
                    File.Delete(sourceForRight);
                }

                Console.WriteLine($"  Created: {pageNum}{extension} (left) and {nextPage}{extension} (right)");
                processedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {imagePath}: {ex.Message}");
            }
        }

        Console.WriteLine($"\nComplete! Processed: {processedCount}, Skipped: {skippedCount}");
    }

    static (int width, int height) GetImageDimensions(string imagePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "identify",
            ArgumentList = { "-format", "%w %h", imagePath },
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start identify process");

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var parts = output.Trim().Split(' ');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    static void RunConvert(string inputPath, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "convert",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        psi.ArgumentList.Add(inputPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start convert process");

        process.WaitForExit();
        
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception($"ImageMagick error: {error}");
        }
    }
}

