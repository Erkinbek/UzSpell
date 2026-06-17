using System.Windows;
using UzSpell.Core;

namespace UzSpell.App;

public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();

        // Mavjud sozlamalardan nusxa (Bekor qilinsa asl saqlanadi)
        Settings = new AppSettings
        {
            Script = current.Script,
            Grammar = current.Grammar,
            CheckAllCaps = current.CheckAllCaps,
            MaxSuggestions = current.MaxSuggestions,
        };

        CmbScript.SelectedIndex = current.Script switch
        {
            UzbekScript.Latin => 1,
            UzbekScript.Cyrillic => 2,
            _ => 0,
        };
        ChkGrammar.IsChecked = current.Grammar;
        ChkAllCaps.IsChecked = current.CheckAllCaps;
        SldMax.Value = current.MaxSuggestions;
        LblMax.Text = current.MaxSuggestions.ToString();
    }

    private void OnMaxChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblMax is not null)
            LblMax.Text = ((int)e.NewValue).ToString();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Settings.Script = CmbScript.SelectedIndex switch
        {
            1 => UzbekScript.Latin,
            2 => UzbekScript.Cyrillic,
            _ => null,
        };
        Settings.Grammar = ChkGrammar.IsChecked == true;
        Settings.CheckAllCaps = ChkAllCaps.IsChecked == true;
        Settings.MaxSuggestions = (int)SldMax.Value;

        DialogResult = true;
        Close();
    }
}
