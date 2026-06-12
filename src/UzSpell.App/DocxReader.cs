using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace UzSpell.App;

/// <summary>.docx faylidan matnni ajratib oladi (Word shart emas, oflayn).</summary>
public static class DocxReader
{
    public static string ExtractText(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entry = zip.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("Bu fayl .docx formatida emas.");

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var sb = new StringBuilder();
        foreach (var paragraph in doc.Descendants(w + "p"))
        {
            foreach (var node in paragraph.Descendants())
            {
                if (node.Name == w + "t")
                    sb.Append(node.Value);
                else if (node.Name == w + "tab")
                    sb.Append('\t');
                else if (node.Name == w + "br" || node.Name == w + "cr")
                    sb.AppendLine();
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
