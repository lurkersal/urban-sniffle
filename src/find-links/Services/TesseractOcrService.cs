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
            var tempTextFile = Path.GetTempFileName();
            var startInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{imagePath}\" \"{tempTextFile.Replace(".tmp", "")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    return string.Empty;
                process.WaitForExit();
            }
            var outputFile = tempTextFile.Replace(".tmp", "") + ".txt";
            if (!File.Exists(outputFile))
                return string.Empty;
            var text = File.ReadAllText(outputFile);
            File.Delete(outputFile);
            File.Delete(tempTextFile);
            return text;
        }
        catch
        {
            return string.Empty;
        }
    }
}
