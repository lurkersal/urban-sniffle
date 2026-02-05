# MagazineIndexer
MagazineIndexer is an Avalonia-based .NET 8.0 desktop application for magazine content indexing and review. It features:

- Article list and editor
- Page segment management
- Conflict review and auto-adjustment tools

## How to Build & Run

1. Navigate to the project directory:
   ```sh
   cd magazine/src/MagazineIndexer
   ```
2. Restore dependencies:
   ```sh
   dotnet restore
   ```
3. Build the project:
   ```sh
   dotnet build
   ```
4. Run the application:
   ```sh
   dotnet run
   ```

## Project Structure
- `Program.cs`: Application entry point
- `App.axaml` / `App.axaml.cs`: Application setup
- `MainWindow.axaml` / `MainWindow.axaml.cs`: Main UI window
- `Views/`: UI components for editing, reviewing, and segment management

## Requirements
- .NET 8.0 SDK
- Avalonia UI dependencies (restored via NuGet)

---

For more details, see the code and comments in each file.
