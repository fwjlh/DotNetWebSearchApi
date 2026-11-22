using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class DuckDuckGoSearchResultParser
{
    private const string ResultSelector =
        "//div[@id='links']/div[contains(concat(' ', normalize-space(@class), ' '), ' web-result ')]";

    public static bool IsCaptchaPage(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);
        return document.DocumentNode.SelectSingleNode("//form[@id='challenge-form']") is not null;
    }

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

        HtmlNode? snippetNode = item.SelectSingleNode(
            ".//a[contains(concat(' ', normalize-space(@class), ' '), ' result__snippet ')]");
        string snippet = CleanText(snippetNode?.InnerText ?? string.Empty);
        result = new SearchResult(title, url, snippet, "duckduckgo", 1.0 / (position + 1));
        return true;
    }

    private static string ResolveResultUrl(string value)
    {
        string target = ReadQueryValue(value, "uddg");
        if (IsHttpUrl(target))
        {
            return target;
        }

        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return "https:" + value;
        }

        return value;
    }

    private static string ReadQueryValue(string value, string key)
    {
        int queryStart = value.IndexOf('?');
        if (queryStart < 0)
        {
            return string.Empty;
        }

        foreach (string part in value[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
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
