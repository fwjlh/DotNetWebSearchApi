using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class JsonApiSearchProviderTests
{
    [Theory]
    [MemberData(nameof(ProviderCases))]
    public async Task SearchAsync_UsesProviderEndpointAndMapsItsJson(
        JsonApiSearchDefinition definition,
        string fixture,
        string expectedTitle,
        string expectedUrl)
    {
        Uri? requestUri = null;
        string? requestBody = null;
        HttpMethod? requestMethod = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            requestMethod = request.Method;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            };
        }));
        JsonApiSearchProvider provider = new(client, definition);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("C# & search", 12),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal(definition.Name, result.Provider);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedUrl, result.Url);
        if (string.Equals(definition.RequestMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(HttpMethod.Post, requestMethod);
            using JsonDocument body = JsonDocument.Parse(requestBody ?? "{}");
            Assert.Equal("C# & search", body.RootElement.GetProperty("query").GetString());
            Assert.True(body.RootElement.TryGetProperty(definition.BodyLimitParameter, out _));
        }
        else
        {
            Assert.Equal(HttpMethod.Get, requestMethod);
            Assert.Contains("C%23%20%26%20search", requestUri!.Query);
            if (!string.IsNullOrWhiteSpace(definition.LimitParameter))
            {
                Assert.Contains(definition.LimitParameter + "=", Uri.UnescapeDataString(requestUri.Query));
            }
        }
        if (definition.Name == "wiby")
        {
            Assert.Contains("https://wiby.me", result.Snippet);
        }
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForUnexpectedJsonShape()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"unexpected\":true}")
            }));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("missing", 5),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RejectsHttpErrors()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.SearchAsync(new SearchQuery("rate limited", 5), CancellationToken.None));
    }

    public static IEnumerable<object[]> ProviderCases()
    {
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "crossref"),
            "{\"message\":{\"items\":[{\"title\":[\"Research paper\"],\"URL\":\"https://doi.org/10.1000/example\",\"abstract\":\"<p>Research abstract</p>\"}]}}",
            "Research paper",
            "https://doi.org/10.1000/example"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openalex"),
            "{\"results\":[{\"title\":\"Open scholarly work\",\"id\":\"https://openalex.org/W123\"}]}",
            "Open scholarly work",
            "https://openalex.org/W123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "hackernews"),
            "{\"hits\":[{\"title\":\"HN story\",\"url\":\"https://example.com/hn\",\"story_text\":\"A story\"}]}",
            "HN story",
            "https://example.com/hn"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "stackoverflow"),
            "{\"items\":[{\"title\":\"C# answer\",\"link\":\"https://stackoverflow.com/questions/1\",\"body\":\"<p>Example</p>\"}]}",
            "C# answer",
            "https://stackoverflow.com/questions/1"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "npm"),
            "{\"objects\":[{\"package\":{\"name\":\"example-package\",\"links\":{\"npm\":\"https://www.npmjs.com/package/example-package\"},\"description\":\"A package\"}}]}",
            "example-package",
            "https://www.npmjs.com/package/example-package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "cratesio"),
            "{\"crates\":[{\"name\":\"example-crate\",\"id\":\"example-crate\",\"description\":\"A crate\"}]}",
            "example-crate",
            "https://crates.io/crates/example-crate"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "nuget"),
            "{\"data\":[{\"id\":\"Example.Package\",\"@id\":\"https://www.nuget.org/packages/Example.Package\",\"description\":\"A package\"}]}",
            "Example.Package",
            "https://www.nuget.org/packages/Example.Package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openlibrary"),
            "{\"docs\":[{\"title\":\"Example book\",\"key\":\"/works/OL123W\"}]}",
            "Example book",
            "https://openlibrary.org/works/OL123W"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "europepmc"),
            "{\"resultList\":{\"result\":[{\"title\":\"Medical paper\",\"id\":\"12345\",\"authorString\":\"A. Author\"}]}}",
            "Medical paper",
            "https://europepmc.org/article/MED/12345"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "semanticscholar"),
            "{\"data\":[{\"title\":\"Semantic paper\",\"url\":\"https://www.semanticscholar.org/paper/example\",\"abstract\":\"A paper abstract\"}]}",
            "Semantic paper",
            "https://www.semanticscholar.org/paper/example"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "github"),
            "{\"items\":[{\"full_name\":\"owner/project\",\"html_url\":\"https://github.com/owner/project\",\"description\":\"A repository\"}]}",
            "owner/project",
            "https://github.com/owner/project"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "dockerhub"),
            "{\"results\":[{\"repo_name\":\"library/nginx\",\"short_description\":\"Web server\"}]}",
            "library/nginx",
            "https://hub.docker.com/r/library/nginx"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "googlebooks"),
            "{\"items\":[{\"volumeInfo\":{\"title\":\"Book title\",\"infoLink\":\"https://books.google.com/books?id=123\",\"description\":\"Book description\"}}]}",
            "Book title",
            "https://books.google.com/books?id=123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "internetarchive"),
            "{\"response\":{\"docs\":[{\"title\":\"Archive item\",\"identifier\":\"archive-item\",\"description\":\"An archived record\"}]}}",
            "Archive item",
            "https://archive.org/details/archive-item"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "rubygems"),
            "[{\"name\":\"example-gem\",\"project_uri\":\"https://rubygems.org/gems/example-gem\",\"info\":\"A gem\"}]",
            "example-gem",
            "https://rubygems.org/gems/example-gem"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "huggingface"),
            "[{\"id\":\"org/model\",\"pipeline_tag\":\"text-generation\"}]",
            "org/model",
            "https://huggingface.co/org/model"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "superuser"),
            "{\"items\":[{\"title\":\"Linux desktop help\",\"link\":\"https://superuser.com/questions/1\",\"body\":\"<p>Question</p>\"}]}",
            "Linux desktop help",
            "https://superuser.com/questions/1"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "serverfault"),
            "{\"items\":[{\"title\":\"Server configuration\",\"link\":\"https://serverfault.com/questions/1\",\"body\":\"<p>Admin question</p>\"}]}",
            "Server configuration",
            "https://serverfault.com/questions/1"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "askubuntu"),
            "{\"items\":[{\"title\":\"Ubuntu networking\",\"link\":\"https://askubuntu.com/questions/1\",\"body\":\"<p>Ubuntu question</p>\"}]}",
            "Ubuntu networking",
            "https://askubuntu.com/questions/1"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "gitlab"),
            "[{\"name_with_namespace\":\"group/project\",\"web_url\":\"https://gitlab.com/group/project\",\"description\":\"A GitLab project\"}]",
            "group/project",
            "https://gitlab.com/group/project"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "gitea"),
            "{\"data\":[{\"full_name\":\"owner/repository\",\"html_url\":\"https://gitea.com/owner/repository\",\"description\":\"A Gitea repository\"}]}",
            "owner/repository",
            "https://gitea.com/owner/repository"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "mavencentral"),
            "{\"response\":{\"docs\":[{\"id\":\"org.example:library\",\"latestVersion\":\"1.2.3\"}]}}",
            "org.example:library",
            "https://central.sonatype.com/artifact/org.example%3Alibrary"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "packagist"),
            "{\"results\":[{\"name\":\"vendor/package\",\"url\":\"https://packagist.org/packages/vendor/package\",\"description\":\"A PHP package\"}]}",
            "vendor/package",
            "https://packagist.org/packages/vendor/package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "pubdev"),
            "{\"packages\":[{\"package\":\"example_package\"}]}",
            "example_package",
            "https://pub.dev/packages/example_package"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "bilibili"),
            "{\"data\":{\"result\":[{\"title\":\"Video title\",\"arcurl\":\"https://www.bilibili.com/video/BV123\",\"description\":\"A video\"}]}}",
            "Video title",
            "https://www.bilibili.com/video/BV123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "dailymotion"),
            "{\"list\":[{\"title\":\"Dailymotion clip\",\"url\":\"https://www.dailymotion.com/video/x123\",\"description\":\"A clip\"}]}",
            "Dailymotion clip",
            "https://www.dailymotion.com/video/x123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "deezer"),
            "{\"data\":[{\"title\":\"Track title\",\"link\":\"https://www.deezer.com/track/123\",\"artist\":{\"name\":\"Artist\"}}]}",
            "Track title",
            "https://www.deezer.com/track/123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "steam"),
            "{\"items\":[{\"name\":\"Example game\",\"id\":12345}]}",
            "Example game",
            "https://store.steampowered.com/app/12345"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "musicbrainz"),
            "{\"artists\":[{\"name\":\"Example artist\",\"id\":\"1234-abcd\",\"disambiguation\":\"A musician\"}]}",
            "Example artist",
            "https://musicbrainz.org/artist/1234-abcd"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikidata"),
            "{\"query\":{\"search\":[{\"title\":\"Q42\"}]}}",
            "Q42",
            "https://www.wikidata.org/wiki/Q42"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikipedia_de"),
            "{\"query\":{\"search\":[{\"title\":\"Beispiel\"}]}}",
            "Beispiel",
            "https://de.wikipedia.org/wiki/Beispiel"
        };

        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wiby"),
            "[{\"URL\":\"https://example.org/wiby\",\"Title\":\"Wiby result\",\"Snippet\":\"A small web result\"}]",
            "Wiby result",
            "https://example.org/wiby"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "tvmaze"),
            "[{\"show\":{\"name\":\"Example Show\",\"url\":\"https://www.tvmaze.com/shows/1/example\",\"summary\":\"<p>A television show</p>\"}}]",
            "Example Show",
            "https://www.tvmaze.com/shows/1/example"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "radiobrowser"),
            "[{\"name\":\"Example Radio\",\"url\":\"https://stream.example.org/live\",\"tags\":\"jazz,pop\"}]",
            "Example Radio",
            "https://stream.example.org/live"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "peertube"),
            "{\"data\":[{\"name\":\"PeerTube video\",\"url\":\"https://peertube.cpy.re/videos/watch/123\",\"description\":\"A federated video\"}]}",
            "PeerTube video",
            "https://peertube.cpy.re/videos/watch/123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikimedia_commons"),
            "{\"query\":{\"search\":[{\"title\":\"File:Example.jpg\",\"snippet\":\"An image\"}]}}",
            "File:Example.jpg",
            "https://commons.wikimedia.org/wiki/File%3AExample.jpg"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikinews"),
            "{\"query\":{\"search\":[{\"title\":\"Example news\",\"snippet\":\"A news report\"}]}}",
            "Example news",
            "https://en.wikinews.org/wiki/Example%20news"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikipedia_es"),
            "{\"query\":{\"search\":[{\"title\":\"Ejemplo\",\"snippet\":\"Una página\"}]}}",
            "Ejemplo",
            "https://es.wikipedia.org/wiki/Ejemplo"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openverse"),
            "{\"results\":[{\"title\":\"Openly licensed image\",\"foreign_landing_url\":\"https://example.org/media/1\",\"attribution\":\"CC BY 4.0 by Example\"}]}",
            "Openly licensed image",
            "https://example.org/media/1"
        };

        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "mwmbl"),
            "{\"results\":[{\"title\":\"Mwmbl result\",\"url\":\"https://example.org/mwmbl\",\"content\":\"An independent web search result\"}]}",
            "Mwmbl result",
            "https://example.org/mwmbl"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "marginalia"),
            "{\"results\":[{\"title\":\"Small web result\",\"url\":\"https://example.org/small-web\",\"description\":\"An independent index\"}]}",
            "Small web result",
            "https://example.org/small-web"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "yep"),
            "[{}, {\"results\":[{\"title\":\"Yep result\",\"url\":\"https://example.org/yep\",\"snippet\":\"Search result\"}]}]",
            "Yep result",
            "https://example.org/yep"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "kagi"),
            "{\"data\":{\"search\":[{\"title\":\"Kagi result\",\"url\":\"https://example.org/kagi\",\"snippet\":\"Premium search result\"}]}}",
            "Kagi result",
            "https://example.org/kagi"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "exa"),
            "{\"results\":[{\"title\":\"Exa result\",\"url\":\"https://example.org/exa\",\"highlights\":[\"A search highlight\"]}]}",
            "Exa result",
            "https://example.org/exa"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wiktionary"),
            "{\"query\":{\"search\":[{\"title\":\"search\",\"snippet\":\"An English word\"}]}}",
            "search",
            "https://en.wiktionary.org/wiki/search"
        };

        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "datacite"),
            "{\"data\":[{\"attributes\":{\"titles\":[{\"title\":\"DataCite record\"}],\"url\":\"https://example.org/dataset\",\"descriptions\":[{\"description\":\"Research data metadata\"}]}}]}",
            "DataCite record",
            "https://example.org/dataset"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "zenodo"),
            "{\"hits\":{\"hits\":[{\"metadata\":{\"title\":\"Zenodo record\",\"description\":\"A published research output\"},\"links\":{\"html\":\"https://zenodo.org/records/123\"}}]}}",
            "Zenodo record",
            "https://zenodo.org/records/123"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openaire"),
            "{\"results\":[{\"id\":\"dedup_wf_001\",\"mainTitle\":\"OpenAIRE record\",\"descriptions\":[\"Open research metadata\"]}]}",
            "OpenAIRE record",
            "https://explore.openaire.eu/search/publication?pid=dedup_wf_001"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "plos"),
            "{\"response\":{\"docs\":[{\"id\":\"10.1371/journal.pone.0000001\",\"title_display\":\"PLOS article\",\"abstract\":\"Open access abstract\"}]}}",
            "PLOS article",
            "https://doi.org/10.1371/journal.pone.0000001"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikisource"),
            "{\"query\":{\"search\":[{\"title\":\"Example text\",\"snippet\":\"A source text\"}]}}",
            "Example text",
            "https://en.wikisource.org/wiki/Example%20text"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikibooks"),
            "{\"query\":{\"search\":[{\"title\":\"Example book\",\"snippet\":\"A learning book\"}]}}",
            "Example book",
            "https://en.wikibooks.org/wiki/Example%20book"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikiversity"),
            "{\"query\":{\"search\":[{\"title\":\"Example course\",\"snippet\":\"An open course\"}]}}",
            "Example course",
            "https://en.wikiversity.org/wiki/Example%20course"
        };
        yield return new object[]
        {
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikivoyage"),
            "{\"query\":{\"search\":[{\"title\":\"Example destination\",\"snippet\":\"A travel guide\"}]}}",
            "Example destination",
            "https://en.wikivoyage.org/wiki/Example%20destination"
        };
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
