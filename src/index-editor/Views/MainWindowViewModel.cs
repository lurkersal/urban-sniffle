using System;
using System.Linq;
using System.Windows.Input;
using IndexEditor.Shared;

namespace IndexEditor.Views
{
    public class MainWindowViewModel
    {
        public IPageControllerBridge? PageControllerBridge { get; set; }

        public MainWindowViewModel()
        {
            AddSegmentCommand = new RelayCommand(_ => AddSegment());
            NewArticleCommand = new RelayCommand(_ => NewArticle());
            EndSegmentCommand = new RelayCommand(_ => EndSegment());
            SaveIndexCommand = new RelayCommand(_ => SaveIndex());
            MoveLeftCommand = new RelayCommand(_ => MoveLeft());
            MoveRightCommand = new RelayCommand(_ => MoveRight());
        }

        public ICommand AddSegmentCommand { get; }
        public ICommand NewArticleCommand { get; }
        public ICommand EndSegmentCommand { get; }
        public ICommand SaveIndexCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }

        public void AddSegment()
        {
            try
            {
                EditorActions.AddSegmentAtCurrentPage();
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.AddSegment", ex); }
        }

        public void NewArticle()
        {
            try
            {
                EditorActions.CreateNewArticle();
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.NewArticle", ex); }
        }

        public void EndSegment()
        {
            try
            {
                EditorActions.EndActiveSegment();
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.EndSegment", ex); }
        }

        public void MoveLeft() { try { var pc = PageControllerBridge; if (pc != null) pc.MoveLeft(); else { /* fallback: change EditorState */ EditorState.CurrentPage = Math.Max(1, EditorState.CurrentPage - 1); EditorState.NotifyStateChanged(); } } catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.MoveLeft", ex); } }
        public void MoveRight() { try { var pc = PageControllerBridge; if (pc != null) pc.MoveRight(); else { EditorState.CurrentPage = EditorState.CurrentPage + 1; EditorState.NotifyStateChanged(); } } catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.MoveRight", ex); } }
        public void SaveIndex()
        {
            try
            {
                var active = EditorState.ActiveSegment;
                if (active != null && active.IsActive)
                {
                    ToastService.Show("End or cancel the active segment before saving");
                    return;
                }

                var folder = EditorState.CurrentFolder;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    ToastService.Show("No folder open; cannot save _index.txt");
                    return;
                }

                var indexPath = System.IO.Path.Combine(folder, "_index.txt");
                string Escape(string s) => s?.Replace(",", "\\,") ?? string.Empty;
                var lines = new System.Collections.Generic.List<string>();
                foreach (var a in EditorState.Articles)
                {
                    var pagesText = a.PagesText ?? string.Empty;
                    var modelNames = (a.ModelNames != null && a.ModelNames.Count > 0) ? string.Join('|', a.ModelNames) : string.Empty;
                    var ages = (a.Ages != null && a.Ages.Count > 0) ? string.Join('|', a.Ages.Select(v => v.HasValue ? v.Value.ToString() : string.Empty)) : string.Empty;
                    var photographers = (a.Photographers != null && a.Photographers.Count > 0) ? string.Join('|', a.Photographers) : string.Empty;
                    var authors = (a.Authors != null && a.Authors.Count > 0) ? string.Join('|', a.Authors) : string.Empty;
                    var measurements = (a.Measurements != null && a.Measurements.Count > 0) ? string.Join('|', a.Measurements) : string.Empty;
                    var parts = new System.Collections.Generic.List<string> { pagesText, Escape(a.Category), Escape(a.Title), Escape(modelNames), Escape(ages), Escape(photographers), Escape(authors), Escape(measurements) };
                    lines.Add(string.Join(",", parts));
                }

                var outLinesList = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(EditorState.CurrentMagazine) || !string.IsNullOrWhiteSpace(EditorState.CurrentVolume) || !string.IsNullOrWhiteSpace(EditorState.CurrentNumber))
                {
                    var metaParts = new[] { Escape(EditorState.CurrentMagazine ?? string.Empty), Escape(EditorState.CurrentVolume ?? string.Empty), Escape(EditorState.CurrentNumber ?? string.Empty) };
                    outLinesList.Add(string.Join(",", metaParts));
                }
                outLinesList.AddRange(lines);
                var tempPath = indexPath + ".tmp";
                System.IO.File.WriteAllLines(tempPath, outLinesList.ToArray());
                if (System.IO.File.Exists(indexPath)) System.IO.File.Replace(tempPath, indexPath, null);
                else System.IO.File.Move(tempPath, indexPath);

                ToastService.Show("_index.txt saved");
                EditorState.NotifyStateChanged();
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("MainWindowViewModel.SaveIndex", ex);
                ToastService.Show("Failed to save _index.txt");
            }
        }

        public void SaveIndexFromOverlay(string overlayText)
        {
            try
            {
                var folder = EditorState.CurrentFolder;
                if (string.IsNullOrWhiteSpace(folder)) { ToastService.Show("No folder open; cannot save _index.txt"); return; }
                var indexPath = System.IO.Path.Combine(folder, "_index.txt");
                var temp = indexPath + ".tmp";
                System.IO.File.WriteAllText(temp, overlayText ?? string.Empty);
                if (System.IO.File.Exists(indexPath)) System.IO.File.Replace(temp, indexPath, null);
                else System.IO.File.Move(temp, indexPath);
                ToastService.Show("_index.txt saved");
            }
            catch (Exception ex) { DebugLogger.LogException("MainWindowViewModel.SaveIndexFromOverlay", ex); ToastService.Show("Failed to save _index.txt"); }
        }
    }
}
