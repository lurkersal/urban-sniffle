using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Windows.Input;
using IndexEditor.Views; // for EditorStateViewModel

namespace IndexEditor.Views
{
    public partial class ArticleList : UserControl
    {
        public ArticleList()
        {
            InitializeComponent();
            this.DataContextChanged += (_, e) =>
            {
                if (DataContext == null && this.Parent is Window window)
                {
                    DataContext = window.DataContext;
                }
            };
        }

        private void OnArticlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (sender is Control control)
                {
                    // Use a strongly-typed view-model instead of reflection
                    if (this.DataContext is EditorStateViewModel vm)
                    {
                        var cmd = vm.SelectArticleCommand;
                        var parameter = control.Tag;
                        if (cmd != null && cmd.CanExecute(parameter))
                        {
                            cmd.Execute(parameter);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // swallow exceptions to avoid breaking the UI; logging could be added here
            }
        }
    }
}
