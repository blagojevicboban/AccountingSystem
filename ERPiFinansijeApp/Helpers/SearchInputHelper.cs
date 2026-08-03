using System.Windows;
using System.Windows.Controls;

namespace ERPiFinansijeApp.Helpers;

public static class SearchInputHelper
{
    public static readonly DependencyProperty IsSearchProperty =
        DependencyProperty.RegisterAttached(
            "IsSearch",
            typeof(bool),
            typeof(SearchInputHelper),
            new PropertyMetadata(false, OnIsSearchChanged));

    public static bool GetIsSearch(TextBox obj) => (bool)obj.GetValue(IsSearchProperty);
    public static void SetIsSearch(TextBox obj, bool value) => obj.SetValue(IsSearchProperty, value);

    private static void OnIsSearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.Loaded -= TextBox_Loaded;
                textBox.Loaded += TextBox_Loaded;
            }
            else
            {
                textBox.Loaded -= TextBox_Loaded;
            }
        }
    }

    private static void TextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.ApplyTemplate();
            if (textBox.Template?.FindName("PART_ClearButton", textBox) is Button clearBtn)
            {
                clearBtn.Click -= ClearBtn_Click;
                clearBtn.Click += ClearBtn_Click;
            }
        }
    }

    private static void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clearBtn && clearBtn.TemplatedParent is TextBox textBox)
        {
            textBox.Text = string.Empty;
            textBox.Focus();
        }
    }
}
