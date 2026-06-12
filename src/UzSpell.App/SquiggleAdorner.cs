using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace UzSpell.App;

/// <summary>
/// TextBox ustiga xato soʻzlar tagiga qizil toʻlqinli chiziq chizadigan qatlam.
/// </summary>
public sealed class SquiggleAdorner : Adorner
{
    private static readonly Pen WavePen;

    static SquiggleAdorner()
    {
        WavePen = new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)), 1.4)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        WavePen.Freeze();
    }

    private IReadOnlyList<(int Start, int Length)> _spans = Array.Empty<(int, int)>();

    public SquiggleAdorner(TextBox textBox) : base(textBox)
    {
        IsHitTestVisible = false;
    }

    private TextBox Box => (TextBox)AdornedElement;

    public void SetSpans(IReadOnlyList<(int Start, int Length)> spans)
    {
        _spans = spans;
        InvalidateVisual();
    }

    public void Clear() => SetSpans(Array.Empty<(int, int)>());

    protected override void OnRender(DrawingContext dc)
    {
        if (_spans.Count == 0)
            return;

        var box = Box;
        int textLength = box.Text.Length;
        if (textLength == 0)
            return;

        int firstVisible, lastVisible;
        try
        {
            firstVisible = box.GetCharacterIndexFromPoint(new Point(2, 2), true);
            lastVisible = box.GetCharacterIndexFromPoint(
                new Point(Math.Max(2, box.ActualWidth - 2), Math.Max(2, box.ActualHeight - 2)), true);
        }
        catch
        {
            return; // layout hali tayyor emas
        }

        if (firstVisible < 0) firstVisible = 0;
        if (lastVisible < 0) lastVisible = textLength - 1;

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, box.ActualWidth, box.ActualHeight)));
        try
        {
            int drawn = 0;
            foreach (var (start, length) in _spans)
            {
                int end = start + length;
                if (end <= firstVisible || start > lastVisible + 1)
                    continue;
                if (end > textLength)
                    continue; // matn oʻzgargan, eskirgan oraliq

                DrawSpan(dc, start, end);
                if (++drawn > 800)
                    break; // koʻrinadigan hudud uchun yetarli
            }
        }
        finally
        {
            dc.Pop();
        }
    }

    private void DrawSpan(DrawingContext dc, int start, int end)
    {
        var box = Box;
        int i = start;
        int guard = 0;
        while (i < end && guard++ < 64)
        {
            int lineIndex;
            try
            {
                lineIndex = box.GetLineIndexFromCharacterIndex(i);
            }
            catch
            {
                return;
            }
            if (lineIndex < 0)
                return;

            int lineStart = box.GetCharacterIndexFromLineIndex(lineIndex);
            int lineEnd = lineStart + box.GetLineLength(lineIndex);
            int segEnd = Math.Min(end, lineEnd);
            if (segEnd <= i)
                return;

            Rect r1 = box.GetRectFromCharacterIndex(i);
            Rect r2 = box.GetRectFromCharacterIndex(segEnd - 1, true);
            if (!r1.IsEmpty && !r2.IsEmpty && r2.Right > r1.Left)
                DrawWave(dc, r1.Left, r2.Right, r1.Bottom - 1.0);

            i = segEnd;
        }
    }

    private static void DrawWave(DrawingContext dc, double x1, double x2, double y)
    {
        const double step = 3.0;
        const double amplitude = 1.6;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(x1, y), false, false);
            bool up = true;
            for (double x = x1 + step; x < x2 + step; x += step)
            {
                double px = Math.Min(x, x2);
                ctx.LineTo(new Point(px, up ? y - amplitude : y + amplitude), true, true);
                up = !up;
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, WavePen, geometry);
    }
}
