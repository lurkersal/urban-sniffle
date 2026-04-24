# Index Overlay Pretty Print & Syntax Highlighting Implementation

## Summary

The index overlay (accessed via Ctrl+I) now displays JSON content with **syntax highlighting** and proper formatting when viewing `_index.json` files.

## What Was Implemented

### JSON Syntax Highlighting
- **Editor Component**: AvaloniaEdit TextEditor (replaced basic TextBox)
- **Highlighting**: Custom XSHD (XML Syntax Highlighting Definition) for JSON
- **Line Numbers**: Enabled for easy navigation
- **Monospace Font**: For consistent code alignment

### File: `src/index-editor/Helpers/JsonSyntaxHighlighting.xshd`

Custom syntax highlighting definition with color-coded elements:
- **Property Names** (keys): Red (`#CC3333`)
- **String Values**: Green (`#008000`)  
- **Numbers**: Blue (`#0000FF`)
- **Booleans** (true/false): Blue Bold (`#0000FF`)
- **Null**: Gray Bold (`#808080`)
- **Punctuation** (brackets, braces): Black (`#000000`)

### File: `src/index-editor/MainWindow.axaml`

Updated the overlay to use AvaloniaEdit:
```xml
<avaloniaEdit:TextEditor x:Name="IndexOverlayTextBox" 
                         Grid.Row="2" 
                         ShowLineNumbers="True" 
                         FontFamily="'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace" 
                         FontSize="14"
                         WordWrap="False" />
```

### File: `src/index-editor/MainWindow.axaml.cs`

Added code to load the syntax highlighting from embedded resource:
```csharp
// Load JSON syntax highlighting
using (var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("IndexEditor.Helpers.JsonSyntaxHighlighting.xshd"))
{
    if (stream != null)
    {
        using (var reader = new System.Xml.XmlTextReader(stream))
        {
            textEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.Xshd.HighlightingLoader.Load(
                reader, AvaloniaEdit.Highlighting.HighlightingManager.Instance);
        }
    }
}
```

### File: `src/index-editor/IndexEditor.csproj`

Added the XSHD file as an embedded resource:
```xml
<ItemGroup>
  <EmbeddedResource Include="Helpers\JsonSyntaxHighlighting.xshd" />
</ItemGroup>
```

### File: `src/index-editor/Services/OverlayManager.cs`

**Method: `ShowIndexOverlay()`**
- When loading `_index.json` files, the content is passed through `PrettyPrintJson()` method
- The pretty printer formats JSON with proper indentation using `Utf8JsonWriter`
- If pretty printing fails, the original content is displayed
- Plain text `_index.txt` files are still displayed as-is (no formatting applied)

## User Experience

### Before
- JSON files displayed as compact single-line or minimally formatted
- No syntax highlighting
- Difficult to read nested structures
- No line numbers for reference

### After  
- ✅ **Color-coded syntax** highlighting for instant structure recognition
- ✅ **Line numbers** for easy navigation and reference
- ✅ **Proper indentation** with pretty-printed JSON
- ✅ **Monospace font** for consistent alignment
- ✅ **Professional code editor** experience
- ✅ No performance impact (highlighting is instant)

## Technical Notes

### AvaloniaEdit Integration
- AvaloniaEdit package was already in the project (used for other features)
- Custom XSHD definition provides precise control over syntax highlighting
- Compatible with .NET 8 and Native AOT compilation
- Embedded resource ensures the syntax definition is always available

### Pretty Printing
The `PrettyPrintJson()` method uses `Utf8JsonWriter` for Native AOT compatibility:
1. Parses the JSON using `JsonDocument.Parse()`
2. Writes to a `Utf8JsonWriter` with `Indented = true` option
3. Returns formatted JSON with proper indentation
4. Falls back to original text if parsing fails

**Note**: Using `Utf8JsonWriter` instead of `JsonSerializer.Serialize()` because the latter requires reflection-based serialization, which is disabled in Native AOT builds.

## Future Enhancement Options

If desired, these could be added:
- **Code folding**: Collapse/expand JSON sections
- **Search within JSON**: Filter and highlight search terms
- **JSON validation**: Real-time validation as user edits
- **Dark theme**: Alternative color scheme for syntax highlighting
- **Schema validation**: Validate against expected index.json schema

