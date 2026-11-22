using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class YahooSearchResultParser
{
    private const string ResultSelector = "//div[contains(concat(' ', normalize-space(@class), ' '), ' algo-sr ')]";

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
            ".//div[contains(concat(' ', normalize-space(@class), ' '), ' compTitle ')]//a");
        if (link is null)
        {
            return false;
        }

        string url = ResolveYahooUrl(link.GetAttributeValue("href", string.Empty));
        HtmlNode? titleNode = card.SelectSingleNode(".//h3//a[@aria-label]");
        string title = CleanText(titleNode?.GetAttributeValue("aria-label", string.Empty)
            ?? card.SelectSingleNode(".//a/h3")?.InnerText
            ?? string.Empty);
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        HtmlNode? snippetNode = card.SelectSingleNode(
            ".//div[contains(concat(' ', normalize-space(@class), ' '), ' compText ')]");
        result = new SearchResult(title, url, CleanText(snippetNode?.InnerText ?? string.Empty), "yahoo", 1.0 / (position + 1));
        return true;
    }

    private static string ResolveYahooUrl(string value)
    {
        int redirectStart = value.IndexOf("/RU=", StringComparison.OrdinalIgnoreCase);
        if (redirectStart < 0)
        {
            return value;
        }

        int targetStart = value.IndexOf("http", redirectStart + 4, StringComparison.OrdinalIgnoreCase);
        int resultEnd = FindRedirectEnd(value);
        if (targetStart < 0 || resultEnd <= targetStart)
        {
            return value;
        }

        return Uri.UnescapeDataString(value[targetStart..resultEnd]);
    }

    private static int FindRedirectEnd(string value)
    {
        int resultEnd = value.Length;
        int rs = value.LastIndexOf("/RS", StringComparison.OrdinalIgnoreCase);
        int rk = value.LastIndexOf("/RK", StringComparison.OrdinalIgnoreCase);
        if (rs >= 0)
        {
            resultEnd = Math.Min(resultEnd, rs);
        }

        if (rk >= 0)
        {
            resultEnd = Math.Min(resultEnd, rk);
        }

        return resultEnd == value.Length ? -1 : resultEnd;
    }

    private static string CleanText(string value)
    {
        HtmlDocument document = new();
        document.LoadHtml(value);
        return Regex.Replace(HtmlEntity.DeEntitize(document.DocumentNode.InnerText), @"\s+", " ").Trim();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
