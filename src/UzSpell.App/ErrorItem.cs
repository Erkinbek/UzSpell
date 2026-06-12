using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using UzSpell.Core;

namespace UzSpell.App;

/// <summary>Xatolar panelidagi bitta element.</summary>
public sealed class ErrorItem : INotifyPropertyChanged
{
    public required string Word { get; init; }
    public required string Normalized { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
    public UzbekScript Script { get; init; }

    /// <summary>Qaysi tekshiruv natijasiga tegishli (eskirgan almashtirishlardan saqlaydi).</summary>
    public int CheckVersion { get; init; }

    /// <summary>Word hujjatida nechta joyda uchragani (faqat Word rejimida).</summary>
    public int Occurrences { get; set; }

    public string ScriptLabel => Script == UzbekScript.Latin ? "lotin" : "kirill";

    public ObservableCollection<string> Suggestions { get; } = new();

    private bool _suggestionsLoaded;
    public bool SuggestionsLoaded
    {
        get => _suggestionsLoaded;
        set
        {
            _suggestionsLoaded = value;
            OnChanged(nameof(SuggestionsHint));
            OnChanged(nameof(HintVisibility));
        }
    }

    public string SuggestionsHint =>
        SuggestionsLoaded && Suggestions.Count == 0 ? "taklif topilmadi" : "takliflar yuklanmoqda…";

    public Visibility HintVisibility =>
        SuggestionsLoaded && Suggestions.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
