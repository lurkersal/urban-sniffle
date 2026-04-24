# The Archive - Quick Start Guide

## First Time Setup

### 1. Configure Database Connection

Edit the connection string in `src/TheArchive/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MagazineDb": "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

**Note**: Use the same credentials as the existing `magazine-viewer` project.

### 2. Build the Project

```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
dotnet build
```

### 3. Run the Application

```bash
dotnet run
```

The application will start and display URLs like:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

### 4. Test Database Connection

Open your browser and navigate to:
```
http://localhost:5000
```

You should see:
- ✅ "Database Connected!" message (green)
- List of magazines in the database
- Links to test API endpoints

If you see a red error message, check your database connection string.

---

## Testing API Endpoints

Once the application is running, test these endpoints:

### Magazines
```bash
# List all magazines
curl http://localhost:5000/api/v1/magazines | jq

# Get specific magazine
curl http://localhost:5000/api/v1/magazines/1 | jq

# Get magazine issues
curl http://localhost:5000/api/v1/magazines/1/issues | jq
```

### Issues
```bash
# List all issues
curl http://localhost:5000/api/v1/issues | jq

# Get specific issue
curl http://localhost:5000/api/v1/issues/1 | jq

# Get issue articles
curl http://localhost:5000/api/v1/issues/1/articles | jq
```

### Models
```bash
# List all models
curl http://localhost:5000/api/v1/models | jq

# Get specific model
curl http://localhost:5000/api/v1/models/1 | jq

# Get model articles
curl http://localhost:5000/api/v1/models/1/articles | jq
```

### Articles
```bash
# List all articles
curl http://localhost:5000/api/v1/articles | jq

# Filter by category
curl "http://localhost:5000/api/v1/articles?category=model" | jq
```

---

## Common Issues

### Issue: "Connection refused" or database error

**Solution**: 
1. Verify PostgreSQL is running: `sudo systemctl status postgresql`
2. Check connection string in `appsettings.json`
3. Test connection with psql: `psql -h localhost -U postgres -d magazines`

### Issue: Port 5000 already in use

**Solution**:
1. Kill existing process: `sudo lsof -i :5000` then `kill <PID>`
2. Or run on different port: `dotnet run --urls="http://localhost:5050"`

### Issue: Build errors

**Solution**:
1. Clean and rebuild: `dotnet clean && dotnet build`
2. Check .NET version: `dotnet --version` (should be 8.0+)
3. Restore packages: `dotnet restore`

---

## Development Workflow

### Hot Reload (File Watching)

For development with automatic reload:

```bash
dotnet watch run
```

This will restart the application when you change C# files.

### View Live Changes

After starting with `dotnet watch run`:
1. Edit a .cshtml file
2. Save the file
3. Refresh browser (no restart needed for views)

### Add New Pages

1. Create controller action in `Controllers/`
2. Create corresponding view in `Views/`
3. Update navigation in `Views/Shared/_Layout.cshtml`

---

## Project Structure Reminder

```
src/TheArchive/
├── Controllers/
│   ├── Api/              ← REST API endpoints (JSON)
│   └── *.cs              ← MVC controllers (HTML views)
├── Models/               ← Data models
├── Services/             ← Database access
├── Views/                ← Razor views (HTML)
│   ├── Shared/           ← Layout, partials
│   └── {Controller}/     ← Controller-specific views
└── wwwroot/              ← Static files (CSS, JS, images)
```

---

## Next Steps

After verifying the API works:

1. **Phase 2**: Implement magazine grid view
   - Update `Views/Home/Index.cshtml`
   - Create `wwwroot/css/archive.css`
   - Add magazine card styling

2. **Phase 3**: Create issue and article views
   - Add `Controllers/IssuesController.cs` (MVC)
   - Create `Views/Issues/` views
   - Implement article listing

3. **Phase 4**: Add models views
   - Add `Controllers/ModelsController.cs` (MVC)
   - Create `Views/Models/` views

See `IMPLEMENTATION_PLAN.md` for full roadmap.

---

## Useful Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Run with hot reload
dotnet watch run

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Check for updates
dotnet list package --outdated

# Publish for deployment
dotnet publish -c Release
```

---

## Getting Help

- **API Documentation**: Visit `/swagger` (if configured)
- **Database Schema**: See `/home/justin/repos/urban-sniffle/scripts/schema_postgres.sql`
- **Implementation Plan**: See `IMPLEMENTATION_PLAN.md`
- **README**: See `README.md`

---

## Success Checklist

- [ ] Application builds without errors
- [ ] Application starts and listens on port 5000
- [ ] Home page shows "Database Connected!" message
- [ ] Home page lists magazines with issue counts
- [ ] API endpoint `/api/v1/magazines` returns JSON
- [ ] API endpoint `/api/v1/issues` returns JSON
- [ ] API endpoint `/api/v1/models` returns JSON
- [ ] API endpoint `/api/v1/articles` returns JSON

Once all items are checked, you're ready to continue with Phase 2!

