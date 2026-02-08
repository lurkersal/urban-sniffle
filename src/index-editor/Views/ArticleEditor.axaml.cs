using Avalonia.Controls;

namespace IndexEditor.Views
{
    public partial class ArticleEditor : UserControl
    {
        public ArticleEditor()
        {
            InitializeComponent();
            // When the global EditorState changes, if the ActiveArticle matches our DataContext, focus the category combobox.
            IndexEditor.Shared.EditorState.StateChanged += OnEditorStateChanged;
            this.DetachedFromVisualTree += (s, e) => { IndexEditor.Shared.EditorState.StateChanged -= OnEditorStateChanged; };
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
                        // First preference: Title textbox (now first field)
                        var tb = host.FindControl<TextBox>("TitleTextBox");
                        if (tb != null) { tb.Focus(); return; }
                        var cb = host.FindControl<ComboBox>("CategoryComboBox");
                        System.Console.WriteLine(cb == null ? "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox not found in host" : "[DEBUG] ArticleEditor.FocusEditor: CategoryComboBox found in host");
                        if (cb != null) { cb.Focus(); return; }
                    }
                }
                catch { }

                // Fallback: search entire visual tree of this control
                var fallbackTitle = this.FindControl<TextBox>("TitleTextBox");
                if (fallbackTitle != null)
                {
                    System.Console.WriteLine("[DEBUG] ArticleEditor.FocusEditor: found TitleTextBox via fallback");
                    fallbackTitle.Focus();
                    return;
                }
                var fallback = this.FindControl<ComboBox>("CategoryComboBox");
                if (fallback != null)
                {
                    System.Console.WriteLine("[DEBUG] ArticleEditor.FocusEditor: found CategoryComboBox via fallback");
                    fallback.Focus();
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
                    var tb = host.FindControl<TextBox>("TitleTextBox");
                    System.Console.WriteLine(tb == null ? "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox not found in host" : "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox found in host");
                    if (tb != null) { tb.Focus(); return; }
                }
                var fallback = this.FindControl<TextBox>("TitleTextBox");
                System.Console.WriteLine(fallback == null ? "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox not found via fallback" : "[DEBUG] ArticleEditor.FocusTitle: TitleTextBox found via fallback");
                if (fallback != null) fallback.Focus();
            }
            catch { }
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
                                                // Prefer the Title textbox
                                                var host = this.FindControl<ContentControl>("EditorContent");
                                                TextBox? tb = null;
                                                if (host != null)
                                                {
                                                    try { tb = host.FindControl<TextBox>("TitleTextBox"); } catch { }
                                                }
                                                if (tb == null) try { tb = this.FindControl<TextBox>("TitleTextBox"); } catch { }
                                                if (tb != null)
                                                {
                                                    System.Console.WriteLine($"[DEBUG] ArticleEditor.OnEditorStateChanged: focused TitleTextBox on attempt {i}");
                                                    try { tb.Focus(); } catch { }
                                                    // success: break out by returning from the Task
                                                    return;
                                                }
                                            }
                                            catch { }
                                        });
                                    }
                                    catch { }
                                }
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
