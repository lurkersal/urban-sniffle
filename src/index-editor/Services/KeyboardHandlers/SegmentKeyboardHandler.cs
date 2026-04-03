using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using IndexEditor.Shared;

namespace IndexEditor.Services.KeyboardHandlers;

/// <summary>
/// Handles keyboard shortcuts related to segment operations.
/// - Ctrl+A: Add segment at current page
/// - Ctrl+Enter: End active segment or focus title
/// - Enter: End active segment or focus article editor
/// - Esc: Cancel active segment or move focus from editor
/// </summary>
public class SegmentKeyboardHandler : IKeyboardShortcutHandler
{
    private readonly Window _window;
    private readonly IEditorState _editorState;

    public SegmentKeyboardHandler(Window window, IEditorState editorState)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
    }

    public int Priority => 100; // High priority for segment operations

    public bool TryHandle(KeyEventArgs e)
    {
        // Esc: Always handle escape - either cancel segment or exit editor focus
        if (e.Key == Key.Escape)
        {
            return HandleEscape(e);
        }

        // When the article editor has focus, don't intercept any shortcuts except ESC
        // to allow normal text editing (Ctrl+A for Select All, Ctrl+C/V for copy/paste, etc.)
        if (_editorState.IsArticleEditorFocused)
        {
            return false;
        }

        // Ctrl+A: Add segment at current page (only when editor is NOT focused)
        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlA(e);
            return true;
        }

        // Ctrl+Enter: End active segment or focus title (only when editor is NOT focused)
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCtrlEnter(e);
            return true;
        }

        // Enter: End active segment or focus article editor (only when editor NOT focused)
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            return HandleEnter(e);
        }


        return false;
    }

    private void HandleCtrlA(KeyEventArgs e)
    {
        try
        {
            try { ToastService.Show("Ctrl+A: add segment"); } 
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: ToastService.Show Ctrl+A", ex); }
            
            try { EditorActions.AddSegmentAtCurrentPage(); } 
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: AddSegmentAtCurrentPage", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Ctrl+A handler", ex); }
        
        e.Handled = true;
    }

    private void HandleCtrlEnter(KeyEventArgs e)
    {
        try
        {
            try
            {
                // Ctrl+Enter: end active segment if present, otherwise focus Title textbox in ArticleEditor (global)
                var active = _editorState.ActiveSegment;
                if (active != null && active.IsActive)
                {
                    try { EditorActions.EndActiveSegment(); } 
                    catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: EndActiveSegment via Ctrl+Enter", ex); }
                    
                    try { ToastService.Show("Segment ended"); } 
                    catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: ToastService.Show on end via Ctrl+Enter", ex); }
                }
                else
                {
                    try { ToastService.Show("Ctrl+Enter: focus title"); } 
                    catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: ToastService.Show Ctrl+Enter focus", ex); }
                    
                    try { EditorActions.FocusArticleTitle(); } 
                    catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: FocusArticleTitle", ex); }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Ctrl+Enter handler", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Ctrl+Enter outer", ex); }
        
        e.Handled = true;
    }

    private bool HandleEnter(KeyEventArgs e)
    {
        // If an active segment exists, end it here
        var seg = _editorState.ActiveSegment;
        if (seg != null && seg.IsActive)
        {
            try
            {
                // Capture start and intended end for user feedback
                var start = seg.Start;
                var end = _editorState.CurrentPage;
                if (end < start) (start, end) = (end, start);

                // Use EditorActions to end the active segment and update pages
                try { EditorActions.EndActiveSegment(); } 
                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: EditorActions.EndActiveSegment", ex); }

                // User feedback
                try { ToastService.Show($"Segment ended ({start}-{end})"); } 
                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: ToastService.Show on end segment", ex); }
                
                // Return focus to page controller
                try { FocusPageController(); }
                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: FocusPageController after end", ex); }
            }
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: EndActiveSegment", ex); }
            
            e.Handled = true;
            return true;
        }

        // No active segment: focus the first editable field in the article editor (Title textbox)
        try
        {
            try { _editorState.RequestArticleEditorFocus(); } 
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: RequestArticleEditorFocus", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Enter handler", ex); }
        
        e.Handled = true;
        return true;
    }

    private bool HandleEscape(KeyEventArgs e)
    {
        // First check if delete confirmation overlay is visible
        try
        {
            var overlay = _window.FindControl<Border>("DeleteArticleConfirmOverlay");
            if (overlay != null && overlay.IsVisible)
            {
                overlay.IsVisible = false;
                e.Handled = true;
                return true;
            }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Esc dismiss overlay", ex); }

        try
        {
            var editorFocused = _editorState.IsArticleEditorFocused;
            var hadActive = _editorState.ActiveSegment != null && _editorState.ActiveSegment.IsActive;

            // If the editor has focus, ask it to end any inner editing (close dropdowns, clear selections)
            if (editorFocused)
            {
                EndArticleEditing();
            }

            // If editor has focus but no active segment, move focus to article list
            if (editorFocused && !hadActive)
            {
                FocusArticleList(e);
                return true;
            }

            // Cancel active segment if present
            if (hadActive)
            {
                CancelActiveSegment(e);
                return true;
            }
            
            // Exit fullscreen if in fullscreen mode
            if (_window.WindowState == Avalonia.Controls.WindowState.FullScreen)
            {
                _window.WindowState = Avalonia.Controls.WindowState.Normal;
                e.Handled = true;
                return true;
            }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Esc overall", ex); }

        return false;
    }

    /// <summary>
    /// Ends article editing when ESC is pressed - moves focus away from editor and clears text selections.
    /// </summary>
    private void EndArticleEditing()
    {
        try
        {
            // Try direct FindControl first (for backward compatibility)
            var aeCtrl = _window.FindControl<Views.ArticleEditor>("ArticleEditorControl") 
                      ?? _window.FindControl<Views.ArticleEditor>("ArticleEditor");
            
            // If not found, search the visual tree (needed when control is inside DataTemplate)
            if (aeCtrl == null)
            {
                try
                {
                    var editors = _window.GetVisualDescendants().OfType<Views.ArticleEditor>().ToList();
                    if (editors.Count > 0)
                    {
                        aeCtrl = editors[0]; // Use the first one
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("SegmentKeyboardHandler.EndArticleEditing: visual tree search", ex);
                }
            }
            
            if (aeCtrl != null)
            {
                try { aeCtrl.EndEdit(); } 
                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: EndEdit on Esc", ex); }
            }
            else
            {
                DebugLogger.Log("[WARNING] SegmentKeyboardHandler.EndArticleEditing: ArticleEditor control not found");
            }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: EndEdit lookup on Esc", ex); }
    }

    private void FocusArticleList(KeyEventArgs e)
    {
        try
        {
            // Ask the ArticleEditor to end any active editing
            EndArticleEditing();

            var articleList = _window.FindControl<Views.ArticleList>("ArticleListControl");
            if (articleList != null)
            {
                var lb = articleList.FindControl<ListBox>("ArticlesListBox");
                if (lb != null)
                {
                    // Defer focus to the UI thread to avoid focus race conditions
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            // Ensure the list is enabled
                            if (!lb.IsEnabled) lb.IsEnabled = true;
                            
                            // Ensure there is a selected item so focus lands predictably
                            if (lb.SelectedIndex < 0 && lb.ItemCount > 0) lb.SelectedIndex = 0;
                            
                            // Clear editor-focused flag since focus is about to move
                            try { _editorState.IsArticleEditorFocused = false; } catch { }

                            // Retry loop: try several times to set focus
                            bool focused = false;
                            for (int attempt = 0; attempt < 6; attempt++)
                            {
                                try
                                {
                                    lb.Focus();
                                    await System.Threading.Tasks.Task.Delay(40);
                                    if (lb.IsFocused)
                                    {
                                        focused = true;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DebugLogger.LogException("SegmentKeyboardHandler: Esc focus attempt", ex);
                                }
                            }

                            if (!focused)
                            {
                                // Fallback: try focusing the ArticleList control itself
                                try { articleList.Focus(); }
                                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Esc focus articleList fallback", ex); }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogException("SegmentKeyboardHandler: Esc focus ArticleList (UIThread)", ex);
                        }
                    });
                }
            }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: Esc focus ArticleList", ex); }
        
        e.Handled = true;
    }

    private void FocusPageController()
    {
        try
        {
            // Find the PageControllerView and focus its PageInput TextBox
            var pageController = _window.FindControl<Views.PageControllerView>("PageControllerView");
            if (pageController != null)
            {
                // Add a small delay to ensure segment ending and UI updates are complete
                // before transferring focus, preventing article from disappearing
                System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            var pageInput = pageController.FindControl<TextBox>("PageInput");
                            if (pageInput != null)
                            {
                                pageInput.Focus();
                                // Select all text so user can easily type a new page number
                                try { pageInput.SelectAll(); }
                                catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: SelectAll on PageInput", ex); }
                            }
                        }
                        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: FocusPageController (UIThread)", ex); }
                    });
                });
            }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: FocusPageController", ex); }
    }

    private void CancelActiveSegment(KeyEventArgs e)
    {
        try
        {
            // Ensure editing has been ended in the editor before cancelling the segment
            EndArticleEditing();
            
            EditorActions.CancelActiveSegment();
            
            try { _editorState.NotifyStateChanged(); } 
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: NotifyStateChanged", ex); }
            
            try { ToastService.Show("Segment cancelled"); } 
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: ToastService.Show on cancel", ex); }
            
            // Return focus to page controller
            try { FocusPageController(); }
            catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: FocusPageController after cancel", ex); }
        }
        catch (Exception ex) { DebugLogger.LogException("SegmentKeyboardHandler: cancel active segment", ex); }
        
        e.Handled = true;
    }
}

