using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace AccountingApp.Views.Pomoc;

public partial class ChangelogWindow : Window
{
    public ChangelogWindow()
    {
        InitializeComponent();
        Loaded += ChangelogWindow_Loaded;
    }

    private void ChangelogWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            TxtAppVersion.Text = version != null ? $"Verzija {version.ToString(3)}" : string.Empty;

            var changelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md");
            if (File.Exists(changelogPath))
            {
                string mdContent = File.ReadAllText(changelogPath, Encoding.UTF8);
                string htmlContent = ConvertMarkdownToHtml(mdContent);
                WbChangelog.NavigateToString(htmlContent);
            }
            else
            {
                WbChangelog.NavigateToString("<html><body><h3>Changelog fajl nije pronađen.</h3></body></html>");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Greška pri učitavanju istorije izmena: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string ConvertMarkdownToHtml(string md)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Arial, sans-serif; margin: 20px; color: #334155; line-height: 1.6; font-size: 13px; }");
        sb.AppendLine("h1 { color: #1E3A8A; font-size: 20px; border-bottom: 2px solid #CBD5E1; padding-bottom: 6px; }");
        sb.AppendLine("h2 { color: #2563EB; font-size: 16px; margin-top: 24px; border-bottom: 1px solid #E2E8F0; padding-bottom: 4px; background: #F1F5F9; padding: 6px 10px; border-radius: 4px; }");
        sb.AppendLine("h3 { color: #0F766E; font-size: 14px; margin-top: 14px; }");
        sb.AppendLine("ul { padding-left: 20px; }");
        sb.AppendLine("li { margin-bottom: 6px; }");
        sb.AppendLine("code { background: #F1F5F9; color: #0F172A; padding: 2px 6px; border-radius: 3px; font-family: Consolas, monospace; font-size: 12px; }");
        sb.AppendLine("hr { border: none; border-top: 1px solid #E2E8F0; margin: 20px 0; }");
        sb.AppendLine("</style></head><body>");

        var lines = md.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        bool inList = false;

        foreach (var rawLine in lines)
        {
            string line = rawLine.TrimEnd();

            if (line.StartsWith("# "))
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
                sb.AppendLine($"<h1>{EscapeHtml(line.Substring(2))}</h1>");
            }
            else if (line.StartsWith("## "))
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
                sb.AppendLine($"<h2>{EscapeHtml(line.Substring(3))}</h2>");
            }
            else if (line.StartsWith("### "))
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
                sb.AppendLine($"<h3>{EscapeHtml(line.Substring(4))}</h3>");
            }
            else if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                if (!inList) { sb.AppendLine("<ul>"); inList = true; }
                sb.AppendLine($"<li>{FormatInline(line.Substring(2))}</li>");
            }
            else if (line.StartsWith("---"))
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
                sb.AppendLine("<hr/>");
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
            }
            else
            {
                if (inList) { sb.AppendLine("</ul>"); inList = false; }
                sb.AppendLine($"<p>{FormatInline(line)}</p>");
            }
        }

        if (inList) { sb.AppendLine("</ul>"); }
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string FormatInline(string input)
    {
        string html = EscapeHtml(input);
        while (html.Contains("**"))
        {
            int first = html.IndexOf("**");
            int second = html.IndexOf("**", first + 2);
            if (first != -1 && second != -1)
            {
                string boldText = html.Substring(first + 2, second - first - 2);
                html = html.Substring(0, first) + "<strong>" + boldText + "</strong>" + html.Substring(second + 2);
            }
            else break;
        }

        while (html.Contains("`"))
        {
            int first = html.IndexOf("`");
            int second = html.IndexOf("`", first + 1);
            if (first != -1 && second != -1)
            {
                string codeText = html.Substring(first + 1, second - first - 1);
                html = html.Substring(0, first) + "<code>" + codeText + "</code>" + html.Substring(second + 1);
            }
            else break;
        }

        return html;
    }

    private string EscapeHtml(string input)
    {
        return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
