using System;
using System.IO;
using System.Text.Json;

namespace IndexEditor.Shared
{
    public class WindowState
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }

    public static class WindowStateStore
    {
        private static string GetStoragePath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appData)) appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? ".";
                var dir = Path.Combine(appData, "urban-sniffle");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "index-editor-windowstate.json");
            }
            catch
            {
                return Path.Combine(".", "index-editor-windowstate.json");
            }
        }

        public static WindowState? GetWindowState()
        {
            try
            {
                var path = GetStoragePath();
                if (!File.Exists(path)) return null;
                var txt = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(txt)) return null;
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var st = JsonSerializer.Deserialize<WindowState>(txt, opts);
                return st;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("WindowStateStore.GetWindowState", ex);
                return null;
            }
        }

        public static void SetWindowState(double width, double height, bool isMaximized)
        {
            try
            {
                var path = GetStoragePath();
                var temp = path + ".tmp";
                var st = new WindowState { Width = width, Height = height, IsMaximized = isMaximized };
                var txt = JsonSerializer.Serialize(st);
                File.WriteAllText(temp, txt);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("WindowStateStore.SetWindowState", ex);
            }
        }
    }
}
