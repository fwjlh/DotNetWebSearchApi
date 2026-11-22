using System.Net;
using DotNetWebSearchApi.Search;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<BingSearchProvider>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)"));
builder.Services.AddHttpClient<DuckDuckGoSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
    client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
    client.DefaultRequestHeaders.Referrer = new Uri("https://html.duckduckgo.com/");
});
builder.Services.AddHttpClient<GoogleSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
    client.DefaultRequestHeaders.Add("Cookie", "CONSENT=YES+");
});
builder.Services.AddHttpClient<NaverSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ko-KR,ko;q=0.9,en;q=0.8");
});
builder.Services.AddHttpClient<QwantSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
    client.DefaultRequestHeaders.Add("Origin", "https://www.qwant.com");
    client.DefaultRequestHeaders.Referrer = new Uri("https://www.qwant.com/");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
});
builder.Services.AddSingleton<CookieContainer>();
builder.Services.AddHttpClient<SeznamSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("cs-CZ,cs;q=0.9,en;q=0.8");
});
builder.Services.AddHttpClient<YahooSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
}).ConfigurePrimaryHttpMessageHandler(serviceProvider => new HttpClientHandler
{
    AllowAutoRedirect = false,
    UseCookies = true,
    CookieContainer = serviceProvider.GetRequiredService<CookieContainer>()
});
builder.Services.AddHttpClient<WikipediaSearchProvider>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)"));
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<BingSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<DuckDuckGoSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<GoogleSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<QwantSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<NaverSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<YahooSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<SeznamSearchProvider>());

string? yandexApiKey = builder.Configuration["YANDEX_SEARCH_API_KEY"];
string? yandexFolderId = builder.Configuration["YANDEX_FOLDER_ID"];
if (!string.IsNullOrWhiteSpace(yandexApiKey) && !string.IsNullOrWhiteSpace(yandexFolderId))
{
    builder.Services.AddSingleton(new YandexSearchOptions(yandexApiKey, yandexFolderId));
    builder.Services.AddHttpClient<YandexSearchProvider>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    });
    builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<YandexSearchProvider>());
}
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<WikipediaSearchProvider>());

string? braveApiKey = builder.Configuration["BRAVE_SEARCH_API_KEY"];
if (!string.IsNullOrWhiteSpace(braveApiKey))
{
    builder.Services.AddHttpClient<BraveSearchProvider>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Add("X-Subscription-Token", braveApiKey);
    });
    builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<BraveSearchProvider>());
}

builder.Services.AddSingleton<SearchDiagnostics>();
builder.Services.AddSingleton<SearchAggregator>();
WebApplication app = builder.Build();
app.UseStaticFiles();
app.MapGet("/health", HealthEndpoint);
app.MapGet("/api/providers", ProvidersEndpoint);
app.MapGet("/api/diagnostics", DiagnosticsEndpoint);
app.MapGet("/admin", AdminEndpoint);
app.MapGet("/api/search", SearchEndpoint);
app.MapGet("/search", SearxngSearchEndpoint);
app.MapGet("/", RootEndpoint);
app.MapPost("/search", SearxngSearchEndpoint);
app.Run();

static IResult HealthEndpoint() => Results.Ok(new { status = "ok" });

static IResult ProvidersEndpoint(SearchAggregator aggregator)
{
    string[] providers = aggregator.ProviderNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    return Results.Ok(new { provider_count = providers.Length, providers });
}

static IResult DiagnosticsEndpoint(SearchAggregator aggregator, SearchDiagnostics diagnostics)
{
    IReadOnlyList<ProviderDiagnosticsSnapshot> providers = diagnostics.GetSnapshot(aggregator.ProviderNames);
    string status = providers.Any(provider => provider.Status == "unresponsive") ? "degraded" : "ok";
    return Results.Ok(new
    {
        status,
        active_provider_count = providers.Count,
        providers,
        features = new
        {
            search_ui = true,
            provider_configuration = false,
            sql_configuration = false,
            api_authentication = false
        }
    });
}

static IResult AdminEndpoint() => Results.Redirect("/admin.html");

static Task<IResult> RootEndpoint(HttpRequest request, SearchAggregator aggregator, CancellationToken cancellationToken)
{
    if (!request.Query.ContainsKey("q")) return Task.FromResult<IResult>(Results.Redirect("/index.html"));
    return SearxngSearchEndpoint(request, aggregator, cancellationToken);
}

static async Task<IResult> SearchEndpoint(string? q, int? limit, SearchAggregator aggregator, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(q)) return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    int resultLimit = limit ?? 10;
    if (resultLimit is < 1 or > 50) return Results.BadRequest(new { error = "Limit must be between 1 and 50." });
    IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(new SearchQuery(q.Trim(), resultLimit), cancellationToken);
    return Results.Ok(new SearchResponse(q.Trim(), results));
}

static async Task<IResult> SearxngSearchEndpoint(HttpRequest request, SearchAggregator aggregator, CancellationToken cancellationToken)
{
    IFormCollection? form = null;
    if (HttpMethods.IsPost(request.Method))
    {
        if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        form = await request.ReadFormAsync(cancellationToken);
    }

    string? queryText = ReadParameter(request, form, "q");
    if (string.IsNullOrWhiteSpace(queryText)) return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    string? format = ReadParameter(request, form, "format");
    if (!string.IsNullOrWhiteSpace(format) && !format.Equals("json", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(StatusCodes.Status406NotAcceptable);

    string? pageValue = ReadParameter(request, form, "pageno");
    if (!string.IsNullOrWhiteSpace(pageValue) && (!int.TryParse(pageValue, out int pageNumber) || pageNumber != 1))
        return Results.StatusCode(StatusCodes.Status501NotImplemented);

    string? categoryValue = ReadParameter(request, form, "categories");
    if (!string.IsNullOrWhiteSpace(categoryValue)
        && categoryValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(category => !category.Equals("general", StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest(new { error = "Only the general category is currently supported." });

    if (!string.IsNullOrWhiteSpace(ReadParameter(request, form, "time_range")))
        return Results.StatusCode(StatusCodes.Status501NotImplemented);

    string? language = ReadParameter(request, form, "language");
    if (!string.IsNullOrWhiteSpace(language) && !language.Equals("auto", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(StatusCodes.Status501NotImplemented);

    string? safeSearchValue = ReadParameter(request, form, "safesearch");
    if (!string.IsNullOrWhiteSpace(safeSearchValue)
        && (!int.TryParse(safeSearchValue, out int safeSearch) || safeSearch != 0))
        return Results.StatusCode(StatusCodes.Status501NotImplemented);

    IReadOnlySet<string>? selectedProviders = ReadSelectedProviders(
        ReadParameter(request, form, "engines"),
        aggregator.ProviderNames,
        out string[] unknownProviders);
    if (unknownProviders.Length > 0)
    {
        return Results.BadRequest(new
        {
            error = "One or more requested engines are not available.",
            unknown_engines = unknownProviders,
            available_engines = aggregator.ProviderNames
        });
    }

    SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
        new SearchQuery(queryText.Trim(), 10, selectedProviders),
        cancellationToken);
    return Results.Ok(SearxngSearchResponseMapper.Map(queryText.Trim(), execution));
}

static string? ReadParameter(HttpRequest request, IFormCollection? form, string name)
{
    string? formValue = form is null ? null : form[name].FirstOrDefault();
    return string.IsNullOrWhiteSpace(formValue) ? request.Query[name].FirstOrDefault() : formValue;
}

static IReadOnlySet<string>? ReadSelectedProviders(
    string? providerValue,
    IReadOnlyList<string> availableProviders,
    out string[] unknownProviders)
{
    unknownProviders = Array.Empty<string>();
    if (string.IsNullOrWhiteSpace(providerValue)) return null;
    HashSet<string> requested = new(
        providerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase);
    HashSet<string> available = new(availableProviders, StringComparer.OrdinalIgnoreCase);
    unknownProviders = requested.Where(provider => !available.Contains(provider)).ToArray();
    return requested;
}

public sealed record SearchResponse(string Query, IReadOnlyList<SearchResult> Results);
public partial class Program;
