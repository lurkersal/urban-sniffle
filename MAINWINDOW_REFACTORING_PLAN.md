# MainWindow.axaml.cs Refactoring Plan

## Current State Analysis

**Total Lines:** 1,818  
**Largest Method:** OnMainWindowKeyDown (899 lines - 50% of file!)  
**Total Methods:** 10  
**Main Issues:**
- God Object anti-pattern
- Massive keyboard handler doing everything
- No separation of concerns
- Extremely difficult to test
- Hard to maintain and extend

## Refactoring Strategy

### Phase 1: Extract Keyboard Shortcut Handlers (Priority: CRITICAL)
**Goal:** Break down the 899-line OnMainWindowKeyDown into separate handler classes

#### New Structure:
```
Services/KeyboardHandlers/
├── IKeyboardShortcutHandler.cs
├── SegmentKeyboardHandler.cs      (Ctrl+A, Ctrl+Enter, Esc for segments)
├── ArticleKeyboardHandler.cs      (Ctrl+N, Ctrl+D, Up/Down arrows)
├── NavigationKeyboardHandler.cs   (Left/Right arrows, Page navigation)
├── FileKeyboardHandler.cs         (Ctrl+O, Ctrl+S, Ctrl+I)
└── KeyboardShortcutDispatcher.cs  (Coordinates all handlers)
```

**Estimated Reduction:** ~800 lines from MainWindow

### Phase 2: Extract View Logic into ViewModels
**Goal:** Move UI logic from code-behind to ViewModels (MVVM pattern)

#### Changes:
- Create `MainWindowViewModel` (properly - currently incomplete)
- Move folder loading logic to ViewModel
- Move article management to ViewModel
- Move fullscreen image logic to separate service

**Estimated Reduction:** ~200-300 lines from MainWindow

### Phase 3: Extract Services
**Goal:** Create proper services for reusable functionality

#### New Services:
```
Services/
├── FolderPickerService.cs        (Handle folder selection)
├── ArticleFileService.cs         (Load/Save articles)
├── FullscreenImageService.cs     (Fullscreen image display)
└── ArticleDeletionService.cs     (Article deletion with confirmation)
```

**Estimated Reduction:** ~150-200 lines

### Phase 4: Simplify Initialization
**Goal:** Clean up constructor and initialization

#### Changes:
- Extract bridge initialization to separate method
- Move event subscriptions to service
- Use builder pattern for complex setup

**Estimated Reduction:** ~50 lines

## Implementation Order

### Step 1: Create Keyboard Handler Infrastructure (Today)
1. Create `IKeyboardShortcutHandler` interface
2. Create `KeyboardShortcutDispatcher` service
3. Register in DI container

### Step 2: Extract First Handler (Today)
1. Create `SegmentKeyboardHandler` 
2. Move Ctrl+A, Ctrl+Enter, Esc segment logic
3. Test thoroughly

### Step 3: Extract Remaining Handlers (This Week)
1. `ArticleKeyboardHandler` - article operations
2. `NavigationKeyboardHandler` - arrow keys
3. `FileKeyboardHandler` - file operations

### Step 4: Extract View Services (Next Week)
1. `FolderPickerService`
2. `FullscreenImageService`
3. `ArticleDeletionService`

### Step 5: Complete ViewModel Migration (Next Week)
1. Enhance `MainWindowViewModel`
2. Move remaining logic from code-behind
3. Add proper property change notifications

## Expected Results

### Before:
- MainWindow.axaml.cs: 1,818 lines
- Single God Object doing everything
- Untestable
- Violates SRP, OCP, ISP

### After:
- MainWindow.axaml.cs: ~200-300 lines
- 10+ focused, single-purpose classes
- Fully testable
- Follows SOLID principles
- Much easier to maintain and extend

## Testing Strategy

- Unit test each handler independently
- Mock EditorState, EditorActions
- Test keyboard shortcut combinations
- Integration tests for full workflow

## Effort Estimate

- Phase 1: 16 hours
- Phase 2: 12 hours
- Phase 3: 8 hours
- Phase 4: 4 hours
- **Total: 40 hours**

## Let's Start!

First target: Extract the keyboard handlers to reduce the 899-line method.

