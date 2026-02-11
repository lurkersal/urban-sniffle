using System;
using System.Linq;
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
                DebugLogger.Log($"FolderPicker: Starting with path: {start}");
            }

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
                                                    var prop = pType.GetProperty("SuggestedStartLocation");
                                                    if (prop != null && prop.CanWrite)
                                                    {
                                                        try 
                                                        {
                                                            // SuggestedStartLocation can be IStorageFolder or Uri
                                                            if (prop.PropertyType.Name.Contains("IStorageFolder") || prop.PropertyType.Name.Contains("IStorageItem"))
                                                            {
                                                                // Try to get IStorageFolder from path
                                                                var tryGetMethod = storageType.GetMethod("TryGetFolderFromPathAsync");
                                                                if (tryGetMethod != null)
                                                                {
                                                                    // TryGetFolderFromPathAsync expects a Uri parameter
                                                                    Uri pathUri;
                                                                    if (!Uri.TryCreate(start, UriKind.Absolute, out pathUri!))
                                                                    {
                                                                        pathUri = new Uri("file://" + start);
                                                                    }
                                                                    var folderTask = tryGetMethod.Invoke(storage, new object[] { pathUri });
                                                                    var folder = await UnwrapTaskResult(folderTask).ConfigureAwait(false);
                                                                    if (folder != null)
                                                                    {
                                                                        prop.SetValue(inst, folder);
                                                                        DebugLogger.Log($"FolderPicker: Set SuggestedStartLocation (IStorageFolder) to {start}");
                                                                    }
                                                                }
                                                            }
                                                            else if (prop.PropertyType == typeof(Uri))
                                                            {
                                                                // Convert path to Uri - handle both absolute paths and URIs
                                                                Uri uri;
                                                                if (Uri.TryCreate(start, UriKind.Absolute, out uri!))
                                                                {
                                                                    prop.SetValue(inst, uri);
                                                                    DebugLogger.Log($"FolderPicker: Set SuggestedStartLocation (Uri) to {start}");
                                                                }
                                                                else
                                                                {
                                                                    // Try as file path
                                                                    var fileUri = new Uri("file://" + start);
                                                                    prop.SetValue(inst, fileUri);
                                                                    DebugLogger.Log($"FolderPicker: Set SuggestedStartLocation (Uri from file path) to {start}");
                                                                }
                                                            }
                                                            else if (prop.PropertyType == typeof(string))
                                                            {
                                                                prop.SetValue(inst, start);
                                                                DebugLogger.Log($"FolderPicker: Set {prop.Name} (string) to {start}");
                                                            }
                                                            else
                                                            {
                                                                DebugLogger.Log($"FolderPicker: Unknown property type {prop.PropertyType.Name} for SuggestedStartLocation");
                                                            }
                                                        } 
                                                        catch (Exception ex) 
                                                        { 
                                                            DebugLogger.LogException($"FolderPicker: set {prop.Name}", ex);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Fallback to other property names
                                                        prop = pType.GetProperty("StartingDirectory") ?? pType.GetProperty("StartPath") ?? pType.GetProperty("InitialDirectory") ?? pType.GetProperty("Directory") ?? pType.GetProperty("Path");
                                                        if (prop != null && prop.CanWrite)
                                                        {
                                                            try 
                                                            { 
                                                                if (prop.PropertyType == typeof(string))
                                                                {
                                                                    prop.SetValue(inst, start);
                                                                    DebugLogger.Log($"FolderPicker: Set {prop.Name} to {start}");
                                                                }
                                                                else if (prop.PropertyType == typeof(Uri))
                                                                {
                                                                    // Create file Uri from local path
                                                                    Uri uri;
                                                                    if (Uri.TryCreate(start, UriKind.Absolute, out uri!))
                                                                    {
                                                                        prop.SetValue(inst, uri);
                                                                        DebugLogger.Log($"FolderPicker: Set {prop.Name} (Uri) to {start}");
                                                                    }
                                                                    else
                                                                    {
                                                                        // Try as file path
                                                                        var fileUri = new Uri("file://" + start);
                                                                        prop.SetValue(inst, fileUri);
                                                                        DebugLogger.Log($"FolderPicker: Set {prop.Name} (Uri from file path) to {start}");
                                                                    }
                                                                }
                                                            } 
                                                            catch (Exception ex) 
                                                            { 
                                                                DebugLogger.LogException($"FolderPicker: set fallback property {prop?.Name}", ex);
                                                            }
                                                        }
                                                    }
                                                }
                                                args[pi] = inst;
                                            }
                                            catch (Exception ex)
                                            {
                                                DebugLogger.LogException("FolderPicker: create options instance", ex);
                                                args[pi] = null;
                                            }
                                        }

                                        invokeResult = method.Invoke(storage, args);
                                    }
                                }
                                catch (Exception ex) { DebugLogger.LogException("FolderPicker: invoke storage method", ex); try { invokeResult = method.Invoke(storage, null); } catch (Exception ex2) { DebugLogger.LogException("FolderPicker: invoke fallback", ex2); invokeResult = null; } }

                                var unwrapped = await UnwrapTaskResult(invokeResult).ConfigureAwait(false);
                                DebugLogger.Log($"FolderPicker: Unwrapped result type: {unwrapped?.GetType().Name ?? "null"}");
                                if (unwrapped is IEnumerable enumerable)
                                {
                                    foreach (var item in enumerable)
                                    {
                                        var candidate = ExtractPathFromStorageItem(item);
                                        if (!string.IsNullOrWhiteSpace(candidate))
                                        {
                                            DebugLogger.Log($"FolderPicker: Returning path from enumerable: {candidate}");
                                            return candidate;
                                        }
                                    }
                                }
                                else if (unwrapped != null)
                                {
                                    var candidate = ExtractPathFromStorageItem(unwrapped);
                                    if (!string.IsNullOrWhiteSpace(candidate))
                                    {
                                        DebugLogger.Log($"FolderPicker: Returning path: {candidate}");
                                        return candidate;
                                    }
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
            if (item == null)
            {
                DebugLogger.Log("FolderPicker.ExtractPath: item is null");
                return null;
            }
            var t = item.GetType();
            DebugLogger.Log($"FolderPicker.ExtractPath: item type is {t.FullName}");
            
            // Log all available properties for debugging
            var allProps = t.GetProperties();
            DebugLogger.Log($"FolderPicker.ExtractPath: Available properties: {string.Join(", ", allProps.Select(p => $"{p.Name}({p.PropertyType.Name})"))}");
            
            // Try TryGetLocalPath first (most reliable for IStorageFolder)
            var m = t.GetMethod("TryGetLocalPath") ?? t.GetMethod("TryGetFullPath");
            if (m != null)
            {
                DebugLogger.Log($"FolderPicker.ExtractPath: Found method {m.Name}");
                try
                {
                    var parameters = m.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsByRef)
                    {
                        var args = new object?[] { null };
                        var resultObj = m.Invoke(item, args);
                        var ok = resultObj is bool b && b;
                        DebugLogger.Log($"FolderPicker.ExtractPath: Method result: {ok}, args[0]: {args[0]}");
                        if (ok && args[0] is string s && !string.IsNullOrWhiteSpace(s))
                        {
                            DebugLogger.Log($"FolderPicker.ExtractPath: Method returned: {s}");
                            return s;
                        }
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("FolderPicker.ExtractPathFromStorageItem: TryGetPath", ex); }
            }
            
            // Common property names
            var p = t.GetProperty("Path");
            if (p != null)
            {
                DebugLogger.Log($"FolderPicker.ExtractPath: Found property Path");
                try
                {
                    var v = p.GetValue(item);
                    DebugLogger.Log($"FolderPicker.ExtractPath: Path value type: {v?.GetType().Name ?? "null"}");
                    
                    // Handle Uri type
                    if (v is Uri uri)
                    {
                        var localPath = uri.LocalPath;
                        if (!string.IsNullOrWhiteSpace(localPath))
                        {
                            DebugLogger.Log($"FolderPicker.ExtractPath: Extracted from Uri: {localPath}");
                            return localPath;
                        }
                    }
                    else if (v is string str && !string.IsNullOrWhiteSpace(str))
                    {
                        DebugLogger.Log($"FolderPicker.ExtractPath: Extracted from Path: {str}");
                        return str;
                    }
                }
                catch (Exception ex) { DebugLogger.LogException("FolderPicker.ExtractPath: Path property", ex); }
            }

            // Try other common property names
            p = t.GetProperty("FullPath") ?? t.GetProperty("Name") ?? t.GetProperty("LocalPath");
            if (p != null)
            {
                DebugLogger.Log($"FolderPicker.ExtractPath: Found property {p.Name}");
                var v = p.GetValue(item) as string;
                if (!string.IsNullOrWhiteSpace(v))
                {
                    DebugLogger.Log($"FolderPicker.ExtractPath: Extracted value: {v}");
                    return v;
                }
            }

            DebugLogger.Log("FolderPicker.ExtractPath: No path found");
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
