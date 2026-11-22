using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class SeznamSearchResultParser
{
    private const string ResultSelector =
        "//div[@id='searchpage-root']//div[@class='Layout--left']/div[@class='f2c528']";

    public static IReadOnlyDictionary<string, string> ReadHiddenFields(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);
        HtmlNodeCollection? inputs = document.DocumentNode.SelectNodes("//input[@type='hidden']");
        if (inputs is null)
        {
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        foreach (HtmlNode input in inputs)
        {
            string name = input.GetAttributeValue("name", string.Empty);
            if (name.Length == 0)
            {
                continue;
            }

            fields[name] = input.GetAttributeValue("value", string.Empty);
        }

        return fields;
    }

    public static bool IsVerificationPage(Uri? uri)
    {
        return uri is not null && uri.AbsolutePath.StartsWith("/verify", StringComparison.OrdinalIgnoreCase);
    }

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
        HtmlNode? contentNode = card.SelectSingleNode(
            ".//div[@class='c8774a' or @class='e69e8d a11657']");
        HtmlNode? titleNode = card.SelectSingleNode(".//h3/a");
        if (contentNode is null || titleNode is null)
        {
            return false;
        }

        string title = CleanText(titleNode.InnerText);
        string url = titleNode.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        result = new SearchResult(title, url, CleanText(contentNode.InnerText), "seznam", 1.0 / (position + 1));
        return true;
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
