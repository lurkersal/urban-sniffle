using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Interactivity;
using IndexEditor.Shared;

namespace IndexEditor.Views
{
    public partial class ArticleList : UserControl
    {
        // Helper: find the owning article for a given segment
        private Common.Shared.ArticleLine? GetArticleForSegment(Common.Shared.Segment? seg)
        {
            if (seg == null) return null;
            try
            {
                var vm = this.DataContext as EditorStateViewModel;
                if (vm != null)
                {
                    var a = vm.Articles.FirstOrDefault(ar => ar.Segments != null && ar.Segments.Contains(seg));
                    if (a != null) return a;
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleList.GetArticleForSegment: vm search", ex); }
            try
            {
                return IndexEditor.Shared.EditorState.Articles?.FirstOrDefault(ar => ar.Segments != null && ar.Segments.Contains(seg));
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleList.GetArticleForSegment: state search", ex); }
            return null;
        }

        private bool _listHandlersAttached = false;

        // Track last click timestamp per article as a fallback for double-click detection
        private readonly System.Collections.Generic.Dictionary<Common.Shared.ArticleLine, DateTime> _lastClickTime = new();

        private static readonly TimeSpan DoubleClickThreshold = TimeSpan.FromMilliseconds(400);

        public ArticleList()
        {
            InitializeComponent();
            // Console.WriteLine("[DEBUG] ArticleList constructed");
            // When attached to the visual tree, ensure we inherit the Window DataContext (EditorStateViewModel)
            // When attached, inherit the Window's DataContext (EditorStateViewModel) if not set,
            // attach property-changed handlers to articles so UI animations trigger, and wire collection changes.
            this.AttachedToVisualTree += (s, e) =>
            {
                // Console.WriteLine("[DEBUG] ArticleList attached to visual tree");
                if (this.DataContext == null || !(this.DataContext is EditorStateViewModel))
                {
                    var root = this.VisualRoot as Window;
                    if (root != null && root.DataContext is EditorStateViewModel vmRoot)
                        this.DataContext = vmRoot;
                }

                if (this.DataContext is EditorStateViewModel vmModel)
                {
                    // Console.WriteLine($"[DEBUG] ArticleList VM articles count: {vmModel.Articles?.Count ?? 0}");
                    foreach (var art in vmModel.Articles)
                    {
                        try { art.PropertyChanged -= Article_PropertyChanged; } catch (Exception ex) { DebugLogger.LogException("ArticleList.AttachedToVisualTree: remove handler", ex); }
                        art.PropertyChanged += Article_PropertyChanged;
                        // Console.WriteLine($"[DEBUG] Article: '{art.DisplayTitle}' Segments: {art.Segments?.Count ?? 0}");
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
                                        try { newArt.PropertyChanged -= Article_PropertyChanged; } catch (Exception ex) { DebugLogger.LogException("ArticleList.Incc.CollectionChanged: remove handler", ex); }
                                        newArt.PropertyChanged += Article_PropertyChanged;
                                        // Console.WriteLine($"[DEBUG] New Article added: '{newArt.DisplayTitle}' Segments: {newArt.Segments?.Count ?? 0}");
                                    }
                                }
                            }
                        };
                    }
                }

                // Now that the control is attached and templates are applied, find the ListBox and attach handlers once
                try
                {
                    var listOnAttach = this.FindControl<ListBox>("ArticlesListBox");
                    if (listOnAttach != null && !_listHandlersAttached)
                    {
                        listOnAttach.PointerPressed += OnListPointerPressed;
                        listOnAttach.DoubleTapped += OnListDoubleTapped;
                        listOnAttach.KeyDown += OnListKeyDown;
                        _listHandlersAttached = true;
                        try { DebugLogger.Log("ArticleList: attached list handlers in AttachedToVisualTree"); } catch {}
                    }
                    else
                    {
                        try { DebugLogger.Log($"ArticleList: listOnAttach is {(listOnAttach==null?"null":"found but handlers attached already")}"); } catch {}
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleList.AttachedToVisualTree: attach list handlers", ex); }
            };

            var list = this.FindControl<ListBox>("ArticlesListBox");

            // Immediate fallback: try attaching handlers now in case templates have already been applied
            try
            {
                if (list != null && !_listHandlersAttached)
                {
                    list.PointerPressed += OnListPointerPressed;
                    list.DoubleTapped += OnListDoubleTapped;
                    list.KeyDown += OnListKeyDown;
                    _listHandlersAttached = true;
                    try { DebugLogger.Log("ArticleList: attached list handlers immediately in constructor"); } catch {}
                }
                else
                {
                    try { DebugLogger.Log($"ArticleList: constructor immediate attach - list is {(list==null?"null":"found but already attached")}\n"); } catch {}
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleList.constructor: immediate attach", ex); }

            // Note: ListBox-level handlers are attached in AttachedToVisualTree so they run after templates are applied.

            if (list != null)
             {
                // Initial enable/selection state based on EditorState
                try
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    // Disable the list entirely when an active segment exists to prevent user selecting any other article
                    list.IsEnabled = !(activeSeg != null && activeSeg.IsActive);
                    if (activeSeg != null && activeSeg.IsActive)
                    {
                        var owner = GetArticleForSegment(activeSeg);
                        if (owner != null) list.SelectedItem = owner;
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("ArticleList.constructor: initial enable state", ex); }

                // Subscribe to global state changes to enforce selection and enabledness
                IndexEditor.Shared.EditorState.StateChanged += () =>
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        // Disable the list entirely while there's an active open segment
                        list.IsEnabled = !(activeSeg != null && activeSeg.IsActive);
                        // If a segment is active, force the SelectedItem to the owning article (derived from the segment)
                        if (activeSeg != null && activeSeg.IsActive)
                        {
                            var owner = GetArticleForSegment(activeSeg);
                            if (owner != null) list.SelectedItem = owner;
                        }

                        // Clear highlighting on all segments, then set only the active one
                        try
                        {
                            foreach (var a in IndexEditor.Shared.EditorState.Articles ?? new System.Collections.Generic.List<Common.Shared.ArticleLine>())
                            {
                                if (a.Segments != null)
                                {
                                    foreach (var s in a.Segments)
                                        s.IsHighlighted = false;
                                }
                            }
                            if (activeSeg != null)
                                activeSeg.IsHighlighted = true;
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleList.StateChanged: clearing highlights", ex); }
                    });
                };

                list.SelectionChanged += (s, e) =>
                {
                    var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                    var activeArticle = GetArticleForSegment(activeSeg);
                    var selected = list.SelectedItem as Common.Shared.ArticleLine;
                    if (activeSeg != null && activeSeg.IsActive && selected != null && activeArticle != null && !object.ReferenceEquals(selected, activeArticle))
                    {
                        // Notify user and revert selection
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleList.SelectionChanged: toast", ex); }
                        try { list.SelectedItem = activeArticle; } catch (Exception ex) { DebugLogger.LogException("ArticleList.SelectionChanged: revert selection", ex); }
                        return;
                    }

                    // Normal selection flow: clear IsSelected on others (VM will update)
                    if (selected != null)
                    {
                        try
                        {
                            foreach (var a in (this.DataContext as EditorStateViewModel)?.Articles ?? new System.Collections.ObjectModel.ObservableCollection<Common.Shared.ArticleLine>())
                                a.IsSelected = false;
                            // Mark the selected as selected so converters update border brush
                            selected.IsSelected = true;
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleList.SelectionChanged: mark selected", ex); }
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
                     try {
                         #pragma warning disable CS0618 // use legacy ItemContainerGenerator.ContainerFromIndex as compatibility fallback
                         container = list.ItemContainerGenerator.ContainerFromIndex(idx) as Control;
                         #pragma warning restore CS0618
                     } catch (Exception ex) { DebugLogger.LogException("ArticleList.Article_PropertyChanged: ContainerFromIndex", ex); }
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
                             try { article.WasAutoHighlighted = false; } catch (Exception ex) { DebugLogger.LogException("ArticleList.Animation: clear WasAutoHighlighted", ex); }
                         }
                         catch (Exception ex) { DebugLogger.LogException("ArticleList.Animation: outer", ex); }
                     });
                 }
                 catch (Exception ex) { DebugLogger.LogException("ArticleList.Article_PropertyChanged: animation outer", ex); }
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
            catch (Exception ex) { DebugLogger.LogException("ArticleList.OnListKeyDown: outer", ex); }
        }

        // Handle pointer pressed on the article Border - select the clicked article
        public void OnArticlePointerPressed(object sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try { DebugLogger.Log($"OnArticlePointerPressed invoked: Source={e.Source} ClickCount={e.ClickCount}"); } catch {}
             try
             {
                if (sender is Border b && b.Tag is Common.Shared.ArticleLine art)
                {
                    try { DebugLogger.Log($"ArticleList.OnArticlePointerPressed: ClickCount={e.ClickCount} Article='{art.DisplayTitle}'"); } catch { }
                     // Respect active segment: disallow selecting other articles
                     var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                     var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                     if (activeSeg != null && activeSeg.IsActive && activeArticle != null && !object.ReferenceEquals(art, activeArticle))
                     {
                         try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticlePointerPressed: toast", ex); }
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

                        // Double-click behavior: set current page to the first page of the clicked article.
                        try 
                        {
                            var now = DateTime.UtcNow;
                            bool isDouble = false;
                            if (e.ClickCount >= 2) isDouble = true;
                            else
                            {
                                // Fallback timestamp-based detection using the VM instance as the key
                                if (_lastClickTime.TryGetValue(toSelect, out var last))
                                {
                                    if (now - last <= DoubleClickThreshold) isDouble = true;
                                }
                            }

                            // Update last click time (use VM instance as key)
                            _lastClickTime[toSelect] = now;

                            if (isDouble)
                            {
                                // Only allow jumping pages if there is no active open segment
                                var activeSeg2 = IndexEditor.Shared.EditorState.ActiveSegment;
                                if (activeSeg2 != null && activeSeg2.IsActive)
                                {
                                    try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticlePointerPressed: double-click blocked", ex); }
                                }
                                else
                                {
                                    try
                                    {
                                        var pages = toSelect.Pages ?? new System.Collections.Generic.List<int>();
                                        if (pages.Count > 0)
                                        {
                                            var first = pages.Min();
                                            IndexEditor.Shared.EditorState.CurrentPage = first;
                                            IndexEditor.Shared.EditorState.NotifyStateChanged();
                                            e.Handled = true;
                                            try { DebugLogger.Log($"ArticleList.OnArticlePointerPressed: double-click set CurrentPage={first} for '{toSelect.DisplayTitle}'"); } catch { }
                                        }
                                    }
                                    catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticlePointerPressed: double-click action", ex); }
                                }
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticlePointerPressed: double-click outer", ex); }
                     }
                 }
             }
             catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticlePointerPressed: outer", ex); }
         }

        public void OnArticleDoubleTapped(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (sender is Border b && b.Tag is Common.Shared.ArticleLine art)
                {
                    // Only allow jumping pages if there is no active open segment
                    var activeSeg2 = IndexEditor.Shared.EditorState.ActiveSegment;
                    if (activeSeg2 != null && activeSeg2.IsActive)
                    {
                        try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticleDoubleTapped: blocked", ex); }
                        return;
                    }

                    // Resolve VM instance and act on it
                    var vm = this.DataContext as EditorStateViewModel;
                    Common.Shared.ArticleLine? toSelect = art;
                    if (vm != null)
                    {
                        var viewArticle = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, art))
                                        ?? vm.Articles.FirstOrDefault(a => a.Pages != null && art.Pages != null && a.Pages.SequenceEqual(art.Pages) && (a.Title ?? string.Empty) == (art.Title ?? string.Empty));
                        if (viewArticle != null) toSelect = viewArticle;
                        try { vm.SelectedArticle = toSelect; } catch { }
                    }

                    if (toSelect != null)
                    {
                        try
                        {
                            var pages = toSelect.Pages ?? new System.Collections.Generic.List<int>();
                            if (pages.Count > 0)
                            {
                                var first = pages.Min();
                                IndexEditor.Shared.EditorState.CurrentPage = first;
                                IndexEditor.Shared.EditorState.NotifyStateChanged();
                                try { DebugLogger.Log($"ArticleList.OnArticleDoubleTapped: set CurrentPage={first} for '{toSelect.DisplayTitle}'"); } catch { }
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticleDoubleTapped: action", ex); }
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleList.OnArticleDoubleTapped: outer", ex); }
        }
        // Handle pointer pressed bubbled from children of the ListBox
        private void OnListPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try
            {
                try { DebugLogger.Log($"OnListPointerPressed invoked: Source={e.Source} ClickCount={e.ClickCount}"); } catch {}
                 // Walk up the visual tree from the event source to find the nearest Border with a Tag (article or segment lozenge)
                 var v = e.Source as Avalonia.Visual;
                 Avalonia.Visual? foundBorder = null;
                 while (v != null)
                 {
                     if (v is Border vb && vb.Tag != null)
                     {
                         foundBorder = vb;
                         break;
                     }
                     v = Avalonia.VisualTree.VisualExtensions.GetVisualParent(v) as Avalonia.Visual;
                 }
                 if (foundBorder is Border b)
                 {
                     if (b.Tag is Common.Shared.Segment)
                         OnSegmentPointerPressed(b, e);
                     else
                         OnArticlePointerPressed(b, e);
                 }
             }
             catch (Exception ex) { DebugLogger.LogException("ArticleList.OnListPointerPressed", ex); }
        }

        private void OnListDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try { DebugLogger.Log($"OnListDoubleTapped invoked: Source={e.Source}"); } catch {}
            try
            {
                // Find nearest Border with a Tag like pointer handler
                var v = e.Source as Avalonia.Visual;
                Avalonia.Visual? found = null;
                while (v != null)
                {
                    if (v is Border vb && vb.Tag != null)
                    {
                        found = vb;
                        break;
                    }
                    v = Avalonia.VisualTree.VisualExtensions.GetVisualParent(v) as Avalonia.Visual;
                }
                if (found is Border b)
                {
                    OnArticleDoubleTapped(b, e);
                }
            }
            catch (Exception ex) { DebugLogger.LogException("ArticleList.OnListDoubleTapped", ex); }
        }

        // New: handle pointer on a segment row - set active segment and jump page controller to segment start
        public void OnSegmentPointerPressed(object sender, PointerPressedEventArgs e)
         {
             try
             {
                 try { DebugLogger.Log($"OnSegmentPointerPressed invoked: Source={sender} SegTag={(sender as Border)?.Tag}"); } catch {}
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
                         try { IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first"); } catch (Exception ex) { DebugLogger.LogException("ArticleList.OnSegmentPointerPressed: toast", ex); }
                         return;
                     }

                     // Set the owner as selected/active article
                     try
                     {
                         foreach (var a in (this.DataContext as EditorStateViewModel)?.Articles ?? new System.Collections.ObjectModel.ObservableCollection<Common.Shared.ArticleLine>())
                             a.IsSelected = false;
                     }
                     catch (Exception ex) { DebugLogger.LogException("ArticleList.OnSegmentPointerPressed: clear IsSelected", ex); }
                     owner.IsSelected = true;
                     IndexEditor.Shared.EditorState.ActiveArticle = owner;

                     // Ensure the clicked segment becomes active (End == null) so IsActive becomes true
                     try
                     {
                         if (seg.End.HasValue)
                         {
                             // Opening a previously closed segment: remember the original end to allow cancel to restore it
                             seg.OriginalEnd = seg.End;
                             seg.WasNew = false;
                             seg.End = null;
                         }
                     }
                     catch (Exception ex) { DebugLogger.LogException("ArticleList.OnSegmentPointerPressed: reopen seg", ex); }

                     // Set global active segment (if it's not already)
                     IndexEditor.Shared.EditorState.ActiveSegment = seg;

                     // Jump page controller to the segment start
                     IndexEditor.Shared.EditorState.CurrentPage = seg.Start;

                     // Notify UI to update (selection, buttons, etc.)
                     IndexEditor.Shared.EditorState.NotifyStateChanged();
                 }
             }
             catch (Exception ex) { DebugLogger.LogException("ArticleList.OnSegmentPointerPressed: outer", ex); }
         }
    }
}
