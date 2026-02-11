using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using IndexEditor.Shared;

namespace IndexEditor.Views
{
    public partial class ArticleEditor : UserControl
    {
        public ArticleEditor()
        {
            InitializeComponent();
            // When the global EditorState changes, if the ActiveArticle matches our DataContext, focus the category combobox.
            IndexEditor.Shared.EditorState.StateChanged += OnEditorStateChanged;
            // Also listen for explicit focus requests initiated by the window/key handlers
            IndexEditor.Shared.EditorState.StateChanged += OnEditorStateFocusRequested;
            this.DetachedFromVisualTree += (s, e) =>
            {
                try { IndexEditor.Shared.EditorState.StateChanged -= OnEditorStateChanged; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.DetachedFromVisualTree: remove OnEditorStateChanged", ex); }
                try { IndexEditor.Shared.EditorState.StateChanged -= OnEditorStateFocusRequested; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.DetachedFromVisualTree: remove OnEditorStateFocusRequested", ex); }
            };

            // Track focus within the article editor so global key handlers can avoid intercepting arrow keys
            this.GotFocus += (s, e) => { try { IndexEditor.Shared.EditorState.IsArticleEditorFocused = true; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.GotFocus: set IsArticleEditorFocused", ex); } };
            this.LostFocus += (s, e) => { try { IndexEditor.Shared.EditorState.IsArticleEditorFocused = false; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.LostFocus: clear IsArticleEditorFocused", ex); } };
        }

        private int _lastFocusRequest = 0;
        private void OnEditorStateFocusRequested()
        {
            try
            {
                var current = IndexEditor.Shared.EditorState.ArticleEditorFocusRequest;
                if (current == _lastFocusRequest) return;
                _lastFocusRequest = current;
                DebugLogger.Log($"[DEBUG] ArticleEditor: focus request received (count={current})");
                // Schedule a UI thread attempt to focus the title textbox
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try { FocusTitle(); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateFocusRequested: FocusTitle", ex); }
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateFocusRequested: outer", ex); }
        }

        // Public helper to focus the category combobox in the editor
        public void FocusEditor()
        {
            try
            {
                DebugLogger.Log("[DEBUG] ArticleEditor.FocusEditor: called");
                // Prefer searching inside the ContentControl that hosts the DataTemplate
                try
                {
                    var host = this.FindControl<ContentControl>("EditorContent");
                    if (host != null)
                    {
                        // The DataTemplate content is hosted in host.Content. Search within that content first.
                        if (host.Content is Avalonia.Controls.Control hostContent)
                        {
                            var tb = hostContent.FindControl<TextBox>("TitleTextBox");
                            if (tb != null) { tb.Focus(); FlashControl(tb); DebugLogger.Log("[FOCUS OK] ArticleEditor.FocusEditor: TitleTextBox focused via hostContent"); return; }
                            var cb = hostContent.FindControl<ComboBox>("CategoryComboBox");
                            DebugLogger.Log(cb == null ? "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox not found in host.Content" : "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox found in host.Content");
                            if (cb != null) { cb.Focus(); FlashControl(cb); DebugLogger.Log("[FOCUS OK] ArticleEditor.FocusEditor: CategoryComboBox focused via hostContent"); return; }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusEditor: search in host.Content", ex); }

                // Fallback: search entire visual tree of this control
                var fallbackTitle = this.FindControl<TextBox>("TitleTextBox");
                if (fallbackTitle != null)
                {
                    DebugLogger.Log("[DEBUG] ArticleEditor.FocusEditor: found TitleTextBox via fallback");
                    fallbackTitle.Focus();
                    FlashControl(fallbackTitle);
                    DebugLogger.Log("[FOCUS OK] ArticleEditor.FocusEditor: TitleTextBox focused via fallback");
                    return;
                }
                var fallback = this.FindControl<ComboBox>("CategoryComboBox");
                if (fallback != null)
                {
                    DebugLogger.Log("[DEBUG] ArticleEditor.FocusEditor: found CategoryComboBox via fallback");
                    fallback.Focus();
                    FlashControl(fallback);
                    DebugLogger.Log("[FOCUS OK] ArticleEditor.FocusEditor: CategoryComboBox focused via fallback");
                    try
                    {
                        // briefly open dropdown to make focus obvious
                        fallback.IsDropDownOpen = true;
                        System.Threading.Tasks.Task.Delay(400).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { fallback.IsDropDownOpen = false; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusEditor: close dropdown fallback", ex); } }));
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusEditor: open dropdown", ex); }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusEditor: outer", ex); }
        }

        // Specifically focus the title textbox (for new-article flow)
        public void FocusTitle()
        {
            DebugLogger.Log("[DEBUG] ArticleEditor.FocusTitle: called");
            try
            {
                // Perform multiple attempts on the UI thread to handle timing/templating races
                async System.Threading.Tasks.Task DoFocusAsync()
                {
                    Avalonia.Controls.TextBox? tb = null;
                    for (int attempt = 0; attempt < 15; attempt++)
                    {
                        try
                        {
                            await System.Threading.Tasks.Task.Delay(60).ConfigureAwait(false);
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                try
                                {
                                    // 1) Try host.Content search (DataTemplate-host scenario)
                                    var host = this.FindControl<ContentControl>("EditorContent") ?? this.FindControl<ContentControl>("EditorContentHost");
                                    if (host?.Content is Avalonia.Controls.Control hostContent)
                                    {
                                        try
                                        {
                                            tb = hostContent.FindControl<TextBox>("TitleTextBox");
                                            if (tb != null)
                                            {
                                                // Ensure control is focusable
                                                if (tb.IsVisible && tb.IsEnabled)
                                                {
                                                    tb.Focus();
                                                    try { tb.SelectionStart = 0; tb.SelectionEnd = tb.Text?.Length ?? 0; } catch { }
                                                    FlashControl(tb);
                                                    DebugLogger.Log($"[FOCUS OK] ArticleEditor.FocusTitle: focused TitleTextBox (hostContent) on attempt {attempt}");
                                                    return;
                                                }
                                            }
                                        }
                                        catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: hostContent.FindControl", ex); }
                                    }

                                    // 2) Direct FindControl fallback on this control
                                    try
                                    {
                                        var fallback = this.FindControl<TextBox>("TitleTextBox");
                                        if (fallback != null && fallback.IsVisible && fallback.IsEnabled)
                                        {
                                            tb = fallback;
                                            fallback.Focus();
                                            try { fallback.SelectionStart = 0; fallback.SelectionEnd = fallback.Text?.Length ?? 0; } catch { }
                                            FlashControl(fallback);
                                            DebugLogger.Log($"[FOCUS OK] ArticleEditor.FocusTitle: focused TitleTextBox (direct fallback) on attempt {attempt}");
                                            return;
                                        }
                                    }
                                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: direct FindControl", ex); }

                                    // 3) Visual-tree search for any control named TitleTextBox
                                    try
                                    {
                                        var root = this.VisualRoot as Avalonia.Visual;
                                        if (root != null)
                                        {
                                            var q = new System.Collections.Generic.Queue<Avalonia.Visual>();
                                            q.Enqueue(root);
                                            while (q.Count > 0)
                                            {
                                                var v = q.Dequeue();
                                                if (v is Avalonia.Controls.Control c && string.Equals(c.Name, "TitleTextBox", StringComparison.Ordinal))
                                                {
                                                    if (c is TextBox found && found.IsVisible && found.IsEnabled)
                                                    {
                                                        tb = found;
                                                        found.Focus();
                                                        try { found.SelectionStart = 0; found.SelectionEnd = found.Text?.Length ?? 0; } catch { }
                                                        FlashControl(found);
                                                        DebugLogger.Log($"[FOCUS OK] ArticleEditor.FocusTitle: focused TitleTextBox (visual search) on attempt {attempt}");
                                                        return;
                                                    }
                                                }
                                                try
                                                {
                                                    foreach (var child in Avalonia.VisualTree.VisualExtensions.GetVisualChildren(v).OfType<Avalonia.Visual>())
                                                        q.Enqueue(child);
                                                }
                                                catch { /* ignore traversal issues */ }
                                            }
                                        }
                                    }
                                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: visual-tree search", ex); }
                                }
                                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: UI invoke", ex); }
                            }, Avalonia.Threading.DispatcherPriority.Background);

                            // After invoking, check if we obtained focus
                            if (tb != null)
                            {
                                bool isFocused = false;
                                try { isFocused = tb.IsFocused; } catch { }
                                if (isFocused) return;
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: attempt loop", ex); }
                    }

                    // If we reach here, focusing failed — flash the overlay so user sees visual cue
                    try { TriggerOverlayFlash(false); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: TriggerOverlayFlash fallback", ex); }
                }

                // Kick off the async focus attempts but don't await synchronously here
                _ = DoFocusAsync();
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FocusTitle: outer", ex); }
        }

        // Show a brief overlay flash over the entire editor (used when inner controls aren't available)
        public void TriggerOverlayFlash(bool persistent = false)
        {
            try
            {
                var overlay = this.FindControl<Border>("FocusFlashOverlay");
                if (overlay == null) return;
                if (persistent)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            DebugLogger.Log("[DEBUG] ArticleEditor.ShowOverlayFlash: showing persistent overlay (awaiting key to clear)");
                            overlay.IsVisible = true;
                            overlay.Opacity = 1.0;
                            overlay.IsHitTestVisible = true;
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: show persistent", ex); }
                    });

                    // Attach a one-time key handler on the window to dismiss the overlay
                    try
                    {
                        var wnd = this.VisualRoot as Window;
                        if (wnd != null)
                        {
                            EventHandler<KeyEventArgs>? handler = null;
                            handler = (s, e) =>
                            {
                                try
                                {
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        try { overlay.IsVisible = false; overlay.IsHitTestVisible = false; overlay.Opacity = 1.0; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: clear persistent overlay inner", ex); }
                                    });
                                    wnd.KeyDown -= handler!;
                                    DebugLogger.Log("[DEBUG] ArticleEditor.ShowOverlayFlash: persistent overlay cleared by key");
                                }
                                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: persistent handler", ex); }
                            };
                            wnd.KeyDown += handler;
                        }
                        else
                        {
                            DebugLogger.Log("[DEBUG] ArticleEditor.ShowOverlayFlash: window not found for persistent overlay; falling back to timed flash");
                            // Fall through to timed flash
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.ShowOverlayFlash: failed to attach persistent key handler: " + ex.Message);
                    }

                    // If we attached persistent behavior, return now (overlay will be dismissed by key handler)
                    return;
                }

                // Normal transient flash: show overlay briefly and fade out
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.ShowOverlayFlash: showing transient overlay");
                        overlay.IsVisible = true;
                        overlay.Opacity = 0.85;
                        overlay.IsHitTestVisible = false;
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: show transient", ex); }
                });
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(380).ConfigureAwait(false);
                        const int steps = 8; const int stepMs = 40;
                        for (int i = 0; i < steps; i++)
                        {
                            var t = i + 1;
                            var newOp = 0.85 - ((double)t / steps * 0.85);
                            if (newOp < 0.0) newOp = 0.0;
                            Dispatcher.UIThread.Post(() => { try { overlay.Opacity = newOp; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: fade out", ex); } });
                            await System.Threading.Tasks.Task.Delay(stepMs).ConfigureAwait(false);
                        }
                        Dispatcher.UIThread.Post(() => { try { overlay.IsVisible = false; overlay.Opacity = 0.85; } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: hide", ex); } });
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.ShowOverlayFlash: transient task", ex); }
                });
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.TriggerOverlayFlash: outer", ex); }
        }

        // End editing in the article editor: close open dropdowns and move focus away so edits stop.
        public void EndEdit()
        {
            try
            {
                // Close any open ComboBox dropdowns inside the editor
                try
                {
                    var host = this.FindControl<ContentControl>("EditorContent");
                    if (host?.Content is Avalonia.Controls.Control hostContent)
                    {
                        foreach (var cb in hostContent.GetLogicalDescendants().OfType<ComboBox>())
                        {
                            try { cb.IsDropDownOpen = false; } catch { }
                        }
                        foreach (var tb in hostContent.GetLogicalDescendants().OfType<TextBox>())
                        {
                            // Optionally trigger LostFocus by moving focus off the textbox later
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.EndEdit: close dropdowns", ex); }

                // Clear editor-focused flag so global handlers know editor is no longer focused
                try { IndexEditor.Shared.EditorState.IsArticleEditorFocused = false; } catch { }

                // Move keyboard focus to the invisible host to remove focus from inner editor controls
                try
                {
                    var wnd = this.VisualRoot as Window;
                    if (wnd != null)
                    {
                        var host = wnd.FindControl<Border>("KeyboardFocusHost");
                        if (host != null)
                        {
                            host.Focus();
                            return;
                        }
                        // Fallback: focus the window itself
                        wnd.Focus();
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.EndEdit: move focus to host/window", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.EndEdit: outer", ex); }
        }

        // Flash a control briefly so the user can see which control got focus (debugging aid)
        private void FlashControl(Control ctrl)
        {
            if (ctrl == null) return;
            // Work on UI thread to read/set properties synchronously
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Save originals and apply highlight depending on concrete control type
                    if (ctrl is TextBox tb)
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.FlashControl: flashing TextBox");
                        var origBg = tb.Background;
                        var origBorder = tb.BorderBrush;
                        tb.Background = Brushes.LightGoldenrodYellow;
                        tb.BorderBrush = Brushes.OrangeRed;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try
                                {
                                    if (origBg != null) tb.Background = origBg; else tb.ClearValue(TextBox.BackgroundProperty);
                                    if (origBorder != null) tb.BorderBrush = origBorder; else tb.ClearValue(TextBox.BorderBrushProperty);
                                }
                                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FlashControl: restore TextBox visuals", ex); }
                            });
                        });
                    }
                    else if (ctrl is ComboBox cb)
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.FlashControl: flashing ComboBox");
                        var origBg = cb.Background;
                        cb.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) cb.Background = origBg; else cb.ClearValue(ComboBox.BackgroundProperty); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FlashControl: restore ComboBox visuals", ex); }
                            });
                        });
                    }
                    else if (ctrl is Border br)
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.FlashControl: flashing Border");
                        var origBg = br.Background;
                        br.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) br.Background = origBg; else br.ClearValue(Border.BackgroundProperty); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FlashControl: restore Border visuals", ex); }
                            });
                        });
                    }
                    else if (ctrl is Panel p)
                    {
                        DebugLogger.Log("[DEBUG] ArticleEditor.FlashControl: flashing Panel");
                        var origBg = p.Background;
                        p.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) p.Background = origBg; else p.ClearValue(Panel.BackgroundProperty); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FlashControl: restore Panel visuals", ex); }
                            });
                        });
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleEditor.FlashControl: outer", ex); }
            });
        }

        private void OnEditorStateChanged()
        {
            try
            {
                // Post to UI thread and start a background retry loop to focus Title when the DataTemplate is realized.
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (this.DataContext is Common.Shared.ArticleLine art && IndexEditor.Shared.EditorState.ActiveArticle != null && object.ReferenceEquals(art, IndexEditor.Shared.EditorState.ActiveArticle))
                        {
                            // Start a background task to retry focusing until success or until attempts exhausted
                            _ = System.Threading.Tasks.Task.Run(async () =>
                            {
                                const int attempts = 16;
                                const int delayMs = 80;
                                for (int i = 0; i < attempts; i++)
                                {
                                    DebugLogger.Log($"[DEBUG] ArticleEditor.OnEditorStateChanged: focus attempt {i}");
                                    try
                                    {
                                        await System.Threading.Tasks.Task.Delay(delayMs).ConfigureAwait(false);
                                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                        {
                                            try
                                            {
                                                // Prefer the Title textbox inside the DataTemplate content
                                                var host = this.FindControl<ContentControl>("EditorContent");
                                                TextBox? tb = null;
                                                if (host != null && host.Content is Avalonia.Controls.Control hostContent)
                                                {
                                                    try { tb = hostContent.FindControl<TextBox>("TitleTextBox"); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: find TitleTextBox in hostContent", ex); }
                                                    DebugLogger.Log(tb == null ? $"[DEBUG] ArticleEditor.OnEditorStateChanged: TitleTextBox not found in host.Content on attempt {i}" : $"[DEBUG] ArticleEditor.OnEditorStateChanged: TitleTextBox found in host.Content on attempt {i}");
                                                    if (tb != null)
                                                    {
                                                        try { tb.Focus(); DebugLogger.Log($"[DEBUG] ArticleEditor.OnEditorStateChanged: focused TitleTextBox on attempt {i}"); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: focus tb", ex); }
                                                        return;
                                                    }
                                                }

                                                // fallback: search whole visual tree
                                                try { tb = this.FindControl<TextBox>("TitleTextBox"); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: find TitleTextBox fallback", ex); }
                                                if (tb != null)
                                                {
                                                    try { tb.Focus(); DebugLogger.Log($"[DEBUG] ArticleEditor.OnEditorStateChanged: focused TitleTextBox via fallback on attempt {i}"); } catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: focus tb fallback", ex); }
                                                    return;
                                                }
                                            }
                                            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: inner post", ex); }
                                        });
                                    }
                                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: delay", ex); }
                                }
                                DebugLogger.Log("[DEBUG] ArticleEditor.OnEditorStateChanged: focus retry loop finished");
                            });
                        }
                    }
                    catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: post scheduling", ex); }
                });
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleEditor.OnEditorStateChanged: outermost", ex); }
        }
    }
}
