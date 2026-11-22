using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace DotNetWebSearchApi.Search;

public static class YandexSearchResultParser
{
    public static IReadOnlyList<SearchResult> Parse(string base64Xml)
    {
        byte[] xmlBytes = Convert.FromBase64String(base64Xml);
        string xml = Encoding.UTF8.GetString(xmlBytes);
        XDocument document = XDocument.Parse(xml);

        XElement? error = document.Descendants("error").FirstOrDefault();
        if (error is not null)
        {
            if (error.Attribute("code")?.Value == "15")
            {
                return Array.Empty<SearchResult>();
            }

            throw new InvalidOperationException($"Yandex Search API returned error {error.Attribute("code")?.Value}.");
        }

        List<SearchResult> results = new();
        foreach (XElement item in document.Descendants("doc"))
        {
            if (!TryParseResult(item, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    private static bool TryParseResult(XElement item, int position, out SearchResult? result)
    {
        result = null;
        string title = ReadText(item.Element("title"));
        string url = ReadText(item.Element("url"));
        if (string.IsNullOrWhiteSpace(title) || !IsHttpUrl(url))
        {
            return false;
        }

        string snippet = ReadText(item.Element("headline"));
        if (snippet.Length == 0)
        {
            snippet = ReadPassages(item.Element("passages"));
        }

        result = new SearchResult(title, url, snippet, "yandex", 1.0 / (position + 1));
        return true;
    }

    private static string ReadPassages(XElement? passages)
    {
        if (passages is null)
        {
            return string.Empty;
        }

        IEnumerable<string> values = passages.Elements("passage").Select(ReadText);
        return string.Join(" ", values.Where(value => value.Length > 0));
    }

    private static string ReadText(XElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        HtmlDocument document = new();
        document.LoadHtml(element.Value);
        string decoded = HtmlEntity.DeEntitize(document.DocumentNode.InnerText);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
