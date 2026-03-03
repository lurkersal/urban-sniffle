using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace IndexEditor.Views;

/// <summary>
/// Custom control for rendering a collapsible JSON tree with syntax highlighting.
/// </summary>
public class JsonTreeViewer : UserControl
{
    private readonly Dictionary<string, bool> _expandedState = new();
    private string _jsonText = string.Empty;
    
    public JsonTreeViewer()
    {
        Background = new SolidColorBrush(Color.Parse("#1E1E1E")); // Dark background
    }

    /// <summary>
    /// Loads JSON text and renders it as a collapsible tree.
    /// </summary>
    public void LoadJson(string jsonText)
    {
        _jsonText = jsonText;
        RenderTree();
    }

    /// <summary>
    /// Expands all nodes in the tree.
    /// </summary>
    public void ExpandAll()
    {
        foreach (var key in _expandedState.Keys.ToList())
        {
            _expandedState[key] = true;
        }
        RenderTree();
    }

    /// <summary>
    /// Collapses all nodes in the tree.
    /// </summary>
    public void CollapseAll()
    {
        foreach (var key in _expandedState.Keys.ToList())
        {
            _expandedState[key] = false;
        }
        RenderTree();
    }

    private void RenderTree()
    {
        try
        {
            using var doc = JsonDocument.Parse(_jsonText);
            var root = doc.RootElement;

            var panel = new StackPanel
            {
                Margin = new Thickness(10),
                Spacing = 2
            };

            RenderElement(panel, root, "", 0);

            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = scrollViewer;
        }
        catch (Exception ex)
        {
            // If parsing fails, show error
            var errorText = new TextBlock
            {
                Text = $"Failed to parse JSON: {ex.Message}",
                Foreground = new SolidColorBrush(Colors.Red),
                Margin = new Thickness(10)
            };
            Content = errorText;
        }
    }

    private void RenderElement(StackPanel parent, JsonElement element, string path, int indent)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                RenderObject(parent, element, path, indent);
                break;
            case JsonValueKind.Array:
                RenderArray(parent, element, path, indent);
                break;
            default:
                // Primitives are rendered by their parent
                break;
        }
    }

    private void RenderObject(StackPanel parent, JsonElement element, string path, int indent)
    {
        foreach (var prop in element.EnumerateObject())
        {
            var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            
            if (!_expandedState.ContainsKey(propPath))
            {
                _expandedState[propPath] = false; // Collapsed by default
            }

            var isExpanded = _expandedState[propPath];

            if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
            {
                // Collapsible property
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(indent * 20, 0, 0, 0)
                };

                var expandButton = new Button
                {
                    Content = isExpanded ? "▼" : "▶",
                    Padding = new Thickness(4, 0, 4, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };

                expandButton.Click += (s, e) =>
                {
                    _expandedState[propPath] = !_expandedState[propPath];
                    RenderTree();
                };

                headerPanel.Children.Add(expandButton);

                // Special formatting for articles
                if (prop.Name == "articles" || IsArticleElement(prop.Value))
                {
                    var summaryText = GetArticleSummary(prop.Value);
                    headerPanel.Children.Add(CreatePropertyName(isExpanded ? prop.Name : summaryText));
                }
                else
                {
                    headerPanel.Children.Add(CreatePropertyName(prop.Name));
                }

                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = isExpanded ? " {" : " {...}",
                        Foreground = new SolidColorBrush(Color.Parse("#808080"))
                    });
                }
                else // Array
                {
                    var count = prop.Value.GetArrayLength();
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = isExpanded ? $" [{count}]" : $" [{count} items]",
                        Foreground = new SolidColorBrush(Color.Parse("#808080"))
                    });
                }

                parent.Children.Add(headerPanel);

                if (isExpanded)
                {
                    RenderElement(parent, prop.Value, propPath, indent + 1);
                    
                    // Closing brace/bracket
                    parent.Children.Add(new TextBlock
                    {
                        Text = prop.Value.ValueKind == JsonValueKind.Object ? "}" : "]",
                        Foreground = new SolidColorBrush(Color.Parse("#808080")),
                        Margin = new Thickness(indent * 20, 0, 0, 0)
                    });
                }
            }
            else
            {
                // Simple property
                var propPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(indent * 20, 0, 0, 0)
                };

                propPanel.Children.Add(CreatePropertyName(prop.Name));
                propPanel.Children.Add(new TextBlock
                {
                    Text = ": ",
                    Foreground = new SolidColorBrush(Color.Parse("#808080"))
                });
                propPanel.Children.Add(CreateValue(prop.Value));

                parent.Children.Add(propPanel);
            }
        }
    }

    private void RenderArray(StackPanel parent, JsonElement element, string path, int indent)
    {
        int index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var itemPath = $"{path}[{index}]";
            
            if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
            {
                if (!_expandedState.ContainsKey(itemPath))
                {
                    _expandedState[itemPath] = false; // Collapsed by default
                }

                var isExpanded = _expandedState[itemPath];

                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(indent * 20, 0, 0, 0)
                };

                var expandButton = new Button
                {
                    Content = isExpanded ? "▼" : "▶",
                    Padding = new Thickness(4, 0, 4, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };

                expandButton.Click += (s, e) =>
                {
                    _expandedState[itemPath] = !_expandedState[itemPath];
                    RenderTree();
                };

                headerPanel.Children.Add(expandButton);

                // Special formatting for article objects
                if (IsArticleElement(item))
                {
                    var summaryText = GetArticleSummary(item);
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = summaryText,
                        Foreground = new SolidColorBrush(Color.Parse("#D4D4D4"))
                    });
                }
                else
                {
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = $"[{index}]",
                        Foreground = new SolidColorBrush(Color.Parse("#9CDCFE"))
                    });
                }

                if (item.ValueKind == JsonValueKind.Object)
                {
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = isExpanded ? " {" : " {...}",
                        Foreground = new SolidColorBrush(Color.Parse("#808080"))
                    });
                }

                parent.Children.Add(headerPanel);

                if (isExpanded)
                {
                    RenderElement(parent, item, itemPath, indent + 1);
                    
                    parent.Children.Add(new TextBlock
                    {
                        Text = "}",
                        Foreground = new SolidColorBrush(Color.Parse("#808080")),
                        Margin = new Thickness(indent * 20, 0, 0, 0)
                    });
                }
            }
            else
            {
                // Simple array item
                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(indent * 20, 0, 0, 0)
                };

                itemPanel.Children.Add(CreateValue(item));

                parent.Children.Add(itemPanel);
            }

            index++;
        }
    }

    private bool IsArticleElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        // Check if it has typical article properties
        return element.TryGetProperty("category", out _) || element.TryGetProperty("title", out _);
    }

    private string GetArticleSummary(JsonElement element)
    {
        var category = "";
        var title = "";

        if (element.TryGetProperty("category", out var catProp))
        {
            category = catProp.GetString() ?? "";
        }

        if (element.TryGetProperty("title", out var titleProp))
        {
            title = titleProp.GetString() ?? "";
        }

        if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(title))
        {
            return $"{category} - {title}";
        }
        else if (!string.IsNullOrEmpty(category))
        {
            return category;
        }
        else if (!string.IsNullOrEmpty(title))
        {
            return title;
        }

        return "(untitled)";
    }

    private TextBlock CreatePropertyName(string name)
    {
        return new TextBlock
        {
            Text = name,
            Foreground = new SolidColorBrush(Color.Parse("#9CDCFE")), // Light blue for property names
            FontWeight = FontWeight.Bold
        };
    }

    private TextBlock CreateValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => new TextBlock
            {
                Text = $"\"{value.GetString()}\"",
                Foreground = new SolidColorBrush(Color.Parse("#CE9178")) // Orange for strings
            },
            JsonValueKind.Number => new TextBlock
            {
                Text = value.GetRawText(),
                Foreground = new SolidColorBrush(Color.Parse("#B5CEA8")) // Green for numbers
            },
            JsonValueKind.True or JsonValueKind.False => new TextBlock
            {
                Text = value.GetBoolean().ToString().ToLower(),
                Foreground = new SolidColorBrush(Color.Parse("#569CD6")) // Blue for booleans
            },
            JsonValueKind.Null => new TextBlock
            {
                Text = "null",
                Foreground = new SolidColorBrush(Color.Parse("#569CD6")) // Blue for null
            },
            _ => new TextBlock
            {
                Text = value.GetRawText(),
                Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")) // Default gray
            }
        };
    }
}


