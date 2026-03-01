using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IndexEditor.Services
{
    public class LinkDiscoveryService
    {
        private CancellationTokenSource? _cts;
        
        public event EventHandler<LinkDiscoveryProgressEventArgs>? ProgressChanged;
        public event EventHandler<LinkDiscoveredEventArgs>? LinkDiscovered;
        public event EventHandler? DiscoveryCompleted;

        public void StartDiscovery(string folder, string magazineName)
        {
            StopDiscovery();
            _cts = new CancellationTokenSource();
            Task.Run(() => DiscoverAsync(folder, magazineName, _cts.Token), _cts.Token);
        }

        public void StopDiscovery()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task DiscoverAsync(string folder, string magazineName, CancellationToken ct)
        {
            await Task.Yield();
            var imageFiles = IndexEditor.Shared.ImageHelper.GetAllImageFiles(folder);
            int total = imageFiles.Count;
            int processed = 0;

            foreach (var (page, path) in imageFiles)
            {
                if (ct.IsCancellationRequested) return;
                
                ProgressChanged?.Invoke(this, new LinkDiscoveryProgressEventArgs 
                { 
                    CurrentPage = page, 
                    TotalPages = total, 
                    ProcessedPages = processed 
                });

                try
                {
                    var text = ExtractText(path);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var links = FindLinks(text);
                        foreach (var (vol, num) in links)
                        {
                            if (vol > 0 && num > 0)
                            {
                                LinkDiscovered?.Invoke(this, new LinkDiscoveredEventArgs
                                {
                                    Page = page,
                                    Magazine = magazineName,
                                    Volume = vol.ToString(),
                                    Issue = num.ToString()
                                });
                            }
                        }
                    }
                }
                catch { }
                
                processed++;
            }
            
            DiscoveryCompleted?.Invoke(this, EventArgs.Empty);
        }

        private string ExtractText(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return string.Empty;
                var temp = System.IO.Path.GetTempFileName();
                var output = temp.Replace(".tmp", "");
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "tesseract",
                    Arguments = $"\"{path}\" \"{output}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit();
                
                var txtFile = output + ".txt";
                if (System.IO.File.Exists(txtFile))
                {
                    var text = System.IO.File.ReadAllText(txtFile);
                    try { System.IO.File.Delete(txtFile); } catch { }
                    try { System.IO.File.Delete(temp); } catch { }
                    return text;
                }
            }
            catch { }
            return string.Empty;
        }

        private List<(int, int)> FindLinks(string text)
        {
            var links = new List<(int, int)>();
            var regex = new System.Text.RegularExpressions.Regex(
                @"(vol(?:ume)?)[\s\.:/]*([0-9]+)[\s\S]*?(no(?:\.|umber)?)[\s\.:/]*([0-9]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            foreach (System.Text.RegularExpressions.Match m in regex.Matches(text))
            {
                if (m.Success && 
                    int.TryParse(m.Groups[2].Value, out int vol) &&
                    int.TryParse(m.Groups[4].Value, out int num))
                {
                    links.Add((vol, num));
                }
            }
            return links;
        }
    }

    public class LinkDiscoveryProgressEventArgs : EventArgs
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int ProcessedPages { get; set; }
        public int PercentComplete => TotalPages > 0 ? (ProcessedPages * 100) / TotalPages : 0;
    }

    public class LinkDiscoveredEventArgs : EventArgs
    {
        public int Page { get; set; }
        public string Magazine { get; set; } = string.Empty;
        public string Volume { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
    }
}

