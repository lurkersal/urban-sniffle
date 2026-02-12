using System;
using System.Linq;
using System.Windows.Input;
using IndexEditor.Shared;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

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

                try
                {
                    IndexSaver.SaveIndex(folder);
                    ToastService.Show("_index.txt saved");
                    EditorState.NotifyStateChanged();
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException("MainWindowViewModel.SaveIndex", ex);
                    ToastService.Show("Failed to save _index.txt");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException("MainWindowViewModel.SaveIndex: outer", ex);
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
