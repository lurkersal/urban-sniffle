using Avalonia.Controls;
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
