using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.VisualTree;

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
                try { IndexEditor.Shared.EditorState.StateChanged -= OnEditorStateChanged; } catch { }
                try { IndexEditor.Shared.EditorState.StateChanged -= OnEditorStateFocusRequested; } catch { }
            };

            // Track focus within the article editor so global key handlers can avoid intercepting arrow keys
            this.GotFocus += (s, e) => { try { IndexEditor.Shared.EditorState.IsArticleEditorFocused = true; } catch { } };
            this.LostFocus += (s, e) => { try { IndexEditor.Shared.EditorState.IsArticleEditorFocused = false; } catch { } };
        }

        private int _lastFocusRequest = 0;
        private void OnEditorStateFocusRequested()
        {
            try
            {
                var current = IndexEditor.Shared.EditorState.ArticleEditorFocusRequest;
                if (current == _lastFocusRequest) return;
                _lastFocusRequest = current;
                System.Console.WriteLine($"[DEBUG] ArticleEditor: focus request received (count={current})");
                // Schedule a UI thread attempt to focus the title textbox
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try { FocusTitle(); } catch { }
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
            catch { }
        }

        // Public helper to focus the category combobox in the editor
        public void FocusEditor()
        {
            try
            {
                System.Console.WriteLine("[DEBUG] ArticleEditor.FocusEditor: called");
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
                            if (tb != null) { tb.Focus(); FlashControl(tb); return; }
                            var cb = hostContent.FindControl<ComboBox>("CategoryComboBox");
                            System.Console.WriteLine(cb == null ? "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox not found in host.Content" : "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox found in host.Content");
                            if (cb != null) { cb.Focus(); FlashControl(cb); return; }
                        }
                    }
                }
                catch { }

                // Fallback: search entire visual tree of this control
                var fallbackTitle = this.FindControl<TextBox>("TitleTextBox");
                if (fallbackTitle != null)
                {
                    System.Console.WriteLine("[DEBUG] ArticleEditor.FocusEditor: found TitleTextBox via fallback");
                    fallbackTitle.Focus();
                    FlashControl(fallbackTitle);
                    return;
                }
                var fallback = this.FindControl<ComboBox>("CategoryComboBox");
                if (fallback != null)
                {
                    System.Console.WriteLine("[DEBUG] ArticleEditor.FocusEditor: found CategoryComboBox via fallback");
                    fallback.Focus();
                    FlashControl(fallback);
                    try
                    {
                        // briefly open dropdown to make focus obvious
                        fallback.IsDropDownOpen = true;
                        System.Threading.Tasks.Task.Delay(400).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { fallback.IsDropDownOpen = false; } catch { } }));
                    }
                    catch { }
                }
            }
            catch { }
        }

        // Specifically focus the title textbox (for new-article flow)
        public void FocusTitle()
        {
            System.Console.WriteLine("[DEBUG] ArticleEditor.FocusTitle: called");
            try
            {
                var host = this.FindControl<ContentControl>("EditorContent");
                if (host != null)
                {
                    if (host.Content is Avalonia.Controls.Control hostContent)
                    {
                        var tb = hostContent.FindControl<TextBox>("TitleTextBox");
                        System.Console.WriteLine(tb == null ? "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox not found in host.Content" : "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox found in host.Content");
                        if (tb != null) { tb.Focus(); FlashControl(tb); return; }
                    }
                }
                var fallback = this.FindControl<TextBox>("TitleTextBox");
                System.Console.WriteLine(fallback == null ? "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox not found via fallback" : "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox found via fallback");
                if (fallback != null) { fallback.Focus(); FlashControl(fallback); return; }

                // Stronger fallback: search visual descendants of the window/root for any control named TitleTextBox
                // If we couldn't find the inner TitleTextBox via the DataTemplate or direct FindControl fallback,
                // fall back to flashing the editor overlay so the user sees an indication. We avoid scanning the
                // entire visual tree here to keep compatibility across Avalonia versions.

                // If we couldn't find the inner TitleTextBox, flash the editor overlay so user sees an indication
                try { TriggerOverlayFlash(false); } catch { }
            }
            catch { }
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
                            System.Console.WriteLine("[DEBUG] ArticleEditor.ShowOverlayFlash: showing persistent overlay (awaiting key to clear)");
                            overlay.IsVisible = true;
                            overlay.Opacity = 1.0;
                            overlay.IsHitTestVisible = true;
                        }
                        catch { }
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
                                        try { overlay.IsVisible = false; overlay.IsHitTestVisible = false; overlay.Opacity = 1.0; } catch { }
                                    });
                                    wnd.KeyDown -= handler!;
                                    System.Console.WriteLine("[DEBUG] ArticleEditor.ShowOverlayFlash: persistent overlay cleared by key");
                                }
                                catch { }
                            };
                            wnd.KeyDown += handler;
                        }
                        else
                        {
                            System.Console.WriteLine("[DEBUG] ArticleEditor.ShowOverlayFlash: window not found for persistent overlay; falling back to timed flash");
                            // Fall through to timed flash
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("[DEBUG] ArticleEditor.ShowOverlayFlash: failed to attach persistent key handler: " + ex.Message);
                    }

                    // If we attached persistent behavior, return now (overlay will be dismissed by key handler)
                    return;
                }

                // Normal transient flash: show overlay briefly and fade out
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        System.Console.WriteLine("[DEBUG] ArticleEditor.ShowOverlayFlash: showing transient overlay");
                        overlay.IsVisible = true;
                        overlay.Opacity = 0.85;
                        overlay.IsHitTestVisible = false;
                    }
                    catch { }
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
                            Dispatcher.UIThread.Post(() => { try { overlay.Opacity = newOp; } catch { } });
                            await System.Threading.Tasks.Task.Delay(stepMs).ConfigureAwait(false);
                        }
                        Dispatcher.UIThread.Post(() => { try { overlay.IsVisible = false; overlay.Opacity = 0.85; } catch { } });
                    }
                    catch { }
                });
            }
            catch { }
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
                        System.Console.WriteLine("[DEBUG] ArticleEditor.FlashControl: flashing TextBox");
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
                                catch { }
                            });
                        });
                    }
                    else if (ctrl is ComboBox cb)
                    {
                        System.Console.WriteLine("[DEBUG] ArticleEditor.FlashControl: flashing ComboBox");
                        var origBg = cb.Background;
                        cb.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) cb.Background = origBg; else cb.ClearValue(ComboBox.BackgroundProperty); } catch { }
                            });
                        });
                    }
                    else if (ctrl is Border br)
                    {
                        System.Console.WriteLine("[DEBUG] ArticleEditor.FlashControl: flashing Border");
                        var origBg = br.Background;
                        br.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) br.Background = origBg; else br.ClearValue(Border.BackgroundProperty); } catch { }
                            });
                        });
                    }
                    else if (ctrl is Panel p)
                    {
                        System.Console.WriteLine("[DEBUG] ArticleEditor.FlashControl: flashing Panel");
                        var origBg = p.Background;
                        p.Background = Brushes.LightGoldenrodYellow;
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(420).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                try { if (origBg != null) p.Background = origBg; else p.ClearValue(Panel.BackgroundProperty); } catch { }
                            });
                        });
                    }
                }
                catch { }
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
                                    System.Console.WriteLine($"[DEBUG] ArticleEditor.OnEditorStateChanged: focus attempt {i}");
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
                                                    try { tb = hostContent.FindControl<TextBox>("TitleTextBox"); } catch { }
                                                    System.Console.WriteLine(tb == null ? $"[DEBUG] ArticleEditor.OnEditorStateChanged: TitleTextBox not found in host.Content on attempt {i}" : $"[DEBUG] ArticleEditor.OnEditorStateChanged: TitleTextBox found in host.Content on attempt {i}");
                                                    if (tb != null)
                                                    {
                                                        try { tb.Focus(); System.Console.WriteLine($"[DEBUG] ArticleEditor.OnEditorStateChanged: focused TitleTextBox on attempt {i}"); } catch { }
                                                        return;
                                                    }
                                                }

                                                // fallback: search whole visual tree
                                                try { tb = this.FindControl<TextBox>("TitleTextBox"); } catch { }
                                                if (tb != null)
                                                {
                                                    try { tb.Focus(); System.Console.WriteLine($"[DEBUG] ArticleEditor.OnEditorStateChanged: focused TitleTextBox via fallback on attempt {i}"); } catch { }
                                                    return;
                                                }
                                            }
                                            catch { }
                                        });
                                    }
                                    catch { }
                                }
                                System.Console.WriteLine("[DEBUG] ArticleEditor.OnEditorStateChanged: focus retry loop finished");
                            });
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }
    }
}
