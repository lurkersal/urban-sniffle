using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
                    catch { IndexEditor.Shared.ToastService.Show("Failed to save _index.txt"); Console.WriteLine("[ERROR] Saving _index.txt failed"); }
                };
            }
            if (openBtn != null)
            {
                openBtn.Click += async (s, e) =>
                {
                    try
                    {
                        var dlg = new Avalonia.Controls.OpenFolderDialog();
                        // If a folder is already open, prefer opening the dialog there
                        try
                        {
                            var cur = IndexEditor.Shared.EditorState.CurrentFolder;
                            if (!string.IsNullOrWhiteSpace(cur))
                            {
                                var t = dlg.GetType();
                                var prop = t.GetProperty("Directory") ?? t.GetProperty("InitialDirectory") ?? t.GetProperty("DefaultDirectory");
                                if (prop != null && prop.CanWrite)
                                {
                                    try { prop.SetValue(dlg, cur); } catch { }
                                }
                            }
                        }
                        catch { }

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
                        // Parse folder name metadata as a fallback (format: "Magazine name Volume-Number, Year")
                        var folderName = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                        try { Debug.WriteLine("[DEBUG] TopBar.Open: folderName='" + folderName + "'"); } catch { }
                        var parsedFolder = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
                        var mag = parsedFolder.mag;
                        var vol = parsedFolder.vol;
                        var num = parsedFolder.num;
                        try { Debug.WriteLine("[DEBUG] TopBar.Open: parsed from folder -> mag='" + mag + "', vol='" + vol + "', num='" + num + "'"); } catch { }

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
                                    mag = content.Substring("Magazine:".Length).Trim();
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

                            var articles = new List<Common.Shared.ArticleLine>();
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                                var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
                                if (parsed != null) articles.Add(parsed);
                            }

                            IndexEditor.Shared.EditorState.Articles = articles;
                            if (vm != null)
                            {
                                try { vm.Articles.Clear(); foreach (var a in articles) vm.Articles.Add(a); } catch { }
                            }
                        }

                        IndexEditor.Shared.EditorState.CurrentMagazine = mag;
                        IndexEditor.Shared.EditorState.CurrentVolume = vol;
                        IndexEditor.Shared.EditorState.CurrentNumber = num;

                        // Choose the first available image page in the folder (prefer page 1).
                        try
                        {
                            int? firstImage = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(path, 1, 2000);
                            if (!firstImage.HasValue)
                            {
                                // Fallback: if articles have page lists, find the first page among them that has an image
                                var allPages = IndexEditor.Shared.EditorState.Articles?.Where(a => a.Pages != null).SelectMany(a => a.Pages).Distinct().OrderBy(p => p).ToList();
                                if (allPages != null && allPages.Count > 0)
                                {
                                    foreach (var p in allPages)
                                    {
                                        try { if (IndexEditor.Shared.ImageHelper.ImageExists(path, p)) { firstImage = p; break; } } catch { }
                                    }
                                }
                            }
                            if (firstImage.HasValue)
                                IndexEditor.Shared.EditorState.CurrentPage = firstImage.Value;
                            else
                                IndexEditor.Shared.EditorState.CurrentPage = 1;
                        }
                        catch { }

                        IndexEditor.Shared.ToastService.Show($"Opened folder: {folderName}");
                        IndexEditor.Shared.EditorState.NotifyStateChanged();
                    }
                    catch { IndexEditor.Shared.ToastService.Show("Failed to open folder"); System.Console.WriteLine("[ERROR] Open folder failed"); }
                };
            }
        }

        // Public helper to programmatically trigger the Save button logic
        public void TriggerSave()
        {
            try
            {
                var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    IndexEditor.Shared.ToastService.Show("No folder opened; cannot save _index.txt");
                    return;
                }

                var indexPath = System.IO.Path.Combine(folder, "_index.txt");
                var lines = new List<string>();
                foreach (var a in IndexEditor.Shared.EditorState.Articles)
                {
                    string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;
                    var pagesText = a.PagesText ?? string.Empty;
                    var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                    var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                    var photographers = (a.Photographers != null && a.Photographers.Count > 0) ? string.Join('|', a.Photographers) : string.Empty;
                    var authors = (a.Authors != null && a.Authors.Count > 0) ? string.Join('|', a.Authors) : string.Empty;
                    var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                    var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(photographers), Escape(authors), Escape(measurements) };
                    lines.Add(string.Join(",", parts));
                }

                var header = new List<string>();
                if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentMagazine))
                    header.Add($"# Magazine: {IndexEditor.Shared.EditorState.CurrentMagazine}");
                if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentVolume))
                    header.Add($"# Volume: {IndexEditor.Shared.EditorState.CurrentVolume}");
                if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentNumber))
                    header.Add($"# Number: {IndexEditor.Shared.EditorState.CurrentNumber}");

                var outLines = header.Concat(lines).ToArray();
                var tempPath = indexPath + ".tmp";
                try
                {
                    System.IO.File.WriteAllLines(tempPath, outLines);
                    if (System.IO.File.Exists(indexPath))
                        System.IO.File.Replace(tempPath, indexPath, null);
                    else
                        System.IO.File.Move(tempPath, indexPath);
                    IndexEditor.Shared.ToastService.Show("_index.txt saved");
                }
                finally
                {
                    try { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); } catch { }
                }
            }
            catch { IndexEditor.Shared.ToastService.Show("Failed to save _index.txt"); Console.WriteLine("[ERROR] Saving _index.txt failed"); }
        }

        // Public helper to programmatically trigger the Open Folder logic (runs async)
        public void TriggerOpen()
        {
            // Run on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var dlg = new Avalonia.Controls.OpenFolderDialog();
                    // If a folder is already open, prefer opening the dialog there
                    try
                    {
                        var cur = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (!string.IsNullOrWhiteSpace(cur))
                        {
                            var t = dlg.GetType();
                            var prop = t.GetProperty("Directory") ?? t.GetProperty("InitialDirectory") ?? t.GetProperty("DefaultDirectory");
                            if (prop != null && prop.CanWrite)
                            {
                                try { prop.SetValue(dlg, cur); } catch { }
                            }
                        }
                    }
                    catch { }

                    var wnd = this.VisualRoot as Window;
                    var path = await dlg.ShowAsync(wnd);
                    if (string.IsNullOrWhiteSpace(path)) return;

                    // Clear existing articles before loading new ones
                    IndexEditor.Shared.EditorState.Articles = new List<Common.Shared.ArticleLine>();
                    var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                    if (vm != null)
                    {
                        try { vm.Articles.Clear(); } catch { }
                    }

                    IndexEditor.Shared.EditorState.CurrentFolder = path;
                    // Parse folder name metadata as a fallback (format: "Magazine name Volume-Number, Year")
                    var folderName = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
                    try { Debug.WriteLine("[DEBUG] TopBar.Open: folderName='" + folderName + "'"); } catch { }
                    var parsedFolder = IndexEditor.Shared.FolderMetadataParser.ParseFolderMetadata(folderName);
                    var mag = parsedFolder.mag;
                    var vol = parsedFolder.vol;
                    var num = parsedFolder.num;
                    try { Debug.WriteLine("[DEBUG] TopBar.Open: parsed from folder -> mag='" + mag + "', vol='" + vol + "', num='" + num + "'"); } catch { }

                    // If _index.txt exists in the folder, parse header metadata and article lines
                    var indexPath = System.IO.Path.Combine(path, "_index.txt");
                    if (System.IO.File.Exists(indexPath))
                    {
                        var lines = System.IO.File.ReadAllLines(indexPath);
                        foreach (var raw in lines)
                        {
                            var line = raw.Trim();
                            if (!line.StartsWith("#")) break;
                            var content = line.TrimStart('#').Trim();
                            if (content.StartsWith("Magazine:", StringComparison.OrdinalIgnoreCase))
                                mag = content.Substring("Magazine:".Length).Trim();
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

                        var articles = new List<Common.Shared.ArticleLine>();
                        foreach (var line in System.IO.File.ReadAllLines(indexPath))
                        {
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                            var parsed = IndexEditor.Shared.IndexFileParser.ParseArticleLine(line);
                            if (parsed != null) articles.Add(parsed);
                        }

                        IndexEditor.Shared.EditorState.Articles = articles;
                        if (vm != null)
                        {
                            try { vm.Articles.Clear(); foreach (var a in articles) vm.Articles.Add(a); } catch { }
                        }
                    }

                    IndexEditor.Shared.EditorState.CurrentMagazine = mag;
                    IndexEditor.Shared.EditorState.CurrentVolume = vol;
                    IndexEditor.Shared.EditorState.CurrentNumber = num;

                    // Choose the first available image page in the folder (prefer page 1).
                    try
                    {
                        int? firstImage = IndexEditor.Shared.ImageHelper.FindFirstImageInFolder(path, 1, 2000);
                        if (!firstImage.HasValue)
                        {
                            // Fallback: if articles have page lists, find the first page among them that has an image
                            var allPages = IndexEditor.Shared.EditorState.Articles?.Where(a => a.Pages != null).SelectMany(a => a.Pages).Distinct().OrderBy(p => p).ToList();
                            if (allPages != null && allPages.Count > 0)
                            {
                                foreach (var p in allPages)
                                {
                                    try { if (IndexEditor.Shared.ImageHelper.ImageExists(path, p)) { firstImage = p; break; } } catch { }
                                }
                            }
                        }
                        if (firstImage.HasValue)
                            IndexEditor.Shared.EditorState.CurrentPage = firstImage.Value;
                        else
                            IndexEditor.Shared.EditorState.CurrentPage = 1;
                    }
                    catch { }

                    IndexEditor.Shared.ToastService.Show($"Opened folder: {folderName}");
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                }
                catch { IndexEditor.Shared.ToastService.Show("Failed to open folder"); System.Console.WriteLine("[ERROR] Open folder failed"); }
            });
        }
    }
}
