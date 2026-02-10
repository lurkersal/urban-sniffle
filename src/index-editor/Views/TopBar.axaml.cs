using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;
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
                catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar.RefreshMetadataDisplay", ex); }
            }
            RefreshMetadataDisplay();
            IndexEditor.Shared.EditorState.StateChanged += RefreshMetadataDisplay;

            if (btn != null)
            {
                // Initial enablement
                btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                // Update enablement when state changes
                IndexEditor.Shared.EditorState.StateChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null; } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: update btn.IsEnabled", ex); } });

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
                        // Escape helper for commas (uses same convention as reading code)
                        string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;
                        foreach (var a in IndexEditor.Shared.EditorState.Articles)
                        {
                            // Use public PagesText which returns formatted segments like "1|2-3"
                            var pagesText = a.PagesText ?? string.Empty;
                            var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                            var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                            var photographers = (a.Photographers != null && a.Photographers.Count > 0) ? string.Join('|', a.Photographers) : string.Empty;
                            var authors = (a.Authors != null && a.Authors.Count > 0) ? string.Join('|', a.Authors) : string.Empty;
                            var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                            var parts = new List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(photographers), Escape(authors), Escape(measurements) };
                            var line = string.Join(",", parts);
                            lines.Add(line);
                        }
                        // Compose output lines: the first non-comment line must be the CSV metadata (Magazine,Volume,Number) if available
                        var outLinesList = new List<string>();
                        if (!string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentMagazine) || !string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentVolume) || !string.IsNullOrWhiteSpace(IndexEditor.Shared.EditorState.CurrentNumber))
                        {
                            // Write an uncommented CSV metadata line as the first non-comment line
                            var metaParts = new[] { Escape(IndexEditor.Shared.EditorState.CurrentMagazine ?? string.Empty), Escape(IndexEditor.Shared.EditorState.CurrentVolume ?? string.Empty), Escape(IndexEditor.Shared.EditorState.CurrentNumber ?? string.Empty) };
                            outLinesList.Add(string.Join(",", metaParts));
                        }
                        // Do not write legacy commented headers; metadata is represented by the first CSV line only.
                        // Append article lines
                        outLinesList.AddRange(lines);
                        var outLines = outLinesList.ToArray();

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
                            try { if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: delete temp file", ex); }
                            // re-evaluate enablement based on active segment
                            btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                        }
                    }
                    catch (Exception ex) { IndexEditor.Shared.ToastService.Show("Failed to save _index.txt"); IndexEditor.Shared.DebugLogger.LogException("TopBar: save _index.txt", ex); }
                };
            }
            if (openBtn != null)
            {
                openBtn.Click += async (s, e) =>
                {
                    try
                    {
                        // Use custom folder browser window that lists only folders and selects a folder when it has no subfolders on double-click
                        var wnd = this.VisualRoot as Window;
                        var start = IndexEditor.Shared.EditorState.CurrentFolder;
                        string? path = null;
                        try
                        {
                            path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
                        }
                        catch (Exception ex)
                        {
                            try { IndexEditor.Shared.ToastService.Show("Open folder dialog failed: " + ex.Message); } catch (Exception tex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: ToastService.Show open folder failed", tex); }
                            IndexEditor.Shared.DebugLogger.LogException("TopBar: FolderPicker failed", ex);
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(path))
                            return;

                        // Clear existing articles before loading new ones
                        IndexEditor.Shared.EditorState.Articles = new List<Common.Shared.ArticleLine>();
                        var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                        if (vm != null)
                        {
                            try { vm.Articles.Clear(); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: clear vm.Articles", ex); }
                        }

                        IndexEditor.Shared.EditorState.CurrentFolder = path;
                        // Reuse existing MainWindow-loading logic by delegating to MainWindow.LoadArticlesFromFolder if available.
                        try
                        {
                            var main = this.VisualRoot as MainWindow;
                            if (main != null)
                            {
                                // Use reflection to call LoadArticlesFromFolder in case its protection level changes
                                var mi = typeof(MainWindow).GetMethod("LoadArticlesFromFolder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                if (mi != null)
                                {
                                    mi.Invoke(main, new object[] { path });
                                    return;
                                }
                            }
                        }
                        catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: reflection invoke LoadArticlesFromFolder", ex); }

                        // As an absolute fallback, set the CurrentFolder and notify state so the MainWindow may react elsewhere
                        IndexEditor.Shared.EditorState.CurrentFolder = path;
                        try { IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: NotifyStateChanged fallback", ex); }
                    }
                    catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: Open button click handler outer", ex); }
                };
            }
            try
            {
                var debugBtn = this.FindControl<Button>("DebugFlashBtn");
                if (debugBtn != null)
                {
                    debugBtn.Click += (s, e) =>
                    {
                        try
                        {
                            var wnd = this.VisualRoot as MainWindow;
                            // Try to find the ArticleEditor instance and call its TriggerOverlayFlash
                            var ae = wnd?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditorControl") ?? wnd?.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                            if (ae != null)
                            {
                                System.Console.WriteLine("[DEBUG] TopBar.DebugFlashBtn: triggering persistent overlay on ArticleEditor");
                                ae.TriggerOverlayFlash(true);
                                try { ae.FocusEditor(); } catch (Exception ex) { DebugLogger.LogException("TopBar.DebugFlashBtn: ae.FocusEditor", ex); }
                                return;
                            }
                            // As fallback, find any ArticleEditor in visual tree
                            try
                            {
                                var any = this.FindControl<IndexEditor.Views.ArticleEditor>("ArticleEditor");
                                if (any != null) { any.TriggerOverlayFlash(true); try { any.FocusEditor(); } catch (Exception ex) { DebugLogger.LogException("TopBar.DebugFlashBtn: any.FocusEditor", ex); } return; }
                            }
                            catch (Exception ex) { DebugLogger.LogException("TopBar.DebugFlashBtn: find any ArticleEditor", ex); }
                            System.Console.WriteLine("[DEBUG] TopBar.DebugFlashBtn: ArticleEditor instance not found to trigger overlay");
                        }
                        catch (Exception ex) { DebugLogger.LogException("TopBar.DebugFlashBtn: exception on click", ex); }
                    };
                }
            }
            catch (Exception ex) { DebugLogger.LogException("TopBar ctor: debugBtn wiring", ex); }
        }
    }
}
