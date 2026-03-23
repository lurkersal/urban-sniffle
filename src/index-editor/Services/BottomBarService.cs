using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using IndexEditor.Shared;

namespace IndexEditor.Services;

/// <summary>
/// Service to show temporary messages in the bottom status bar with visual feedback.
/// Thread-safe: all UI updates are dispatched to the UI thread.
/// </summary>
public class BottomBarService
{
    private readonly Window _window;
    private static BottomBarService? _instance;

    public static void Initialize(Window window)
    {
        _instance = new BottomBarService(window);
    }

    public static void ShowMessage(string message, bool success)
    {
        _instance?.ShowMessageInternal(message, success);
    }

    public static void ShowMessageWithLink(string message, string url, bool success)
    {
        _instance?.ShowMessageWithLinkInternal(message, url, success);
    }

    private BottomBarService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    private void ShowMessageInternal(string message, bool success)
    {
        try
        {
            // Ensure all UI access happens on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var statusText = _window.FindControl<TextBlock>("StatusText");
                    if (statusText != null)
                    {
                        statusText.Text = message;
                        statusText.Foreground = success 
                            ? new SolidColorBrush(Color.FromRgb(0, 128, 0))  // Green for success
                            : new SolidColorBrush(Color.FromRgb(192, 0, 0)); // Red for failure/info
                        
                        // Flash the background briefly
                        FlashStatusBarBackground(success);
                        
                        // Reset color after a delay
                        Task.Delay(3000).ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                try
                                {
                                    if (statusText.Text == message)
                                    {
                                        statusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Black
                                        statusText.Text = "Ready";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DebugLogger.LogException("BottomBarService: reset color", ex);
                                }
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("BottomBarService: ShowMessage UI update", ex);
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("BottomBarService: ShowMessage", ex);
        }
    }

    private void FlashStatusBarBackground(bool success)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Find the status bar border - it's at Grid.Row="2"
                    var mainGrid = _window.FindControl<Grid>("MainGrid");
                    if (mainGrid == null) return;

                    // Find the border in row 2
                    Border? statusBorder = null;
                    foreach (var child in mainGrid.Children)
                    {
                        if (child is Border border && Grid.GetRow(border) == 2)
                        {
                            statusBorder = border;
                            break;
                        }
                    }

                    if (statusBorder == null) return;

                    var originalBackground = statusBorder.Background;
                    var flashColor = success 
                        ? Color.FromRgb(200, 255, 200)  // Light green
                        : Color.FromRgb(255, 220, 220); // Light red

                    statusBorder.Background = new SolidColorBrush(flashColor);

                    // Fade back to original color
                    Task.Delay(150).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                statusBorder.Background = originalBackground;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogException("BottomBarService: restore background", ex);
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("BottomBarService: flash background", ex);
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("BottomBarService: FlashStatusBarBackground", ex);
        }
    }

    private void ShowMessageWithLinkInternal(string message, string url, bool success)
    {
        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var statusText = _window.FindControl<TextBlock>("StatusText");
                    var statusLink = _window.FindControl<TextBlock>("StatusLink");

                    if (statusText != null)
                    {
                        statusText.Text = message;
                        statusText.Foreground = success 
                            ? new SolidColorBrush(Color.FromRgb(0, 128, 0))
                            : new SolidColorBrush(Color.FromRgb(192, 0, 0));
                    }

                    if (statusLink != null && !string.IsNullOrWhiteSpace(url))
                    {
                        statusLink.Text = url;
                        statusLink.IsVisible = true;
                        statusLink.Tag = url;

                        // Wire up click handler
                        statusLink.PointerPressed -= OnStatusLinkClicked;
                        statusLink.PointerPressed += OnStatusLinkClicked;
                    }

                    FlashStatusBarBackground(success);

                    // Reset after delay
                    Task.Delay(5000).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                if (statusText != null && statusText.Text == message)
                                {
                                    statusText.Text = "Ready";
                                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                                }
                                if (statusLink != null)
                                {
                                    statusLink.IsVisible = false;
                                    statusLink.Text = string.Empty;
                                    statusLink.Tag = null;
                                    statusLink.PointerPressed -= OnStatusLinkClicked;
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogException("BottomBarService: reset with link", ex);
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("BottomBarService: ShowMessageWithLink UI update", ex);
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("BottomBarService: ShowMessageWithLink", ex);
        }
    }

    private void OnStatusLinkClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        try
        {
            if (sender is TextBlock tb && tb.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    DebugLogger.Info($"Opened URL: {url}");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("BottomBarService: open URL", ex);
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException("BottomBarService: OnStatusLinkClicked", ex);
        }
    }
}

