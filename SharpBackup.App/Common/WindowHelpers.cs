using System.Collections.Generic;
using Avalonia.Controls;

namespace SharpBackup.App.Common;

public static class WindowHelpers
{
    public static void UpdateTextBox(TextBox? textBox, string text)
    {
        if (textBox != null)
        {
            textBox.Text = text;
        }
    }
    public static void UpdateListBox<T>(ListBox? listBox, List<T> list)
    {
        if (listBox == null) return;
        listBox.Items.Clear();
        foreach (var item in list)
        {
            listBox.Items.Add(item);
        }
    }
}