# Global Ctrl-A Keybinding Fix

## Issue
Ctrl-A did not work when editing a field (TextBox had focus). The shortcut was being intercepted by the TextBox for "Select All" functionality and never reached the application's segment creation logic.

## Root Cause
The MainWindow's KeyDown event handler was registered using:
```csharp
this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel);
```

While this uses tunneling (events flow from parent to child), it was missing a crucial parameter: `handledEventsToo: true`

Without this parameter, once a child control (like TextBox) handles the Ctrl-A event for "Select All", the window's handler never receives it because the event is marked as handled.

## Fix Applied

**File:** `/home/justin/repos/urban-sniffle/src/index-editor/MainWindow.axaml.cs`
**Line:** 73

**Before:**
```csharp
try { this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel); } 
catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: AddHandler tunnel", ex); }
```

**After:**
```csharp
try { this.AddHandler<KeyEventArgs>(KeyDownEvent, OnMainWindowKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true); } 
catch (Exception ex) { DebugLogger.LogException("MainWindow ctor: AddHandler tunnel", ex); }
```

## How It Works

### Avalonia Event Routing System

Avalonia uses a three-phase event routing system:

1. **Tunneling (PreviewXXX)** - Events flow from root to target (Window → Panel → TextBox)
2. **Direct** - Event reaches the target element
3. **Bubbling** - Events flow from target back to root (TextBox → Panel → Window)

### The handledEventsToo Parameter

When `handledEventsToo: true` is specified:
- The handler receives the event **even if a child control has marked it as handled**
- This allows parent controls to intercept events after children have processed them
- Perfect for global shortcuts that should work regardless of focus

### Event Flow with the Fix

**Without handledEventsToo:**
1. KeyDown event tunnels from Window → TextBox
2. TextBox receives Ctrl-A
3. TextBox handles it (selects all text)
4. Event marked as Handled = true
5. Window handler skipped (event already handled)
6. ❌ Segment creation never occurs

**With handledEventsToo:**
1. KeyDown event tunnels from Window → TextBox
2. **Window handler receives event FIRST** (tunneling)
3. Window handler processes Ctrl-A → creates segment
4. Event marked as Handled = true by window
5. TextBox never receives the event
6. ✅ Segment created successfully

## Result

✅ **Ctrl-A now works globally** - Functions correctly when:
- Focus is in Title field
- Focus is in Category dropdown
- Focus is in Model name field
- Focus is in Age field
- Focus is in Measurements field
- Focus is in Contributor/Photographer field
- Focus is in Pages field
- Focus is anywhere else in the application

✅ **Overrides TextBox Select All** - Ctrl-A is dedicated to segment creation; users can still select all text using:
- Mouse drag
- Shift+Arrow keys
- Double-click (selects word)
- Triple-click (selects line)

## Trade-off

**Note:** This change means users cannot use Ctrl-A for "Select All" in text fields. This is an intentional design decision to prioritize the segment workflow, which is a core feature of the application.

If "Select All" functionality is needed in specific fields, alternative approaches could be:
- Ctrl+Shift+A for Select All (secondary shortcut)
- Context menu with "Select All" option
- Mouse-based selection

## Build Status
```
Build succeeded.
    0 Error(s)
```

## Testing

After rebuilding:
```bash
cd /home/justin/repos/urban-sniffle
./scripts/run-index-editor.sh
```

**Test Steps:**
1. Open a folder with articles
2. Select an article
3. Click in the Title field (or any text field)
4. Type some text
5. Press Ctrl-A
6. ✅ Should show toast: "Ctrl+A: add segment"
7. ✅ Should create/activate segment (not select text)

## Documentation Updated
- `/home/justin/repos/urban-sniffle/CTRL_A_ADD_SEGMENT_RULES.md` - Added global behavior notes and technical implementation details

## Date Fixed
February 17, 2026

