using System.Windows;
using System.Windows.Input;

namespace AccountingApp.Views.Nalozi;

public partial class NalogHelpWindow : Window
{
    public NalogHelpWindow()
    {
        InitializeComponent();
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Enter)
        {
            Close();
        }
    }
}
