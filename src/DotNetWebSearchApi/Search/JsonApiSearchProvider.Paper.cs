using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed partial class JsonApiSearchProvider
{
    private PaperResultMetadata? ReadPaperMetadata(JsonElement item)
    {
        JsonApiPaperMetadataPaths? paths = _definition.PaperMetadata;
        if (paths is null)
        {
            return null;
        }

        DateTimeOffset? date = ReadOptionalDate(item, paths.PublishedDate)
            ?? ReadOptionalDate(item, _definition.PublishedDatePath);
        IReadOnlyList<string> authors = ReadPaperAuthors(item, paths);
        string author = ReadOptionalText(item, paths.Author);
        if (author.Length == 0 && authors.Count > 0)
        {
            author = authors[0];
        }

        string content = CleanText(ReadOptionalText(item, paths.Content));
        string comments = CleanText(ReadOptionalText(item, paths.Comments));
        IReadOnlyList<string> tags = ReadTextArray(item, paths.Tags);
        string type = ReadOptionalText(item, paths.Type);
        string editor = ReadOptionalText(item, paths.Editor);
        string publisher = ReadOptionalText(item, paths.Publisher);
        string journal = ReadOptionalText(item, paths.Journal);
        string volume = ReadOptionalText(item, paths.Volume);
        string pages = ReadOptionalText(item, paths.Pages);
        string number = ReadOptionalText(item, paths.Number);
        string doi = ReadOptionalText(item, paths.Doi);
        IReadOnlyList<string> issn = ReadTextArray(item, paths.Issn);
        IReadOnlyList<string> isbn = ReadTextArray(item, paths.Isbn);
        string pdfUrl = ReadSafeUrl(item, paths.PdfUrl);
        string htmlUrl = ReadSafeUrl(item, paths.HtmlUrl);

        if (date is null && author.Length == 0 && authors.Count == 0 && content.Length == 0
            && comments.Length == 0 && tags.Count == 0 && type.Length == 0
            && editor.Length == 0 && publisher.Length == 0 && journal.Length == 0
            && volume.Length == 0 && pages.Length == 0 && number.Length == 0
            && doi.Length == 0 && issn.Count == 0 && isbn.Count == 0
            && pdfUrl.Length == 0 && htmlUrl.Length == 0)
        {
            return null;
        }

        return new PaperResultMetadata
        {
            DateOfPublication = date,
            Content = content,
            Comments = comments,
            Tags = tags,
            Type = type,
            Author = author,
            Authors = authors,
            Editor = editor,
            Publisher = publisher,
            Journal = journal,
            Volume = volume,
            Pages = pages,
            Number = number,
            Doi = doi,
            Issn = issn,
            Isbn = isbn,
            PdfUrl = pdfUrl,
            HtmlUrl = htmlUrl
        };
    }

    private static IReadOnlyList<string> ReadPaperAuthors(
        JsonElement item,
        JsonApiPaperMetadataPaths paths)
    {
        if (string.IsNullOrWhiteSpace(paths.Authors))
        {
            return Array.Empty<string>();
        }

        if (!TryReadPath(item, paths.Authors, out JsonElement authorsElement)
            || authorsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        List<string> authors = new();
        foreach (JsonElement authorElement in authorsElement.EnumerateArray())
        {
            string name = ReadOptionalText(authorElement, paths.AuthorName);
            if (name.Length == 0)
            {
                string given = ReadOptionalText(authorElement, paths.AuthorGiven);
                string family = ReadOptionalText(authorElement, paths.AuthorFamily);
                name = string.Join(" ", new[] { given, family }.Where(part => part.Length > 0));
            }

            if (name.Length > 0)
            {
                authors.Add(name);
            }
        }

        return authors;
    }

    private static IReadOnlyList<string> ReadTextArray(JsonElement item, string path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || !TryReadPath(item, path, out JsonElement array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return array.EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.ToString())
            .Where(value => value.Length > 0)
            .ToArray();
    }
}

public sealed record JsonApiPaperMetadataPaths
{
    public string PublishedDate { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Comments { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorGiven { get; init; } = string.Empty;
    public string AuthorFamily { get; init; } = string.Empty;
    public string Editor { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public string Journal { get; init; } = string.Empty;
    public string Volume { get; init; } = string.Empty;
    public string Pages { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Doi { get; init; } = string.Empty;
    public string Issn { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public string PdfUrl { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
}
