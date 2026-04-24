# Index-Editor Keyboard Shortcuts Quick Reference

## Editor Mode (text fields have focus)

### Text Editing (Standard Behavior)
| Shortcut | Action |
|----------|--------|
| Ctrl-A | Select All |
| Ctrl-C | Copy |
| Ctrl-V | Paste |
| Ctrl-X | Cut |
| ← → | Move cursor |
| ↑ ↓ | Navigate dropdowns / Move cursor |
| Enter | Submit / Next field |
| Delete | Delete text |
| Backspace | Delete character |

### Application Commands (Override Editor)
| Shortcut | Action |
|----------|--------|
| **ESC** | **Exit editor mode** |
| **Ctrl-S** | **Save index file** |
| **Ctrl-O** | **Open folder** |

---

## Application Mode (editor not focused)

### File Operations
| Shortcut | Action |
|----------|--------|
| Ctrl-O | Open folder |
| Ctrl-S | Save index file |
| Ctrl-I | Toggle JSON index overlay |

### Article Management
| Shortcut | Action |
|----------|--------|
| Ctrl-N | New article (focuses title field) |
| Ctrl-D | Delete selected article |
| Delete | Delete selected article |
| Ctrl-↑ | Previous article |
| Ctrl-↓ | Next article |
| ↑ ↓ | Navigate article list |

### Segment Management
| Shortcut | Action |
|----------|--------|
| **Ctrl-A** | **Add segment at current page** |
| Enter | End active segment |
| Ctrl-Enter | End segment or focus title |
| ESC | Cancel active segment |

### Page Navigation
| Shortcut | Action |
|----------|--------|
| ← | Previous page |
| → | Next page |

### Other
| Shortcut | Action |
|----------|--------|
| F11 | Toggle fullscreen |
| F1 | Show help overlay |
| Ctrl-B | Check Babepedia for model |
| ESC | Close overlay / Cancel segment |

---

## Context Switcher

### How to Switch Between Modes

**Enter Editor Mode**:
- Click on any text field in the article editor
- Press Ctrl-N to create new article (auto-focuses title)
- Press Enter to focus editor from page controller

**Exit Editor Mode**:
- Press **ESC** (returns focus to article list)
- Click elsewhere (article list, page controller, etc.)

---

## Tips

💡 **When in doubt**: Press ESC to exit editor mode, then your shortcuts will work

💡 **Quick save**: Ctrl-S works everywhere (even while editing)

💡 **Add segments**: Must exit editor mode first (press ESC), then Ctrl-A

💡 **Text selection**: Ctrl-A works normally in text fields (no need to worry!)

💡 **Page navigation**: Arrow keys work for navigation only when NOT editing text

---

## Quick Workflow Examples

### Create New Article
1. `Ctrl-N` → New article created, title field focused
2. Type title
3. `Tab` to next field
4. Fill in details
5. `ESC` when done → Returns to application mode

### Add Segment to Existing Article
1. Select article in list
2. Navigate to starting page with arrow keys
3. `Ctrl-A` → Start segment
4. Navigate to ending page with arrow keys
5. `Enter` → End segment

### Edit Article Then Save
1. Click article in list to select
2. Click on a text field in editor
3. Edit the text (use Ctrl-A, Ctrl-C/V, arrows, etc.)
4. `Ctrl-S` → Save (works even while editing!)
5. `ESC` → Return to application mode

---

**Version**: March 20, 2026  
**Feature**: Context-Aware Keyboard Handling

