using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;
using Common.Shared;
using System.Collections.ObjectModel;

#pragma warning disable CS0618 // Intentional use of backward-compatible static wrappers

namespace IndexEditor.Views
{
    public class SelectArticleCommand : ICommand
    {
        private readonly EditorStateViewModel _viewModel;
        public SelectArticleCommand(EditorStateViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // Implement explicit add/remove so the event exists but does not allocate observers by default.
        // This avoids the 'never used' warning while keeping the ICommand contract.
        public event EventHandler? CanExecuteChanged
        {
            add { /* intentionally no-op; command is always enabled via CanExecute */ }
            remove { /* intentionally no-op */ }
        }

        public bool CanExecute(object? parameter) => parameter is ArticleLine;
        public void Execute(object? parameter)
        {
            if (parameter is ArticleLine article)
            {
                // If there's an active segment, prevent selecting a different article
                var activeSeg = IndexEditor.Shared.EditorState.ActiveSegment;
                var activeArticle = IndexEditor.Shared.EditorState.ActiveArticle;
                if (activeSeg != null && activeSeg.IsActive)
                {
                    // Only allow selecting the article that already owns the active segment
                    if (activeArticle != null && !object.ReferenceEquals(activeArticle, article))
                    {
                        // ignore selection change while segment is active and show a toast to explain why
                        IndexEditor.Shared.ToastService.Show("Finish or cancel the open segment first");
                        return;
                    }
                }

                // Try to pick the article instance that exists in the view-model's Articles collection
                var viewArticle = _viewModel.Articles.FirstOrDefault(a => object.ReferenceEquals(a, article))
                              ?? _viewModel.Articles.FirstOrDefault(a =>
                                  a.Pages != null && article.Pages != null && a.Pages.SequenceEqual(article.Pages) && (a.Title ?? string.Empty) == (article.Title ?? string.Empty));

                // If we didn't find a logical in-list match, fall back to the passed instance
                var toSelect = viewArticle ?? article;

                foreach (var a in _viewModel.Articles)
                    a.IsSelected = false;
                toSelect.IsSelected = true;
                // Update global editor state and viewmodel selection
                IndexEditor.Shared.EditorState.ActiveArticle = toSelect;
                IndexEditor.Shared.EditorState.NotifyStateChanged();
                _viewModel.SelectedArticle = toSelect;
            }
        }

        // Navigation checks are performed by the UI (double-click handler) to avoid coupling selection with navigation.
    }
}
