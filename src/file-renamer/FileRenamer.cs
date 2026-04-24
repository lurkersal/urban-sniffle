using System;
using System.Collections.Generic;
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

            // Build rename plan
            var renamePlan = new List<(string SourcePath, string SourceName, string TargetName, string TargetPath)>();
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var newFileName = fileName.Replace(stringToRemove, string.Empty);

                if (fileName != newFileName)
                {
                    var newFilePath = Path.Combine(directoryPath, newFileName);
                    renamePlan.Add((file, fileName, newFileName, newFilePath));
                }
            }

            // Check for conflicts
            var targetNames = new HashSet<string>(renamePlan.Select(p => p.TargetName));
            var sourceNames = new HashSet<string>(renamePlan.Select(p => p.SourceName));
            
            // Check for duplicate targets (multiple files renaming to same name)
            var targetNameCounts = renamePlan.GroupBy(p => p.TargetName).Where(g => g.Count() > 1).ToList();
            if (targetNameCounts.Any())
            {
                Console.WriteLine("ERROR: Cannot rename - multiple files would be renamed to the same name:");
                foreach (var group in targetNameCounts)
                {
                    Console.WriteLine($"  Target: '{group.Key}' ← from:");
                    foreach (var plan in group)
                    {
                        Console.WriteLine($"    - '{plan.SourceName}'");
                    }
                }
                Console.WriteLine($"DEBUG: Rename operation aborted due to duplicate target conflicts.");
                return;
            }
            
            // Type 1: Target name exists as a source file that will also be renamed
            bool hasRenameCollision = targetNames.Intersect(sourceNames).Any();
            
            // Type 2: Target name exists as a file that won't be renamed
            var allFileNames = new HashSet<string>(files.Select(f => Path.GetFileName(f)));
            var staticFileNames = allFileNames.Except(sourceNames);
            var conflictingTargets = targetNames.Intersect(staticFileNames).ToList();
            
            if (conflictingTargets.Any())
            {
                Console.WriteLine("ERROR: Cannot rename - the following target file(s) already exist:");
                foreach (var conflict in conflictingTargets)
                {
                    var source = renamePlan.First(p => p.TargetName == conflict).SourceName;
                    Console.WriteLine($"  '{source}' → '{conflict}' (CONFLICT: '{conflict}' already exists and won't be moved)");
                }
                Console.WriteLine($"DEBUG: Rename operation aborted due to {conflictingTargets.Count} conflict(s).");
                return;
            }

            if (hasRenameCollision && !dryRun)
            {
                Console.WriteLine("DEBUG: Collision detected, using two-phase rename (temp names first)");
                
                // Phase 1: Rename all to temporary names
                var tempMappings = new List<(string TempPath, string FinalPath, string OriginalName, string FinalName)>();
                
                foreach (var (sourcePath, sourceName, targetName, targetPath) in renamePlan)
                {
                    var tempFileName = $"__temp_rename_{Guid.NewGuid()}{Path.GetExtension(sourceName)}";
                    var tempFilePath = Path.Combine(directoryPath, tempFileName);
                    
                    File.Move(sourcePath, tempFilePath);
                    tempMappings.Add((tempFilePath, targetPath, sourceName, targetName));
                }
                
                // Phase 2: Rename from temporary to final names
                foreach (var (tempPath, finalPath, originalName, finalName) in tempMappings)
                {
                    File.Move(tempPath, finalPath);
                    Console.WriteLine($"✓ RENAMED: '{originalName}' → '{finalName}'");
                }
            }
            else
            {
                // No collision or dry run
                foreach (var (sourcePath, sourceName, targetName, targetPath) in renamePlan)
                {
                    if (!dryRun)
                    {
                        File.Move(sourcePath, targetPath);
                    }
                    var prefix = dryRun ? "WOULD RENAME" : "✓ RENAMED";
                    Console.WriteLine($"{prefix}: '{sourceName}' → '{targetName}'");
                }
            }
            
            // Report skipped files
            var skippedCount = files.Length - renamePlan.Count;
            if (skippedCount > 0)
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (!renamePlan.Any(p => p.SourceName == fileName))
                    {
                        var prefix = dryRun ? "WOULD SKIP" : "✗ SKIPPED";
                        Console.WriteLine($"{prefix}: '{fileName}' (no match found)");
                    }
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

            var allFiles = Directory.GetFiles(directoryPath);
            
            // Filter and parse numeric files
            var numericFiles = new List<(string Path, string FileName, int Number, string Extension)>();
            var nonNumericFiles = new List<string>();
            
            foreach (var file in allFiles)
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(fileName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                if (int.TryParse(nameWithoutExtension, out int number))
                {
                    numericFiles.Add((file, fileName, number, extension));
                }
                else
                {
                    nonNumericFiles.Add(fileName);
                }
            }

            Console.WriteLine($"DEBUG: Found {allFiles.Length} files in {directoryPath}");
            Console.WriteLine($"DEBUG: {numericFiles.Count} numeric files, {nonNumericFiles.Count} non-numeric files");
            Console.WriteLine($"DEBUG: Starting renumbering operation with increment: {incrementValue}");
            if (dryRun)
            {
                Console.WriteLine("DEBUG: DRY RUN MODE - No files will be renamed");
            }

            // Sort based on increment direction to avoid collisions
            // If incrementing (positive), process from highest to lowest
            // If decrementing (negative), process from lowest to highest
            var orderedFiles = incrementValue > 0 
                ? numericFiles.OrderByDescending(f => f.Number).ToList()
                : numericFiles.OrderBy(f => f.Number).ToList();

            // Check for conflicts with existing non-numeric files
            var allExistingNames = new HashSet<string>(allFiles.Select(f => Path.GetFileName(f)));
            var conflicts = new List<(string OriginalName, string TargetName)>();
            
            // Also check for duplicate targets (multiple files renaming to same number)
            var targetNameCounts = new Dictionary<string, List<string>>();
            
            foreach (var (path, fileName, number, extension) in numericFiles)
            {
                var newNumber = number + incrementValue;
                var newFileName = $"{newNumber}{extension}";
                
                // Track duplicate targets
                if (!targetNameCounts.ContainsKey(newFileName))
                {
                    targetNameCounts[newFileName] = new List<string>();
                }
                targetNameCounts[newFileName].Add(fileName);
                
                // Check if target exists as a non-numeric file (won't be moved)
                if (allExistingNames.Contains(newFileName) && !numericFiles.Any(f => f.FileName == newFileName))
                {
                    conflicts.Add((fileName, newFileName));
                }
            }
            
            // Check for duplicate targets
            var duplicateTargets = targetNameCounts.Where(kvp => kvp.Value.Count > 1).ToList();
            if (duplicateTargets.Any())
            {
                Console.WriteLine("ERROR: Cannot renumber - multiple files would be renamed to the same name:");
                foreach (var kvp in duplicateTargets)
                {
                    Console.WriteLine($"  Target: '{kvp.Key}' ← from:");
                    foreach (var source in kvp.Value)
                    {
                        Console.WriteLine($"    - '{source}'");
                    }
                }
                Console.WriteLine($"DEBUG: Renumber operation aborted due to duplicate target conflicts.");
                return;
            }
            
            if (conflicts.Any())
            {
                Console.WriteLine("ERROR: Cannot renumber - the following target file(s) already exist:");
                foreach (var (original, target) in conflicts)
                {
                    Console.WriteLine($"  '{original}' → '{target}' (CONFLICT: '{target}' already exists and won't be moved)");
                }
                Console.WriteLine($"DEBUG: Renumber operation aborted due to {conflicts.Count} conflict(s).");
                return;
            }

            // Check for potential collisions with other numeric files and use two-phase rename if needed
            var targetNumbers = new HashSet<int>(numericFiles.Select(f => f.Number + incrementValue));
            var sourceNumbers = new HashSet<int>(numericFiles.Select(f => f.Number));
            bool hasCollision = targetNumbers.Intersect(sourceNumbers).Any();

            if (hasCollision && !dryRun)
            {
                Console.WriteLine("DEBUG: Collision detected, using two-phase rename (temp names first)");
                
                // Phase 1: Rename all to temporary names
                var tempMappings = new List<(string TempPath, string FinalPath, string OriginalName, string FinalName, int OldNumber, int NewNumber)>();
                
                foreach (var (filePath, fileName, number, extension) in orderedFiles)
                {
                    var tempFileName = $"__temp_rename_{Guid.NewGuid()}_{number}{extension}";
                    var tempFilePath = Path.Combine(directoryPath, tempFileName);
                    
                    var newNumber = number + incrementValue;
                    var finalFileName = $"{newNumber}{extension}";
                    var finalFilePath = Path.Combine(directoryPath, finalFileName);
                    
                    File.Move(filePath, tempFilePath);
                    tempMappings.Add((tempFilePath, finalFilePath, fileName, finalFileName, number, newNumber));
                }
                
                // Phase 2: Rename from temporary to final names
                foreach (var (tempPath, finalPath, originalName, finalName, oldNumber, newNumber) in tempMappings)
                {
                    File.Move(tempPath, finalPath);
                    Console.WriteLine($"✓ RENAMED: '{originalName}' → '{finalName}' ({oldNumber} + {incrementValue} = {newNumber})");
                }
            }
            else
            {
                // No collision, safe to rename directly
                foreach (var (filePath, fileName, number, extension) in orderedFiles)
                {
                    var newNumber = number + incrementValue;
                    var newFileName = $"{newNumber}{extension}";
                    var newFilePath = Path.Combine(directoryPath, newFileName);

                    if (!dryRun)
                    {
                        File.Move(filePath, newFilePath);
                    }
                    var prefix = dryRun ? "WOULD RENAME" : "✓ RENAMED";
                    Console.WriteLine($"{prefix}: '{fileName}' → '{newFileName}' ({number} + {incrementValue} = {newNumber})");
                }
            }
            
            // Report skipped files
            foreach (var fileName in nonNumericFiles)
            {
                var prefix = dryRun ? "WOULD SKIP" : "✗ SKIPPED";
                Console.WriteLine($"{prefix}: '{fileName}' (filename is not a number)");
            }
            
            Console.WriteLine($"DEBUG: Renumber operation {(dryRun ? "preview" : "completed")}.");
        }
    }
}
