using System;
using System.IO;
using System.Linq;

namespace FileRenamer
{
    public class FileRenamer
    {
        public void RenameFiles(string directoryPath, string stringToRemove, bool dryRun = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(directoryPath);
            Console.WriteLine($"DEBUG: Found {files.Length} files in {directoryPath}");
            Console.WriteLine($"DEBUG: Searching for string to remove: '{stringToRemove}'");
            if (dryRun)
            {
                Console.WriteLine("DEBUG: DRY RUN MODE - No files will be renamed");
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var newFileName = fileName.Replace(stringToRemove, string.Empty);
                var newFilePath = Path.Combine(directoryPath, newFileName);

                if (fileName != newFileName)
                {
                    if (!dryRun)
                    {
                        File.Move(file, newFilePath);
                    }
                    var prefix = dryRun ? "WOULD RENAME" : "✓ RENAMED";
                    Console.WriteLine($"{prefix}: '{fileName}' → '{newFileName}'");
                }
                else
                {
                    var prefix = dryRun ? "WOULD SKIP" : "✗ SKIPPED";
                    Console.WriteLine($"{prefix}: '{fileName}' (no match found)");
                }
            }
            
            Console.WriteLine($"DEBUG: Rename operation {(dryRun ? "preview" : "completed")}.");
        }

        public void RenumberFiles(string directoryPath, int incrementValue, bool dryRun = false)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }

            var files = Directory.GetFiles(directoryPath).OrderBy(f => f).ToArray();
            Console.WriteLine($"DEBUG: Found {files.Length} files in {directoryPath}");
            Console.WriteLine($"DEBUG: Starting renumbering operation with increment: {incrementValue}");
            if (dryRun)
            {
                Console.WriteLine("DEBUG: DRY RUN MODE - No files will be renamed");
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(fileName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                // Check if the filename (without extension) is a number
                if (int.TryParse(nameWithoutExtension, out int currentNumber))
                {
                    var newNumber = currentNumber + incrementValue;
                    var newFileName = $"{newNumber}{extension}";
                    var newFilePath = Path.Combine(directoryPath, newFileName);

                    if (!dryRun)
                    {
                        File.Move(file, newFilePath);
                    }
                    var prefix = dryRun ? "WOULD RENAME" : "✓ RENAMED";
                    Console.WriteLine($"{prefix}: '{fileName}' → '{newFileName}' ({currentNumber} + {incrementValue} = {newNumber})");
                }
                else
                {
                    var prefix = dryRun ? "WOULD SKIP" : "✗ SKIPPED";
                    Console.WriteLine($"{prefix}: '{fileName}' (filename is not a number)");
                }
            }
            
            Console.WriteLine($"DEBUG: Renumber operation {(dryRun ? "preview" : "completed")}.");
        }
    }
}
