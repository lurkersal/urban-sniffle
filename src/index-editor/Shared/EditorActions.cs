using System;
using System.Linq;
using Common.Shared;

namespace IndexEditor.Shared
{
    public static class EditorActions
    {
        // Cancel the currently active segment if one exists.
        // Behavior:
        // - If segment.WasNew -> remove it from its article
        // - Else if segment.OriginalEnd.HasValue -> restore OriginalEnd
        // - Else -> set End = Start (close to single page)
        // After cancellation, set WasNew=false, clear ActiveSegment and notify state changed.
        public static void CancelActiveSegment()
        {
            try
            {
                var seg = EditorState.ActiveSegment;
                if (seg == null || !seg.IsActive) return;
                var art = EditorState.ActiveArticle;
                if (seg.WasNew)
                {
                    try { art?.Segments?.Remove(seg); } catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: remove seg", ex); }
                }
                else if (seg.OriginalEnd.HasValue)
                {
                    seg.End = seg.OriginalEnd;
                    seg.OriginalEnd = null;
                }
                else
                {
                    seg.End = seg.Start;
                }
                seg.WasNew = false;
                try { seg.CurrentPreviewEnd = null; } catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: clear preview", ex); }
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.CancelActiveSegment: outer", ex); }
        }

        // Create a new ACTIVE segment at the current page for the active article. Returns true if created.
        public static bool AddSegmentAtCurrentPage()
        {
            try
            {
                var art = EditorState.ActiveArticle;
                if (art == null)
                {
                    ToastService.Show("No active article selected");
                    return false;
                }
                // If there's any active segment open elsewhere, disallow creating a new one
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                {
                    ToastService.Show("Finish or cancel the active segment first");
                    try { Console.WriteLine("[TRACE] EditorActions.AddSegmentAtCurrentPage: blocked - active segment exists"); } catch {};
                    return false;
                }

                var page = EditorState.CurrentPage;

                // If the page already belongs to this article, attempt to find the segment that contains it and make it active
                if (art.Pages != null && art.Pages.Contains(page))
                {
                    try
                    {
                        var existingSeg = art.Segments?.FirstOrDefault(s =>
                        {
                            try
                            {
                                var sEnd = s.End ?? s.Start;
                                return page >= s.Start && page <= sEnd;
                            }
                            catch { return false; }
                        });

                        if (existingSeg != null)
                        {
                            // Open the existing (closed) segment so it becomes active
                            if (existingSeg.End.HasValue)
                            {
                                existingSeg.OriginalEnd = existingSeg.End;
                                existingSeg.WasNew = false;
                                existingSeg.End = null; // make active
                            }
                            // Set global active segment and article
                            EditorState.ActiveSegment = existingSeg;
                            try { existingSeg.CurrentPreviewEnd = EditorState.CurrentPage; } catch (Exception ex) { DebugLogger.LogException("EditorActions.AddSegmentAtCurrentPage:set preview on existing", ex); }
                            try { Console.WriteLine($"[TRACE] EditorActions.AddSegmentAtCurrentPage: activated existing segment start={existingSeg.Start} article='{art.Title ?? "<untitled>"}'"); } catch {};
                            EditorState.NotifyStateChanged();
                            return true;
                        }
                        else
                        {
                            // No existing segment found that contains the page: treat as 'page already in article' and do not create duplicate
                            ToastService.Show($"Page {page} already in article");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("EditorActions.AddSegmentAtCurrentPage: find existing segment", ex);
                        ToastService.Show($"Page {page} already in article");
                        return false;
                    }
                }

                // NOTE: Intentionally do not activate segments from OTHER articles. Add/activate applies only to the current active article.

                // Page is not in article -> create a new active segment
                 var seg = new Common.Shared.Segment(page);
                 seg.WasNew = true;
                 seg.End = null;
                 art.Segments.Add(seg);
                 if (art.Pages == null) art.Pages = new System.Collections.Generic.List<int> { page };
                 else if (!art.Pages.Contains(page)) { art.Pages.Add(page); art.Pages.Sort(); }
                 // Set global active segment and active article so UI locks selection to this article
                 EditorState.ActiveSegment = seg;
                 try { seg.CurrentPreviewEnd = EditorState.CurrentPage; } catch (Exception ex) { DebugLogger.LogException("EditorActions.AddSegmentAtCurrentPage:set preview", ex); }
                 try { Console.WriteLine($"[TRACE] EditorActions.AddSegmentAtCurrentPage: created new active segment start={seg.Start} article='{art.Title ?? "<untitled>"}'"); } catch {};
                 EditorState.NotifyStateChanged();
                 return true;
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.AddSegmentAtCurrentPage: outer", ex); return false; }
        }

        // Request focus for the ArticleEditor's Title TextBox.
        // This increments the EditorState counter which ArticleEditor instances observe and will
        // attempt to focus the TitleTextBox when they see the request.
        public static void FocusArticleTitle()
        {
            try
            {
                EditorState.RequestArticleEditorFocus();
                try { Console.WriteLine("[TRACE] EditorActions.FocusArticleTitle: requested focus"); } catch { }
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.FocusArticleTitle", ex); }
        }

        // Create a new closed single-page article at the current page. Does not make it active.
        public static void CreateNewArticle()
        {
            try
            {
                if (EditorState.ActiveSegment != null && EditorState.ActiveSegment.IsActive)
                {
                    ToastService.Show("End or cancel the active segment before adding a new article");
                    return;
                }
                var article = new Common.Shared.ArticleLine();
                article.Pages = new System.Collections.Generic.List<int> { EditorState.CurrentPage };
                if (EditorState.Articles == null)
                    EditorState.Articles = new System.Collections.Generic.List<Common.Shared.ArticleLine> { article };
                else
                {
                    int insertIndex = EditorState.Articles.FindIndex(a => a.Pages != null && a.Pages.Count > 0 && a.Pages.Min() > article.Pages.Min());
                    if (insertIndex == -1) EditorState.Articles.Add(article); else EditorState.Articles.Insert(insertIndex, article);
                }
                EditorState.ActiveArticle = article;
                bool alreadyHas = false;
                try
                {
                    if (article.Segments != null)
                    {
                        foreach (var s in article.Segments)
                        {
                            try
                            {
                                var start = s.Start;
                                var end = s.End ?? s.Start;
                                if (EditorState.CurrentPage >= start && EditorState.CurrentPage <= end) { alreadyHas = true; break; }
                            }
                            catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: checking existing segments", ex); }
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: outer segment check", ex); }
                if (!alreadyHas)
                {
                    var seg = new Common.Shared.Segment(EditorState.CurrentPage);
                    seg.End = EditorState.CurrentPage;
                    seg.WasNew = false;
                    article.Segments?.Add(seg);
                }
                EditorState.NotifyStateChanged();
                
                // Select the new article in the ViewModel so it appears in the article list and editor
                try
                {
                    // Find the MainWindow and get the EditorStateViewModel
                    var app = Avalonia.Application.Current;
                    if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow;
                        if (mainWindow != null)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                try
                                {
                                    var vm = mainWindow.DataContext as IndexEditor.Views.EditorStateViewModel;
                                    if (vm != null)
                                    {
                                        // Find the article in the VM's collection (it should match by reference or pages)
                                        var inList = vm.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                                                  ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages));
                                        var toSelect = inList ?? article;
                                        
                                        // Select the article
                                        if (vm.SelectArticleCommand != null && vm.SelectArticleCommand.CanExecute(toSelect))
                                            vm.SelectArticleCommand.Execute(toSelect);
                                        vm.SelectedArticle = toSelect;
                                        
                                        DebugLogger.Log($"EditorActions.CreateNewArticle: Selected article in ViewModel");
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: select article in VM", ex); }
                            }, Avalonia.Threading.DispatcherPriority.Background);
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: find and select article", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.CreateNewArticle: outer", ex); }
        }

        // End the currently active segment by setting its End to EditorState.CurrentPage and update article pages accordingly.
        public static void EndActiveSegment()
        {
            try
            {
                var seg = EditorState.ActiveSegment;
                if (seg == null || !seg.IsActive) return;
                var start = seg.Start;
                var end = EditorState.CurrentPage;
                if (end < start) (start, end) = (end, start);
                var art = EditorState.ActiveArticle;
                if (art != null)
                {
                    var newPages = new System.Collections.Generic.List<int>(art.Pages ?? new System.Collections.Generic.List<int>());
                    for (int p = start; p <= end; p++) if (!newPages.Contains(p)) newPages.Add(p);
                    newPages.Sort();
                    art.Pages = newPages;
                }
                seg.End = EditorState.CurrentPage;
                seg.CurrentPreviewEnd = null;
                EditorState.ActiveSegment = null;
                EditorState.NotifyStateChanged();
            }
            catch (Exception ex) { DebugLogger.LogException("EditorActions.EndActiveSegment", ex); }
        }
    }
}
