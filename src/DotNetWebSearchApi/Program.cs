using DotNetWebSearchApi.Search;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<BingSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
});
builder.Services.AddHttpClient<GoogleSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
    client.DefaultRequestHeaders.Add("Cookie", "CONSENT=YES+");
});
builder.Services.AddHttpClient<WikipediaSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
});
builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<BingSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<GoogleSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<WikipediaSearchProvider>());

string? braveApiKey = builder.Configuration["BRAVE_SEARCH_API_KEY"];
if (!string.IsNullOrWhiteSpace(braveApiKey))
{
    builder.Services.AddHttpClient<BraveSearchProvider>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Add("X-Subscription-Token", braveApiKey);
    });
    builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<BraveSearchProvider>());
}

builder.Services.AddSingleton<SearchAggregator>();

WebApplication app = builder.Build();
app.MapGet("/health", HealthEndpoint);
app.MapGet("/api/search", SearchEndpoint);
app.Run();

static IResult HealthEndpoint()
{
    return Results.Ok(new { status = "ok" });
}

static async Task<IResult> SearchEndpoint(
    string? q,
    int? limit,
    SearchAggregator aggregator,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    }

    int resultLimit = limit ?? 10;
    if (resultLimit is < 1 or > 50)
    {
        return Results.BadRequest(new { error = "Limit must be between 1 and 50." });
    }

    IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
        new SearchQuery(q.Trim(), resultLimit),
        cancellationToken);

    return Results.Ok(new SearchResponse(q.Trim(), results));
}

public sealed record SearchResponse(string Query, IReadOnlyList<SearchResult> Results);

public partial class Program;
