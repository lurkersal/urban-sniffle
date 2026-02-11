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
                                    {
                                        invokeResult = method.Invoke(storage, null);
                                    }
                                    else
                                    {
                                        // Build args array to match parameter types
                                        var args = new object?[parameters.Length];
                                        for (int pi = 0; pi < parameters.Length; pi++)
                                        {
                                            var pType = parameters[pi].ParameterType;
                                            // If parameter expects a TopLevel/Window, pass top
                                            if (typeof(TopLevel).IsAssignableFrom(pType) || typeof(Window).IsAssignableFrom(pType))
                                            {
                                                args[pi] = top;
                                                continue;
                                            }

                                            // If parameter expects a string, pass start
                                            if (pType == typeof(string) || pType == typeof(object))
                                            {
                                                args[pi] = start;
                                                continue;
                                            }

                                            // Otherwise, attempt to create an instance (likely options object)
                                            try
                                            {
                                                var inst = Activator.CreateInstance(pType);
                                                // If we have a start path, try to set common property names to it
                                                if (!string.IsNullOrWhiteSpace(start))
                                                {
                                                    var prop = pType.GetProperty("StartingDirectory") ?? pType.GetProperty("StartPath") ?? pType.GetProperty("InitialDirectory") ?? pType.GetProperty("SuggestedStartLocation") ?? pType.GetProperty("Directory") ?? pType.GetProperty("Path");
                                                    if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
                                                    {
                                                        try { prop.SetValue(inst, start); } catch { }
                                                    }
                                                }
                                                args[pi] = inst;
                                            }
                                            catch
                                            {
                                                args[pi] = null;
                                            }
                                        }

                                        invokeResult = method.Invoke(storage, args);
                                    }
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

            // No modern storage provider path succeeded; return null so caller may fall back.
            return null;
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
                        var resultObj = m.Invoke(item, args);
                        var ok = resultObj is bool b && b;
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
