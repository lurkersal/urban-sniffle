# Magazine Solution

A unified solution for managing magazine content, including viewing, parsing, file operations, and image processing.

## Projects

### magazine-viewer
ASP.NET Core MVC web application for viewing magazine content, issues, and models.
- Location: `magazine-viewer/src/`
- Port: 5031
- Database: PostgreSQL (localhost:5432/magazines)

### magazine-parser
Console application for parsing and importing magazine metadata into the database.
- Location: `magazine-parser/src/`
- Database Schema: `magazine-parser/schema_postgres.sql`

### file-renamer
Utility for batch renaming files with pattern matching.
- Location: `file-renamer/src/`

## Running magazine-viewer

```bash
cd magazine-viewer
./restart.sh
```

Or manually:
```bash
cd magazine-viewer/src
dotnet run
```

## Database Setup

```bash
PGPASSWORD=Barnowl1 psql -h localhost -U postgres -d magazines -f magazine-parser/schema_postgres.sql
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
