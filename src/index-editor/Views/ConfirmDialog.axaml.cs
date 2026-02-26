using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace IndexEditor.Views
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog()
        {
            InitializeComponent();
            OkButton.Click += (_, __) => Close(true);
            CancelButton.Click += (_, __) => Close(false);
            
            // Add keyboard shortcuts: Y for Yes/Save, N for No/Cancel
            this.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Y)
            {
                e.Handled = true;
                Close(true);
            }
            else if (e.Key == Key.N)
            {
                e.Handled = true;
                Close(false);
            }
        }

        public void SetMessage(string msg)
        {
            MessageText.Text = msg;
        }

        public static async Task<bool> ShowDialog(Window owner, string message)
        {
            var dlg = new ConfirmDialog();
            dlg.SetMessage(message);
            var result = await dlg.ShowDialog<bool>(owner);
            return result;
        }
    }
}
