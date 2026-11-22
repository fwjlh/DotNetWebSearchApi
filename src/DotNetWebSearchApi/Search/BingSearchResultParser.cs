using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class BingSearchResultParser
{
    private const string ResultSelector =
        "//ol[@id='b_results']/li[contains(concat(' ', normalize-space(@class), ' '), ' b_algo ')]";

    public static IReadOnlyList<SearchResult> Parse(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNodeCollection? items = document.DocumentNode.SelectNodes(ResultSelector);
        if (items is null)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        foreach (HtmlNode item in items)
        {
            if (!TryParseResult(item, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    private static bool TryParseResult(HtmlNode item, int position, out SearchResult? result)
    {
        result = null;
        HtmlNode? link = item.SelectSingleNode(".//h2/a");
        if (link is null)
        {
            return false;
        }

        string title = CleanText(link.InnerText);
        string url = ResolveResultUrl(link.GetAttributeValue("href", string.Empty));
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        result = new SearchResult(title, url, ReadSnippet(item), "bing", 1.0 / (position + 1));
        return true;
    }

    private static string ReadSnippet(HtmlNode item)
    {
        HtmlNodeCollection? paragraphs = item.SelectNodes(".//p");
        if (paragraphs is null)
        {
            return string.Empty;
        }

        List<string> snippets = new();
        foreach (HtmlNode paragraph in paragraphs)
        {
            RemoveDecorativeIcons(paragraph);
            string snippet = CleanText(paragraph.InnerText);
            if (snippet.Length > 0)
            {
                snippets.Add(snippet);
            }
        }

        return string.Join(" ", snippets);
    }

    private static void RemoveDecorativeIcons(HtmlNode paragraph)
    {
        HtmlNodeCollection? icons = paragraph.SelectNodes(
            ".//span[contains(concat(' ', normalize-space(@class), ' '), ' algoSlug_icon ')]");
        if (icons is null)
        {
            return;
        }

        foreach (HtmlNode icon in icons)
        {
            icon.Remove();
        }
    }

    private static string ResolveResultUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !uri.Host.EndsWith("bing.com", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.StartsWith("/ck/a", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        string encodedTarget = ReadQueryValue(uri.Query, "u");
        if (!encodedTarget.StartsWith("a1", StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            string base64 = encodedTarget[2..].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static string ReadQueryValue(string query, string key)
    {
        foreach (string part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            if (Uri.UnescapeDataString(pair[0]).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
            }
        }

        return string.Empty;
    }

    private static string CleanText(string value)
    {
        string decoded = HtmlEntity.DeEntitize(value);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
