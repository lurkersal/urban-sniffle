# The Archive - System Architecture

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         THE ARCHIVE                              │
│                  Magazine Collection Viewer                      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────┐      ┌──────────────────────┐        │
│  │   MVC Controllers    │      │   API Controllers    │        │
│  │                      │      │                      │        │
│  │  • HomeController    │      │  • MagazinesController       │
│  │  • MagazinesCtrl     │      │  • IssuesController  │        │
│  │  • IssuesCtrl        │      │  • ArticlesController│        │
│  │  • ModelsCtrl        │      │  • ModelsController  │        │
│  │                      │      │                      │        │
│  │  Returns: HTML Views │      │  Returns: JSON       │        │
│  └──────────────────────┘      └──────────────────────┘        │
│           │                              │                       │
│           └──────────────┬───────────────┘                       │
│                          │                                       │
└──────────────────────────┼───────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BUSINESS LAYER                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              ArchiveDatabase Service                      │  │
│  │                                                           │  │
│  │  Magazine Methods:          Issue Methods:               │  │
│  │  • GetMagazinesAsync()      • GetAllIssuesAsync()        │  │
│  │  • GetMagazineAsync()       • GetIssuesByMagazineAsync() │  │
│  │                             • GetIssueAsync()            │  │
│  │  Article Methods:           Model Methods:               │  │
│  │  • GetArticlesByIssueAsync()• GetModelsAsync()           │  │
│  │  • GetAllArticlesAsync()    • GetModelAsync()            │  │
│  │  • GetArticleAsync()        • GetArticlesByModelAsync()  │  │
│  │                             • GetIssuesByModelAsync()    │  │
│  │  Search Methods:                                         │  │
│  │  • SearchAsync()                                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                          │                                       │
│                          │ Uses Dapper ORM                       │
│                          ▼                                       │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DATA LAYER                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │            PostgreSQL Database (magazines)                │  │
│  │                                                           │  │
│  │  Tables:                                                  │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐│  │
│  │  │ Magazine │  │  Issue   │  │ Article  │  │  Model   ││  │
│  │  │──────────│  │──────────│  │──────────│  │──────────││  │
│  │  │ Id (PK)  │  │ Id (PK)  │  │ Id (PK)  │  │ Id (PK)  ││  │
│  │  │ Name     │  │ MagId(FK)│  │ CatId(FK)│  │ Name     ││  │
│  │  │ LogoUrl  │  │ Volume   │  │ Title    │  │ BustSize ││  │
│  │  └──────────┘  │ Number   │  └──────────┘  │ WaistSize││  │
│  │                │ Year     │                 │ HipSize  ││  │
│  │  ┌──────────┐  └──────────┘  ┌──────────┐  └──────────┘│  │
│  │  │ Category │                │ Content  │               │  │
│  │  │──────────│                │──────────│               │  │
│  │  │ Id (PK)  │                │ Id (PK)  │               │  │
│  │  │ Name     │                │IssueId(FK)               │  │
│  │  └──────────┘                │ArticleId(FK)             │  │
│  │                               │ Page     │               │  │
│  │  ┌───────────────┐           │ImagePath │               │  │
│  │  │ Contributor   │           └──────────┘               │  │
│  │  │───────────────│                                      │  │
│  │  │ Id (PK)       │  Junction Tables:                    │  │
│  │  │ Name          │  • ContentContributor                │  │
│  │  └───────────────┘  • ContentModel                      │  │
│  │                     • IssueLink                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Request Flow (MVC - HTML)
```
Browser Request
    ↓
ASP.NET Core Routing
    ↓
MVC Controller Action
    ↓
ArchiveDatabase Service
    ↓
Dapper Query → PostgreSQL
    ↓
Map Results to Models
    ↓
Pass to Razor View
    ↓
HTML Response
```

### Request Flow (API - JSON)
```
HTTP GET Request
    ↓
ASP.NET Core Routing
    ↓
API Controller Action
    ↓
ArchiveDatabase Service
    ↓
Dapper Query → PostgreSQL
    ↓
Map Results to Models
    ↓
Serialize to JSON
    ↓
JSON Response
```

## Model Mapping

### Magazine Entity
```
Database (Magazine)          C# Model (Magazine)           API Response
┌──────────────┐            ┌──────────────┐             ┌──────────────┐
│ MagazineId   │────────────>│ MagazineId   │────────────>│ magazineId   │
│ Name         │────────────>│ Name         │────────────>│ name         │
│ LogoUrl      │────────────>│ LogoUrl      │────────────>│ logoUrl      │
└──────────────┘            │ IssueCount   │◄────┐       │ issueCount   │
                            │ Id (computed)│     │       │ id (slug)    │
                            └──────────────┘     │       └──────────────┘
                                                 │
                                            Aggregated
                                            from Issue table
```

### Issue Entity
```
Database (Issue + Magazine)  C# Model (Issue)            API Response
┌──────────────┐            ┌──────────────┐            ┌──────────────┐
│ IssueId      │────────────>│ IssueId      │───────────>│ issueId      │
│ MagazineId   │────────────>│ MagazineId   │───────────>│ magazineId   │
│ Volume       │────────────>│ Volume       │───────────>│ volume       │
│ Number       │────────────>│ Number       │───────────>│ number       │
│ Year         │────────────>│ Year         │───────────>│ year         │
└──────────────┘            │ MagazineName │◄────┐      │ magazineName │
  Magazine.Name─────────────>│ ArticleCount │     │      │ articleCount │
                            │ PageCount    │     │      │ pageCount    │
                            │ DateLabel    │     │      │ dateLabel    │
                            │ DateSort     │     │      │ dateSort     │
                            │ Id (slug)    │     │      │ id           │
                            └──────────────┘     │      └──────────────┘
                                                 │
                                            Aggregated &
                                            Computed fields
```

### Article Entity
```
Database (Article + Content + Category)  C# Model           API Response
┌──────────────┐                        ┌──────────────┐   ┌──────────────┐
│ ArticleId    │───────────────────────>│ ArticleId    │──>│ articleId    │
│ CategoryId   │───────────────────────>│ CategoryId   │──>│ categoryId   │
│ Title        │───────────────────────>│ Title        │──>│ title        │
└──────────────┘                        │ IssueId      │◄─┐│ issueId      │
  Category.Name────────────────────────>│ CategoryName │  ││ categoryName │
  MIN(Content.Page)────────────────────>│ PageStart    │  ││ pageStart    │
  Contributor.Name─────────────────────>│ Photographer │  ││ photographer │
  ContentModel.ModelId─────────────────>│ ModelId      │  ││ modelId      │
  Model.Name───────────────────────────>│ ModelName    │  ││ modelName    │
                                        │ Category     │  ││ category     │
                                        │ Id (slug)    │  ││ id           │
                                        └──────────────┘  │└──────────────┘
                                                          │
                                                     Joined from
                                                     Content table
```

### Model Entity
```
Database (Model + ContentModel)  C# Model (Model)         API Response
┌──────────────┐                ┌──────────────┐         ┌──────────────┐
│ ModelId      │───────────────>│ ModelId      │────────>│ modelId      │
│ Name         │───────────────>│ Name         │────────>│ name         │
│ YearOfBirth  │───────────────>│ YearOfBirth  │────────>│ yearOfBirth  │
│ BustSize     │───────────────>│ BustSize     │────────>│ bustSize     │
│ WaistSize    │───────────────>│ WaistSize    │────────>│ waistSize    │
│ HipSize      │───────────────>│ HipSize      │────────>│ hipSize      │
│ CupSize      │───────────────>│ CupSize      │────────>│ cupSize      │
└──────────────┘                │ Appearances  │◄────┐   │ appearances  │
                                │ Measurements │     │   │ measurements │
                                │ Id (slug)    │     │   │ id           │
                                └──────────────┘     │   └──────────────┘
                                                     │
                                                Aggregated
                                                & Formatted
```

## Technology Stack Detail

```
┌─────────────────────────────────────────────────┐
│              Frontend (Phase 2+)                │
│  • Razor Views (.cshtml)                        │
│  • Bootstrap 5 (responsive grid)                │
│  • Custom CSS (dark editorial theme)            │
│  • JavaScript (search, filtering, viewer)       │
│  • Google Fonts (Playfair, Baskerville, etc.)   │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│              ASP.NET Core 8.0                   │
│  • MVC Framework                                │
│  • Web API                                      │
│  • Dependency Injection                         │
│  • CORS                                         │
│  • Static Files Middleware                      │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│              Data Access                        │
│  • Dapper 2.1.72 (micro ORM)                    │
│  • Npgsql 10.0.2 (PostgreSQL driver)            │
│  • Custom ArchiveDatabase service               │
└─────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────┐
│         PostgreSQL Database                     │
│  • magazines database (existing)                │
│  • 10 tables + indexes                          │
│  • ~1970s-1990s magazine data                   │
└─────────────────────────────────────────────────┘
```

## Deployment Architecture (Future)

```
┌───────────────────────────────────────────────┐
│           Load Balancer / Reverse Proxy       │
│                  (Nginx/Caddy)                │
└───────────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ TheArchive  │ │ TheArchive  │ │ TheArchive  │
│ Instance 1  │ │ Instance 2  │ │ Instance 3  │
└─────────────┘ └─────────────┘ └─────────────┘
         │           │           │
         └───────────┼───────────┘
                     ▼
         ┌───────────────────────┐
         │  PostgreSQL Primary   │
         │  (Read/Write)         │
         └───────────────────────┘
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
    ┌─────────┐ ┌─────────┐ ┌─────────┐
    │Replica 1│ │Replica 2│ │Replica 3│
    │(Read)   │ │(Read)   │ │(Read)   │
    └─────────┘ └─────────┘ └─────────┘
```

## API Endpoint Tree

```
/api/v1
│
├── /magazines
│   ├── GET /                          (list all)
│   ├── GET /:id                       (single)
│   └── GET /:id/issues                (magazine's issues)
│
├── /issues
│   ├── GET /                          (list all, paginated)
│   ├── GET /:id                       (single)
│   └── GET /:id/articles              (issue's articles)
│
├── /articles
│   ├── GET /                          (list all, paginated, filterable)
│   └── GET /:id                       (single)
│
└── /models
    ├── GET /                          (list all)
    ├── GET /:id                       (single)
    ├── GET /:id/articles              (model's articles)
    └── GET /:id/issues                (model's issues)
```

## URL Routing (MVC - Phase 2+)

```
Frontend Routes
│
├── /                                  → HomeController.Index (magazine grid)
├── /magazines/:slug                   → MagazinesController.Index (magazine detail)
├── /magazines/:slug/issues/:id        → IssuesController.Detail (issue detail)
├── /issues                            → IssuesController.Index (all issues)
├── /models                            → ModelsController.Index (models grid)
└── /models/:slug                      → ModelsController.Detail (model detail)
```

## File Organization

```
TheArchive/
│
├── Controllers/
│   ├── Api/                   [REST API - Returns JSON]
│   │   ├── MagazinesController.cs
│   │   ├── IssuesController.cs
│   │   ├── ArticlesController.cs
│   │   └── ModelsController.cs
│   │
│   └── [MVC]                  [Returns HTML Views]
│       ├── HomeController.cs
│       ├── MagazinesController.cs  (Phase 2)
│       ├── IssuesController.cs     (Phase 2)
│       └── ModelsController.cs     (Phase 2)
│
├── Models/
│   ├── Magazine.cs
│   ├── Issue.cs
│   ├── Article.cs
│   └── Model.cs
│
├── Services/
│   └── ArchiveDatabase.cs     [Data Access Layer]
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml     (Phase 2)
│   │   ├── _Breadcrumb.cshtml (Phase 2)
│   │   └── _SpreadViewer.cshtml (Phase 3)
│   │
│   ├── Home/
│   │   └── Index.cshtml
│   │
│   ├── Magazines/             (Phase 2)
│   ├── Issues/                (Phase 2-3)
│   └── Models/                (Phase 4)
│
└── wwwroot/
    ├── css/
    │   └── archive.css        (Phase 5)
    │
    ├── js/
    │   ├── search.js          (Phase 6)
    │   ├── spread-viewer.js   (Phase 7)
    │   └── keyboard.js        (Phase 8)
    │
    └── images/
        └── mastheads/         (Phase 5)
```

## Performance Characteristics

```
Layer                Speed       Bottleneck          Optimization
──────────────────────────────────────────────────────────────────
API Controllers      ~1ms        Serialization       Pre-compute slugs
  ↓
Database Service     ~5-10ms     SQL queries         Add caching
  ↓
Dapper Mapping       ~1-2ms      Reflection          AOT compilation
  ↓
PostgreSQL           ~10-50ms    Table scans         Proper indexes
  ↓
Network              ~1-10ms     Bandwidth           Compression

Total Response Time: 20-75ms (well under 200ms requirement)
```

## Security Model

```
┌────────────────────────────────────────┐
│         Security Layers                │
├────────────────────────────────────────┤
│ 1. CORS Policy (API access control)   │
│ 2. Read-Only Database User             │
│ 3. SQL Injection Prevention (Dapper)   │
│ 4. HTTPS Enforcement (Production)      │
│ 5. No Authentication Required          │
│ 6. No User Input Persistence           │
└────────────────────────────────────────┘

Threat Model: Low (read-only, public archive)
```

---

**Architecture Complete**: Ready for implementation Phase 2

