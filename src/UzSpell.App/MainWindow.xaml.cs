using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using UzSpell.Core;

namespace UzSpell.App;

public partial class MainWindow : Window
{
    private readonly UzbekSpellChecker _checker;
    private readonly object _gate = new();
    private readonly DispatcherTimer _debounce;
    private readonly string _customWordsPath;
    private readonly HashSet<string> _ignoredGrammar = new(StringComparer.Ordinal);

    private GrammarChecker? _grammar;
    private SquiggleAdorner? _adorner;
    private int _checkVersion;
    private bool _wordMode;
    private dynamic? _wordApp;

    public MainWindow()
    {
        InitializeComponent();

        string dictDir = Path.Combine(AppContext.BaseDirectory, "dictionaries");
        _checker = new UzbekSpellChecker(dictDir);

        _customWordsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UzSpell", "custom_words.txt");
        LoadCustomWords();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            RunCheck();
        };
    }

    private void LoadCustomWords()
    {
        try
        {
            if (File.Exists(_customWordsPath))
            {
                foreach (var line in File.ReadAllLines(_customWordsPath, Encoding.UTF8))
                {
                    var word = line.Trim();
                    if (word.Length > 0)
                        _checker.CustomWords.Add(word);
                }
            }
        }
        catch
        {
            // shaxsiy lugʻatni oʻqib boʻlmasa ham dastur ishlayveradi
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var layer = AdornerLayer.GetAdornerLayer(Editor);
        if (layer is not null)
        {
            _adorner = new SquiggleAdorner(Editor);
            layer.Add(_adorner);
        }

        Editor.AddHandler(ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler((_, _) => _adorner?.InvalidateVisual()));
        Editor.SizeChanged += (_, _) => _adorner?.InvalidateVisual();

        Editor.Text =
            "Bu yerga matnni yozing yoki qoʻying — imlo xatolari avtomatik tekshiriladi.\n" +
            "Sinab koʻrish uchun: kitobb, hatolik, togri.";

        LblStatus.Text = "Lugʻat yuklanmoqda…";
        string dictDir = Path.Combine(AppContext.BaseDirectory, "dictionaries");
        await Task.Run(() =>
        {
            lock (_gate)
            {
                _checker.WarmUp();
                _grammar = GrammarChecker.CreateFromDictionary(dictDir, _checker);
            }
        });
        LblStatus.Text = "Tayyor";
        RunCheck();
    }

    // ----- Tekshiruv -----

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_wordMode)
            SwitchToEditorMode();

        _adorner?.Clear();
        _debounce.Stop();
        _debounce.Start();
    }

    private async void RunCheck()
    {
        int version = ++_checkVersion;
        string text = Editor.Text;
        LblStatus.Text = "Tekshirilmoqda…";

        CheckResult result;
        List<GrammarIssue> grammarIssues;
        try
        {
            (result, grammarIssues) = await Task.Run(() =>
            {
                lock (_gate)
                {
                    var spelling = _checker.CheckText(text);
                    var grammar = _grammar?.Check(text) ?? new List<GrammarIssue>();
                    return (spelling, grammar);
                }
            });
        }
        catch (Exception ex)
        {
            LblStatus.Text = "Xatolik: " + ex.Message;
            return;
        }

        if (version != _checkVersion || _wordMode)
            return;

        var items = result.Errors.Select(err => new ErrorItem
        {
            Word = err.Word,
            Normalized = err.Normalized,
            Start = err.Start,
            Length = err.Length,
            Script = err.Script,
            CheckVersion = version,
        }).ToList();

        foreach (var issue in grammarIssues)
        {
            string span = SafeSubstring(text, issue.Start, issue.Length);
            if (_ignoredGrammar.Contains(GrammarKey(issue.RuleId, span)))
                continue;

            var item = new ErrorItem
            {
                Word = span,
                Normalized = span,
                Start = issue.Start,
                Length = issue.Length,
                Script = UzbekScript.Latin,
                IsGrammar = true,
                RuleId = issue.RuleId,
                Message = issue.Message,
                CheckVersion = version,
            };
            foreach (var s in issue.Suggestions)
                item.Suggestions.Add(s);
            item.SuggestionsLoaded = true;
            items.Add(item);
        }

        items.Sort((a, b) => a.Start.CompareTo(b.Start));

        LstErrors.ItemsSource = items;
        LblPanelTitle.Text = items.Count > 0 ? $"Xatolar ({items.Count})" : "Xatolar";
        _adorner?.SetSpans(
            items.Where(i => !i.IsGrammar).Select(i => (i.Start, i.Length)).ToList(),
            items.Where(i => i.IsGrammar).Select(i => (i.Start, i.Length)).ToList());
        int spellCount = items.Count(i => !i.IsGrammar);
        LblStats.Text = $"Soʻzlar: {result.TotalWords} • Imlo: {spellCount} • Grammatika: {items.Count - spellCount}";
        LblStatus.Text = "Tayyor";

        _ = LoadSuggestionsAsync(items, version);
    }

    private static string SafeSubstring(string text, int start, int length)
    {
        if (start < 0 || start >= text.Length)
            return "";
        return text.Substring(start, Math.Min(length, text.Length - start));
    }

    private static string GrammarKey(string ruleId, string span) => ruleId + "|" + span;

    private async Task LoadSuggestionsAsync(List<ErrorItem> items, int version)
    {
        foreach (var item in items.Take(40))
        {
            if (version != _checkVersion && !_wordMode)
                return;
            await LoadSuggestionsForItem(item);
        }
    }

    private async Task LoadSuggestionsForItem(ErrorItem item)
    {
        if (item.SuggestionsLoaded)
            return;

        var suggestions = await Task.Run(() =>
        {
            lock (_gate)
                return _checker.Suggest(item.Normalized, item.Script);
        });

        item.Suggestions.Clear();
        foreach (var s in suggestions)
            item.Suggestions.Add(s);
        item.SuggestionsLoaded = true;
    }

    // ----- Xatolar paneli -----

    private static ErrorItem? ItemFromTag(object sender) =>
        (sender as FrameworkElement)?.Tag as ErrorItem;

    private void OnSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: string suggestion } button)
            return;
        if (button.Tag is not ErrorItem item)
            return;

        if (_wordMode)
        {
            ApplyWordReplacement(item, suggestion);
            return;
        }

        if (item.CheckVersion != _checkVersion)
            return; // matn oʻzgargan, joylashuv eskirgan

        if (item.Start + item.Length > Editor.Text.Length ||
            Editor.Text.Substring(item.Start, item.Length) != item.Word)
            return;

        Editor.Select(item.Start, item.Length);
        Editor.SelectedText = suggestion;
        Editor.CaretIndex = item.Start + suggestion.Length;
        Editor.Focus();
    }

    private void OnIgnoreClick(object sender, RoutedEventArgs e)
    {
        var item = ItemFromTag(sender);
        if (item is null)
            return;

        if (item.IsGrammar)
        {
            _ignoredGrammar.Add(GrammarKey(item.RuleId ?? "", item.Word));
        }
        else
        {
            lock (_gate)
                _checker.IgnoredWords.Add(item.Normalized);
        }

        if (_wordMode)
            RemoveWordItem(item, unmark: true);
        else
            RunCheck();
    }

    private void OnAddToDictClick(object sender, RoutedEventArgs e)
    {
        var item = ItemFromTag(sender);
        if (item is null)
            return;

        lock (_gate)
            _checker.CustomWords.Add(item.Normalized);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_customWordsPath)!);
            File.AppendAllText(_customWordsPath, item.Normalized + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Shaxsiy lugʻatga yozib boʻlmadi: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (_wordMode)
            RemoveWordItem(item, unmark: true);
        else
            RunCheck();
    }

    private async void OnErrorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LstErrors.SelectedItem is not ErrorItem item)
            return;

        if (!_wordMode && item.CheckVersion == _checkVersion &&
            item.Start + item.Length <= Editor.Text.Length)
        {
            Editor.Focus();
            Editor.Select(item.Start, item.Length);
            try
            {
                int line = Editor.GetLineIndexFromCharacterIndex(item.Start);
                Editor.ScrollToLine(line);
            }
            catch
            {
                // joylashib boʻlmasa ham muammo emas
            }
        }

        await LoadSuggestionsForItem(item);
    }

    // ----- Kontekst menyu (oʻng tugma) -----

    private void OnEditorContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var menu = new ContextMenu();

        int index = Editor.CaretIndex;
        if (e.CursorLeft >= 0 && e.CursorTop >= 0)
        {
            int fromPoint = Editor.GetCharacterIndexFromPoint(
                new Point(e.CursorLeft, e.CursorTop), true);
            if (fromPoint >= 0)
                index = fromPoint;
        }

        var item = (LstErrors.ItemsSource as IEnumerable<ErrorItem>)?
            .FirstOrDefault(i => !_wordMode &&
                                 i.CheckVersion == _checkVersion &&
                                 index >= i.Start && index <= i.Start + i.Length);

        if (item is not null)
        {
            IReadOnlyList<string> suggestions;
            if (item.IsGrammar)
            {
                suggestions = item.Suggestions.ToList();
            }
            else
            {
                lock (_gate)
                    suggestions = _checker.Suggest(item.Normalized, item.Script);
            }

            if (suggestions.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "Taklif topilmadi", IsEnabled = false });
            }
            else
            {
                foreach (var s in suggestions)
                {
                    var mi = new MenuItem { Header = s, FontWeight = FontWeights.SemiBold };
                    string suggestion = s;
                    var captured = item;
                    mi.Click += (_, _) =>
                    {
                        if (captured.CheckVersion != _checkVersion)
                            return;
                        if (captured.Start + captured.Length > Editor.Text.Length)
                            return;
                        Editor.Select(captured.Start, captured.Length);
                        Editor.SelectedText = suggestion;
                    };
                    menu.Items.Add(mi);
                }
            }

            menu.Items.Add(new Separator());

            var ignore = new MenuItem { Header = "Eʼtiborsiz qoldirish" };
            ignore.Click += (_, _) =>
            {
                if (item.IsGrammar)
                {
                    _ignoredGrammar.Add(GrammarKey(item.RuleId ?? "", item.Word));
                }
                else
                {
                    lock (_gate)
                        _checker.IgnoredWords.Add(item.Normalized);
                }
                RunCheck();
            };
            menu.Items.Add(ignore);

            if (!item.IsGrammar)
            {
                var addDict = new MenuItem { Header = "Lugʻatga qoʻshish" };
                addDict.Click += (_, _) => OnAddToDictClick(new Button { Tag = item }, new RoutedEventArgs());
                menu.Items.Add(addDict);
            }

            menu.Items.Add(new Separator());
        }

        menu.Items.Add(new MenuItem { Command = ApplicationCommands.Cut, Header = "Kesish" });
        menu.Items.Add(new MenuItem { Command = ApplicationCommands.Copy, Header = "Nusxalash" });
        menu.Items.Add(new MenuItem { Command = ApplicationCommands.Paste, Header = "Qoʻyish" });
        menu.Items.Add(new MenuItem { Command = ApplicationCommands.SelectAll, Header = "Hammasini belgilash" });

        Editor.ContextMenu = menu;
    }

    // ----- Fayl amallari -----

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Matn va Word fayllari|*.txt;*.md;*.docx|Matn fayllari|*.txt;*.md|Word hujjati|*.docx|Barcha fayllar|*.*",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            string path = dialog.FileName;
            Editor.Text = Path.GetExtension(path).Equals(".docx", StringComparison.OrdinalIgnoreCase)
                ? DocxReader.ExtractText(path)
                : File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Faylni ochib boʻlmadi: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Matn fayli|*.txt",
            FileName = "matn.txt",
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, Editor.Text, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Faylni saqlab boʻlmadi: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ----- Transliteratsiya -----

    private void OnToCyrillic(object sender, RoutedEventArgs e) =>
        Transliterate(UzbekTransliterator.ToCyrillic, "Lotin → kirill oʻgirildi");

    private void OnToLatin(object sender, RoutedEventArgs e) =>
        Transliterate(UzbekTransliterator.ToLatin, "Kirill → lotin oʻgirildi");

    private void Transliterate(Func<string, string> convert, string statusText)
    {
        if (Editor.SelectionLength > 0)
        {
            int start = Editor.SelectionStart;
            string converted = convert(Editor.SelectedText);
            Editor.SelectedText = converted;
            Editor.Select(start, converted.Length);
        }
        else
        {
            Editor.Text = convert(Editor.Text);
        }
        Editor.Focus();
        LblStatus.Text = statusText;
    }

    private void OnScriptChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_checker is null)
            return;

        lock (_gate)
        {
            _checker.ForcedScript = CmbScript.SelectedIndex switch
            {
                1 => UzbekScript.Latin,
                2 => UzbekScript.Cyrillic,
                _ => null,
            };
        }
        RunCheck();
    }

    // ----- Microsoft Word integratsiyasi -----

    private async void OnCheckWordDoc(object sender, RoutedEventArgs e)
    {
        var app = WordInterop.GetRunningWordApp();
        if (app is null)
        {
            MessageBox.Show(this,
                "Microsoft Word ishlamayapti. Avval Word'da hujjatni oching, soʻng qayta urinib koʻring.",
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!WordInterop.HasActiveDocument(app))
        {
            MessageBox.Show(this, "Word'da ochiq hujjat topilmadi.",
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _wordApp = app;
        string docText;
        try
        {
            docText = WordInterop.GetActiveDocumentText(app);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Word hujjatini oʻqib boʻlmadi: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        LblStatus.Text = "Word hujjati tekshirilmoqda…";
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var (result, grammarIssues) = await Task.Run(() =>
            {
                lock (_gate)
                {
                    var spelling = _checker.CheckText(docText);
                    var grammar = _grammar?.Check(docText) ?? new List<GrammarIssue>();
                    return (spelling, grammar);
                }
            });

            // Bir xil soʻzlarni birlashtiramiz
            var distinct = result.Errors
                .GroupBy(err => err.Word, StringComparer.Ordinal)
                .Select(g => new ErrorItem
                {
                    Word = g.Key,
                    Normalized = g.First().Normalized,
                    Script = g.First().Script,
                    Occurrences = g.Count(),
                    CheckVersion = -1,
                })
                .ToList();

            // Grammatik xatolar — parcha matni boʻyicha birlashtiramiz
            foreach (var g in grammarIssues
                         .GroupBy(i => (i.RuleId, Span: SafeSubstring(docText, i.Start, i.Length))))
            {
                string span = g.Key.Span;
                if (span.Trim().Length == 0 || _ignoredGrammar.Contains(GrammarKey(g.Key.RuleId, span)))
                    continue;

                var first = g.First();
                var item = new ErrorItem
                {
                    Word = span,
                    Normalized = span,
                    Script = UzbekScript.Latin,
                    IsGrammar = true,
                    RuleId = first.RuleId,
                    Message = first.Message,
                    Occurrences = g.Count(),
                    CheckVersion = -1,
                };
                foreach (var s in first.Suggestions)
                    item.Suggestions.Add(s);
                item.SuggestionsLoaded = true;
                distinct.Add(item);
            }

            int marked = 0;
            foreach (var item in distinct)
            {
                LblStatus.Text = $"Belgilanmoqda: {item.DisplayWord} ({++marked}/{distinct.Count})";
                try
                {
                    if (item.IsGrammar)
                        WordInterop.MarkPhrase(app, item.Word);
                    else
                        WordInterop.MarkWord(app, item.Word);
                }
                catch
                {
                    // bitta soʻzni belgilab boʻlmasa davom etamiz
                }
            }

            _wordMode = true;
            LstErrors.ItemsSource = distinct;
            LblPanelTitle.Text = $"Word hujjatidagi xatolar ({distinct.Count})";
            _adorner?.Clear();
            int spellCount = distinct.Count(i => !i.IsGrammar);
            LblStats.Text = $"Word: {result.TotalWords} ta soʻz • Imlo: {spellCount} • Grammatika: {distinct.Count - spellCount}";
            LblStatus.Text = distinct.Count > 0
                ? "Word hujjatida xatolar toʻlqinli chiziq bilan belgilandi (qizil — imlo, koʻk — grammatika)"
                : "Word hujjatida xato topilmadi";

            _ = LoadSuggestionsAsync(distinct, _checkVersion);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Word bilan ishlashda xatolik: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
            LblStatus.Text = "Tayyor";
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OnClearWordMarks(object sender, RoutedEventArgs e)
    {
        var app = _wordApp ?? WordInterop.GetRunningWordApp();
        if (app is null || !WordInterop.HasActiveDocument(app))
        {
            MessageBox.Show(this, "Word'da ochiq hujjat topilmadi.",
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            WordInterop.ClearAllMarks(app);
            LblStatus.Text = "Word hujjatidagi belgilashlar tozalandi";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Tozalashda xatolik: " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        if (_wordMode)
            SwitchToEditorMode();
    }

    private void ApplyWordReplacement(ErrorItem item, string suggestion)
    {
        if (_wordApp is null)
            return;

        try
        {
            WordInterop.ReplaceAll(_wordApp, item.Word, suggestion);
            RemoveWordItem(item, unmark: false);
            LblStatus.Text = $"«{item.Word}» → «{suggestion}» almashtirildi";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Almashtirib boʻlmadi (Word yopilgan boʻlishi mumkin): " + ex.Message,
                "UzSpell", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveWordItem(ErrorItem item, bool unmark)
    {
        if (unmark && _wordApp is not null)
        {
            try
            {
                WordInterop.UnmarkWord(_wordApp, item.Word);
            }
            catch
            {
                // belgilashni olib tashlab boʻlmasa ham roʻyxatdan oʻchiramiz
            }
        }

        if (LstErrors.ItemsSource is List<ErrorItem> list)
        {
            list.Remove(item);
            LstErrors.ItemsSource = null;
            LstErrors.ItemsSource = list;
            LblPanelTitle.Text = $"Word hujjatidagi xatolar ({list.Count})";
        }
    }

    private void SwitchToEditorMode()
    {
        _wordMode = false;
        LblPanelTitle.Text = "Xatolar";
        LstErrors.ItemsSource = null;
    }
}
