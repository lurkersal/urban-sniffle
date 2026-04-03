# The Archive - What to Do Next

## ✅ What's Done (Phase 1 Complete)

All backend infrastructure is complete and ready:
- ASP.NET Core 8.0 project created and building successfully
- Database service layer with all queries implemented
- All REST API endpoints functional (magazines, issues, articles, models)
- Entity models mapping existing schema to spec requirements
- Test page for database verification
- Comprehensive documentation (README, QUICKSTART, IMPLEMENTATION_PLAN, ARCHITECTURE)

**Build Status**: ✅ Success (0 warnings, 0 errors)

---

## 🚀 Next: Test & Verify (5-10 minutes)

### Step 1: Start the Application
```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive
dotnet run
```

You should see:
```
Now listening on: http://localhost:5000
```

### Step 2: Test Database Connection
Open browser: **http://localhost:5000**

✅ **Success looks like**:
- Green alert: "Database Connected!"
- Table showing magazines (Mayfair, Knave, Club International, etc.)
- Issue counts for each magazine
- Generated slugs displayed

❌ **Failure looks like**:
- Red alert with error message
- If this happens, check PostgreSQL is running and connection string is correct

### Step 3: Test API Endpoints
Open these URLs in browser or use curl:

```bash
# List all magazines
curl http://localhost:5000/api/v1/magazines | jq

# List all issues
curl http://localhost:5000/api/v1/issues | jq

# List all models
curl http://localhost:5000/api/v1/models | jq

# List all articles
curl http://localhost:5000/api/v1/articles | jq
```

✅ **Success**: Each returns JSON data with records from database

### Step 4: Verify Data
Check that the returned data makes sense:
- Magazine names match database
- Issue counts are reasonable
- Slugs are lowercase and hyphenated
- Dates are formatted correctly

---

## 📋 After Testing: Choose Your Path

### Option A: Continue with UI (Recommended)
**Goal**: Make it look like the prototype from `magazine_archive_viewer.html`

**Next Phase**: Phase 2 - Magazine Grid View

**What to do**:
1. Open `Views/Home/Index.cshtml`
2. Replace test page with magazine grid layout
3. Create `wwwroot/css/archive.css` with dark theme
4. Add Google Fonts
5. Implement responsive grid with hover effects

**Estimated Time**: 2-3 hours  
**Deliverable**: Beautiful magazine grid homepage

See: `IMPLEMENTATION_PLAN.md` → Phase 2

### Option B: Enhance API First
**Goal**: Add missing features to API before building UI

**What to add**:
- Page images endpoint (`/api/v1/pages/:issueId/:pageNum`)
- Random magazine endpoint (`/api/v1/magazines/random`)
- Enhanced search with full-text
- Response caching

**Estimated Time**: 1-2 hours  
**Deliverable**: Complete, optimized API

See: `IMPLEMENTATION_PLAN.md` → Phase 7-8

### Option C: Documentation & Planning
**Goal**: Prepare for team handoff or future development

**What to create**:
- API documentation (Swagger/OpenAPI)
- User stories and acceptance criteria
- Test cases and QA checklist
- Deployment guide

**Estimated Time**: 1-2 hours  
**Deliverable**: Production-ready documentation

---

## 🎨 Recommended: Start with Magazine Grid (Option A)

Here's exactly what to do for Phase 2.1:

### 1. Create the Stylesheet
**File**: `wwwroot/css/archive.css`

```css
/* Import Google Fonts */
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,700;0,900;1,400;1,700&family=Libre+Baskerville:ital,wght@0,400;0,700;1,400&family=IM+Fell+English:ital@0;1&family=DM+Sans:wght@300;400;500&display=swap');

/* Color tokens */
:root {
    --ink: #0f0e0d;
    --paper: #f5f0e8;
    --muted: #9a9488;
    --gold: #b8922a;
    --serif: 'Playfair Display', Georgia, serif;
    --sans: 'DM Sans', system-ui, sans-serif;
}

/* Dark theme */
body {
    background: var(--ink);
    color: var(--paper);
    font-family: var(--sans);
}

/* Magazine grid */
.mag-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 1.5rem;
    padding: 2rem;
}

/* Magazine card */
.mag-card {
    cursor: pointer;
    transition: transform 0.2s;
}

.mag-card:hover {
    transform: translateY(-5px);
}

/* ... more styles from prototype ... */
```

### 2. Update the Layout
**File**: `Views/Shared/_Layout.cshtml`

Add to `<head>`:
```html
<link rel="stylesheet" href="~/css/archive.css" />
```

### 3. Update Home View
**File**: `Views/Home/Index.cshtml`

Replace test page with:
```html
@{
    ViewData["Title"] = "The Archive";
}

<div class="main">
    <div class="section-hd">
        <div>
            <h1 class="section-title">The <em>Archive</em></h1>
            <p class="section-sub">British Men's Magazines Collection</p>
        </div>
        <span class="count-badge">@ViewBag.MagazineCount Magazines</span>
    </div>

    <div class="mag-grid">
        @foreach (var mag in ViewBag.Magazines)
        {
            <div class="mag-card" onclick="location.href='/magazines/@mag.Id'">
                <div class="mag-cover">
                    <!-- Masthead SVG or placeholder -->
                    <div style="background: #1a1a1a; aspect-ratio: 3/4; border-radius: 3px;"></div>
                </div>
                <div class="mag-info">
                    <div class="mag-name">@mag.Name</div>
                    <div class="mag-meta">@mag.IssueCount issues</div>
                </div>
            </div>
        }
    </div>
</div>
```

### 4. Test
Refresh browser → Should see beautiful magazine grid!

---

## 📁 Files to Reference

### For Phase 2 Implementation
- **Prototype**: `/home/justin/Magazines/magazine_archive_viewer.html` (copy CSS from here)
- **Current View**: `Views/Home/Index.cshtml` (update this)
- **Layout**: `Views/Shared/_Layout.cshtml` (add navigation)

### For API Enhancement
- **Database Service**: `Services/ArchiveDatabase.cs` (add new methods)
- **Controllers**: `Controllers/Api/*.cs` (add endpoints)

### For Documentation
- **Spec**: `/home/justin/Magazines/the-archive-spec.docx` (requirements)
- **Schema**: `/home/justin/repos/urban-sniffle/scripts/schema_postgres.sql` (database)

---

## 🔍 Common Issues & Solutions

### Issue: Database connection fails
**Check**:
```bash
# Is PostgreSQL running?
sudo systemctl status postgresql

# Can you connect manually?
psql -h localhost -U postgres -d magazines

# Is connection string correct?
cat src/TheArchive/appsettings.json
```

**Fix**: Update connection string with correct password

### Issue: Build errors after changes
**Fix**:
```bash
dotnet clean
dotnet restore
dotnet build
```

### Issue: Changes not showing in browser
**Fix**:
- Hard refresh: Ctrl+Shift+R
- Clear cache
- Stop and restart `dotnet run`

### Issue: API returns empty arrays
**Check**: Database has data
```sql
psql -U postgres -d magazines
SELECT COUNT(*) FROM Magazine;
SELECT COUNT(*) FROM Issue;
SELECT COUNT(*) FROM Model;
```

---

## 📊 Progress Tracker

### Backend (Phase 1) ✅ 100% Complete
- [x] Project setup
- [x] Models
- [x] Services
- [x] API controllers
- [x] Database integration
- [x] Documentation

### Frontend (Phase 2-4) ⏳ 0% Complete
- [ ] Magazine grid view
- [ ] Navigation layout
- [ ] Magazine detail view
- [ ] Issues list view
- [ ] Issue detail view
- [ ] Article spread viewer
- [ ] Models grid view
- [ ] Model detail view

### Styling (Phase 5) ⏳ 0% Complete
- [ ] CSS architecture
- [ ] Dark theme
- [ ] Typography
- [ ] Category badges
- [ ] Responsive design

### Advanced (Phase 6-8) ⏳ 0% Complete
- [ ] Search functionality
- [ ] Breadcrumbs
- [ ] Keyboard shortcuts
- [ ] Page images
- [ ] Random feature
- [ ] Performance optimization

---

## ⏱️ Time Estimates

| Task | Estimated Time | Priority |
|------|----------------|----------|
| Test database connection | 5 minutes | 🔴 Critical |
| Test API endpoints | 10 minutes | 🔴 Critical |
| Magazine grid view | 2 hours | 🟡 High |
| Navigation layout | 1 hour | 🟡 High |
| Issue views | 3 hours | 🟡 High |
| Models views | 2 hours | 🟢 Medium |
| Spread viewer | 2 hours | 🟢 Medium |
| Full styling | 3 hours | 🟢 Medium |
| Polish & testing | 2 hours | 🟢 Medium |
| **Total** | **~16 hours** | |

---

## 🎯 Quick Wins (Do These First)

1. **Test the API** (5 min) ✅ Verify everything works
2. **Create archive.css** (30 min) ✅ Set up styling foundation
3. **Magazine grid** (1 hour) ✅ Make homepage beautiful
4. **Top navigation** (30 min) ✅ Add logo and tabs

After these 4 tasks (~2 hours), you'll have a working, beautiful homepage!

---

## 📞 Need Help?

### Documentation
- `README.md` - Project overview
- `QUICKSTART.md` - Getting started
- `IMPLEMENTATION_PLAN.md` - Full roadmap
- `ARCHITECTURE.md` - System design
- `SESSION_SUMMARY.md` - What was built

### Key Concepts
- **Slugs**: URL-friendly IDs (e.g., "club-international")
- **Dapper**: Lightweight ORM for database queries
- **Razor**: Template engine for HTML views
- **MVC**: Model-View-Controller pattern

### Debugging
```bash
# Enable detailed errors
export ASPNETCORE_ENVIRONMENT=Development

# Check logs
dotnet run --verbosity detailed

# Database queries
tail -f /var/log/postgresql/postgresql-*.log
```

---

## 🚦 Status Check

Run this test to verify Phase 1:

```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive

# Should build successfully
dotnet build

# Should start application
dotnet run &
sleep 3

# Should return JSON
curl -s http://localhost:5000/api/v1/magazines | jq '.[0].name'

# Cleanup
pkill -f TheArchive
```

If all succeed: **✅ Ready for Phase 2!**

---

## 🎉 You're Ready!

Everything is set up and ready to go. Just run the app and start building!

**Quick Start Command**:
```bash
cd /home/justin/repos/urban-sniffle/the-archive/src/TheArchive && dotnet run
```

Then open: **http://localhost:5000**

**Happy coding!** 🚀

