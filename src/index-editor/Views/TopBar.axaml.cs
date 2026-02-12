using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using IndexEditor.Shared;
using Common.Shared;
using System.Diagnostics;
using System.Text.RegularExpressions;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Views
{
    public partial class TopBar : UserControl
    {
        public TopBar()
        {
            InitializeComponent();
            var btn = this.FindControl<Button>("SaveIndexBtn");
            var openBtn = this.FindControl<Button>("OpenFolderBtn");
            var magTypeText = this.FindControl<TextBlock>("MagTypeText");
            var volText = this.FindControl<TextBlock>("VolumeText");
            var numText = this.FindControl<TextBlock>("NumberText");
            // Initialize magazine metadata display from EditorState (may be populated by MainWindow when loading _index.txt)
            void RefreshMetadataDisplay()
            {
                try
                {
                    var mag = IndexEditor.Shared.EditorState.CurrentMagazine ?? "—";
                    var vol = IndexEditor.Shared.EditorState.CurrentVolume ?? "—";
                    var num = IndexEditor.Shared.EditorState.CurrentNumber ?? "—";
                    if (magTypeText != null) magTypeText.Text = $"Magazine: {mag}";
                    if (volText != null) volText.Text = $"Vol: {vol}";
                    if (numText != null) numText.Text = $"No: {num}";
                }
                catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar.RefreshMetadataDisplay", ex); }
            }
            RefreshMetadataDisplay();
            IndexEditor.Shared.EditorState.StateChanged += RefreshMetadataDisplay;

            if (btn != null)
            {
                // Initial enablement
                btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                // Update enablement when state changes
                IndexEditor.Shared.EditorState.StateChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null; } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: update btn.IsEnabled", ex); } });

                btn.Click += (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            IndexEditor.Shared.ToastService.Show("No folder opened; cannot save _index.txt");
                            return;
                        }

                        // Prevent double-save
                        btn.IsEnabled = false;
                        try
                        {
                            IndexEditor.Shared.IndexSaver.SaveIndex(folder);
                            IndexEditor.Shared.ToastService.Show("_index.txt saved");
                        }
                        catch (Exception ex)
                        {
                            IndexEditor.Shared.ToastService.Show("Failed to save _index.txt");
                            IndexEditor.Shared.DebugLogger.LogException("TopBar: save _index.txt", ex);
                        }
                        finally
                        {
                            // re-evaluate enablement based on active segment
                            btn.IsEnabled = IndexEditor.Shared.EditorState.ActiveSegment == null;
                        }
                    }
                    catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: save click outer", ex); }
                };
            }
            if (openBtn != null)
            {
                openBtn.Click += async (s, e) =>
                {
                    try
                    {
                        // Use custom folder browser window that lists only folders and selects a folder when it has no subfolders on double-click
                        var wnd = this.VisualRoot as Window;
                        var start = IndexEditor.Shared.EditorState.CurrentFolder;
                        IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: Current folder is: {start ?? "(null)"}");
                        string? path = null;
                        try
                        {
                            path = await IndexEditor.Shared.FolderPicker.PickFolderAsync(wnd, start);
                            IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: FolderPicker returned: {path ?? "(null)"}");
                        }
                        catch (Exception ex)
                        {
                            try { IndexEditor.Shared.ToastService.Show("Open folder dialog failed: " + ex.Message); } catch (Exception tex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: ToastService.Show open folder failed", tex); }
                            IndexEditor.Shared.DebugLogger.LogException("TopBar: FolderPicker failed", ex);
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            IndexEditor.Shared.DebugLogger.Log("TopBar.OpenClick: Path is null or empty, returning");
                            return;
                        }

                        IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: Loading folder: {path}");

                        // Clear existing articles before loading new ones
                        IndexEditor.Shared.EditorState.Articles = new List<Common.Shared.ArticleLine>();
                        var vm = this.DataContext as IndexEditor.Views.EditorStateViewModel;
                        if (vm != null)
                        {
                            try { vm.Articles.Clear(); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: clear vm.Articles", ex); }
                        }

                        IndexEditor.Shared.EditorState.CurrentFolder = path;
                        // Reuse existing MainWindow-loading logic by delegating to MainWindow.LoadArticlesFromFolder if available.
                        try
                        {
                            var main = this.VisualRoot as MainWindow;
                            IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: MainWindow found: {main != null}");
                            if (main != null)
                            {
                                // Use reflection to call LoadArticlesFromFolder in case its protection level changes
                                var mi = typeof(MainWindow).GetMethod("LoadArticlesFromFolder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: LoadArticlesFromFolder method found: {mi != null}");
                                if (mi != null)
                                {
                                    IndexEditor.Shared.DebugLogger.Log($"TopBar.OpenClick: Invoking LoadArticlesFromFolder with path: {path}");
                                    mi.Invoke(main, new object[] { path });
                                    IndexEditor.Shared.DebugLogger.Log("TopBar.OpenClick: LoadArticlesFromFolder invoked successfully");
                                    return;
                                }
                            }
                        }
                        catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: reflection invoke LoadArticlesFromFolder", ex); }

                        // As an absolute fallback, set the CurrentFolder and notify state so the MainWindow may react elsewhere
                        IndexEditor.Shared.EditorState.CurrentFolder = path;
                        try { IndexEditor.Shared.EditorState.NotifyStateChanged(); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: NotifyStateChanged fallback", ex); }
                    }
                    catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: Open button click handler outer", ex); }
                };
            }
            
            var testBtn = this.FindControl<Button>("TestBtn");
            if (testBtn != null)
            {
                // Test button should remain enabled at all times per spec
                testBtn.IsEnabled = true;
                testBtn.Click += async (s, e) =>
                {
                    try
                    {
                        var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                        if (string.IsNullOrWhiteSpace(folder))
                        {
                            IndexEditor.Shared.ToastService.Show("No folder opened; cannot run test");
                            return;
                        }

                        // Save current edits first
                        try
                        {
                            IndexEditor.Shared.IndexSaver.SaveIndex(folder);
                            IndexEditor.Shared.ToastService.Show("_index.txt saved");
                        }
                        catch (Exception ex)
                        {
                            IndexEditor.Shared.ToastService.Show("Failed to save _index.txt before test");
                            IndexEditor.Shared.DebugLogger.LogException("TopBar: Test save before run", ex);
                            return;
                        }

                         // Prepare to run magazine-parser with --no-insert
                         var wnd = this.VisualRoot as MainWindow;
                         try
                         {
                             // Use dotnet run to invoke the project in-repo; compute absolute project path
                             var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
                             // If the app is running from build output, try to locate the repository root via the VisualRoot location fallback
                             var projectPath = System.IO.Path.Combine(repoRoot, "src", "magazine-parser", "magazine-parser.csproj");
                             if (!System.IO.File.Exists(projectPath))
                             {
                                 // Fallback: assume repo root is two levels up from the executable location
                                 projectPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "magazine-parser", "magazine-parser.csproj");
                                 projectPath = System.IO.Path.GetFullPath(projectPath);
                             }

                            if (!System.IO.File.Exists(projectPath))
                            {
                                var msg = $"magazine-parser project not found at expected location: {projectPath}";
                                IndexEditor.Shared.DebugLogger.Log(msg);
                                IndexEditor.Shared.ToastService.Show("Test run failed: magazine-parser project not found");
                                // Still show overlay with message
                                if (wnd != null)
                                {
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                    {
                                        try
                                        {
                                            var overlay = wnd.FindControl<Border>("ParserOutputOverlay");
                                            var tb = wnd.FindControl<TextBox>("ParserOutputTextBox");
                                            if (overlay != null && tb != null)
                                            {
                                                tb.Text = msg;
                                                tb.CaretIndex = tb.Text.Length;
                                                overlay.IsVisible = true;
                                            }
                                        }
                                        catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: show missing project msg", ex); }
                                    });
                                }
                                return;
                            }

                            var psi = new System.Diagnostics.ProcessStartInfo
                             {
                                 FileName = "dotnet",
                                 Arguments = $"run --project \"{projectPath}\" -- --no-insert \"{folder}\"",
                                 RedirectStandardOutput = true,
                                 RedirectStandardError = true,
                                 UseShellExecute = false,
                                 CreateNoWindow = true,
                             };

                            // Prepare overlay for streaming output (if available)
                            Border? overlayRef = null;
                            TextBox? tbRef = null;
                            if (wnd != null)
                            {
                                try
                                {
                                    overlayRef = wnd.FindControl<Border>("ParserOutputOverlay");
                                    tbRef = wnd.FindControl<TextBox>("ParserOutputTextBox");
                                    if (overlayRef != null && tbRef != null)
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                        {
                                            try { tbRef.Text = ""; overlayRef.IsVisible = true; tbRef.Focus(); } catch { }
                                        });
                                    }
                                }
                                catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: find overlay before run", ex); }
                            }

                            var proc = new System.Diagnostics.Process { StartInfo = psi };

                             var outputBuilder = new System.Text.StringBuilder();
                             proc.OutputDataReceived += (sender, ea) =>
                             {
                                 if (ea.Data == null) return;
                                 outputBuilder.AppendLine(ea.Data);
                                 if (tbRef != null)
                                 {
                                     Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                     {
                                         try
                                         {
                                             tbRef.Text += ea.Data + "\n";
                                             tbRef.CaretIndex = tbRef.Text.Length;
                                         }
                                         catch { }
                                     });
                                 }
                             };
                             proc.ErrorDataReceived += (sender, ea) =>
                             {
                                 if (ea.Data == null) return;
                                 outputBuilder.AppendLine(ea.Data);
                                 if (tbRef != null)
                                 {
                                     Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                     {
                                         try
                                         {
                                             tbRef.Text += ea.Data + "\n";
                                             tbRef.CaretIndex = tbRef.Text.Length;
                                         }
                                         catch { }
                                     });
                                 }
                             };

                            // Start process
                            try
                            {
                                proc.Start();
                            }
                            catch (System.ComponentModel.Win32Exception wx)
                            {
                                // Could not start 'dotnet' - try to run published binary in ~/bin
                                IndexEditor.Shared.DebugLogger.LogException("TopBar: process start failed", wx);
                                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                                var binCandidate = System.IO.Path.Combine(home, "bin", "magazine-parser");
                                if (System.IO.File.Exists(binCandidate))
                                {
                                    psi = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = binCandidate,
                                        Arguments = $"--no-insert \"{folder}\"",
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                    };
                                    proc = new System.Diagnostics.Process { StartInfo = psi };
                                    proc.OutputDataReceived += (sender, ea) =>
                                    {
                                        if (ea.Data == null) return;
                                        outputBuilder.AppendLine(ea.Data);
                                        if (tbRef != null) Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { tbRef.Text += ea.Data + "\n"; tbRef.CaretIndex = tbRef.Text.Length; } catch { } });
                                    };
                                    proc.ErrorDataReceived += (sender, ea) =>
                                    {
                                        if (ea.Data == null) return;
                                        outputBuilder.AppendLine(ea.Data);
                                        if (tbRef != null) Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { tbRef.Text += ea.Data + "\n"; tbRef.CaretIndex = tbRef.Text.Length; } catch { } });
                                    };
                                    try { proc.Start(); } catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: fallback binary start failed", ex); throw; }
                                }
                                else
                                {
                                    throw; // propagate original exception to outer catch
                                }
                            }

                            proc.BeginOutputReadLine();
                            proc.BeginErrorReadLine();
                            await System.Threading.Tasks.Task.Run(() => proc.WaitForExit());

                            var output = outputBuilder.ToString();

                            // Append exit code info
                            var exitInfo = $"\n[Process exited with code {proc.ExitCode}]";
                            if (tbRef != null)
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { tbRef.Text += exitInfo; tbRef.CaretIndex = tbRef.Text.Length; } catch { } });
                            }
                            else
                            {
                                IndexEditor.Shared.DebugLogger.Log("Parser test output:\n" + output + exitInfo);
                                IndexEditor.Shared.ToastService.Show("Test run completed. Output length: " + output.Length + " ExitCode:" + proc.ExitCode);
                            }
                         }
                         catch (Exception ex)
                         {
                             IndexEditor.Shared.ToastService.Show("Test run failed: " + ex.Message);
                             IndexEditor.Shared.DebugLogger.LogException("TopBar: Test run process", ex);
                         }
                     }
                     catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("TopBar: Test click outer", ex); }
                 };
             }
        }
    }
}
