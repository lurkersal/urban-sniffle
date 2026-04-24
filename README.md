# Magazine Solution

A unified solution for managing magazine content, including viewing, parsing, file operations, and image processing.

## Projects

### magazine-viewer
ASP.NET Core MVC web application for viewing magazine content, issues, and models.
- Location: `src/magazine-viewer/`
- Port: 5031 (http), 7268 (https)
- Database: PostgreSQL (configured via environment variable or user secrets)

### magazine-parser
Console application for parsing and importing magazine metadata into the database.
- Location: `src/magazine-parser/`
- Database Schema: `scripts/schema_postgres.sql`

### index-editor
Avalonia-based desktop application for editing magazine indexes.
- Location: `src/index-editor/`

### file-renamer
Utility for batch renaming files with pattern matching.
- Location: `src/file-renamer/`


### image-splitter
Utility for splitting magazine page images.
- Location: `src/image-splitter/`

## First-Time Setup

### 1. Database Configuration

**IMPORTANT:** This project does NOT store database passwords in source control.

Choose one of these options to configure your database connection:

#### Option A: Quick Setup Script (Recommended)
```bash
./scripts/setup-magazine-viewer.sh
```

#### Option B: Manual Setup with User Secrets
```bash
cd src/magazine-viewer
dotnet user-secrets set "ConnectionStrings:MagazineDb" "Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines"
```

#### Option C: Environment Variable
```bash
export MAGAZINE_DB="Host=localhost;Username=postgres;Password=YOUR_PASSWORD;Database=magazines"
```

**For detailed configuration options, see:** [DATABASE_CONFIG_SETUP.md](DATABASE_CONFIG_SETUP.md)

### 2. Database Schema Setup

```bash
./scripts/restoredb.sh
```

Or manually:
```bash
PGPASSWORD=YOUR_PASSWORD psql -h localhost -U postgres -d magazines -f scripts/schema_postgres.sql
```

## Notes

### Data Corrections Completed
- **Mayfair v21, n12**: Beverly's feature starts at page 179 (corrected - now uses 178.jpg for odd page display)

## Project Structure

```
magazine/
├── Magazine.sln
├── README.md
├── magazine-viewer/
│   ├── src/
│   │   ├── MagazineViewer.csproj
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Views/
│   │   └── wwwroot/
│   └── restart.sh
├── magazine-parser/
│   ├── src/
│   │   ├── magazine-parser.csproj
│   │   └── Program.cs
│   └── schema_postgres.sql
├── file-renamer/
│   └── src/
│       ├── file-renamer.csproj
│       └── Program.cs
```
