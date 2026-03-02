using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using IndexEditor.Shared;

namespace IndexEditor.Shared
{
    public static class FolderPicker
    {
        public static async Task<string?> PickFolderAsync(Window? parent, string? start = null)
        {
            // If no start path provided, use current working directory
            if (string.IsNullOrWhiteSpace(start))
            {
                try
                {
                    start = System.IO.Directory.GetCurrentDirectory();
                    DebugLogger.Log($"FolderPicker: No start path, using current directory: {start}");
                }
                catch { }
            }
            else
            {
                // Normalize to absolute path to prevent issues with relative paths like './'
                try
                {
                    var originalStart = start;
                    if (!System.IO.Path.IsPathRooted(start))
                    {
                        start = System.IO.Path.GetFullPath(start);
                        DebugLogger.Log($"FolderPicker: Normalized relative path '{originalStart}' to '{start}'");
                    }
                    else
                    {
                        DebugLogger.Log($"FolderPicker: Starting with absolute path: {start}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("FolderPicker: normalize start path", ex);
                    DebugLogger.Log($"FolderPicker: Starting with path (normalization failed): {start}");
                }
            }

            // Try to get the TopLevel (Window) for the storage provider
            var topLevel = parent ?? TopLevel.GetTopLevel(parent as Visual);
            
            if (topLevel == null)
            {
                DebugLogger.Log("FolderPicker: No TopLevel found, trying application main window");
                var app = Avalonia.Application.Current;
                if (app?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    topLevel = desktop.MainWindow;
                }
            }

            if (topLevel == null)
            {
                DebugLogger.Log("FolderPicker: No TopLevel available, cannot open dialog");
                return null;
            }

            try
            {
                var storageProvider = topLevel.StorageProvider;
                if (storageProvider == null)
                {
                    DebugLogger.Log("FolderPicker: StorageProvider is null");
                    return null;
                }

                DebugLogger.Log($"FolderPicker: Using StorageProvider, can pick folders: {storageProvider.CanPickFolder}");
                
                if (!storageProvider.CanPickFolder)
                {
                    DebugLogger.Log("FolderPicker: StorageProvider cannot pick folders");
                    return null;
                }

                // Try to get the suggested start location
                IStorageFolder? suggestedStartLocation = null;
                if (!string.IsNullOrWhiteSpace(start) && System.IO.Directory.Exists(start))
                {
                    try
                    {
                        // Convert path to file URI
                        var uriPath = start.StartsWith("/") ? "file://" + start : "file:///" + start.Replace("\\", "/");
                        var uri = new Uri(uriPath);
                        suggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(uri);
                        
                        if (suggestedStartLocation != null)
                        {
                            DebugLogger.Log($"FolderPicker: Got suggested start location from {start}");
                        }
                        else
                        {
                            DebugLogger.Log($"FolderPicker: Could not get IStorageFolder from {start}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogException("FolderPicker: get suggested start location", ex);
                    }
                }

                // Create picker options
                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Folder",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedStartLocation
                };

                DebugLogger.Log("FolderPicker: Opening folder picker dialog");
                var result = await storageProvider.OpenFolderPickerAsync(options);

                if (result == null || result.Count == 0)
                {
                    DebugLogger.Log("FolderPicker: User cancelled or no folder selected");
                    return null;
                }

                var selectedFolder = result[0];
                DebugLogger.Log($"FolderPicker: Selected folder type: {selectedFolder?.GetType().Name ?? "null"}");

                // Try to get the local path
                if (selectedFolder != null)
                {
                    var localPath = selectedFolder.TryGetLocalPath();
                    if (!string.IsNullOrWhiteSpace(localPath))
                    {
                        DebugLogger.Log($"FolderPicker: Returning selected path: {localPath}");
                        return localPath;
                    }
                }

                // Fallback: try to get path from the Path property (which is a Uri)
                if (selectedFolder?.Path != null)
                {
                    var path = selectedFolder.Path.LocalPath;
                    DebugLogger.Log($"FolderPicker: Returning path from Uri.LocalPath: {path}");
                    return path;
                }

                DebugLogger.Log("FolderPicker: Could not extract path from selected folder");
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("FolderPicker: storage provider dialog", ex);
                return null;
            }
        }
    }
}
