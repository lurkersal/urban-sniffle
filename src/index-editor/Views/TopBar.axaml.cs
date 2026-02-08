using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
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
                        try { Console.WriteLine($"[DEBUG] TopBar.Open: folderName='{folderName}'"); } catch { }
                        var (mag, vol, num) = ParseFolderMetadata(folderName);
                        try { Console.WriteLine($"[DEBUG] TopBar.Open: parsed from folder -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }

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
                                    var old = mag; mag = content.Substring("Magazine:".Length).Trim();
                                    try { Console.WriteLine($"[DEBUG] TopBar.Open: _index.txt header override Magazine: '{old}' -> '{mag}'"); } catch { }
                                }
                                else if (content.StartsWith("Volume:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("Vol:", StringComparison.OrdinalIgnoreCase))
                                {
                                    var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                                    var old = vol; vol = val.Replace("Volume:", string.Empty).Replace("Vol:", string.Empty).Trim();
                                    try { Console.WriteLine($"[DEBUG] TopBar.Open: _index.txt header override Volume: '{old}' -> '{vol}'"); } catch { }
                                }
                                else if (content.StartsWith("Number:", StringComparison.OrdinalIgnoreCase) || content.StartsWith("No:", StringComparison.OrdinalIgnoreCase))
                                {
                                    var val = content.Contains(":") ? content.Substring(content.IndexOf(':') + 1).Trim() : content;
                                    var old = num; num = val.Replace("Number:", string.Empty).Replace("No:", string.Empty).Trim();
                                    try { Console.WriteLine($"[DEBUG] TopBar.Open: _index.txt header override Number: '{old}' -> '{num}'"); } catch { }
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
                        try { Console.WriteLine($"[DEBUG] TopBar.Open: final metadata -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }

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
                    try { Console.WriteLine($"[DEBUG] TopBar.Open: folderName='{folderName}'"); } catch { }
                    var (mag, vol, num) = ParseFolderMetadata(folderName);
                    try { Console.WriteLine($"[DEBUG] TopBar.Open: parsed from folder -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }

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
                    IndexEditor.Shared.ToastService.Show($"Opened folder: {folderName}");
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                }
                catch { IndexEditor.Shared.ToastService.Show("Failed to open folder"); Console.WriteLine("[ERROR] Open folder failed"); }
            });
        }

        // Helper: parse folder basename for metadata (magazine, volume, number).
        // Expected basename formats (examples):
        //   "MagazineName 10-3, 1950"
        //   "Magazine Name Volume 10-3"
        //   "MagazineName Vol10-3"
        // This finds the last 'digits-digits' pair and treats the leading text as the magazine name.
        public static (string mag, string vol, string num) ParseFolderMetadata(string folderName)
        {
            try
            {
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: input folderName='{folderName}'"); } catch { }
                if (string.IsNullOrWhiteSpace(folderName)) return (folderName, "—", "—");
                var mag = folderName;
                var vol = "—";
                var num = "—";

                // Remove trailing year if present (', 1950' or ',1950')
                var yearMatch = Regex.Match(folderName, @",\s*(\d{4})\s*$");
                var nameNoYear = yearMatch.Success ? folderName.Substring(0, yearMatch.Index).Trim() : folderName;
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: nameNoYear='{nameNoYear}' (year stripped={yearMatch.Success})"); } catch { }

                // Find last occurrence of pattern like '10-3' or '10–3' (dash or en-dash)
                var matches = Regex.Matches(nameNoYear, @"(?<vol>\d+)\s*[-–]\s*(?<num>\d+)");
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: dash-pair matches found={matches.Count}"); } catch { }
                if (matches.Count > 0)
                {
                    var m = matches[matches.Count - 1];
                    vol = m.Groups["vol"].Value;
                    num = m.Groups["num"].Value;
                    // Magazine name is the portion before the match
                    mag = nameNoYear.Substring(0, m.Index).Trim();
                    // Clean up trailing punctuation or 'Vol' words
                    mag = Regex.Replace(mag, @"[,_\-\s]+$", "").Trim();
                    mag = Regex.Replace(mag, @"\b(?:vol(?:ume)?|v|issue|no|number)\b\s*$", "", RegexOptions.IgnoreCase).Trim();
                    try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: using dash-pair -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }
                    if (string.IsNullOrWhiteSpace(mag)) mag = folderName;
                    return (mag, vol, num);
                }

                // If no explicit dash pair found, try to catch 'Vol10No3' or 'v10n3' using digits near end
                var m2 = Regex.Match(nameNoYear, @"(?<vol>\d{1,3})\D+(?<num>\d{1,3})\s*$");
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: digits-near-end match success={m2.Success}"); } catch { }
                if (m2.Success)
                {
                    vol = m2.Groups["vol"].Value;
                    num = m2.Groups["num"].Value;
                    mag = nameNoYear.Substring(0, m2.Index).Trim();
                    mag = Regex.Replace(mag, @"[,_\-\s]+$", "").Trim();
                    mag = Regex.Replace(mag, @"\b(?:vol(?:ume)?|v|issue|no|number)\b\s*$", "", RegexOptions.IgnoreCase).Trim();
                    try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: using digits-near-end -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }
                    if (string.IsNullOrWhiteSpace(mag)) mag = folderName;
                    return (mag, vol, num);
                }

                // Fallback: split on underscores or hyphens and pick likely numeric tokens
                var parts = folderName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: fallback parts={string.Join("|", parts)}"); } catch { }
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    if (Regex.IsMatch(parts[i], "^\\d+$"))
                    {
                        if (num == "—") { num = parts[i]; continue; }
                        if (vol == "—") { vol = parts[i]; continue; }
                    }
                }
                try { Console.WriteLine($"[DEBUG] ParseFolderMetadata: fallback -> mag='{mag}', vol='{vol}', num='{num}'"); } catch { }
                // Magazine default as full folderName
                return (mag, vol, num);
            }
            catch { return (folderName, "—", "—"); }
        }
    }
}
