using System.Windows;
using WpfInput = System.Windows.Input;

namespace VYRA.WPF.Views;

public partial class OpenAiTokenWindow : Window
{
    public OpenAiTokenWindow(bool hasExistingToken)
    {
        InitializeComponent();

        if (hasExistingToken)
            Title = "OpenAI token (saved)";

        Loaded += (_, _) =>
        {
            TokenBox.Focus();
            TokenBox.SelectAll();
        };
    }

    public string? Token { get; private set; }
    public bool ClearRequested { get; private set; }

    private void Save()
    {
        Token = TokenBox.Password.Trim();
        DialogResult = true;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Save();

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ClearRequested = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TokenBox_KeyDown(object sender, WpfInput.KeyEventArgs e)
    {
        if (e.Key == WpfInput.Key.Enter)
        {
            Save();
            e.Handled = true;
        }

        if (e.Key == WpfInput.Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }
}
