using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Collections.Specialized;
using Avalonia.Input;

namespace IndexEditor.Views
{
    public partial class ArticleList : UserControl
    {
        public ArticleList()
        {
            InitializeComponent();
            // When attached to the visual tree, ensure we inherit the Window DataContext (EditorStateViewModel)
            // When attached, inherit the Window's DataContext (EditorStateViewModel) if not set,
            // attach property-changed handlers to articles so UI animations trigger, and wire collection changes.
            this.AttachedToVisualTree += (s, e) =>
            {
                if (this.DataContext == null || !(this.DataContext is EditorStateViewModel))
                {
                    var root = this.VisualRoot as Window;
                    if (root != null && root.DataContext is EditorStateViewModel vmRoot)
                        this.DataContext = vmRoot;
                }

                if (this.DataContext is EditorStateViewModel vmModel)
                {
                    foreach (var art in vmModel.Articles)
                    {
                        try { art.PropertyChanged -= Article_PropertyChanged; } catch { }
                        art.PropertyChanged += Article_PropertyChanged;
                    }

                    if (vmModel.Articles is INotifyCollectionChanged incc)
                    {
                        incc.CollectionChanged += (sender, args) =>
                        {
                            if (args.NewItems != null)
                            {
                                foreach (var ni in args.NewItems)
                                {
                                    if (ni is Common.Shared.ArticleLine newArt)
                                    {
                                        try { newArt.PropertyChanged -= Article_PropertyChanged; } catch { }
                                        newArt.PropertyChanged += Article_PropertyChanged;
                                    }
                                }
                            }
                        };
                    }
                }
            };

            var list = this.FindControl<ListBox>("ArticlesListBox");

            // Container preparation event isn't available on ListBox on all runtimes; instead attach property changed handlers via the VM collection above.

            if (list != null)
            {
                // Initial enable/selection state based on EditorState
                try
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    // Disable the list entirely when an active segment exists to prevent user selecting any other article
                    list.IsEnabled = !(activeSeg != null && activeSeg.IsActive);
                    if (activeSeg != null && activeSeg.IsActive && IndexEditor.Shared.EditorState.ActiveArticle != null)
                        list.SelectedItem = IndexEditor.Shared.EditorState.ActiveArticle;
                }
                catch { }

                // Subscribe to global state changes to enforce selection and enabledness
                IndexEditor.Shared.EditorState.StateChanged += () =>
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        // Disable the list entirely while there's an active open segment
                        list.IsEnabled = !(activeSeg != null && activeSeg.IsActive);
                        // If a segment is active, force the SelectedItem to the owning active article
                        if (activeSeg != null && activeSeg.IsActive && IndexEditor.Shared.EditorState.ActiveArticle != null)
                        {
                            list.SelectedItem = IndexEditor.Shared.EditorState.ActiveArticle;
                        }
                    });
                };

                list.SelectionChanged += (s, e) =>
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                    var selected = list.SelectedItem as Common.Shared.ArticleLine;
                    if (activeSeg != null && activeSeg.IsActive && selected != null && activeArticle != null && !object.ReferenceEquals(selected, activeArticle))
                    {
                        // Notify user and revert selection
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
                        try { list.SelectedItem = activeArticle; } catch { }
                        return;
                    }

                    // Normal selection flow: clear IsSelected on others (VM will update)
                    if (selected != null)
                    {
                        try
                        {
                            foreach (var a in (this.DataContext as EditorStateViewModel)?.Articles ?? new System.Collections.ObjectModel.ObservableCollection<Common.Shared.ArticleLine>())
                                a.IsSelected = false;
                        }
                        catch { }
                    }
                };
            }

            // Public helper to scroll to an article (used by MainWindow)
            // Keep this method instance-level so other components can call ArticleListControl.ScrollToArticle(article)
        }

        public void ScrollToArticle(Common.Shared.ArticleLine? article)
        {
            if (article == null) return;
            var list = this.FindControl<ListBox>("ArticlesListBox");
            if (list == null) return;
            list.ScrollIntoView(article);
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

        // Handle keyboard events from the ListBox (wired via XAML KeyDown="OnListKeyDown")
        private void OnListKeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                var list = this.FindControl<ListBox>("ArticlesListBox");
                if (list == null) return;
                if (e.Key == Key.Delete)
                {
                    // Optionally implement delete behavior later; for now, ignore to avoid unhandled event errors
                    e.Handled = true;
                }
            }
            catch { }
        }

        // Handle pointer pressed on the article Border - select the clicked article
        private void OnArticlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Border b && b.Tag is Common.Shared.ArticleLine art)
                {
                    // Respect active segment: disallow selecting other articles
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                    if (activeSeg != null && activeSeg.IsActive && activeArticle != null && !object.ReferenceEquals(art, activeArticle))
                    {
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
                        return;
                    }

                    var vm = this.DataContext as EditorStateViewModel;
                    if (vm != null)
                    {
                        // Find in-VM instance
                        var viewArticle = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, art))
                                        ?? vm.Articles.FirstOrDefault(a => a.Pages != null && art.Pages != null && a.Pages.SequenceEqual(art.Pages) && (a.Title ?? string.Empty) == (art.Title ?? string.Empty));
                        var toSelect = viewArticle ?? art;
                        if (vm.SelectArticleCommand.CanExecute(toSelect))
                            vm.SelectArticleCommand.Execute(toSelect);
                        vm.SelectedArticle = toSelect;
                    }
                }
            }
            catch { }
        }

        // New: handle pointer on a segment row - set active segment and jump page controller to segment start
        private void OnSegmentPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                // Determine the segment and containing article
                if (sender is Border b && b.Tag is Common.Shared.Segment seg)
                {
                    // Find the article owning this segment by walking up the visual tree or searching VM
                    var vm = this.DataContext as EditorStateViewModel;
                    Common.Shared.ArticleLine? owner = null;
                    if (vm != null)
                    {
                        owner = vm.Articles.FirstOrDefault(a => a.Segments != null && a.Segments.Contains(seg));
                    }
                    // Fallback: search shared EditorState.Articles
                    if (owner == null)
                    {
                        owner = IndexEditor.Shared.EditorState.Articles.FirstOrDefault(a => a.Segments != null && a.Segments.Contains(seg));
                    }

                    if (owner == null) return;

                    // If there's already an active segment on a different article, block and toast
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                    if (activeSeg != null && activeSeg.IsActive && activeArticle != null && !object.ReferenceEquals(activeArticle, owner))
                    {
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch { }
                        return;
                    }

                    // Set the owner as selected/active article
                    try
                    {
                        foreach (var a in (this.DataContext as EditorStateViewModel)?.Articles ?? new System.Collections.ObjectModel.ObservableCollection<Common.Shared.ArticleLine>())
                            a.IsSelected = false;
                    }
                    catch { }
                    owner.IsSelected = true;
                    IndexEditor.Shared.EditorState.ActiveArticle = owner;

                    // Set global active segment (if it's not already)
                    IndexEditor.Shared.EditorState.ActiveSegment = seg;

                    // Jump page controller to the segment start
                    IndexEditor.Shared.EditorState.CurrentPage = seg.Start;

                    // Notify UI to update (selection, buttons, etc.)
                    IndexEditor.Shared.EditorState.NotifyStateChanged();
                }
            }
            catch { }
        }

    }
}
