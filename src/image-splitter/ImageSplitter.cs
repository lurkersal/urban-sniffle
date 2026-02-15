using System;
using System.IO;

namespace image_splitter
{
    public class ImageSplitter
    {
        public void SplitImageByPath(string filePath, bool force)
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: The specified file does not exist: {filePath}");
                return;
            }

            var workingFolder = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
            var fileName = Path.GetFileName(filePath);

            Console.WriteLine($"Working Folder: {workingFolder}");
            Console.WriteLine($"File Name: {fileName}");
            Console.WriteLine($"Force: {force}");
            Console.WriteLine($"Full Path: {filePath}");
            
            // TODO: Implement image splitting logic
            Console.WriteLine("Image splitting functionality not yet implemented.");
        }

        public void SplitImage(string workingFolder, string fileName, bool force)
        {
            // Validate working folder
            if (!Directory.Exists(workingFolder))
            {
                Console.WriteLine($"Error: The specified working folder does not exist: {workingFolder}");
                return;
            }

            // Build full file path
            var filePath = Path.Combine(workingFolder, fileName);
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: The specified file does not exist: {filePath}");
                return;
            }

            Console.WriteLine($"Working Folder: {workingFolder}");
            Console.WriteLine($"File Name: {fileName}");
            Console.WriteLine($"Force: {force}");
            Console.WriteLine($"Full Path: {filePath}");
            
            // TODO: Implement image splitting logic
            Console.WriteLine("Image splitting functionality not yet implemented.");
        }
    }
}



