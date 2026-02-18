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
    private readonly Window _window;

    public FullscreenImageService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
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

            var overlay = _window.FindControl<Border>("FullscreenImageOverlay");
            var img = _window.FindControl<Image>("FullscreenImage");
            var pageText = _window.FindControl<TextBlock>("FullscreenPageNumber");

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
                _window.WindowState = Avalonia.Controls.WindowState.FullScreen;
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
            
            var overlay = _window.FindControl<Border>("FullscreenImageOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }

            // Exit fullscreen mode
            try
            {
                if (_window.WindowState == Avalonia.Controls.WindowState.FullScreen)
                {
                    _window.WindowState = Avalonia.Controls.WindowState.Normal;
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
            var overlay = _window.FindControl<Border>("FullscreenImageOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("FullscreenImageService.IsFullscreenVisible", ex);
            return false;
        }
    }
}

