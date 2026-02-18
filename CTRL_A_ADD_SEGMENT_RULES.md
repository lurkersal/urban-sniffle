# Ctrl-A (Add Segment) Rules and Behavior

## Overview
Ctrl-A is a **global** keyboard shortcut to add or activate a segment for the current article at the current page.

**Important:** This shortcut works globally throughout the application, including when editing text fields. The global handler captures Ctrl-A before TextBox controls can use it for "Select All".

## Enabled Conditions

The Ctrl-A shortcut is **ENABLED** when:
```csharp
// From MainWindow.axaml.cs line 143
() => { var s = EditorState.ActiveSegment; return s == null || !s.IsActive; }
```

**Translation:** Ctrl-A is enabled when:
- **No active segment exists** (`ActiveSegment == null`), OR
- **An active segment exists but is not active** (`!IsActive`)

The `IsActive` property on a Segment returns true when `End == null`, meaning the segment is currently being edited/extended.

## Disabled Conditions

The Ctrl-A shortcut is **DISABLED** when:
- **An active segment is currently being edited** (`ActiveSegment != null && ActiveSegment.IsActive`)

When disabled and the user presses Ctrl-A anyway, the validation in `EditorActionsService.ValidateCanAddSegment()` will show a toast message:
```
"Finish or cancel the active segment first"
```

## Validation Rules

When Ctrl-A is pressed, the following validations occur in `EditorActionsService.AddSegmentAtCurrentPage()`:

### 1. Active Article Check
```csharp
if (article == null)
{
    ToastService.Show("No active article selected");
    return false;
}
```
**Requirement:** An article must be selected in the article list.

### 2. Active Segment Check
```csharp
if (_state.ActiveSegment != null && _state.ActiveSegment.IsActive)
{
    ToastService.Show("Finish or cancel the active segment first");
    return false;
}
```
**Requirement:** No segment can be currently active (being edited).

### 3. Page Location Check
After validation passes, the behavior depends on whether the current page already belongs to the article:

**If page ALREADY belongs to article:**
```csharp
if (PageBelongsToArticle(article, page))
    return ActivateExistingSegment(article, page);
```
- Finds the segment containing that page
- Re-opens that segment for editing
- Sets it as the active segment
- Allows user to extend/modify the segment

**If page DOES NOT belong to article:**
```csharp
return CreateNewActiveSegment(article, page);
```
- Creates a new segment starting at the current page
- Marks it as active (End = null)
- Marks it as new (WasNew = true) so it can be canceled cleanly
- Sets it as the active segment

## Behavior Flow

### Scenario 1: Adding a New Segment
1. User selects an article
2. User navigates to a page not already in the article
3. User presses Ctrl-A
4. **Result:** New segment created starting at current page, active for editing

### Scenario 2: Re-opening an Existing Segment
1. User selects an article with existing segments
2. User navigates to a page that belongs to one of the article's segments
3. User presses Ctrl-A
4. **Result:** Existing segment re-opened for editing, becomes active

### Scenario 3: Blocked by Active Segment
1. User has an active segment being edited
2. User tries to press Ctrl-A (either on same article or different article)
3. **Result:** Toast message "Finish or cancel the active segment first"
4. User must either:
   - Press Enter/Ctrl+Enter to end the segment
   - Press Esc to cancel the segment

### Scenario 4: No Article Selected
1. User presses Ctrl-A without selecting an article
2. **Result:** Toast message "No active article selected"

## Related Shortcuts

**End Active Segment:**
- **Enter** or **Ctrl+Enter** - Ends the active segment at the current page
- Sets `segment.End = CurrentPage`
- Clears `ActiveSegment`
- Updates article's Pages list

**Cancel Active Segment:**
- **Esc** - Cancels the active segment
- If segment was new, removes it entirely
- If segment was re-opened, restores original End value
- Clears `ActiveSegment`

**Navigate Pages:**
- **← / →** - Move to previous/next page (updates CurrentPage)
- While segment is active, page changes update the segment's preview end

## Code Locations

### Shortcut Registration
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs`
**Line:** 143
```csharp
_shortcutService.Register(
    Key.A, 
    KeyModifiers.Control, 
    (ke) => { IndexEditor.Shared.EditorActions.AddSegmentAtCurrentPage(); return true; }, 
    () => { var s = EditorState.ActiveSegment; return s == null || !s.IsActive; }, 
    "AddSegment"
);
```

### Implementation
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Shared/IEditorActions.cs`
**Method:** `EditorActionsService.AddSegmentAtCurrentPage()` (line 110-132)

### Validation
**File:** `/home/justin/repos/urban-sniffle/src/index-editor/Shared/IEditorActions.cs`
**Method:** `EditorActionsService.ValidateCanAddSegment()` (line 134-150)

## Summary Table

| Condition | Ctrl-A Enabled? | Result |
|-----------|----------------|--------|
| No article selected | ✅ Yes (but validation fails) | Toast: "No active article selected" |
| Article selected, no active segment | ✅ Yes | Creates new segment OR re-opens existing |
| Article selected, active segment exists | ❌ No | Toast: "Finish or cancel the active segment first" |
| Page belongs to current article | ✅ Yes (if no active segment) | Re-opens existing segment |
| Page doesn't belong to article | ✅ Yes (if no active segment) | Creates new segment at current page |

## Important Notes

1. **One Active Segment at a Time:** Only one segment can be active across the entire application. You cannot have multiple segments being edited simultaneously, even for different articles.

2. **Article Selection Lock:** While a segment is active, article selection is typically locked/restricted to prevent conflicts.

3. **Toast Notifications:** The application provides clear feedback via toast messages for all validation failures.

4. **Segment States:**
   - **Active:** `End == null` (currently being edited)
   - **Closed:** `End != null` (editing complete)
   - **New:** `WasNew == true` (created in current editing session)

5. **Global Keybinding:** Ctrl-A works globally throughout the application, even when focus is inside a TextBox or other input control. This is achieved using `handledEventsToo: true` in the event handler registration, which allows the window to intercept the key event even after child controls have handled it.

## Technical Implementation

The global behavior is implemented using Avalonia's tunneling event system with the `handledEventsToo` parameter:

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs` (line 73)
```csharp
this.AddHandler<KeyEventArgs>(
    KeyDownEvent, 
    OnMainWindowKeyDown, 
    RoutingStrategies.Tunnel, 
    handledEventsToo: true
);
```

This ensures that:
- The window's KeyDown handler receives ALL key events via tunneling (before child controls)
- Even if a child control (like TextBox) handles the event, the window handler still sees it
- Ctrl-A is intercepted at the window level before TextBox can use it for "Select All"

## Date Documented
February 17, 2026



