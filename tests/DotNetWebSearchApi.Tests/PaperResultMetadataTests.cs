using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class PaperResultMetadataTests
{
    [Fact]
    public void Map_UsesBasePublishedDateWhenPaperDateIsMissing()
    {
        DateTimeOffset publishedDate = new(2026, 11, 23, 9, 15, 0, TimeSpan.Zero);
        SearchResult result = new(
            "Paper with base date",
            "https://example.org/paper",
            "Abstract",
            "fixture",
            1.0)
        {
            Category = "science",
            PublishedDate = publishedDate,
            PaperMetadata = new PaperResultMetadata
            {
                Author = "Example Author",
                Authors = new[] { "Example Author" }
            }
        };

        SearxngSearchResult mapped = MapSingle(result);

        Assert.Equal("paper.html", mapped.Template);
        Assert.Equal(publishedDate, mapped.PaperDateOfPublication);
    }

    [Fact]
    public async Task SearchAsync_MapsCrossrefAuthorsAndJournalDetails()
    {
        const string fixture = """
            {
              "message": {
                "items": [
                  {
                    "title": ["A Sample Paper"],
                    "URL": "https://doi.org/10.1234/sample",
                    "abstract": "<jats:p>Sample abstract.</jats:p>",
                    "author": [
                      { "given": "Ada", "family": "Lovelace" },
                      { "given": "Grace", "family": "Hopper" }
                    ],
                    "publisher": "Example Press",
                    "container-title": ["Journal of Examples"],
                    "volume": "12",
                    "page": "1-9",
                    "issue": "2",
                    "DOI": "10.1234/sample",
                    "ISSN": ["1234-5678"],
                    "type": "journal-article"
                  }
                ]
              }
            }
            """;
        SearchResult result = await SearchAsync("crossref", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);
        SearxngSearchResult mapped = MapSingle(result);

        Assert.Equal("science", result.Category);
        Assert.Equal("Ada Lovelace", paper.Author);
        Assert.Equal("Sample abstract.", paper.Content);
        Assert.Equal(new[] { "Ada Lovelace", "Grace Hopper" }, paper.Authors);
        Assert.Equal("Journal of Examples", paper.Journal);
        Assert.Equal("10.1234/sample", paper.Doi);
        Assert.Equal("paper.html", mapped.Template);
        Assert.Equal("Ada Lovelace", mapped.Author);
        Assert.Equal("Journal of Examples", mapped.PaperJournal);
    }

    [Fact]
    public async Task SearchAsync_MapsOpenAlexPublicationAndPdfMetadata()
    {
        const string fixture = """
            {
              "results": [
                {
                  "title": "OpenAlex Paper",
                  "id": "https://openalex.org/W123",
                  "publication_date": "2026-11-23",
                  "authorships": [
                    { "author": { "display_name": "Katherine Johnson" } }
                  ],
                  "primary_location": {
                    "landing_page_url": "https://journal.example.org/paper",
                    "source": {
                      "display_name": "Journal of Flight",
                      "host_organization_name": "Example University"
                    }
                  },
                  "best_oa_location": {
                    "pdf_url": "https://files.example.org/paper.pdf"
                  },
                  "biblio": { "volume": "4", "issue": "1", "first_page": "22" },
                  "doi": "https://doi.org/10.5678/openalex",
                  "type": "article"
                }
              ]
            }
            """;
        SearchResult result = await SearchAsync("openalex", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Katherine Johnson", paper.Author);
        Assert.Equal("Journal of Flight", paper.Journal);
        Assert.Equal("Example University", paper.Publisher);
        Assert.Equal("10.5678/openalex", paper.Doi);
        Assert.Equal("https://files.example.org/paper.pdf", paper.PdfUrl);
        Assert.Equal(new DateTimeOffset(2026, 11, 23, 0, 0, 0, TimeSpan.Zero), paper.DateOfPublication);
    }

    [Fact]
    public async Task SearchAsync_MapsEuropePmcPublicationAndJournalMetadata()
    {
        const string fixture = """
            {
              "resultList": {
                "result": [
                  {
                    "title": "Europe PMC Paper",
                    "id": "123456",
                    "authorString": "Marie Curie, Irene Joliot-Curie",
                    "firstPublicationDate": "2026-11-20",
                    "journalTitle": "European Research",
                    "journalVolume": "8",
                    "pageInfo": "55-62",
                    "doi": "10.9012/epmc",
                    "pubType": "research-article",
                    "fullTextUrlList": {
                      "fullTextUrl": [
                        { "url": "https://europepmc.org/articles/PMC123456" }
                      ]
                    }
                  }
                ]
              }
            }
            """;
        SearchResult result = await SearchAsync("europepmc", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal("Marie Curie, Irene Joliot-Curie", paper.Author);
        Assert.Equal("European Research", paper.Journal);
        Assert.Equal("55-62", paper.Pages);
        Assert.Equal("10.9012/epmc", paper.Doi);
        Assert.Equal("https://europepmc.org/articles/PMC123456", paper.HtmlUrl);
        Assert.Equal("paper.html", MapSingle(result).Template);
    }

    [Fact]
    public async Task SearchAsync_MapsSemanticScholarAuthorsAndOpenAccessPdf()
    {
        const string fixture = """
            {
              "data": [
                {
                  "title": "Semantic Scholar Paper",
                  "url": "https://www.semanticscholar.org/paper/example",
                  "abstract": "A concise abstract.",
                  "publicationDate": "2026-11-18",
                  "venue": "Computational Science",
                  "authors": [
                    { "name": "Grace Hopper" },
                    { "name": "Edsger Dijkstra" }
                  ],
                  "externalIds": { "DOI": "10.3456/semantic" },
                  "openAccessPdf": { "url": "https://pdf.example.org/semantic.pdf" },
                  "publicationTypes": ["JournalArticle"]
                }
              ]
            }
            """;
        SearchResult result = await SearchAsync("semanticscholar", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal(new[] { "Grace Hopper", "Edsger Dijkstra" }, paper.Authors);
        Assert.Equal("Computational Science", paper.Journal);
        Assert.Equal("10.3456/semantic", paper.Doi);
        Assert.Equal("https://pdf.example.org/semantic.pdf", paper.PdfUrl);
        Assert.Equal("JournalArticle", paper.Type);
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero), result.PublishedDate);
    }

    [Fact]
    public async Task SearchAsync_RejectsUnsafePaperPdfUrls()
    {
        const string fixture = """
            {
              "data": [
                {
                  "title": "Unsafe PDF",
                  "url": "https://www.semanticscholar.org/paper/example",
                  "authors": [{ "name": "Example Author" }],
                  "openAccessPdf": { "url": "javascript:alert(1)" }
                }
              ]
            }
            """;
        SearchResult result = await SearchAsync("semanticscholar", fixture);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);

        Assert.Equal(string.Empty, paper.PdfUrl);
    }

    private static async Task<SearchResult> SearchAsync(string providerName, string fixture)
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            }));
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == providerName);
        JsonApiSearchProvider provider = new(client, definition);

        return Assert.Single(await provider.SearchAsync(
            new SearchQuery("sample paper", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" }
            },
            CancellationToken.None));
    }

    private static SearxngSearchResult MapSingle(SearchResult result)
    {
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            result.Title,
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        return Assert.Single(response.Results);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
