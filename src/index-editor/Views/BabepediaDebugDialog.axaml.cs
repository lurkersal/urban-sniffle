using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace IndexEditor.Views;

public partial class BabepediaDebugDialog : Window
{
    private TextBlock? _modelNameText;
    private TextBlock? _urlText;
    private string? _url;

    public BabepediaDebugDialog()
    {
        InitializeComponent();
        
        _modelNameText = this.FindControl<TextBlock>("ModelNameText");
        _urlText = this.FindControl<TextBlock>("UrlText");
        
        var okButton = this.FindControl<Button>("OkButton");
        if (okButton != null)
        {
            okButton.Click += (_, __) => Close();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetData(string modelName, string url)
    {
        if (_modelNameText != null)
        {
            _modelNameText.Text = modelName;
        }
        
        if (_urlText != null)
        {
            _urlText.Text = url;
        }
        
        _url = url;
    }

    private void OnUrlClicked(object? sender, PointerPressedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_url))
        {
            try
            {
                // Open URL in default browser
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                IndexEditor.Shared.DebugLogger.LogException("BabepediaDebugDialog: Failed to open URL", ex);
                IndexEditor.Shared.ToastService.Show("Failed to open URL");
            }
        }
    }

    public static void ShowDialog(Window owner, string modelName, string url)
    {
        try
        {
            var dialog = new BabepediaDebugDialog();
            dialog.SetData(modelName, url);
            dialog.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            IndexEditor.Shared.DebugLogger.LogException("BabepediaDebugDialog.ShowDialog", ex);
        }
    }
}

