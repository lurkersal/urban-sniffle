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
                        try
                        {
                            IndexEditor.Shared.IndexSaver.SaveIndex(folder);
                            IndexEditor.Shared.ToastService.Show("_index.txt saved");
                        }
                        catch (Exception ex)
                        {
                            IndexEditor.Shared.ToastService.Show("Failed to save _index.txt");
                            IndexEditor.Shared.DebugLogger.LogException("TopBar: save _index.txt", ex);
                        }
                        finally
                        {
                            // re-evaluate enablement based on active segment
                            btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                        }
                    }
                    catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: save click outer", ex); }
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
            // Debug button removed from XAML; no debug wiring required.
        }
    }
}
