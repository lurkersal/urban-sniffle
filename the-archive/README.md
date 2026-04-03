# The Archive - Magazine Collection Viewer

ASP.NET Core 8.0 web application for viewing a curated collection of British men's magazines from the 1970s, 80s, and 90s.

## 🚀 Quick Start

### Start the Application
```bash
cd /home/justin/repos/urban-sniffle/the-archive
./start-archive.sh
```

### Stop the Application
```bash
./stop-archive.sh
```

### ⚠️ "Address Already In Use" Error?
If you see `Failed to bind to address http://127.0.0.1:5163: address already in use`:
```bash
./stop-archive.sh  # Stops any running instances
./start-archive.sh # Starts fresh
```

**See `QUICK_START_GUIDE.md` for detailed troubleshooting.**

---

## Overview

This application provides a read-only interface to browse magazines, issues, articles, and models. It uses the **existing database schema** from the `magazine-viewer` project and adapts it to the requirements specified in `the-archive-spec.docx`.

## Technology Stack

- **Backend**: ASP.NET Core 8.0 (Web API + MVC)
- **Database**: PostgreSQL (existing `magazines` database)
- **ORM**: Dapper (lightweight, fast)
- **Frontend**: Razor Pages + Bootstrap 5 (to be implemented)

## Database Mapping

The application maps the existing database schema to the spec's requirements:

| Spec Entity | Database Tables | Notes |
|-------------|----------------|-------|
| magazines | Magazine | Direct mapping |
| issues | Issue + Magazine | Joined for display |
| articles | Article + Content + Category | Content links articles to issues/pages |
| models | Model + ContentModel | ContentModel is junction table |

## Project Structure

```
the-archive/
├── src/
│   └── TheArchive/
│       ├── Controllers/
│       │   ├── Api/              # REST API endpoints
│       │   │   ├── MagazinesController.cs
│       │   │   ├── IssuesController.cs
│       │   │   ├── ArticlesController.cs
│       │   │   └── ModelsController.cs
│       │   └── HomeController.cs  # MVC pages
│       ├── Models/
│       │   ├── Magazine.cs
│       │   ├── Issue.cs
│       │   ├── Article.cs
│       │   └── Model.cs
│       ├── Services/
│       │   └── ArchiveDatabase.cs  # Data access layer
│       ├── Views/
│       │   └── Home/
│       │       └── Index.cshtml
│       ├── wwwroot/               # Static files (CSS, JS, images)
│       ├── appsettings.json
│       └── Program.cs
└── README.md
```

## API Endpoints (Implemented ✅)

All REST API endpoints from the spec are implemented:

### Magazines
- `GET /api/v1/magazines` - List all magazines with issue count
- `GET /api/v1/magazines/:id` - Single magazine
- `GET /api/v1/magazines/:id/issues` - Issues for a magazine

### Issues
- `GET /api/v1/issues` - All issues (paginated)
- `GET /api/v1/issues/:id` - Single issue
- `GET /api/v1/issues/:id/articles` - Articles in an issue

### Articles
- `GET /api/v1/articles` - All articles (paginated, filterable by category)
- `GET /api/v1/articles/:id` - Single article

### Models
- `GET /api/v1/models` - All models with appearance count
- `GET /api/v1/models/:id` - Single model profile
- `GET /api/v1/models/:id/articles` - Articles featuring a model
- `GET /api/v1/models/:id/issues` - Issues featuring a model

## Setup & Running

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL database (existing `magazines` database)

### Configuration

Edit `appsettings.json` to set your database connection:

```json
{
  "ConnectionStrings": {
    "MagazineDb": "Host=localhost;Port=5432;Database=magazines;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### Build & Run

```bash
cd the-archive/src/TheArchive
dotnet build
dotnet run
```

The application will start on `http://localhost:5000` (or similar).

### Test Database Connection

Navigate to `http://localhost:5000` to see:
- Database connection status
- List of magazines in the database
- Links to test API endpoints

## Implementation Status

### Phase 1: Backend & API ✅ COMPLETE
- [x] Project setup
- [x] Database service layer
- [x] Entity models with spec mapping
- [x] All REST API endpoints
- [x] Database connection test page

### Phase 2: Frontend Views (TODO)
- [ ] Magazines grid view
- [ ] Issues list/grid
- [ ] Issue detail with article list
- [ ] Article spread viewer (overlay)
- [ ] Models grid
- [ ] Model detail page
- [ ] Search functionality
- [ ] Navigation breadcrumbs

### Phase 3: UI/UX Styling (TODO)
- [ ] Dark editorial theme (from prototype)
- [ ] Magazine masthead SVGs
- [ ] Responsive grid layouts
- [ ] Typography (Playfair Display, Libre Baskerville, etc.)
- [ ] Category badges and styling
- [ ] Hover effects and animations

### Phase 4: Advanced Features (TODO)
- [ ] Client-side search filtering
- [ ] Keyboard navigation
- [ ] Random magazine feature
- [ ] Page image serving
- [ ] Spread viewer with prev/next

## Schema Differences

The existing database schema differs from the spec in these areas:

| Spec Field | Database Equivalent | Status |
|------------|---------------------|--------|
| issues.date_label | Computed from Year + Number | ✅ Implemented |
| issues.date_sort | Computed from Year, Number | ✅ Implemented |
| issues.page_count | MAX(Content.Page) | ✅ Implemented |
| articles.page_start | MIN(Content.Page) | ✅ Implemented |
| articles.author | Not in schema | ⚠️ Always null |
| articles.photographer | Contributor table | ✅ Implemented |
| models.real_name | Not in schema | ⚠️ Always null |
| models.height | Not in schema | ⚠️ Always null |
| models.hair | Not in schema | ⚠️ Always null |
| models.eyes | Not in schema | ⚠️ Always null |

## Category Mapping

The spec defines 13 categories. These are mapped from existing database categories:

| Spec Category | Database Categories |
|---------------|---------------------|
| feature | Feature |
| model | Model, Pinup |
| motoring | Motoring |
| letters | Letters |
| interview | Interview |
| editorial | Editorial |
| travel | Travel |
| technology | Review |
| culture | Fiction, Humour, Cartoons |
| fashion | Fashion |
| health | Health |
| sport | (not in DB) |

## Next Steps

1. **Test API Endpoints**: Use the test page or tools like Postman to verify all endpoints
2. **Implement UI Views**: Create Razor views for each major page type
3. **Add Styling**: Implement the dark editorial theme from the HTML prototype
4. **Static Assets**: Set up page image serving
5. **Polish**: Add search, keyboard shortcuts, and UX enhancements

## Testing

### Manual API Testing

Visit these URLs after starting the app:
- http://localhost:5000/api/v1/magazines
- http://localhost:5000/api/v1/issues
- http://localhost:5000/api/v1/models
- http://localhost:5000/api/v1/articles

### Database Test

Visit http://localhost:5000 to see:
- Connection status
- Magazine count
- Full magazine list with generated slugs

## Notes

- This application is **read-only** (no write operations)
- Uses the **existing database schema** (no migrations needed)
- Adapts the spec's requirements to work with current data structure
- Slug generation is handled in C# (not stored in database)
- All API responses use camelCase JSON

## License

Internal project - all rights reserved.

