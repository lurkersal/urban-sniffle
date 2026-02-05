
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace IndexEditor;

public partial class MainWindow : Window
{
    public string? FolderToOpen { get; }

    public MainWindow() : this(null) { }

    public MainWindow(string? folderToOpen = null)
    {
        FolderToOpen = folderToOpen;
        InitializeComponent();
        this.DataContext = new IndexEditor.Views.EditorStateViewModel();

        if (!string.IsNullOrWhiteSpace(FolderToOpen))
        {
            var indexPath = System.IO.Path.Combine(FolderToOpen, "_index.txt");
            if (System.IO.File.Exists(indexPath))
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(indexPath);
                    var articles = new List<Common.Shared.ArticleLine>();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;
                        var parsed = ParseArticleLine(line);
                        if (parsed != null)
                        {
                            articles.Add(parsed);
                            System.Console.WriteLine($"[DEBUG] Parsed article: Category='{parsed.Category}', Title='{parsed.Title}', Pages=[{string.Join(",", parsed.Pages)}]");
                        }
                        else
                        {
                            System.Console.WriteLine($"[DEBUG] Skipped line: '{line}'");
                        }
                    }
                    System.Console.WriteLine($"[DEBUG] Total articles parsed: {articles.Count}");
                    IndexEditor.Shared.EditorState.Articles = articles;
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error reading _index.txt: {ex.Message}");
                }
            }
        }
    }

    // Minimal parser based on ContentLineParser from magazine-parser
    private Common.Shared.ArticleLine? ParseArticleLine(string line)
    {
        var parts = SplitRespectingEscapedCommas(line);
        if (parts.Count < 2)
            return null;
        var article = new Common.Shared.ArticleLine();
        // Pages
        article.Pages = ParsePageNumbers(parts[0], out bool hasError);
        article.HasPageNumberError = hasError;
        if (article.Pages.Count == 0)
            return null;
        // Category
        article.Category = parts.Count > 1 ? parts[1] : "";
        if (string.IsNullOrWhiteSpace(article.Category))
            return null;
        if (article.Category.Equals("Contents", System.StringComparison.OrdinalIgnoreCase))
            article.Category = "Index";
        // Title
        article.Title = parts.Count > 2 ? parts[2] : "";
        // ModelNames
        if (parts.Count > 3)
            article.ModelNames = parts[3].Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();
        // Ages
        if (parts.Count > 4)
        {
            var ageParts = parts[4].Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
            var ages = new List<int?>();
            foreach (var ap in ageParts)
            {
                if (int.TryParse(ap, out int a))
                    ages.Add(a);
                else
                    ages.Add(null);
            }
            article.Ages = ages;
        }
        // Photographers
        if (parts.Count > 5 && !string.IsNullOrWhiteSpace(parts[5]))
            article.Photographers = parts[5].Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();
        // Measurements
        if (parts.Count > 6)
            article.Measurements = parts[6].Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();
        return article;
    }

    private List<string> SplitRespectingEscapedCommas(string line)
    {
        var parts = new List<string>();
        var currentPart = new System.Text.StringBuilder();
        bool escaped = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (escaped)
            {
                currentPart.Append(c);
                escaped = false;
            }
            else if (c == '\\' && i + 1 < line.Length)
            {
                escaped = true;
            }
            else if (c == ',')
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(c);
            }
        }
        parts.Add(currentPart.ToString().Trim());
        return parts;
    }

    private List<int> ParsePageNumbers(string pageStr, out bool hasError)
    {
        var pages = new List<int>();
        hasError = false;
        if (string.IsNullOrWhiteSpace(pageStr))
        {
            hasError = true;
            return pages;
        }
        var parts = pageStr.Split('|');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Contains("-"))
            {
                var range = trimmed.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                        pages.Add(i);
                }
                else
                {
                    hasError = true;
                }
            }
            else if (int.TryParse(trimmed, out int singlePage))
            {
                pages.Add(singlePage);
            }
            else
            {
                hasError = true;
            }
        }
        return pages;
    }
}