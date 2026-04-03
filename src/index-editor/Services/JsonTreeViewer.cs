using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace IndexEditor.Services;

/// <summary>
/// A collapsible JSON tree viewer control.
/// </summary>
public class JsonTreeViewer : StackPanel
{
    private static readonly SolidColorBrush KeyBrush = new(Color.Parse("#0451A5"));
    private static readonly SolidColorBrush StringBrush = new(Color.Parse("#D14"));
    private static readonly SolidColorBrush NumberBrush = new(Color.Parse("#09885A"));
    private static readonly SolidColorBrush BoolNullBrush = new(Color.Parse("#0000FF"));
    private static readonly SolidColorBrush PunctuationBrush = new(Color.Parse("#000000"));
    private static readonly SolidColorBrush CollapseIndicatorBrush = new(Color.Parse("#666666"));
    
    private List<JsonTreeNode> _rootNodes = new();
    
    public JsonTreeViewer()
    {
        Orientation = Orientation.Vertical;
    }
    
    /// <summary>
    /// Load and display JSON from text.
    /// </summary>
    public void LoadJson(string jsonText)
    {
        Children.Clear();
        _rootNodes = JsonTreeNode.ParseJson(jsonText);
        
        foreach (var node in _rootNodes)
        {
            RenderNode(node);
        }
    }
    
    private void RenderNode(JsonTreeNode node)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        
        // Indentation
        if (node.IndentLevel > 0)
        {
            panel.Children.Add(new TextBlock 
            { 
                Text = new string(' ', node.IndentLevel * 2),
                FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace")
            });
        }
        
        // Collapse/expand indicator for objects and arrays
        if (node.Type == JsonNodeType.Object || node.Type == JsonNodeType.Array)
        {
            var indicator = new TextBlock
            {
                Text = node.IsExpanded ? "▼ " : "▶ ",
                Foreground = CollapseIndicatorBrush,
                FontSize = 12,
                Cursor = new Cursor(StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            indicator.PointerPressed += (s, e) =>
            {
                node.IsExpanded = !node.IsExpanded;
                RefreshDisplay();
            };
            
            panel.Children.Add(indicator);
        }
        else
        {
            // Add spacing for leaf nodes to align with collapsible items
            panel.Children.Add(new TextBlock 
            { 
                Text = "  ",
                FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace")
            });
        }
        
        // Key (if present)
        if (!string.IsNullOrEmpty(node.Key))
        {
            var keyBlock = new TextBlock
            {
                FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace"),
                FontSize = 14
            };
            keyBlock.Inlines.Add(new Run($"\"{node.Key}\"") { Foreground = KeyBrush });
            keyBlock.Inlines.Add(new Run(": ") { Foreground = PunctuationBrush });
            panel.Children.Add(keyBlock);
        }
        
        // Value rendering based on type
        var valueBlock = new TextBlock
        {
            FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace"),
            FontSize = 14
        };
        
        switch (node.Type)
        {
            case JsonNodeType.Object:
                var objCount = node.Children.Count;
                string objPreview;
                
                if (node.IsExpanded)
                {
                    objPreview = "{";
                }
                else
                {
                    // Check if this is an article object (has category and optionally title)
                    var categoryNode = node.Children.FirstOrDefault(c => c.Key == "category");
                    var titleNode = node.Children.FirstOrDefault(c => c.Key == "title");
                    
                    // Show preview if category exists (title is optional)
                    if (categoryNode != null && !string.IsNullOrWhiteSpace(categoryNode.Value))
                    {
                        var title = titleNode != null && !string.IsNullOrWhiteSpace(titleNode.Value)
                            ? titleNode.Value
                            : "(no title)";
                        
                        // Display "Category - Title" or "Category - (no title)" for collapsed articles
                        objPreview = $"{{ {categoryNode.Value} - {title} }}";
                    }
                    else
                    {
                        objPreview = $"{{ ... {objCount} {(objCount == 1 ? "property" : "properties")} }}";
                    }
                }
                
                valueBlock.Inlines.Add(new Run(objPreview) { Foreground = PunctuationBrush });
                break;
                
            case JsonNodeType.Array:
                var arrCount = node.Children.Count;
                var arrPreview = node.IsExpanded ? "[" : $"[ ... {arrCount} {(arrCount == 1 ? "item" : "items")} ]";
                valueBlock.Inlines.Add(new Run(arrPreview) { Foreground = PunctuationBrush });
                break;
                
            case JsonNodeType.String:
                valueBlock.Inlines.Add(new Run($"\"{node.Value}\"") { Foreground = StringBrush });
                break;
                
            case JsonNodeType.Number:
                valueBlock.Inlines.Add(new Run(node.Value) { Foreground = NumberBrush });
                break;
                
            case JsonNodeType.Boolean:
            case JsonNodeType.Null:
                valueBlock.Inlines.Add(new Run(node.Value) { Foreground = BoolNullBrush });
                break;
        }
        
        panel.Children.Add(valueBlock);
        Children.Add(panel);
        
        // Render children if expanded
        if (node.IsExpanded && node.Children.Count > 0)
        {
            foreach (var child in node.Children)
            {
                RenderNode(child);
            }
            
            // Closing brace/bracket
            if (node.Type == JsonNodeType.Object || node.Type == JsonNodeType.Array)
            {
                var closingPanel = new StackPanel { Orientation = Orientation.Horizontal };
                
                if (node.IndentLevel > 0)
                {
                    closingPanel.Children.Add(new TextBlock 
                    { 
                        Text = new string(' ', node.IndentLevel * 2),
                        FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace")
                    });
                }
                
                closingPanel.Children.Add(new TextBlock 
                { 
                    Text = node.Type == JsonNodeType.Object ? "}" : "]",
                    FontFamily = new FontFamily("'Ubuntu Mono', 'DejaVu Sans Mono', 'Courier New', monospace"),
                    FontSize = 14,
                    Foreground = PunctuationBrush
                });
                
                Children.Add(closingPanel);
            }
        }
    }
    
    private void RefreshDisplay()
    {
        Children.Clear();
        foreach (var node in _rootNodes)
        {
            RenderNode(node);
        }
    }
    
    /// <summary>
    /// Expand all nodes.
    /// </summary>
    public void ExpandAll()
    {
        SetExpansionRecursive(_rootNodes, true);
        RefreshDisplay();
    }
    
    /// <summary>
    /// Collapse all nodes.
    /// </summary>
    public void CollapseAll()
    {
        SetExpansionRecursive(_rootNodes, false);
        RefreshDisplay();
    }
    
    private void SetExpansionRecursive(List<JsonTreeNode> nodes, bool expanded)
    {
        foreach (var node in nodes)
        {
            if (node.Type == JsonNodeType.Object || node.Type == JsonNodeType.Array)
            {
                node.IsExpanded = expanded;
                SetExpansionRecursive(node.Children, expanded);
            }
        }
    }
}

