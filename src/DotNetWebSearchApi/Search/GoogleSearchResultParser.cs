using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class GoogleSearchResultParser
{
    private const string ResultSelector =
        "//div[contains(concat(' ', normalize-space(@class), ' '), ' zMzFAb ') and .//a[contains(concat(' ', normalize-space(@class), ' '), ' fuLhoc ')]]";

    public static IReadOnlyList<SearchResult> Parse(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNodeCollection? cards = document.DocumentNode.SelectNodes(ResultSelector);
        if (cards is null)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        foreach (HtmlNode card in cards)
        {
            if (!TryParseResult(card, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    private static bool TryParseResult(HtmlNode card, int position, out SearchResult? result)
    {
        result = null;
        HtmlNode? link = card.SelectSingleNode(
            ".//a[contains(concat(' ', normalize-space(@class), ' '), ' fuLhoc ')]");
        if (link is null)
        {
            return false;
        }

        HtmlNode? titleNode = link.SelectSingleNode(
            ".//span[contains(concat(' ', normalize-space(@class), ' '), ' CVA68e ')]");
        string title = CleanText(titleNode?.InnerText ?? link.InnerText);
        string url = ResolveGoogleUrl(link.GetAttributeValue("href", string.Empty));
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        result = new SearchResult(title, url, ReadSnippet(card), "google", 1.0 / (position + 1));
        return true;
    }

    private static string ReadSnippet(HtmlNode card)
    {
        HtmlNode? snippetNode = card.SelectSingleNode(
            ".//span[contains(concat(' ', normalize-space(@class), ' '), ' FrIlee ')]"
            + "/span[contains(concat(' ', normalize-space(@class), ' '), ' fYyStc ')"
            + " and not(contains(concat(' ', normalize-space(@class), ' '), ' YVIcad '))]");
        return CleanText(snippetNode?.InnerText ?? string.Empty);
    }

    private static string ResolveGoogleUrl(string value)
    {
        string target = ReadQueryValue(value, "q");
        return IsHttpUrl(target) ? target : value;
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
