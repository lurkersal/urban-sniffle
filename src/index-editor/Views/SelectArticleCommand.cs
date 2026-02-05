using System;
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
                _viewModel.SelectedArticle = article;
            }
        }
    }
}
