using System;
using System.IO;

namespace IndexEditor.Shared
{
    public static class RecentFolderStore
    {
        private static string GetStoragePath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appData)) appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? ".";
                var dir = Path.Combine(appData, "urban-sniffle");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "index-editor-lastfolder.txt");
            }
            catch
            {
                return Path.Combine(".", "index-editor-lastfolder.txt");
            }
        }

        public static string? GetLastOpenedFolder()
        {
            try
            {
                var path = GetStoragePath();
                if (!File.Exists(path)) return null;
                var txt = File.ReadAllText(path).Trim();
                if (string.IsNullOrWhiteSpace(txt)) return null;
                return txt;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RecentFolderStore.GetLastOpenedFolder", ex);
                return null;
            }
        }

        public static void SetLastOpenedFolder(string? folder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder)) return;
                var path = GetStoragePath();
                var temp = path + ".tmp";
                File.WriteAllText(temp, folder);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("RecentFolderStore.SetLastOpenedFolder", ex);
            }
        }
    }
}

