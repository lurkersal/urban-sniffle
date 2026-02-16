using System;
using System.IO;

namespace FileRenamer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Usage: file-renamer [(-d|--folder) <directory>] (-s|--string <string-to-remove> | -r|--renumber)
            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            string? directory = null;
            string? stringToRemove = null;
            int? renumberIncrement = null;
            bool dryRun = false;

            // Parse arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "-d" || arg == "--folder")
                {
                    if (i + 1 < args.Length)
                    {
                        directory = args[i + 1];
                        i++; // Skip next arg since we consumed it
                    }
                    else
                    {
                        Console.WriteLine("Error: -d/--folder requires a directory path");
                        PrintUsage();
                        return 1;
                    }
                }
                else if (arg == "-s" || arg == "--string")
                {
                    if (i + 1 < args.Length)
                    {
                        stringToRemove = args[i + 1];
                        i++; // Skip next arg since we consumed it
                    }
                    else
                    {
                        Console.WriteLine("Error: -s/--string requires a string value");
                        PrintUsage();
                        return 1;
                    }
                }
                else if (arg == "-r" || arg == "--renumber")
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int increment))
                    {
                        renumberIncrement = increment;
                        i++; // Skip next arg since we consumed it
                    }
                    else
                    {
                        Console.WriteLine("Error: -r/--renumber requires an integer value");
                        PrintUsage();
                        return 1;
                    }
                }
                else if (arg == "-n" || arg == "--no")
                {
                    dryRun = true;
                }
                else
                {
                    Console.WriteLine($"Error: Unknown argument '{arg}'");
                    PrintUsage();
                    return 1;
                }
            }

            // Default to current directory if not specified
            if (directory == null)
            {
                directory = Directory.GetCurrentDirectory();
            }

            // Validate arguments

            if (renumberIncrement.HasValue && stringToRemove != null)
            {
                Console.WriteLine("Error: -r/--renumber and -s/--string are mutually exclusive");
                PrintUsage();
                return 1;
            }

            if (!renumberIncrement.HasValue && stringToRemove == null)
            {
                Console.WriteLine("Error: Either -r/--renumber or -s/--string must be specified");
                PrintUsage();
                return 1;
            }

            // Execute operation
            var fileRenamer = new FileRenamer();
            
            if (renumberIncrement.HasValue)
            {
                fileRenamer.RenumberFiles(directory, renumberIncrement.Value, dryRun);
            }
            else
            {
                fileRenamer.RenameFiles(directory, stringToRemove!, dryRun);
            }

            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: file-renamer [(-d|--folder) <directory>] (-s|--string <string-to-remove> | -r|--renumber <increment>) [-n|--no]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -d, --folder <path>        Directory path (optional, defaults to current directory)");
            Console.WriteLine("  -s, --string <text>        String to remove from file names");
            Console.WriteLine("  -r, --renumber <increment> Increment numeric file names by the specified value");
            Console.WriteLine("  -n, --no                   Dry run - show what would be done without renaming files");
            Console.WriteLine();
            Console.WriteLine("Note: -s/--string and -r/--renumber are mutually exclusive");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  file-renamer -d /path/to/files -s \"unwanted_text\"");
            Console.WriteLine("  file-renamer --folder /path/to/files --renumber 10");
            Console.WriteLine("  file-renamer -s \"text_to_remove\"  # Uses current directory");
            Console.WriteLine("  file-renamer --renumber 5           # Increments numeric files by 5 in current directory");
            Console.WriteLine("  file-renamer -r 10 -n               # Preview renumbering without making changes");
        }
    }
}

