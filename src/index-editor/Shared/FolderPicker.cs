using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Collections;
using System.Reflection;
using IndexEditor.Shared;

namespace IndexEditor.Shared
{
    public static class FolderPicker
    {
        public static async Task<string?> PickFolderAsync(Window? parent, string? start = null)
        {
            // Try modern StorageProvider API via reflection
            try
            {
                var top = parent as TopLevel ?? (Avalonia.Application.Current?.ApplicationLifetime as TopLevel);
                if (top != null)
                {
                    var topType = top.GetType();
                    var storageProp = topType.GetProperty("StorageProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (storageProp != null)
                    {
                        var storage = storageProp.GetValue(top);
                        if (storage != null)
                        {
                            var storageType = storage.GetType();
                            var method = storageType.GetMethod("OpenFolderPickerAsync") ?? storageType.GetMethod("OpenFolderPicker");
                            if (method != null)
                            {
                                // Invoke the method and await the Task/result if necessary
                                object? invokeResult = null;
                                var parameters = method.GetParameters();
                                try
                                {
                                    if (parameters.Length == 0)
                                        invokeResult = method.Invoke(storage, null);
                                    else
                                        invokeResult = method.Invoke(storage, new object[] { null });
                                }
                                catch (Exception ex) { DebugLogger.LogException("FolderPicker: invoke storage method", ex); try { invokeResult = method.Invoke(storage, null); } catch (Exception ex2) { DebugLogger.LogException("FolderPicker: invoke fallback", ex2); invokeResult = null; } }

                                var unwrapped = await UnwrapTaskResult(invokeResult).ConfigureAwait(false);
                                if (unwrapped is IEnumerable enumerable)
                                {
                                    foreach (var item in enumerable)
                                    {
                                        var candidate = ExtractPathFromStorageItem(item);
                                        if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
                                    }
                                }
                                else if (unwrapped != null)
                                {
                                    var candidate = ExtractPathFromStorageItem(unwrapped);
                                    if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { DebugLogger.LogException("FolderPicker: storage provider attempt", ex); }

            // Fallback: use the native OpenFolderDialog (preferred over custom UI)
            try
            {
#pragma warning disable CS0618 // OpenFolderDialog is obsolete in newer Avalonia; keep for compatibility
                var dlg = new OpenFolderDialog();
                if (!string.IsNullOrWhiteSpace(start)) dlg.Directory = start;
                var picked = await dlg.ShowAsync(parent).ConfigureAwait(false);
#pragma warning restore CS0618
                return picked;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("FolderPicker: OpenFolderDialog fallback", ex);
                return null;
            }
        }

        private static string? ExtractPathFromStorageItem(object? item)
        {
            if (item == null) return null;
            var t = item.GetType();
            // Common property names
            var p = t.GetProperty("Path") ?? t.GetProperty("FullPath") ?? t.GetProperty("Name");
            if (p != null)
            {
                var v = p.GetValue(item) as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }

            // Some types may have TryGetFullPath(out string)
            var m = t.GetMethod("TryGetFullPath") ?? t.GetMethod("TryGetLocalPath");
            if (m != null)
            {
                try
                {
                    var parameters = m.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsByRef)
                    {
                        var args = new object?[] { null };
                        var ok = (bool)m.Invoke(item, args);
                        if (ok && args[0] is string s && !string.IsNullOrWhiteSpace(s)) return s;
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("FolderPicker.ExtractPathFromStorageItem: TryGetFullPath", ex); }
            }

            return null;
        }

        private static async Task<object?> UnwrapTaskResult(object? taskObj)
        {
            if (taskObj == null) return null;
            if (taskObj is Task t)
            {
                await t.ConfigureAwait(false);
                var rt = taskObj.GetType().GetProperty("Result");
                if (rt != null) return rt.GetValue(taskObj);
                return null;
            }
            return taskObj;
        }
    }
}
