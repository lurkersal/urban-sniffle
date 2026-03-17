using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using IndexEditor.Shared;
using IndexEditor.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IndexEditor.Services
{
    /// <summary>
    /// Service responsible for managing focus when creating new articles.
    /// Handles the complex logic of finding and focusing the appropriate editor controls.
    /// </summary>
    public class ArticleFocusManager : IArticleFocusManager
    {
        private readonly IEditorState _editorState;

        public ArticleFocusManager(IEditorState editorState)
        {
            _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
        }

        /// <summary>
        /// Manages the focus flow after creating a new article
        /// </summary>
        /// <param name="article">The newly created article</param>
        /// <param name="visualRoot">The visual root (typically MainWindow)</param>
        /// <param name="dataContext">The DataContext (typically EditorStateViewModel)</param>
        public void SetupFocusForNewArticle(
            Common.Shared.ArticleLine article, 
            object? visualRoot,
            object? dataContext)
        {
            if (article == null || visualRoot == null)
            {
                return;
            }

            var visual = visualRoot as Visual;
            if (visual == null)
            {
                DebugLogger.Log("ArticleFocusManager: visualRoot is not a Visual, cannot proceed");
                return;
            }

            try
            {
                // Request editor focus through state service
                _editorState.RequestArticleEditorFocus();
                DebugLogger.Log("ArticleFocusManager: requested ArticleEditor focus");

                var vm = dataContext as EditorStateViewModel;
                if (vm != null)
                {
                    // Schedule selection and focus on UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        SelectArticleInViewModel(article, vm, visual);
                    }, DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.SetupFocusForNewArticle", ex);
            }
        }

        /// <summary>
        /// Selects the article in the view model and triggers focus attempts
        /// </summary>
        private void SelectArticleInViewModel(
            Common.Shared.ArticleLine article,
            EditorStateViewModel vm,
            Visual visualRoot)
        {
            try
            {
                Console.WriteLine("[DEBUG] ArticleFocusManager: scheduling selection+focus");

                // Find the article in the VM's list
                var inList = vm.Articles.FirstOrDefault(a => ReferenceEquals(a, article))
                          ?? vm.Articles.FirstOrDefault(a => a.Pages != null && article.Pages != null 
                              && a.Pages.SequenceEqual(article.Pages));
                var toSelect = inList ?? article;

                // Execute selection command
                if (vm.SelectArticleCommand.CanExecute(toSelect))
                {
                    vm.SelectArticleCommand.Execute(toSelect);
                }

                vm.SelectedArticle = toSelect;

                // Ensure the ListBox shows the selection
                UpdateListBoxSelection(toSelect, visualRoot);

                // Re-notify after a short delay to help DataTemplate creation
                ScheduleStateNotification();

                // Request focus again
                _editorState.RequestArticleEditorFocus();
                Console.WriteLine("[DEBUG] ArticleFocusManager: RequestArticleEditorFocus called after scheduling");

                // Start focus attempts
                StartFocusAttempts(visualRoot);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.SelectArticleInViewModel", ex);
            }
        }

        /// <summary>
        /// Updates the ListBox selection to show the newly created article
        /// </summary>
        private void UpdateListBoxSelection(Common.Shared.ArticleLine article, Visual visualRoot)
        {
            try
            {
                var window = visualRoot as Window;
                var articleList = window?.FindControl<Views.ArticleList>("ArticleListControl");
                if (articleList != null)
                {
                    var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                    if (lb != null)
                    {
                        lb.SelectedItem = article;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.UpdateListBoxSelection", ex);
            }
        }

        /// <summary>
        /// Schedules a state notification after a short delay
        /// </summary>
        private void ScheduleStateNotification()
        {
            try
            {
                async void ReNotifyAsync()
                {
                    try
                    {
                        await Task.Delay(120).ConfigureAwait(false);
                        Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                _editorState.NotifyStateChanged();
                                Console.WriteLine("[DEBUG] ArticleFocusManager: re-notified EditorState after selection");
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogException("ArticleFocusManager.ReNotify", ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("ArticleFocusManager.ReNotify background", ex);
                    }
                }
                ReNotifyAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.ScheduleStateNotification", ex);
            }
        }

        /// <summary>
        /// Starts multiple focus attempts using different strategies
        /// </summary>
        private void StartFocusAttempts(Visual visualRoot)
        {
            try
            {
                var mainWindow = visualRoot as Window;
                if (mainWindow == null) return;

                // Strategy 1: Direct ArticleEditor focus
                AttemptArticleEditorFocus(mainWindow);

                // Strategy 2: Retry loop for editor controls
                StartFocusRetryLoop(mainWindow);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.StartFocusAttempts", ex);
            }
        }

        /// <summary>
        /// Attempts to focus the ArticleEditor control directly
        /// </summary>
        private void AttemptArticleEditorFocus(Window mainWindow)
        {
            try
            {
                var ae = mainWindow.FindControl<Views.ArticleEditor>("ArticleEditorControl")
                         ?? mainWindow.FindControl<Views.ArticleEditor>("ArticleEditor");

                if (ae != null)
                {
                    Console.WriteLine("[DEBUG] ArticleFocusManager: performing forced focus retries on ArticleEditor instance");
                    
                    async void ForcedFocusLoopAsync()
                    {
                        try
                        {
                            const int attempts = 10;
                            const int delayMs = 80;
                            for (int i = 0; i < attempts; i++)
                            {
                                await Task.Delay(delayMs).ConfigureAwait(false);
                                Dispatcher.UIThread.Post(() =>
                                {
                                    try
                                    {
                                        Console.WriteLine($"[DEBUG] ArticleFocusManager: forced focus attempt {i}");
                                        try { ae.FocusTitle(); } catch { }
                                        try { ae.FocusEditor(); } catch { }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.LogException("ArticleFocusManager.ForcedFocus post", ex);
                                    }
                                });
                            }
                            Console.WriteLine("[DEBUG] ArticleFocusManager: forced focus retry loop finished");
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("ArticleFocusManager.ForcedFocusLoop", ex);
                        }
                    }
                    ForcedFocusLoopAsync();
                }
                else
                {
                    Console.WriteLine("[DEBUG] ArticleFocusManager: ArticleEditor instance not found on Window");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.AttemptArticleEditorFocus", ex);
            }
        }

        /// <summary>
        /// Starts a retry loop that attempts to find and focus editor controls
        /// </summary>
        private void StartFocusRetryLoop(Window mainWindow)
        {
            try
            {
                async void FocusRetryAsync()
                {
                    try
                    {
                        const int attempts = 12;
                        const int delayMs = 120;
                        
                        for (int i = 0; i < attempts; i++)
                        {
                            await Task.Delay(delayMs).ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() =>
                            {
                                TryFocusEditorControls(mainWindow);
                            });
                        }
                        Console.WriteLine("[DEBUG] ArticleFocusManager: focus attempts completed");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("ArticleFocusManager.FocusRetryLoop", ex);
                    }
                }
                FocusRetryAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.StartFocusRetryLoop", ex);
            }
        }

        /// <summary>
        /// Tries multiple strategies to focus editor controls
        /// </summary>
        private void TryFocusEditorControls(Window mainWindow)
        {
            try
            {
                // Strategy 1: Try to find controls within EditorContentHost
                if (TryFocusInContentHost(mainWindow)) return;

                // Strategy 2: Try to find ArticleEditor control
                if (TryFocusArticleEditor(mainWindow)) return;

                // Strategy 3: Direct search on the window
                TryFocusDirectOnWindow(mainWindow);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.TryFocusEditorControls", ex);
            }
        }

        /// <summary>
        /// Attempts to focus controls within the ContentHost
        /// </summary>
        private bool TryFocusInContentHost(Window mainWindow)
        {
            try
            {
                var host = mainWindow.FindControl<ContentControl>("EditorContentHost") 
                        ?? mainWindow.FindControl<ContentControl>("EditorContent");
                
                if (host?.Content is Control hostContent)
                {
                    var tb = hostContent.FindControl<TextBox>("TitleTextBox");
                    if (tb != null)
                    {
                        Console.WriteLine("[DEBUG] ArticleFocusManager: focusing TitleTextBox inside host.Content");
                        tb.Focus();
                        return true;
                    }

                    var cb = hostContent.FindControl<ComboBox>("CategoryComboBox");
                    if (cb != null)
                    {
                        Console.WriteLine("[DEBUG] ArticleFocusManager: focusing CategoryComboBox inside host.Content");
                        cb.Focus();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.TryFocusInContentHost", ex);
            }
            return false;
        }

        /// <summary>
        /// Attempts to focus through ArticleEditor helper methods
        /// </summary>
        private bool TryFocusArticleEditor(Window mainWindow)
        {
            try
            {
                var ae = mainWindow.FindControl<Views.ArticleEditor>("ArticleEditorControl")
                         ?? mainWindow.FindControl<Views.ArticleEditor>("ArticleEditor");
                
                if (ae != null)
                {
                    Console.WriteLine("[DEBUG] ArticleFocusManager: calling ae.FocusTitle()/FocusEditor()");
                    try { ae.FocusTitle(); } catch { }
                    try { ae.FocusEditor(); } catch { }
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.TryFocusArticleEditor", ex);
            }
            return false;
        }

        /// <summary>
        /// Attempts to focus controls by searching directly on the window
        /// </summary>
        private bool TryFocusDirectOnWindow(Window mainWindow)
        {
            try
            {
                var tbDirect = mainWindow.FindControl<TextBox>("TitleTextBox");
                if (tbDirect != null)
                {
                    Console.WriteLine("[DEBUG] ArticleFocusManager: focusing TitleTextBox directly on Window");
                    try { tbDirect.Focus(); } catch { }
                    return true;
                }

                var cbDirect = mainWindow.FindControl<ComboBox>("CategoryComboBox");
                if (cbDirect != null)
                {
                    Console.WriteLine("[DEBUG] ArticleFocusManager: focusing CategoryComboBox directly on Window");
                    try { cbDirect.Focus(); } catch { }
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("ArticleFocusManager.TryFocusDirectOnWindow", ex);
            }
            return false;
        }
    }
}




