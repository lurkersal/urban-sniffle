# The Archive - Complete Implementation Plan

## Project Context

**Goal**: Create "The Archive" - a read-only web viewer for British men's magazines (1970s-90s)  
**Source Spec**: `/home/justin/Magazines/the-archive-spec.docx`  
**Database**: Existing PostgreSQL `magazines` database from `magazine-viewer` project  
**Approach**: ASP.NET Core 8.0, adapting spec requirements to existing schema

---

## Phase 1: Foundation & Backend ✅ COMPLETE

### Deliverables
- [x] ASP.NET Core 8.0 MVC project created
- [x] NuGet packages installed (Npgsql, Dapper)
- [x] Connection string configuration
- [x] Entity models created (Magazine, Issue, Article, Model)
- [x] Database service layer (ArchiveDatabase.cs)
- [x] All REST API endpoints implemented
- [x] Database connection test page

### Testing
- [x] Build succeeds with no errors
- [x] Application starts
- [ ] Database connection works (test on first run)
- [ ] API endpoints return JSON data

**Current Status**: Code complete, ready for first run test

---

## Phase 2: Basic UI - Magazine Views (NEXT)

### 2.1 Magazines Grid View
**Route**: `/`  
**View**: `Views/Home/Index.cshtml` (update existing)  
**Controller**: `HomeController.Index()`

**Requirements**:
- Responsive CSS grid (auto-fill, min 180px cards)
- Each card shows magazine masthead + issue count
- Hover effect: lift 5px with transition
- Click navigates to magazine detail

**Files to Create/Edit**:
- `Views/Home/Index.cshtml` - Main view
- `wwwroot/css/archive.css` - Custom styles
- `Views/Shared/_Layout.cshtml` - Update with navigation

### 2.2 Magazine Detail View
**Route**: `/magazines/{id}`  
**View**: `Views/Magazines/Index.cshtml`  
**Controller**: Create `MagazinesController` (MVC, not API)

**Requirements**:
- Show magazine metadata
- Grid of all issues for this magazine
- Breadcrumb: Home > Magazine Name

**Files to Create**:
- `Controllers/MagazinesController.cs`
- `Views/Magazines/Index.cshtml`

### 2.3 All Issues View
**Route**: `/issues`  
**View**: `Views/Issues/Index.cshtml`  
**Controller**: Create `IssuesController` (MVC)

**Requirements**:
- Show all issues across all magazines
- Newest first
- Colored thumbnails by magazine brand
- Click navigates to issue detail

**Files to Create**:
- `Controllers/IssuesController.cs` (MVC, separate from API controller)
- `Views/Issues/Index.cshtml`

**Acceptance Criteria**:
- ✅ Three main views render
- ✅ Navigation between views works
- ✅ Responsive grid layouts
- ✅ Data loads from database

---

## Phase 3: Issue & Article Views

### 3.1 Issue Detail View
**Route**: `/magazines/{magazineId}/issues/{issueId}`  
**View**: `Views/Issues/Detail.cshtml`

**Requirements**:
- Show issue metadata (volume, number, date, page count)
- List all articles sorted by page number
- Category filter chips
- Each article row: page number, category badge, title, byline, "View spread" button
- Model names in bylines are clickable links
- Breadcrumb: Home > Magazine > Issue

**Files to Create**:
- `Views/Issues/Detail.cshtml`
- Update `Controllers/IssuesController.cs` with Detail action

### 3.2 Article Category Filtering
**Approach**: Client-side JavaScript filtering (no page reload)

**Requirements**:
- "All" chip shows everything
- Clicking category chip filters list
- Active chip has filled style
- Smooth transition

**Files to Create**:
- `wwwroot/js/article-filter.js`

### 3.3 Spread Viewer Overlay
**Trigger**: Click article row or "View spread" button  
**Approach**: Full-screen modal overlay

**Requirements**:
- Display two pages side by side (even/odd)
- Given page P: left = P if even, else P-1; right = left + 1
- Show article metadata if page has article
- Prev/Next buttons (shift by 2)
- Close button + ESC key
- Dark overlay background (rgba(5,5,5,0.97))

**Files to Create**:
- `wwwroot/js/spread-viewer.js`
- `Views/Shared/_SpreadViewer.cshtml` (partial view)

**Acceptance Criteria**:
- ✅ Issue detail shows articles sorted by page
- ✅ Category filtering works
- ✅ Spread viewer opens on click
- ✅ Spread viewer shows correct pages
- ✅ Prev/Next navigation works
- ✅ ESC key closes viewer

---

## Phase 4: Models Views

### 4.1 Models Grid
**Route**: `/models`  
**View**: `Views/Models/Index.cshtml`  
**Controller**: Create `ModelsController` (MVC)

**Requirements**:
- Grid layout, min 200px cards
- Each card: styled portrait placeholder (initials), name, appearance count
- Click navigates to model detail

**Files to Create**:
- `Controllers/ModelsController.cs` (MVC)
- `Views/Models/Index.cshtml`

### 4.2 Model Detail View
**Route**: `/models/{id}`  
**View**: `Views/Models/Detail.cshtml`

**Requirements**:
- Two-column layout: portrait left (260px), stats right
- Show name, measurements, birth year, etc.
- List of appearances as clickable chips (navigate to issue)
- Breadcrumb: Home > Models > Model Name

**Files to Create**:
- `Views/Models/Detail.cshtml`
- Update `Controllers/ModelsController.cs` with Detail action

**Acceptance Criteria**:
- ✅ Models grid displays
- ✅ Model detail shows full info
- ✅ Appearance chips link to issues
- ✅ Model links in article bylines work

---

## Phase 5: Styling & Theme

### 5.1 Design System Setup
**Based on**: `magazine_archive_viewer.html` prototype

**Color Tokens**:
```css
--ink: #0f0e0d (page background)
--paper: #f5f0e8 (primary text on dark)
--muted: #9a9488 (secondary text)
--accent: #c8392b (feature red)
--accent2: #2b5fc8 (link blue)
--gold: #b8922a (logo accent)
```

**Typography**:
- Playfair Display (serif display)
- Libre Baskerville (serif body)
- IM Fell English (italic editorial)
- DM Sans (UI sans-serif)
- All from Google Fonts

**Files to Create**:
- `wwwroot/css/archive.css` (main stylesheet)
- Update `Views/Shared/_Layout.cshtml` with font imports

### 5.2 Magazine Mastheads
**Approach**: SVG components (future enhancement) or styled placeholders

**Magazine Specs** (from prototype):
- Club International: Dark (#0d0d0d), gold accents
- Mayfair: Cream (#f5f0e8), navy/crimson
- Men Only: Stone (#e8e0d4), burgundy

**Files to Create**:
- `wwwroot/images/mastheads/` (SVG files or CSS-based)

### 5.3 Category Badges
**CSS Classes**: `.cat-feature`, `.cat-model`, `.cat-motoring`, etc.

**Styling**:
- Small caps, letter-spaced
- Colored backgrounds (semi-transparent)
- Border radius 3px

**Files to Update**:
- `wwwroot/css/archive.css`

**Acceptance Criteria**:
- ✅ Dark editorial theme applied
- ✅ Google Fonts load correctly
- ✅ Color scheme matches spec
- ✅ Typography hierarchy clear
- ✅ Category badges styled
- ✅ Responsive at 768px+

---

## Phase 6: Navigation & Chrome

### 6.1 Top Navigation Bar
**Fixed Position**: Sticky top bar

**Elements**:
- Logo: "The Archive" (Playfair Display, gold italic accent)
- Tabs: Magazines | All Issues | Models (pill-style active state)
- Search input (live filter, 150ms debounce)

**Files to Create/Update**:
- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/search.js`

### 6.2 Breadcrumbs
**Position**: Below top nav

**Format**: Home > Magazine > Issue (all ancestors clickable)

**Files to Create**:
- `Views/Shared/_Breadcrumb.cshtml` (partial)
- Update each view to pass breadcrumb data

### 6.3 Search Functionality
**Approach**: Client-side filtering with search API fallback

**Behavior**:
- Keystroke triggers debounced search
- Filters current view by name/title
- No submit button required
- Works on all list views

**Files to Create**:
- `wwwroot/js/search.js`
- Update API controllers with search parameter support

**Acceptance Criteria**:
- ✅ Top nav renders on all pages
- ✅ Tab highlighting shows active section
- ✅ Breadcrumbs reflect current location
- ✅ Search filters update view
- ✅ Logo links to home

---

## Phase 7: Page Images & Spread Viewer

### 7.1 Page Image Serving
**Storage**: File system (existing structure)  
**Convention**: `/static/pages/{issueId}/{pageNum}.jpg`

**Implementation**:
- Create API endpoint or static file middleware
- Handle missing pages gracefully (blank panel)

**Files to Create**:
- `Controllers/Api/PagesController.cs`
- OR configure static file serving in `Program.cs`

### 7.2 Spread Viewer Enhancement
**Features**:
- Load actual page images (if available)
- Show placeholder if image missing
- Display article overlay on page
- Prev/Next with keyboard arrows

**Files to Update**:
- `wwwroot/js/spread-viewer.js`
- `Views/Shared/_SpreadViewer.cshtml`

**Acceptance Criteria**:
- ✅ Page images load in spread viewer
- ✅ Missing pages show blank panel (no errors)
- ✅ Article metadata overlays on page
- ✅ Keyboard navigation works

---

## Phase 8: Advanced Features

### 8.1 Random Magazine
**Button**: "Random" in top nav or sidebar

**Implementation**:
- API endpoint: `/api/v1/magazines/random`
- JavaScript: fetches random, navigates to it

**Files to Create**:
- Update `Services/ArchiveDatabase.cs` with random query
- Update API controller
- `wwwroot/js/random.js`

### 8.2 Keyboard Shortcuts
**Shortcuts** (from spec):
- Arrow keys: Navigate spread viewer
- ESC: Close overlays
- / : Focus search box

**Files to Create**:
- `wwwroot/js/keyboard.js`

### 8.3 Performance Optimization
**Techniques**:
- Lazy load images in grids
- Pagination for large lists (already in API)
- Cache database queries
- Minify CSS/JS

**Files to Update**:
- `wwwroot/css/*.css` (minify)
- `wwwroot/js/*.js` (minify)
- `Services/ArchiveDatabase.cs` (add caching)

**Acceptance Criteria**:
- ✅ Random magazine works
- ✅ Keyboard shortcuts function
- ✅ Initial load < 2s
- ✅ API responses < 200ms

---

## Phase 9: Polish & Testing

### 9.1 Error Handling
- 404 pages for missing magazines/issues/models
- Friendly error messages
- Fallback for database errors

### 9.2 Accessibility
- Keyboard navigation for all interactive elements
- Focus rings visible
- Alt text on images
- ARIA labels where needed

### 9.3 Browser Testing
- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)

### 9.4 Documentation
- API documentation (Swagger/OpenAPI)
- User guide
- Developer setup instructions

**Acceptance Criteria**:
- ✅ All deliverables from spec checked off
- ✅ No console errors
- ✅ Responsive design works
- ✅ Cross-browser tested
- ✅ Documentation complete

---

## Phase 10: Deployment

### 10.1 Configuration
- Production connection string (environment variable)
- HTTPS enforcement
- CORS policies
- Security headers

### 10.2 Hosting Options
- Docker container
- IIS deployment
- Linux systemd service
- Azure App Service

### 10.3 Deployment Script
**Files to Create**:
- `deploy.sh` (Linux)
- `Dockerfile`
- `docker-compose.yml`

**Acceptance Criteria**:
- ✅ Runs in production environment
- ✅ Health check endpoint works
- ✅ Logging configured
- ✅ Deployment documented

---

## Testing Checklist

### Backend Tests
- [ ] Database connection successful
- [ ] GET /api/v1/magazines returns data
- [ ] GET /api/v1/issues returns data
- [ ] GET /api/v1/models returns data
- [ ] GET /api/v1/articles returns data
- [ ] Pagination works (page=2)
- [ ] Category filter works
- [ ] Search parameter works

### Frontend Tests
- [ ] Home page loads magazines grid
- [ ] Click magazine navigates to magazine view
- [ ] Issue list displays correctly
- [ ] Click issue navigates to issue detail
- [ ] Article list shows in issue detail
- [ ] Category filter chips work
- [ ] Click article opens spread viewer
- [ ] Spread viewer shows correct pages
- [ ] Prev/Next in spread viewer works
- [ ] ESC closes spread viewer
- [ ] Models grid displays
- [ ] Click model navigates to model detail
- [ ] Model appearances link to issues
- [ ] Search filters results
- [ ] Breadcrumbs navigate correctly
- [ ] Top nav tabs highlight active section

### Performance Tests
- [ ] Initial page load < 2s
- [ ] API responses < 200ms
- [ ] Images lazy load
- [ ] No memory leaks in spread viewer
- [ ] Database queries optimized

### Accessibility Tests
- [ ] Tab navigation works
- [ ] Focus rings visible
- [ ] Screen reader compatible
- [ ] Alt text present
- [ ] ARIA labels correct

---

## File Structure Overview

```
the-archive/
├── src/
│   └── TheArchive/
│       ├── Controllers/
│       │   ├── Api/
│       │   │   ├── MagazinesController.cs ✅
│       │   │   ├── IssuesController.cs ✅
│       │   │   ├── ArticlesController.cs ✅
│       │   │   ├── ModelsController.cs ✅
│       │   │   └── PagesController.cs ⏳
│       │   ├── HomeController.cs ✅
│       │   ├── MagazinesController.cs ⏳
│       │   ├── IssuesController.cs ⏳
│       │   └── ModelsController.cs ⏳
│       ├── Models/
│       │   ├── Magazine.cs ✅
│       │   ├── Issue.cs ✅
│       │   ├── Article.cs ✅
│       │   └── Model.cs ✅
│       ├── Services/
│       │   └── ArchiveDatabase.cs ✅
│       ├── Views/
│       │   ├── Shared/
│       │   │   ├── _Layout.cshtml ⏳
│       │   │   ├── _Breadcrumb.cshtml ⏳
│       │   │   └── _SpreadViewer.cshtml ⏳
│       │   ├── Home/
│       │   │   └── Index.cshtml ✅
│       │   ├── Magazines/
│       │   │   └── Index.cshtml ⏳
│       │   ├── Issues/
│       │   │   ├── Index.cshtml ⏳
│       │   │   └── Detail.cshtml ⏳
│       │   └── Models/
│       │       ├── Index.cshtml ⏳
│       │       └── Detail.cshtml ⏳
│       ├── wwwroot/
│       │   ├── css/
│       │   │   └── archive.css ⏳
│       │   ├── js/
│       │   │   ├── search.js ⏳
│       │   │   ├── article-filter.js ⏳
│       │   │   ├── spread-viewer.js ⏳
│       │   │   ├── keyboard.js ⏳
│       │   │   └── random.js ⏳
│       │   └── images/
│       │       └── mastheads/ ⏳
│       ├── appsettings.json ✅
│       └── Program.cs ✅
├── README.md ✅
└── IMPLEMENTATION_PLAN.md ✅
```

**Legend**:
- ✅ Complete
- ⏳ To be implemented
- ❌ Blocked/Issue

---

## Current Status Summary

**Phase 1 COMPLETE**: All backend infrastructure, models, and API endpoints are implemented and building successfully.

**Next Immediate Steps**:
1. Run the application and test database connectivity
2. Test all API endpoints with real data
3. Begin Phase 2: Create magazine grid view with styling
4. Implement navigation and layout

**Estimated Timeline**:
- Phase 2-3: 2-3 days (basic views)
- Phase 4-5: 2 days (models + styling)
- Phase 6-8: 2-3 days (nav, images, features)
- Phase 9-10: 1-2 days (polish, deploy)
- **Total**: ~1-2 weeks for full implementation

---

## Notes

- Using existing PostgreSQL schema (no migrations)
- Slug generation is computed in C# models
- Some spec fields are missing from DB (will be null/empty)
- Category mapping adapts DB categories to spec categories
- Page images assumed to exist in file system
- Read-only application (no write operations)

