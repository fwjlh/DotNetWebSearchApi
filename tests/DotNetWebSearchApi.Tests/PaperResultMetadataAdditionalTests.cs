using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class PaperResultMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsDataCiteCreatorsAndPublicationYear()
    {
        const string fixture = """
            {
              "data": [
                {
                  "id": "10.1234/datacite",
                  "attributes": {
                    "titles": [{ "title": "DataCite Research Record" }],
                    "url": "https://example.org/datacite",
                    "descriptions": [{ "description": "DataCite abstract.", "descriptionType": "Abstract" }],
                    "creators": [
                      { "name": "Ada Lovelace" },
                      { "name": "Grace Hopper" }
                    ],
                    "publisher": "Example Data Press",
                    "publicationYear": 2024,
                    "types": { "resourceTypeGeneral": "Dataset" },
                    "doi": "10.1234/datacite",
                    "subjects": [{ "subject": "Computer Science" }, { "subject": "Machine Learning" }]
                  }
                }
              ]
            }
            """;
        SearchResult result = await SearchAsync("datacite", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Ada Lovelace", paper.Author);
        Assert.Equal(new[] { "Ada Lovelace", "Grace Hopper" }, paper.Authors);
        Assert.Equal("Example Data Press", paper.Publisher);
        Assert.Equal("Dataset", paper.Type);
        Assert.Equal("2024", paper.PublicationYear);
        Assert.Equal("10.1234/datacite", paper.Doi);
        Assert.Equal("https://example.org/datacite", paper.HtmlUrl);
        Assert.Equal(new[] { "Computer Science", "Machine Learning" }, paper.Tags);
    }

    [Fact]
    public async Task SearchAsync_MapsZenodoJournalAndPdfMetadata()
    {
        const string fixture = """
            {
              "hits": {
                "hits": [
                  {
                    "id": "123",
                    "links": {
                      "html": "https://zenodo.org/records/123",
                      "pdf": "https://zenodo.org/records/123/files/paper.pdf"
                    },
                    "metadata": {
                      "title": "Zenodo Research Paper",
                      "description": "Zenodo abstract.",
                      "publication_date": "2026-11-20",
                      "creators": [{ "name": "Marie Curie" }],
                      "publisher": "Zenodo",
                      "journal": {
                        "title": "Zenodo Journal",
                        "volume": "8",
                        "issue": "2",
                        "pages": "12-18"
                      },
                      "doi": "10.9999/zenodo",
                      "resource_type": { "title": "Publication" },
                      "subjects": [{ "subject": "Physics" }, { "subject": "Data Science" }]
                    }
                  }
                ]
              }
            }
            """;
        SearchResult result = await SearchAsync("zenodo", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Marie Curie", paper.Author);
        Assert.Equal("Zenodo Journal", paper.Journal);
        Assert.Equal("8", paper.Volume);
        Assert.Equal("2", paper.Number);
        Assert.Equal("12-18", paper.Pages);
        Assert.Equal("10.9999/zenodo", paper.Doi);
        Assert.Equal("https://zenodo.org/records/123/files/paper.pdf", paper.PdfUrl);
        Assert.Equal(new[] { "Physics", "Data Science" }, paper.Tags);
        Assert.Equal("paper.html", MapSingle(result).Template);
    }

    [Fact]
    public async Task SearchAsync_MapsOpenAirePublicationAndAuthorMetadata()
    {
        const string fixture = """
            {
              "results": [
                {
                  "id": "doi:10.1234/openaire",
                  "mainTitle": "OpenAIRE Research Paper",
                  "descriptions": ["OpenAIRE abstract."],
                  "publicationDate": "2026-11-19",
                  "authors": [{ "fullName": "Katherine Johnson" }],
                  "publisher": "OpenAIRE Press",
                  "container": { "name": "OpenAIRE Journal" },
                  "type": "publication",
                  "instances": [{ "urls": ["https://example.org/openaire"] }]
                }
              ]
            }
            """;
        SearchResult result = await SearchAsync("openaire", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Katherine Johnson", paper.Author);
        Assert.Equal("OpenAIRE Press", paper.Publisher);
        Assert.Equal("OpenAIRE Journal", paper.Journal);
        Assert.Equal("publication", paper.Type);
        Assert.Equal(new DateTimeOffset(2026, 11, 19, 0, 0, 0, TimeSpan.Zero), paper.DateOfPublication);
        Assert.Equal("https://example.org/openaire", paper.HtmlUrl);
    }

    [Fact]
    public async Task SearchAsync_MapsPlosStringAuthorsAndArticleMetadata()
    {
        const string fixture = """
            {
              "response": {
                "docs": [
                  {
                    "id": "10.1371/journal.pone.1234567",
                    "title_display": "PLOS Research Paper",
                    "abstract": "<p>PLOS abstract.</p>",
                    "author_display": ["Ada Lovelace", "Grace Hopper"],
                    "publication_date": "2026-11-18",
                    "journal": "PLOS ONE",
                    "volume": "21",
                    "issue": "11",
                    "page": "e1234567",
                    "eissn": "1932-6203",
                    "article_type": "Research Article",
                    "subject": ["Computer Science", "Genetics"]
                  }
                ]
              }
            }
            """;
        SearchResult result = await SearchAsync("plos", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Ada Lovelace", paper.Author);
        Assert.Equal(new[] { "Ada Lovelace", "Grace Hopper" }, paper.Authors);
        Assert.Equal("PLOS ONE", paper.Journal);
        Assert.Equal("21", paper.Volume);
        Assert.Equal("11", paper.Number);
        Assert.Equal("e1234567", paper.Pages);
        Assert.Equal("1932-6203", Assert.Single(paper.Issn));
        Assert.Equal("10.1371/journal.pone.1234567", paper.Doi);
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero), paper.DateOfPublication);
        Assert.Equal(new[] { "Computer Science", "Genetics" }, paper.Tags);
    }
}
