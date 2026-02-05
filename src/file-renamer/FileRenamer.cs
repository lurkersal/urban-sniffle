using System;
using System.IO;

namespace FileRenamer
{
    public class FileRenamer
    {
        public void RenameFiles(string directoryPath, string stringToRemove)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(directoryPath);
            Console.WriteLine($"DEBUG: Found {files.Length} files in {directoryPath}");
            Console.WriteLine($"DEBUG: Searching for string to remove: '{stringToRemove}'");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var newFileName = fileName.Replace(stringToRemove, string.Empty);
                var newFilePath = Path.Combine(directoryPath, newFileName);

                if (fileName != newFileName)
                {
                    File.Move(file, newFilePath);
                    Console.WriteLine($"✓ RENAMED: '{fileName}' → '{newFileName}'");
                }
                else
                {
                    Console.WriteLine($"✗ SKIPPED: '{fileName}' (no match found)");
                }
            }
            
            Console.WriteLine("DEBUG: Rename operation completed.");
        }
    }
}
