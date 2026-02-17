namespace FindLinks.Services;

using System.Diagnostics;

/// <summary>
/// OCR service implementation using Tesseract
/// </summary>
public class TesseractOcrService : IOcrService
{
    public string ExtractText(string imagePath)
    {
        try
        {
            // Validate image file exists
            if (!File.Exists(imagePath))
            {
                Console.Error.WriteLine($"ERROR: OCR image file not found: {imagePath}");
                return string.Empty;
            }

            var tempTextFile = Path.GetTempFileName();
            var outputBasePath = tempTextFile.Replace(".tmp", "");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{imagePath}\" \"{outputBasePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            string stdOut = string.Empty;
            string stdErr = string.Empty;
            
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.Error.WriteLine("ERROR: Failed to start tesseract process");
                    return string.Empty;
                }
                
                stdOut = process.StandardOutput.ReadToEnd();
                stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"ERROR: Tesseract exited with code {process.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(stdErr))
                        Console.Error.WriteLine($"Tesseract stderr: {stdErr}");
                    return string.Empty;
                }
            }
            
            var outputFile = outputBasePath + ".txt";
            if (!File.Exists(outputFile))
            {
                Console.Error.WriteLine($"ERROR: Tesseract did not create output file: {outputFile}");
                if (!string.IsNullOrWhiteSpace(stdErr))
                    Console.Error.WriteLine($"Tesseract stderr: {stdErr}");
                return string.Empty;
            }
            
            var text = File.ReadAllText(outputFile);
            
            // Cleanup temp files
            try { File.Delete(outputFile); } catch { }
            try { File.Delete(tempTextFile); } catch { }
            
            return text;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.Message.Contains("No such file"))
        {
            Console.Error.WriteLine("ERROR: tesseract command not found. Please install Tesseract OCR:");
            Console.Error.WriteLine("  Ubuntu/Debian: sudo apt-get install tesseract-ocr");
            Console.Error.WriteLine("  Fedora: sudo dnf install tesseract");
            Console.Error.WriteLine("  macOS: brew install tesseract");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: OCR extraction failed for {imagePath}: {ex.Message}");
            Console.Error.WriteLine($"Exception: {ex.GetType().Name}");
            return string.Empty;
        }
    }
}
