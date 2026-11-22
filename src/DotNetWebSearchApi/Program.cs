using DotNetWebSearchApi.Search;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<BingSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
});
builder.Services.AddHttpClient<WikipediaSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
});
builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<BingSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(serviceProvider => serviceProvider.GetRequiredService<WikipediaSearchProvider>());
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
