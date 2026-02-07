using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace IndexEditor.Views
{
    public partial class TopBar : UserControl
    {
        public TopBar()
        {
            InitializeComponent();
            var btn = this.FindControl<Button>("SaveIndexBtn");
            var openBtn = this.FindControl<Button>("OpenFolderBtn");
            var magTypeText = this.FindControl<TextBlock>("MagTypeText");
            var volText = this.FindControl<TextBlock>("VolumeText");
            var numText = this.FindControl<TextBlock>("NumberText");
            // Initialize magazine metadata display from EditorState (may be populated by MainWindow when loading _index.txt)
            void RefreshMetadataDisplay()
            {
                try
                {
                    var mag = IndexEditor.Shared.EditorState.CurrentMagazine ?? "—";
                    var vol = IndexEditor.Shared.EditorState.CurrentVolume ?? "—";
                    var num = IndexEditor.Shared.EditorState.CurrentNumber ?? "—";
                    if (magTypeText != null) magTypeText.Text = $"Magazine: {mag}";
                    if (volText != null) volText.Text = $"Vol: {vol}";
                    if (numText != null) numText.Text = $"No: {num}";
                }
                catch { }
            }
            RefreshMetadataDisplay();
            IndexEditor.Shared.EditorState.StateChanged += RefreshMetadataDisplay;

            if (btn != null)
            {
                // Initial enablement
                btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                // Update enablement when state changes
                IndexEditor.Shared.EditorState.StateChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null; } catch { } });

                btn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            IndexEditor.Shared.ToastService.Show("No folder opened; cannot save _index.txt");
                            return;
                        }

                        // Prevent double-save
                        btn.IsEnabled = false;
                        var indexPath = System.IO.Path.Combine(folder, "_index.txt");
                        // Build lines from EditorState.Articles using same formatting as MainWindow.ParseArticleLine reversed
                        var lines = new List<string>();
                        foreach (var a in IndexEditor.Shared.EditorState.Articles)
                        {
                            // Use public PagesText which returns formatted segments like "1|2-3"
                            var pagesText = a.PagesText ?? string.Empty;
                            // Escape commas in fields
                            string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;
                            var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                            var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                            var photographers = (a.Photographers != null && a.Photographers.Count > 0) ? string.Join('|', a.Photographers) : string.Empty;
                            var authors = (a.Authors != null && a.Authors.Count > 0) ? string.Join('|', a.Authors) : string.Empty;
                            var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                            var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(photographers), Escape(authors), Escape(measurements) };
                            var line = string.Join(",", parts);
                            lines.Add(line);
                        }
                        // Prepend metadata header if available
                        var header = new List<string>();
                        if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentMagazine))
                            header.Add($"# Magazine: {IndexEditor.Shared.EditorState.CurrentMagazine}");
                        if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentVolume))
                            header.Add($"# Volume: {IndexEditor.Shared.EditorState.CurrentVolume}");
                        if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentNumber))
                            header.Add($"# Number: {IndexEditor.Shared.EditorState.CurrentNumber}");
                        var outLines = header.Concat(lines).ToArray();

                        // Atomic write: write to temp then replace
                        var tempPath = indexPath + ".tmp";
                        try
                        {
                            System.IO.File.WriteAllLines(tempPath, outLines);
                            // If the target exists, use Replace to be atomic; otherwise move
                            if (System.IO.File.Exists(indexPath))
                            {
                                System.IO.File.Replace(tempPath, indexPath, null);
                            }
                            else
                            {
                                System.IO.File.Move(tempPath, indexPath);
                            }
                            IndexEditor.Shared.ToastService.Show("_index.txt saved");
                        }
                        finally
                        {
                            try { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); } catch { }
                            // re-evaluate enablement based on active segment
                            btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                        }
                    }
                    catch (Exception ex)
                    {
                        IndexEditor.Shared.ToastService.Show("Failed to save _index.txt");
                        Console.WriteLine($"[ERROR] Saving _index.txt failed: {ex.Message}");
                    }
                };
            }
            if (openBtn != null)
            {
                openBtn.Click += async (s, e) =>
                {
                    try
                    {
                        var dlg = new Avalonia.Controls.OpenFolderDialog();
                        var wnd = this.VisualRoot as Window;
                        var path = await dlg.ShowAsync(wnd);
                        if (string.IsNullOrWhiteSpace(path))
                            return;

                        // Clear existing articles before loading new ones
                        IndexEditor.Shared.EditorState.Articles = new List<Common.Shared.ArticleLine>();
                        var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                        if (vm != null)
                        {
                            try { vm.Articles.Clear(); } catch { }
                        }

                        IndexEditor.Shared.EditorState.CurrentFolder = path;

                        // Parse folder name metadata as a fallback
                        var folderName = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                        var parts = folderName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                        string mag = parts.Length > 0 ? parts[0] : folderName;
                        string vol = "—";
                        string num = "—";
                        foreach (var p in parts)
                        {
                            var lower = p.ToLowerInvariant();
                            if (lower.StartsWith("vol") || lower.StartsWith("v")) vol = p;
                            else if (lower.StartsWith("no") || lower.StartsWith("n")) num = p;
                            else if (lower.StartsWith("issue")) num = p;
                            else if (System.Text.RegularExpressions.Regex.IsMatch(p, "^\\d+$"))
                            {
                                if (vol == "—") vol = p; else if (num == "—") num = p;
                            }
                        }

                        // If _index.txt exists in the folder, parse header metadata and article lines
                        var indexPath = System.IO.Path.Combine(path, "_index.txt");
                        if (System.IO.File.Exists(indexPath))
                        {
                            var lines = System.IO.File.ReadAllLines(indexPath);
                            // Header metadata from file overrides folder-derived values
                            foreach (var raw in lines)
                            {
                                var line = raw.Trim();
                                if (!line.StartsWith("#")) break;
                                var content = line.TrimStart('#').Trim();
                                if (content.StartsWith("Magazine:", StringComparison.OrdinalIgnoreCase))
                                {
                                    mag = content.Substring("Magazine:".Length).Trim();
                                }
                                else if (content.StartsWith("Volume:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("Vol:", StringComparison.OrdinalIgnoreCase))
                                {
                                    var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                                    vol = val.Replace("Volume:", string.Empty).Replace("Vol:", string.Empty).Trim();
                                }
                                else if (content.StartsWith("Number:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("No:", StringComparison.OrdinalIgnoreCase))
                                {
                                    var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                                    num = val.Replace("Number:", string.Empty).Replace("No:", string.Empty).Trim();
                                }
                            }

                            // Parse article lines
                            var articles = new List<Common.Shared.ArticleLine>();
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                    continue;
                                var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
                                if (parsed != null)
                                {
                                    articles.Add(parsed);
                                }
                            }

                            IndexEditor.Shared.EditorState.Articles = articles;
                            if (vm != null)
                            {
                                try
                                {
                                    vm.Articles.Clear();
                                    foreach (var a in articles) vm.Articles.Add(a);
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            // no index file; leave articles empty (we already cleared them)
                        }

                        // Update metadata into EditorState and UI
                        IndexEditor.Shared.EditorState.CurrentMagazine = mag;
                        IndexEditor.Shared.EditorState.CurrentVolume = vol;
                        IndexEditor.Shared.EditorState.CurrentNumber = num;
                        if (magTypeText != null) magTypeText.Text = $"Magazine: {mag}";
                        if (volText != null) volText.Text = $"Vol: {vol}";
                        if (numText != null) numText.Text = $"No: {num}";

                        IndexEditor.Shared.ToastService.Show($"Opened folder: {folderName}");
                        IndexEditor.Shared.EditorState.NotifyStateChanged();
                    }
                    catch (System.Exception ex)
                    {
                        IndexEditor.Shared.ToastService.Show("Failed to open folder");
                        System.Console.WriteLine($"[ERROR] Open folder failed: {ex.Message}");
                    }
                };
            }
        }
    }
}
