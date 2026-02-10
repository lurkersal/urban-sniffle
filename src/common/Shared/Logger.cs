using System;

namespace Common.Shared
{
    internal static class Logger
    {
        public static void Log(string message)
        {
            try { Console.WriteLine($"[Common.Shared] {DateTime.Now:O} {message}"); } catch { }
        }

        public static void LogException(string context, Exception ex)
        {
            try { Console.WriteLine($"[Common.Shared] {DateTime.Now:O} EX in {context}: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}"); } catch { }
        }
    }
}

