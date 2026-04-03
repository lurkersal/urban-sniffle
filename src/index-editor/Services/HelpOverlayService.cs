using System;
using Avalonia.Controls;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service for managing the help overlay.
/// </summary>
public class HelpOverlayService : IHelpOverlayService
{
    private readonly IControlFinder _controlFinder;

    public HelpOverlayService(IControlFinder controlFinder)
    {
        _controlFinder = controlFinder ?? throw new ArgumentNullException(nameof(controlFinder));
    }

    /// <summary>
    /// Shows the help overlay.
    /// </summary>
    public void ShowHelpOverlay()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("HelpOverlayService.ShowHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Closes the help overlay.
    /// </summary>
    public void CloseHelpOverlay()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("HelpOverlayService.CloseHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Toggles the help overlay visibility.
    /// </summary>
    public void ToggleHelpOverlay()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
            if (overlay != null)
            {
                overlay.IsVisible = !overlay.IsVisible;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("HelpOverlayService.ToggleHelpOverlay", ex);
        }
    }

    /// <summary>
    /// Checks if the help overlay is currently visible.
    /// </summary>
    public bool IsHelpOverlayVisible()
    {
        try
        {
            var overlay = _controlFinder.FindControl<Border>("HelpOverlay");
            return overlay?.IsVisible ?? false;
        }
        catch
        {
            return false;
        }
    }
}

