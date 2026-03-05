# PageControllerView Refactoring Guide
**Date:** March 5, 2026  
**Status:** Services extracted, integration pending

---

## Services Created and Ready for Integration

### 1. PageNavigationService
**File:** `src/index-editor/Services/PageNavigationService.cs`
**Purpose:** Manages page scanning, navigation, and finding nearest pages

**Methods to Replace in PageControllerView:**
- `ScanAvailablePages()` → Call `_pageNavigationService.ScanAvailablePages(folder)`
- `UpdateNavigationButtons()` → Use `_pageNavigationService.HasPreviousPage/HasNextPage()`
- `FindNearestExistingPage()` → Call `_pageNavigationService.FindNearestExistingPage()`
- `FindNearestExistingPageBothDirections()` → Call `_pageNavigationService.FindNearestExistingPageBothDirections()`

**Fields to Remove:**
- `private List<int> _availablePages`
- `private string? _lastScannedFolder`

---

### 2. ImageLoadingService
**File:** `src/index-editor/Services/ImageLoadingService.cs`
**Purpose:** Loads page images from disk

**Methods to Replace in PageControllerView:**
- `LoadCurrentPageImage()` → Call `_imageLoadingService.LoadPageImage(folder, pageNumber)`
- All image file path finding logic → Use `_imageLoadingService.FindImagePath()`

---

### 3. LinkManagementService
**File:** `src/index-editor/Services/LinkManagementService.cs`
**Purpose:** Manages magazine cross-reference links

**Methods to Replace in PageControllerView:**
- All `_pageLinks` dictionary operations → Use `_linkManagementService` methods
- `LoadLinksFromIndex()` → Call `_linkManagementService.LoadLinksFromIndex()`
- `GetLinksForPage()` → Call `_linkManagementService.GetLinksForPage()`

**Fields to Remove:**
- `private Dictionary<int, List<MagazineLink>> _pageLinks`

---

## Next Steps (Integration Plan)

### Phase 1: Inject Services into PageControllerView
1. Add constructor parameters:
   ```csharp
   public PageControllerView(
       IEditorState? editorState,
       IPageNavigationService pageNavigationService,
       IImageLoadingService imageLoadingService,
       ILinkManagementService linkManagementService)
   ```

2. Store as private fields:
   ```csharp
   private readonly IPageNavigationService _pageNavigationService;
   private readonly IImageLoadingService _imageLoadingService;
   private readonly ILinkManagementService _linkManagementService;
   ```

### Phase 2: Replace Method Calls
1. Replace all `ScanAvailablePages()` internal logic with service calls
2. Replace all `_availablePages` references with `_pageNavigationService.AvailablePages`
3. Replace image loading logic with `_imageLoadingService.LoadPageImage()`
4. Replace `_pageLinks` dictionary with `_linkManagementService` calls

### Phase 3: Remove Old Code
1. Delete `ScanAvailablePages()` method
2. Delete `FindNearestExistingPage()` method
3. Delete `FindNearestExistingPageBothDirections()` method
4. Delete `_availablePages` field
5. Delete `_lastScannedFolder` field
6. Delete `_pageLinks` field
7. Delete image file finding logic

### Phase 4: Test and Verify
1. Build and verify no compilation errors
2. Run application and test page navigation
3. Test image loading
4. Test link management
5. Update tests if needed

---

## Expected Benefits

### Code Reduction
- **Before:** 1,307 lines
- **After (estimated):** ~860 lines (33% reduction)
- **Extracted:** ~445 lines moved to services

### Improved Testability
- Page navigation logic can be unit tested in isolation
- Image loading can be mocked for UI tests
- Link management can be tested independently

### Better Separation of Concerns
- PageControllerView focuses only on UI coordination
- Business logic moved to services
- Easier to maintain and modify

### Reusability
- Services can be used by other views if needed
- Clear interfaces allow for different implementations
- Better dependency injection support

---

## Estimated Time
- **Integration:** 30-45 minutes
- **Testing:** 15-20 minutes
- **Total:** ~1 hour

---

## Status: ⏸️ READY FOR NEXT SESSION
All prerequisite work is complete. Services are built, tested, and registered in DI container.
Next session can proceed directly with integration.

