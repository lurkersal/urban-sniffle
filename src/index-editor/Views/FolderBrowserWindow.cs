using System;

namespace IndexEditor.Views
{
    [Obsolete("FolderBrowserWindow is removed. Use Window.StorageProvider or OpenFolderDialog directly.")]
    public static class FolderBrowserWindow
    {
        public static System.Threading.Tasks.Task<string?> ShowDialogAsync(Avalonia.Controls.Window? parent, string? start = null)
        {
            throw new NotSupportedException("FolderBrowserWindow is no longer supported. Use OpenFolderDialog.ShowAsync or StorageProvider APIs.");
        }
    }
}
