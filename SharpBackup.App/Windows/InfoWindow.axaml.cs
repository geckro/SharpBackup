using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpBackup.App.Windows;

public partial class InfoWindow : Window
{
    private readonly TextBlock? _infoTextBlock;

    public string InfoText
    {
        set => _infoTextBlock!.Text = value;
    }

    public InfoWindow()
    {
        InitializeComponent();
        _infoTextBlock = this.FindControl<TextBlock>("InfoTextBlock");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}