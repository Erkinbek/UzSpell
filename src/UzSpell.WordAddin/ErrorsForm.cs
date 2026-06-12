using System.Drawing;
using System.Windows.Forms;

namespace UzSpell.WordAddin;

/// <summary>
/// Topilgan xatolar roʻyxati va takliflar oynasi. Modeless — Word bilan
/// yonma-yon ishlatish mumkin. Taklif bosilsa hujjatda hamma joyda almashtiriladi.
/// </summary>
internal sealed class ErrorsForm : Form
{
    private readonly dynamic _app;
    private readonly List<ErrorEntry> _entries;

    private readonly ListView _list;
    private readonly Label _selectedLabel;
    private readonly FlowLayoutPanel _suggestionsPanel;
    private readonly Button _ignoreButton;

    public ErrorsForm(dynamic app, List<ErrorEntry> entries, int totalWords)
    {
        _app = app;
        _entries = entries;

        Text = $"UzSpell — {entries.Count} ta xato ({totalWords} ta soʻz tekshirildi)";
        Width = 560;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        ShowIcon = false;
        MinimizeBox = true;
        MaximizeBox = false;

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
        };
        _list.Columns.Add("Xato", 150);
        _list.Columns.Add("Turi", 90);
        _list.Columns.Add("Izoh", 220);
        _list.Columns.Add("Soni", 50);
        _list.SelectedIndexChanged += OnSelectionChanged;
        _list.DoubleClick += OnLocateInDocument;

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 170, Padding = new Padding(10) };

        _selectedLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 10.5f),
            Text = "Xatoni tanlang…",
        };

        _suggestionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
        };

        var buttonsRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        _ignoreButton = new Button { Text = "Roʻyxatdan olib tashlash", AutoSize = true, Enabled = false };
        _ignoreButton.Click += OnIgnore;
        var hint = new Label
        {
            Text = "Ikki marta bosish — hujjatda topish",
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(12, 8, 0, 0),
        };
        buttonsRow.Controls.Add(_ignoreButton);
        buttonsRow.Controls.Add(hint);

        bottom.Controls.Add(_suggestionsPanel);
        bottom.Controls.Add(buttonsRow);
        bottom.Controls.Add(_selectedLabel);

        Controls.Add(_list);
        Controls.Add(bottom);

        Populate();
    }

    private void Populate()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var entry in _entries)
        {
            var item = new ListViewItem(Display(entry.Text)) { Tag = entry };
            item.SubItems.Add(entry.IsGrammar ? "grammatika" : "imlo");
            item.SubItems.Add(entry.Message ?? "Lugʻatda topilmadi");
            item.SubItems.Add(entry.Occurrences.ToString());
            item.ForeColor = entry.IsGrammar ? Color.SteelBlue : Color.Firebrick;
            _list.Items.Add(item);
        }
        _list.EndUpdate();
        Text = $"UzSpell — {_entries.Count} ta xato";
    }

    private static string Display(string text)
    {
        string t = text.Replace("\r", "").Replace("\n", "⏎");
        return string.IsNullOrWhiteSpace(t) ? "‹boʻshliq›" : t;
    }

    private ErrorEntry? Selected =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as ErrorEntry : null;

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        var entry = Selected;
        _suggestionsPanel.Controls.Clear();
        _ignoreButton.Enabled = entry is not null;

        if (entry is null)
        {
            _selectedLabel.Text = "Xatoni tanlang…";
            return;
        }

        _selectedLabel.Text = $"«{Display(entry.Text)}» uchun takliflar:";

        Cursor.Current = Cursors.WaitCursor;
        List<string> suggestions;
        try
        {
            suggestions = CheckerHost.Instance.SuggestionsFor(entry);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        if (suggestions.Count == 0)
        {
            _suggestionsPanel.Controls.Add(new Label
            {
                Text = "Taklif topilmadi",
                ForeColor = Color.Gray,
                AutoSize = true,
            });
            return;
        }

        foreach (var s in suggestions)
        {
            var btn = new Button
            {
                Text = s,
                AutoSize = true,
                Margin = new Padding(0, 0, 6, 6),
                Tag = entry,
            };
            btn.Click += OnSuggestionClick;
            _suggestionsPanel.Controls.Add(btn);
        }
    }

    private void OnSuggestionClick(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: ErrorEntry entry } btn)
            return;

        try
        {
            Cursor.Current = Cursors.WaitCursor;
            WordMarker.ReplaceAll(_app, entry.Text, btn.Text);
            RemoveEntry(entry);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Almashtirib boʻlmadi: " + ex.Message, "UzSpell",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void OnIgnore(object? sender, EventArgs e)
    {
        var entry = Selected;
        if (entry is null)
            return;

        try
        {
            WordMarker.Unmark(_app, entry);
        }
        catch
        {
            // belgilashni olib tashlab boʻlmasa ham roʻyxatdan oʻchiramiz
        }
        RemoveEntry(entry);
    }

    private void OnLocateInDocument(object? sender, EventArgs e)
    {
        var entry = Selected;
        if (entry is null)
            return;

        try
        {
            WordMarker.SelectFirst(_app, entry.Text);
        }
        catch
        {
            // topib boʻlmasa jim qolamiz
        }
    }

    private void RemoveEntry(ErrorEntry entry)
    {
        _entries.Remove(entry);
        _suggestionsPanel.Controls.Clear();
        _selectedLabel.Text = "Xatoni tanlang…";
        _ignoreButton.Enabled = false;
        Populate();
    }
}
