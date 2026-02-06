using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;
using System.IO;
using System.Windows.Input;
using IndexEditor.Views; // for EditorStateViewModel

namespace IndexEditor.Views
{
    public partial class ArticleList : UserControl
    {
        public ArticleList()
        {
            InitializeComponent();
            // When attached to the visual tree, ensure we inherit the Window DataContext (EditorStateViewModel)
            this.AttachedToVisualTree += (s, e) =>
            {
                try
                {
                    if (this.DataContext == null || !(this.DataContext is EditorStateViewModel))
                    {
                        var root = this.VisualRoot as Window;
                        if (root != null && root.DataContext is EditorStateViewModel vmRoot)
                        {
                            this.DataContext = vmRoot;
                        }
                    }
                    // Diagnostic: print VM articles count and titles
                    try
                    {
                        if (this.DataContext is EditorStateViewModel vm)
                        {
                            Console.WriteLine($"[DIAG] ArticleList attached. vm.Articles.Count={vm.Articles.Count}");
                            int i = 0;
                            foreach (var a in vm.Articles)
                            {
                                Console.WriteLine($"[DIAG] Article[{i}] Title='{a.Title}' Category='{a.Category}' Pages=[{string.Join(",", a.Pages)}]");
                                i++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("[DIAG] ArticleList attached but DataContext is not EditorStateViewModel");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DIAG] ArticleList attached diagnostic failed: {ex.Message}");
                    }
                    // Also log the ListBox items count at attach time
                    try
                    {
                        var listBox = this.FindControl<ListBox>("ArticlesListBox");
                        if (listBox != null)
                        {
                            var itemsCountNow = (listBox.Items as System.Collections.ICollection)?.Count ?? -1;
                            Console.WriteLine($"[DIAG] (attach) ArticlesListBox.Items.Count = {itemsCountNow}");
                            Console.WriteLine($"[DIAG] (attach) ArticlesListBox.ItemsSource = {listBox.ItemsSource}");
                        }
                        else
                        {
                            Console.WriteLine("[DIAG] (attach) ArticlesListBox not found via FindControl");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DIAG] ArticleList attached list diagnostic failed: {ex.Message}");
                    }
                }
                catch { }
            };

            // Hook container prepared so we can attach animations to per-item overlay Border
            var list = this.FindControl<ListBox>("ArticlesListBox");
            if (list != null)
            {
                Console.WriteLine("[DIAG] Found ArticlesListBox in ArticleList control");
                list.ContainerPrepared += (s, e) =>
                {
                    try
                    {
                        // e.Container is a ListBoxItem; try find overlay by name and bind to DataContext change
                        if (e.Container is Control container && container.DataContext is Common.Shared.ArticleLine article)
                        {
                            var overlay = container.FindControl<Border>("AutoHighlightOverlay");
                            if (overlay != null)
                            {
                                // Ensure overlay starts invisible
                                overlay.Opacity = 0;
                                // Listen for property changes on the article
                                article.PropertyChanged -= Article_PropertyChanged;
                                article.PropertyChanged += Article_PropertyChanged;
                            }
                        }
                    }
                    catch { }
                };
                // Diagnostic: print the ListBox items count
                try
                {
                    var itemsCount = (list.Items as System.Collections.ICollection)?.Count ?? -1;
                    Console.WriteLine($"[DIAG] ArticlesListBox.Items.Count = {itemsCount}");
                }
                catch { }
                // Ensure the container backgrounds are transparent when selection changes (some themes paint a blue background)
                list.SelectionChanged += (s, e) =>
                {
                    try
                    {
                        var items = list.Items as System.Collections.IList;
                        if (items == null) return;

                        // Prevent selection change while there's an active segment
                        var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                        var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                        var selected = list.SelectedItem as Common.Shared.ArticleLine;
                        if (activeSeg != null && activeSeg.IsActive && selected != null && activeArticle != null && !object.ReferenceEquals(selected, activeArticle))
                        {
                            // Revert to the active article selection (or clear if none)
                            try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
                            try { list.SelectedItem = activeArticle; } catch { }
                        }

                        for (int i = 0; i < items.Count; i++)
                        {
                            try
                            {
                                var container = list.ItemContainerGenerator.ContainerFromIndex(i) as Avalonia.Controls.ListBoxItem;
                                if (container != null)
                                {
                                    container.Background = Avalonia.Media.Brushes.Transparent;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                };
            }
        }

        // Scroll the list so the specified article model is visible
        public void ScrollToArticle(Common.Shared.ArticleLine? article)
        {
            if (article == null) return;
            var list = this.FindControl<ListBox>("ArticlesListBox");
            if (list == null) return;
            list.ScrollIntoView(article);
        }

        private DateTime _lastClickTime = DateTime.MinValue;
        private object? _lastClickTarget = null;
        private readonly TimeSpan DoubleClickThreshold = TimeSpan.FromMilliseconds(350);

        private void OnArticlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Control control)
                {
                    // Use a strongly-typed view-model instead of reflection
                    if (this.DataContext is EditorStateViewModel vm)
                    {
                        var cmd = vm.SelectArticleCommand;
                        var parameter = control.Tag as Common.Shared.ArticleLine;
                        if (parameter == null) return;

                        // Prevent clicking a different article while an active segment exists
                        var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                        var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                        if (activeSeg != null && activeSeg.IsActive && activeArticle != null && !object.ReferenceEquals(activeArticle, parameter))
                        {
                            try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
                            // Stop further handling so selection doesn't change
                            e.Handled = true;
                            return;
                        }

                        var now = DateTime.UtcNow;
                        bool isDouble = false;
                        if (_lastClickTarget == parameter && (now - _lastClickTime) <= DoubleClickThreshold)
                        {
                            isDouble = true;
                        }

                        // Always run selection on single click
                        if (cmd != null && cmd.CanExecute(parameter))
                        {
                            cmd.Execute(parameter);
                        }

                        // On double-click, delegate navigation to the view-model so it can be unit-tested
                        if (isDouble)
                        {
                            try
                            {
                                vm.NavigateToArticle(parameter);
                            }
                            catch { }
                        }

                        _lastClickTarget = parameter;
                        _lastClickTime = now;
                     }
                 }
             }
             catch (Exception)
             {
                 // swallow exceptions to avoid breaking the UI; logging could be added here
             }
         }

        private async void OnListKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Avalonia.Input.Key.Delete)
                {
                    if (this.DataContext is EditorStateViewModel vm && vm.SelectedArticle != null)
                    {
                        var article = vm.SelectedArticle;
                        var window = this.VisualRoot as Window;
                        var confirm = await ConfirmDialog.ShowDialog(window, $"Delete selected article '{article.Title}'? This cannot be undone.");
                        if (confirm)
                        {
                            // Remove from VM collection and shared state
                            vm.Articles.Remove(article);
                            try { IndexEditor.Shared.EditorState.Articles.Remove(article); } catch { }
                            // Clear selection
                            vm.SelectedArticle = null;
                            IndexEditor.Shared.EditorState.ActiveArticle = null;
                            IndexEditor.Shared.EditorState.NotifyStateChanged();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[DEBUG] OnListKeyDown exception: {ex.Message}");
            }
        }

        private void Article_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Common.Shared.ArticleLine article && e.PropertyName == nameof(article.WasAutoHighlighted) && article.WasAutoHighlighted)
            {
                // Find the ListBoxItem container for this article
                var list = this.FindControl<ListBox>("ArticlesListBox");
                if (list == null) return;
                var items = list.Items as System.Collections.IList;
                int idx = -1;
                if (items != null)
                    idx = items.IndexOf(article);
                Control? container = null;
                if (idx >= 0)
                {
                    try { container = list.ItemContainerGenerator.ContainerFromIndex(idx) as Control; } catch { }
                }
                if (container == null) return;
                var overlay = container.FindControl<Border>("AutoHighlightOverlay");
                if (overlay == null) return;

                // Animate: fade in to 0.9 quickly, then fade out to 0 over 700ms
                try
                {
                    // Run animation on UI thread
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            overlay.Opacity = 0;
                            // quick fade in
                            for (int i = 0; i <= 6; i++)
                            {
                                overlay.Opacity = i * (0.9 / 6);
                                await System.Threading.Tasks.Task.Delay(30);
                            }
                            // pause briefly
                            await System.Threading.Tasks.Task.Delay(150);
                            // fade out
                            for (int i = 6; i >= 0; i--)
                            {
                                overlay.Opacity = i * (0.9 / 6);
                                await System.Threading.Tasks.Task.Delay(50);
                            }
                            overlay.Opacity = 0;
                            // Clear the WasAutoHighlighted flag so it can be triggered again
                            try { article.WasAutoHighlighted = false; } catch { }
                        }
                        catch { }
                    });
                }
                catch { }
            }
        }
    }
}
