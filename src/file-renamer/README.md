# File Renamer

This is a .NET console application that renames files in a specified directory by removing a specified string from their filenames.

## Features

- Remove a specified string from filenames in a given directory.
- Simple command-line interface for easy usage.

## Prerequisites

- .NET SDK installed on your Ubuntu system. You can install it by following the instructions on the official [.NET website](https://dotnet.microsoft.com/download).

## Building the Application

1. Open a terminal and navigate to the project directory:

   ```bash
   cd file-renamer
   ```

2. Restore the project dependencies:

   ```bash
   dotnet restore
   ```

3. Build the application:

   ```bash
   dotnet build
   ```

## Running the Application

To run the application, use the following command:

```bash
dotnet run -- <directory-path> <string-to-remove>
```

### Example

To rename files in the `/path/to/directory` by removing the string `old_`, you would run:

```bash
dotnet run -- /path/to/directory old_
```

## Notes

- Ensure that you have the necessary permissions to rename files in the specified directory.
- Use caution when renaming files, as this action cannot be undone.