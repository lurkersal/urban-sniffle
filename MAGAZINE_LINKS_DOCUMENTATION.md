# Magazine Links Feature

## Overview

The JSON index file format now supports links to other magazine issues. This feature allows you to reference other magazine issues from specific pages in the current issue.

## JSON Structure

The `links` array is an optional top-level property in the `_index.json` file:

```json
{
  "metadata": { ... },
  "articles": [ ... ],
  "links": [
    {
      "page": 10,
      "magazine": "Club International",
      "volume": "16",
      "issue": "12"
    }
  ]
}
```

## Link Properties

Each link object in the `links` array has the following properties:

- **`page`** (required, integer): The page number in the current issue that references another magazine
- **`magazine`** (required, string): The name of the referenced magazine
- **`volume`** (required, string): The volume number of the referenced magazine
- **`issue`** (required, string): The issue number of the referenced magazine

## Example

Here's a complete example showing how to use magazine links:

```json
{
  "metadata": {
    "magazine": "Club International",
    "volume": "17",
    "number": "1",
    "year": "1988"
  },
  "articles": [
    {
      "pages": [1, 2, 3],
      "category": "Cover",
      "title": "January Cover",
      "modelNames": ["Jane Doe"],
      "ages": [25],
      "contributors": ["John Smith"],
      "measurements": ["36-24-36"]
    }
  ],
  "links": [
    {
      "page": 10,
      "magazine": "Club International",
      "volume": "16",
      "issue": "12"
    },
    {
      "page": 15,
      "magazine": "Club International",
      "volume": "17",
      "issue": "2"
    },
    {
      "page": 22,
      "magazine": "Mayfair",
      "volume": "23",
      "issue": "4"
    }
  ]
}
```

## Usage

### Reading Links

When loading a JSON index file, the links are automatically parsed and returned:

```csharp
var (magazine, volume, number, year, articles, links) = 
    Common.Shared.IndexJsonSerializer.LoadFromJson(folder);

if (links != null && links.Count > 0)
{
    foreach (var link in links)
    {
        Console.WriteLine($"Page {link.Page} references {link.Magazine} V{link.Volume} #{link.Issue}");
    }
}
```

### Saving Links

When saving a JSON index file, pass the links as an optional parameter:

```csharp
var links = new List<MagazineLink>
{
    new MagazineLink
    {
        Page = 10,
        Magazine = "Club International",
        Volume = "16",
        Issue = "12"
    }
};

IndexJsonSerializer.SaveToJson(folder, magazine, volume, number, year, articles, links);
```

## Backward Compatibility

- The `links` property is optional and will be `null` if not present in the JSON file
- Old JSON files without links will continue to work without modification
- When calling `SaveToJson` without the links parameter, no links array will be written to the JSON

## Use Cases

Magazine links can be used for:

1. **Cross-references**: When an article on one page references content in another issue
2. **Series tracking**: When articles span multiple issues
3. **Related content**: When content in one issue relates to or continues from another issue
4. **Advertisements**: When an ad on a page references content in another issue

## Technical Details

### Data Model

The `MagazineLink` class is defined in `Common.Shared.IndexFileModels`:

```csharp
public class MagazineLink
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("magazine")]
    public string Magazine { get; set; } = string.Empty;

    [JsonPropertyName("volume")]
    public string Volume { get; set; } = string.Empty;

    [JsonPropertyName("issue")]
    public string Issue { get; set; } = string.Empty;
}
```

### API Changes

- `IndexJsonSerializer.SaveToJson()`: Added optional `links` parameter
- `IndexJsonSerializer.LoadFromJson()`: Return value now includes `links` as the 6th tuple element

## Migration Notes

Existing code that calls `LoadFromJson` must be updated to handle the new return value:

**Before:**
```csharp
var (mag, vol, num, year, articles) = IndexJsonSerializer.LoadFromJson(folder);
```

**After:**
```csharp
var (mag, vol, num, year, articles, links) = IndexJsonSerializer.LoadFromJson(folder);
// Or discard if not needed:
var (mag, vol, num, year, articles, _) = IndexJsonSerializer.LoadFromJson(folder);
```

