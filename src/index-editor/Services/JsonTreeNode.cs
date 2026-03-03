using System;
using System.Collections.Generic;
using System.Text.Json;

namespace IndexEditor.Services;

/// <summary>
/// Represents a node in a JSON tree structure for collapsible display.
/// </summary>
public class JsonTreeNode
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public JsonNodeType Type { get; set; }
    public List<JsonTreeNode> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = true;
    public int IndentLevel { get; set; }
    
    /// <summary>
    /// Creates a tree structure from JSON text.
    /// </summary>
    public static List<JsonTreeNode> ParseJson(string jsonText)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var nodes = new List<JsonTreeNode>();
            ParseElement(doc.RootElement, nodes, 0, null);
            return nodes;
        }
        catch (JsonException)
        {
            // Return empty list on parse error
            return new List<JsonTreeNode>();
        }
    }
    
    private static void ParseElement(JsonElement element, List<JsonTreeNode> nodes, int indentLevel, string? key)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var objNode = new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Type = JsonNodeType.Object,
                    IndentLevel = indentLevel,
                    IsExpanded = indentLevel < 2 // Auto-expand first 2 levels
                };
                
                foreach (var prop in element.EnumerateObject())
                {
                    ParseElement(prop.Value, objNode.Children, indentLevel + 1, prop.Name);
                }
                
                nodes.Add(objNode);
                break;
                
            case JsonValueKind.Array:
                var arrayNode = new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Type = JsonNodeType.Array,
                    IndentLevel = indentLevel,
                    IsExpanded = indentLevel < 2
                };
                
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ParseElement(item, arrayNode.Children, indentLevel + 1, $"[{index}]");
                    index++;
                }
                
                nodes.Add(arrayNode);
                break;
                
            case JsonValueKind.String:
                nodes.Add(new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Value = element.GetString() ?? string.Empty,
                    Type = JsonNodeType.String,
                    IndentLevel = indentLevel
                });
                break;
                
            case JsonValueKind.Number:
                nodes.Add(new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Value = element.GetRawText(),
                    Type = JsonNodeType.Number,
                    IndentLevel = indentLevel
                });
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                nodes.Add(new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Value = element.GetBoolean().ToString().ToLower(),
                    Type = JsonNodeType.Boolean,
                    IndentLevel = indentLevel
                });
                break;
                
            case JsonValueKind.Null:
                nodes.Add(new JsonTreeNode
                {
                    Key = key ?? string.Empty,
                    Value = "null",
                    Type = JsonNodeType.Null,
                    IndentLevel = indentLevel
                });
                break;
        }
    }
}

public enum JsonNodeType
{
    Object,
    Array,
    String,
    Number,
    Boolean,
    Null
}

