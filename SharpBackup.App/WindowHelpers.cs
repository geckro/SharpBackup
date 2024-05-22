using Avalonia.Controls;

namespace SharpBackup.App;

public static class WindowHelpers
{
    public static void UpdateTextBox(TextBox? textBox, string text)
    {
        if (textBox != null)
        {
            textBox.Text = text;
        }
    }
}