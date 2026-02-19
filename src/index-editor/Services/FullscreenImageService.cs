using System;
using Avalonia.Controls;
using Avalonia.Media;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing fullscreen image display.
/// Handles showing images in fullscreen mode and toggling between fullscreen states.
/// </summary>
public class FullscreenImageService
{
    private readonly IWindowStateManager _windowStateManager;
    private readonly IControlFinder _controlFinder;

    public FullscreenImageService(Window window)
        : this(new WindowStateManager(window), new WindowControlFinder(window))
    {
    }

    public FullscreenImageService(IWindowStateManager windowStateManager, IControlFinder controlFinder)
    {
        _windowStateManager = windowStateManager ?? throw new ArgumentNullException(nameof(windowStateManager));
        _controlFinder = controlFinder ?? throw new ArgumentNullException(nameof(controlFinder));
    }

    /// <summary>
    /// Shows an image in fullscreen mode.
    /// </summary>
    /// <param name="imageSource">The image to display</param>
    /// <param name="pageNumber">The page number to show in the overlay</param>
    public void ShowFullscreen(IImage imageSource, int pageNumber)
    {
        try
        {
            DebugLogger.Log($"FullscreenImageService.ShowFullscreen: showing page {pageNumber}");

            var overlay = _controlFinder.FindControl<Border>("FullscreenImageOverlay");
            var img = _controlFinder.FindControl<Image>("FullscreenImage");
            var pageText = _controlFinder.FindControl<TextBlock>("FullscreenPageNumber");

            if (overlay == null || img == null)
            {
                DebugLogger.Log("FullscreenImageService.ShowFullscreen: overlay controls not found");
                return;
            }

            img.Source = imageSource;
            if (pageText != null)
            {
                pageText.Text = $"Page {pageNumber}";
            }

            overlay.IsVisible = true;

            // Enter fullscreen mode
            try
            {
                _windowStateManager.WindowState = Avalonia.Controls.WindowState.FullScreen;
                DebugLogger.Log("FullscreenImageService.ShowFullscreen: entered fullscreen mode");
            }
            catch (Exception fsEx)
            {
                DebugLogger.LogException("FullscreenImageService.ShowFullscreen: enter fullscreen", fsEx);
            }

            DebugLogger.Log("FullscreenImageService.ShowFullscreen: overlay shown");
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FullscreenImageService.ShowFullscreen", ex);
        }
    }

    /// <summary>
    /// Closes the fullscreen image overlay and exits fullscreen mode.
    /// </summary>
    public void CloseFullscreen()
    {
        try
        {
            DebugLogger.Log("FullscreenImageService.CloseFullscreen: closing overlay");
            
            var overlay = _controlFinder.FindControl<Border>("FullscreenImageOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }

            // Exit fullscreen mode
            try
            {
                if (_windowStateManager.WindowState == Avalonia.Controls.WindowState.FullScreen)
                {
                    _windowStateManager.WindowState = Avalonia.Controls.WindowState.Normal;
                    DebugLogger.Log("FullscreenImageService.CloseFullscreen: exited fullscreen mode");
                }
            }
            catch (Exception fsEx)
            {
                DebugLogger.LogException("FullscreenImageService.CloseFullscreen: exit fullscreen", fsEx);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FullscreenImageService.CloseFullscreen", ex);
        }
    }

    /// <summary>
    /// Checks if fullscreen image overlay is currently visible.
    /// </summary>
    public bool IsFullscreenVisible()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("FullscreenImageOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FullscreenImageService.IsFullscreenVisible", ex);
            return false;
        }
    }
}

