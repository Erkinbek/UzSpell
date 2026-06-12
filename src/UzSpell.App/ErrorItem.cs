using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using UzSpell.Core;

namespace UzSpell.App;

/// <summary>Xatolar panelidagi bitta element (imlo yoki grammatika).</summary>
public sealed class ErrorItem : INotifyPropertyChanged
{
    public required string Word { get; init; }
    public required string Normalized { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
    public UzbekScript Script { get; init; }

    /// <summary>Grammatik xato uchun true, imlo xatosi uchun false.</summary>
    public bool IsGrammar { get; init; }

    /// <summary>Grammatik qoida identifikatori (TAKROR, SHAXS-SON, …).</summary>
    public string? RuleId { get; init; }

    /// <summary>Grammatik xato tavsifi.</summary>
    public string? Message { get; init; }

    /// <summary>Qaysi tekshiruv natijasiga tegishli (eskirgan almashtirishlardan saqlaydi).</summary>
    public int CheckVersion { get; init; }

    /// <summary>Word hujjatida nechta joyda uchragani (faqat Word rejimida).</summary>
    public int Occurrences { get; set; }

    public string KindLabel => IsGrammar
        ? "grammatika"
        : Script == UzbekScript.Latin ? "imlo · lotin" : "imlo · kirill";

    /// <summary>Panelda koʻrsatiladigan matn (boʻshliq belgilari koʻrinadigan qilinadi).</summary>
    public string DisplayWord
    {
        get
        {
            string w = Word.Replace("\r", "").Replace("\n", "⏎");
            if (string.IsNullOrWhiteSpace(w))
                return "‹boʻshliq›";
            return w.Length > 30 ? w.Substring(0, 30) + "…" : w;
        }
    }

    public System.Windows.Media.Brush WordBrush => IsGrammar
        ? System.Windows.Media.Brushes.SteelBlue
        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));

    public Visibility MessageVisibility =>
        string.IsNullOrEmpty(Message) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>«Lugʻatga qoʻshish» faqat imlo xatolari uchun maʼnoli.</summary>
    public Visibility DictButtonVisibility =>
        IsGrammar ? Visibility.Collapsed : Visibility.Visible;

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
