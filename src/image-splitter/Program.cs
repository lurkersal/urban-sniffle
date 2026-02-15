﻿using System;
using System.IO;

// Usage: image-splitter (<file-path> | [<working-folder>] -f <file-name>) [--force]
if (args.Length < 1)
{
    Console.WriteLine("Usage: image-splitter (<file-path> | [<working-folder>] -f <file-name>) [--force]");
    Console.WriteLine();
    Console.WriteLine("Provide either:");
    Console.WriteLine("  <file-path>       Full path to the image file");
    Console.WriteLine("OR");
    Console.WriteLine("  <working-folder>  Path to working directory (defaults to current directory)");
    Console.WriteLine("  -f <file-name>    Name of the image file to split");
    Console.WriteLine();
    Console.WriteLine("  --force           Optional. Force overwrite of existing files");
    return 1;
}

string? filePath = null;
string? workingFolder = null;
string? fileName = null;
bool force = false;

// First pass: check if -f flag is present to determine mode
bool useFileFlag = args.Any(arg => arg == "-f");

// Parse arguments based on mode
if (useFileFlag)
{
    // Mode 2: [<working-folder>] -f <file-name> [--force]
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "-f")
        {
            if (i + 1 < args.Length)
            {
                fileName = args[i + 1];
                i++; // Skip next arg since we consumed it
            }
        }
        else if (args[i] == "--force")
        {
            force = true;
        }
        else if (!args[i].StartsWith("-") && workingFolder == null)
        {
            workingFolder = args[i];
        }
        else if (!args[i].StartsWith("-"))
        {
            Console.WriteLine($"Error: Unexpected argument '{args[i]}'.");
            return 1;
        }
    }
}
else
{
    // Mode 1: <file-path> [--force]
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--force")
        {
            force = true;
        }
        else if (!args[i].StartsWith("-") && filePath == null)
        {
            filePath = args[i];
        }
        else if (!args[i].StartsWith("-"))
        {
            Console.WriteLine($"Error: Too many file paths specified. Only one file path is allowed.");
            return 1;
        }
        else
        {
            Console.WriteLine($"Error: Unknown option '{args[i]}'.");
            return 1;
        }
    }
}

// Validate required arguments
if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(fileName))
{
    Console.WriteLine("Error: Must provide either a file path or use -f with a file name.");
    Console.WriteLine("Usage: image-splitter (<file-path> | [<working-folder>] -f <file-name>) [--force]");
    return 1;
}

// Run the image splitter
var splitter = new image_splitter.ImageSplitter();

if (!string.IsNullOrEmpty(filePath))
{
    // Mode 1: Full file path provided
    splitter.SplitImageByPath(filePath, force);
}
else
{
    // Mode 2: Working folder + file name
    if (string.IsNullOrEmpty(workingFolder))
    {
        workingFolder = Directory.GetCurrentDirectory();
    }
    splitter.SplitImage(workingFolder, fileName!, force);
}

return 0;
