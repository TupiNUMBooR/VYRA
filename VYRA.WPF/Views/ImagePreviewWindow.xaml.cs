using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace VYRA.WPF.Views;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow(BitmapSource image)
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        PreviewImage.Source = image;

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }
}
