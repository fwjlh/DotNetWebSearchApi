using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class NaverSearchResultParser
{
    private const string ResultSelector =
        "//div[contains(concat(' ', normalize-space(@class), ' '), ' fds-web-normal-doc-root ')]";

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
        HtmlNode? titleNode = item.SelectSingleNode(
            ".//span[contains(@class, 'sds-comps-text-type-headline1')]");
        HtmlNode? link = item.SelectSingleNode(
            ".//a[starts-with(@href, 'http') and not(contains(@href, 'keep.naver.com'))]");
        if (titleNode is null || link is null)
        {
            return false;
        }

        string title = CleanText(titleNode.InnerText);
        string url = link.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        HtmlNode? snippetNode = item.SelectSingleNode(
            ".//span[contains(@class, 'sds-comps-text-type-body1')]");
        HtmlNode? thumbnailNode = item.SelectSingleNode(
            ".//div[contains(@class, 'sds-comps-image') and not(contains(@class, 'sds-comps-image-circle'))]/img");
        string thumbnailUrl = thumbnailNode?.GetAttributeValue("src", string.Empty) ?? string.Empty;
        string? thumbnail = IsHttpUrl(thumbnailUrl) ? thumbnailUrl : null;

        result = new SearchResult(title, url, CleanText(snippetNode?.InnerText ?? string.Empty), "naver", 1.0 / (position + 1))
        {
            ThumbnailUrl = thumbnail
        };
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
