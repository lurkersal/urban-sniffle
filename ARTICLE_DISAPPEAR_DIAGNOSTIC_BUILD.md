# Article Disappearing - DIAGNOSTIC BUILD

## Status
Despite 4 attempted fixes, the article is STILL disappearing after ending a segment.

## Diagnostic Build Created

I've added extensive logging to track EXACTLY what's happening with `SelectedArticle`.

### What Was Added

**File:** `src/index-editor/Views/EditorStateViewModel.cs`

#### 1. SelectedArticle Setter Entry Logging
```csharp
try { DebugLogger.Log($"==> SelectedArticle SETTER CALLED: incoming='{value?.Title}', current='{_selectedArticle?.Title}', stacktrace={Environment.StackTrace}"); } catch { }
```

Logs:
- When setter is called
- What value is being set
- What the current value is
- Full stack trace to see WHO is calling it

#### 2. Setter Guard Logging
```csharp
try { DebugLogger.Log($"==> SelectedArticle SETTER: BLOCKED - active segment prevents selection change"); } catch { }
try { DebugLogger.Log($"==> SelectedArticle SETTER: IGNORING transient null (current article still in collection)"); } catch { }
```

Logs when setter guards block changes.

#### 3. Value Change Logging
```csharp
try { DebugLogger.Log($"==> SelectedArticle CHANGING: FROM '{_selectedArticle?.Title}' TO '{incoming?.Title}'"); } catch {}
_selectedArticle = incoming;
try { DebugLogger.Log($"==> SelectedArticle CHANGED: _selectedArticle is now '{_selectedArticle?.Title}', IsNull={_selectedArticle == null}"); } catch {}
```

Logs before and after the actual change.

#### 4. OnEditorStateChanged Logging
```csharp
try { DebugLogger.Log($"==> OnEditorStateChanged CALLED: SelectedArticle is currently '{_selectedArticle?.Title}'"); } catch { }
// ... work ...
try { DebugLogger.Log($"==> OnEditorStateChanged COMPLETED: SelectedArticle is still '{_selectedArticle?.Title}'"); } catch { }
```

Logs when state change notifications happen.

## How to Use This Build

### Step 1: Run the Application
```bash
cd /home/justin/repos/urban-sniffle
dotnet run --project src/index-editor/IndexEditor.csproj
```

### Step 2: Reproduce the Bug
1. Open a folder
2. Select an article (note which one)
3. Start a segment (Ctrl+A)
4. Navigate a few pages
5. **End the segment (Enter)**
6. **Article disappears**

### Step 3: Check the Log

**The log file is located at:** `/tmp/index-editor-debug.log`

Look for entries starting with `==>`:

```bash
grep "==>" /tmp/index-editor-debug.log | tail -50
```

### What to Look For

#### If Article Becomes Null:
```
==> SelectedArticle SETTER CALLED: incoming='null', current='Article Title'
==> SelectedArticle CHANGING: FROM 'Article Title' TO 'null'
==> SelectedArticle CHANGED: _selectedArticle is now 'null', IsNull=True
```

This means something is explicitly setting SelectedArticle to null.

**The stack trace will show WHO is doing it!**

#### If Setter Is Called Multiple Times:
```
==> SelectedArticle SETTER CALLED: incoming='Article Title', current='Article Title'
==> SelectedArticle SETTER CALLED: incoming='null', current='Article Title'
==> SelectedArticle SETTER CALLED: incoming='Different Article', current='Article Title'
```

Multiple calls might indicate binding loops or cascading updates.

#### If Setter Guard Blocks:
```
==> SelectedArticle SETTER: IGNORING transient null (current article still in collection)
```

The guard is working but maybe something is bypassing it.

#### If OnEditorStateChanged Is Suspicious:
```
==> OnEditorStateChanged CALLED: SelectedArticle is currently 'Article Title'
==> SelectedArticle SETTER CALLED: incoming='null', current='Article Title'
```

If setter is called RIGHT after OnEditorStateChanged, there's still a connection.

## Expected Findings

### Scenario A: Something Sets SelectedArticle to Null
**Log shows:**
- `SETTER CALLED: incoming='null'`
- Stack trace points to the culprit

**Action:** Fix the code that's setting it to null

### Scenario B: Setter Is Never Called
**Log shows:**
- No setter calls around when article disappears

**Action:** Problem is in the UI/XAML binding layer, not ViewModel

### Scenario C: Setter Is Called with Same Value
**Log shows:**
- `SETTER CALLED: incoming='Article Title', current='Article Title'`
- But article still disappears

**Action:** Problem is in how PropertyChanged notification is being handled

### Scenario D: Articles Collection Is Modified
**Log shows:**
- Setter called with different article object (different reference)

**Action:** Collection is being replaced/modified, losing the reference

## Build Status
✅ **Build Successful** - 0 Errors

## Next Steps

1. **Run this diagnostic build**
2. **Reproduce the bug** (end a segment, article disappears)
3. **Check the log:** `grep "==>" /tmp/index-editor-debug.log | tail -50`
4. **Share the log output** with me
5. **I'll analyze it** and identify the ACTUAL root cause

## Why This Will Work

All previous fixes were based on **assumptions**:
- v1: Assumed timing issue
- v2: Assumed selection lost during reordering
- v3: Assumed recursive calls
- v4: Assumed fake PropertyChanged

**This approach:** No assumptions. The log will show EXACTLY what happens.

## Log Analysis Commands

```bash
# See last 50 diagnostic entries
grep "==>" /tmp/index-editor-debug.log | tail -50

# See all SelectedArticle setter calls
grep "SETTER CALLED" /tmp/index-editor-debug.log

# See all state changes
grep "OnEditorStateChanged" /tmp/index-editor-debug.log

# See when article becomes null
grep "IsNull=True" /tmp/index-editor-debug.log

# Full context around article disappearing (after you note the timestamp)
tail -200 /tmp/index-editor-debug.log | grep -A 5 -B 5 "SETTER CALLED"
```

## Promise

Once I see the actual log output showing what happens when the article disappears, I will be able to identify and fix the REAL root cause. No more guessing!

