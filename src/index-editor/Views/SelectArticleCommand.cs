using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;
using Common.Shared;
using System.Collections.ObjectModel;

namespace IndexEditor.Views
{
    public class SelectArticleCommand : ICommand
    {
        private readonly EditorStateViewModel _viewModel;
        public SelectArticleCommand(EditorStateViewModel viewModel)
        {
            _viewModel = viewModel;
        }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => parameter is ArticleLine;
        public void Execute(object? parameter)
        {
            if (parameter is ArticleLine article)
            {
                foreach (var a in _viewModel.Articles)
                    a.IsSelected = false;
                article.IsSelected = true;
                // Update global editor state and viewmodel selection
                IndexEditor.Shared.EditorState.ActiveArticle = article;
                if (article.Pages != null && article.Pages.Count > 0)
                {
                    // Try to pick the first page that has an image in the open folder
                    var folder = IndexEditor.Shared.EditorState.CurrentFolder;
                    int? pick = null;
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        foreach (var p in article.Pages.OrderBy(x => x))
                        {
                            if (ImageExistsInFolder(folder, p)) { pick = p; break; }
                        }
                    }
                    // Fallback to the minimum page if none found
                    IndexEditor.Shared.EditorState.CurrentPage = pick ?? article.Pages.Min();
                }
                IndexEditor.Shared.EditorState.NotifyStateChanged();
                _viewModel.SelectedArticle = article;
            }
        }

        private bool ImageExistsInFolder(string folder, int page)
        {
            // Candidates: plain, padded 2/3, jpg/png, and simple prefixes
            var candidates = new List<string>
            {
                Path.Combine(folder, page.ToString() + ".jpg"),
                Path.Combine(folder, page.ToString() + ".png"),
                Path.Combine(folder, page.ToString("D2") + ".jpg"),
                Path.Combine(folder, page.ToString("D2") + ".png"),
                Path.Combine(folder, page.ToString("D3") + ".jpg"),
                Path.Combine(folder, page.ToString("D3") + ".png"),
                Path.Combine(folder, "page-" + page.ToString() + ".jpg"),
                Path.Combine(folder, "p" + page.ToString() + ".jpg")
            };
            return candidates.Any(Path.Exists);
        }
    }
}
