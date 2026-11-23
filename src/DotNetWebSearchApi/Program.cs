using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using DotNetWebSearchApi.Search;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("search", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});
builder.Services.Configure<FormOptions>(options =>
{
    options.BufferBodyLengthLimit = 16 * 1024;
    options.KeyLengthLimit = 128;
    options.ValueCountLimit = 32;
    options.ValueLengthLimit = 4096;
    options.MultipartBodyLengthLimit = 16 * 1024;
});
JsonApiProviderCatalog.Register(builder.Services, builder.Configuration);
RssProviderCatalog.Register(builder.Services);
builder.Services.AddHttpClient<MojeekSearchProvider>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; DotNetWebSearchApi/0.1; +https://github.com/fwjlh/DotNetWebSearchApi)");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
});
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<MojeekSearchProvider>());
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
builder.Services.AddKeyedSingleton<CookieContainer>("mojeek", (_, _) => new CookieContainer());
builder.Services.AddKeyedSingleton<CookieContainer>("yahoo", (_, _) => new CookieContainer());
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
    CookieContainer = serviceProvider.GetRequiredKeyedService<CookieContainer>("yahoo")
});
builder.Services.AddHttpClient<WikipediaSearchProvider>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)"));
builder.Services.AddHttpClient<ArxivSearchProvider>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)"));
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
string? kagiApiKey = builder.Configuration["KAGI_API_KEY"];
if (!string.IsNullOrWhiteSpace(kagiApiKey))
{
    builder.Services.AddSingleton(new KagiSearchOptions(kagiApiKey));
    builder.Services.AddHttpClient<KagiSearchProvider>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    });
    builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<KagiSearchProvider>());
}

builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<WikipediaSearchProvider>());
builder.Services.AddSingleton<ISearchProvider>(sp => sp.GetRequiredService<ArxivSearchProvider>());

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
builder.Services.AddSingleton<SearchHttpTransport>();
builder.Services.AddSingleton<ProviderHealthRegistry>();
builder.Services.AddSingleton<SearchAggregator>();
WebApplication app = builder.Build();
app.UseRateLimiter();
app.UseStaticFiles();
app.MapGet("/health", HealthEndpoint);
app.MapGet("/api/providers", ProvidersEndpoint);
app.MapGet("/api/diagnostics", DiagnosticsEndpoint);
app.MapGet("/admin", AdminEndpoint);
app.MapGet("/api/search", SearchEndpoint).RequireRateLimiting("search");
app.MapGet("/search", SearxngSearchEndpoint).RequireRateLimiting("search");
app.MapGet("/", RootEndpoint).RequireRateLimiting("search");
app.MapPost("/search", SearxngSearchEndpoint).RequireRateLimiting("search");
app.Run();

static IResult HealthEndpoint() => Results.Ok(new { status = "ok" });

static IResult ProvidersEndpoint(SearchAggregator aggregator)
{
    string[] providers = aggregator.ProviderNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    return Results.Ok(new { provider_count = providers.Length, providers });
}

static IResult DiagnosticsEndpoint(
    SearchAggregator aggregator,
    SearchDiagnostics diagnostics,
    ProviderHealthRegistry health)
{
    IReadOnlyList<ProviderDiagnosticsSnapshot> providers = diagnostics.GetSnapshot(aggregator.ProviderNames);
    IReadOnlyList<ProviderHealthSnapshot> providerHealth = health.GetSnapshot(aggregator.ProviderNames);
    string status = providers.Any(provider => provider.Status == "unresponsive") ? "degraded" : "ok";
    return Results.Ok(new
    {
        status,
        active_provider_count = providers.Count,
        providers,
        provider_health = providerHealth,
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

static async Task<IResult> SearchEndpoint(HttpRequest request, SearchAggregator aggregator, CancellationToken cancellationToken)
{
    string? queryText = ReadParameter(request, null, "q");
    if (string.IsNullOrWhiteSpace(queryText)) return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    string? limitValue = ReadParameter(request, null, "limit");
    if (!string.IsNullOrWhiteSpace(limitValue) && (!int.TryParse(limitValue, out int parsedLimit) || parsedLimit is < 1 or > 50))
        return Results.BadRequest(new { error = "Limit must be between 1 and 50." });
    int resultLimit = string.IsNullOrWhiteSpace(limitValue) ? 10 : int.Parse(limitValue);
    if (!TryBuildSearchQuery(request, null, queryText.Trim(), resultLimit, null, out SearchQuery query, out string? error))
        return Results.BadRequest(new { error });
    SearchExecutionResult execution = await aggregator.SearchDetailedAsync(query, cancellationToken);
    return Results.Ok(new SearchResponse(
        query.Text,
        execution.Results,
        execution.UnsupportedProviders,
        execution.CoolingDownProviders)
    {
        ProviderOutcomes = execution.ProviderOutcomes
    });
}

static async Task<IResult> SearxngSearchEndpoint(HttpRequest request, SearchAggregator aggregator, CancellationToken cancellationToken)
{
    IFormCollection? form = null;
    if (HttpMethods.IsPost(request.Method))
    {
        if (!request.HasFormContentType) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        if (request.ContentLength > 16 * 1024) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        form = await request.ReadFormAsync(cancellationToken);
    }

    string? queryText = ReadParameter(request, form, "q");
    if (string.IsNullOrWhiteSpace(queryText)) return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    string? format = ReadParameter(request, form, "format");
    if (!string.IsNullOrWhiteSpace(format) && !format.Equals("json", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(StatusCodes.Status406NotAcceptable);

    IReadOnlySet<string>? selectedProviders = ReadSelectedProviders(
        ReadParameter(request, form, "engines"),
        aggregator.ProviderNames,
        out string[] unknownProviders,
        out string? providerSelectionError);
    if (providerSelectionError is not null)
    {
        return Results.BadRequest(new { error = providerSelectionError });
    }
    if (unknownProviders.Length > 0)
    {
        return Results.BadRequest(new
        {
            error = "One or more requested engines are not available.",
            unknown_engines = unknownProviders,
            available_engines = aggregator.ProviderNames
        });
    }

    if (!TryBuildSearchQuery(request, form, queryText.Trim(), 10, selectedProviders, out SearchQuery query, out string? error))
        return Results.BadRequest(new { error });
    SearchExecutionResult execution = await aggregator.SearchDetailedAsync(query, cancellationToken);
    return Results.Ok(SearxngSearchResponseMapper.Map(query.Text, execution));
}

static bool TryBuildSearchQuery(
    HttpRequest request,
    IFormCollection? form,
    string queryText,
    int limit,
    IReadOnlySet<string>? selectedProviders,
    out SearchQuery query,
    out string? error)
{
    query = new SearchQuery(queryText, limit, selectedProviders);
    error = null;

    if (queryText.Length > 512)
    {
        error = "Query must be 512 characters or fewer.";
        return false;
    }

    string? pageValue = ReadParameter(request, form, "pageno");
    int page = 1;
    if (!string.IsNullOrWhiteSpace(pageValue)
        && (!int.TryParse(pageValue, out page) || page < 1 || page > 50))
    {
        error = "Page number must be between 1 and 50.";
        return false;
    }

    string? categoryValue = ReadParameter(request, form, "categories");
    IReadOnlySet<string>? categories = string.IsNullOrWhiteSpace(categoryValue)
        ? null
        : new HashSet<string>(
            categoryValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    if (categories is { Count: 0 })
    {
        error = "At least one category must be supplied.";
        return false;
    }

    if (categories is { Count: > 10 } || categories?.Any(category => category.Length > 32) == true)
    {
        error = "Supply no more than 10 categories, each 32 characters or fewer.";
        return false;
    }

    string language = ReadParameter(request, form, "language")?.Trim() ?? "auto";
    if (language.Length > 35)
    {
        error = "Language value is too long.";
        return false;
    }

    string? safeSearchValue = ReadParameter(request, form, "safesearch");
    int safeSearch = 0;
    if (!string.IsNullOrWhiteSpace(safeSearchValue)
        && (!int.TryParse(safeSearchValue, out safeSearch) || safeSearch is < 0 or > 2))
    {
        error = "Safe-search must be 0, 1 or 2.";
        return false;
    }

    string? timeRangeValue = ReadParameter(request, form, "time_range");
    SearchTimeRange? timeRange = null;
    if (!string.IsNullOrWhiteSpace(timeRangeValue))
    {
        if (!Enum.TryParse(timeRangeValue, true, out SearchTimeRange parsedRange)
            || !Enum.IsDefined(parsedRange))
        {
            error = "Time range must be day, week, month or year.";
            return false;
        }
        timeRange = parsedRange;
    }

    query = new SearchQuery(queryText, limit, selectedProviders)
    {
        Categories = categories,
        Language = string.IsNullOrWhiteSpace(language) ? "auto" : language,
        SafeSearch = safeSearch,
        Page = page,
        TimeRange = timeRange
    };
    return true;
}

static string? ReadParameter(HttpRequest request, IFormCollection? form, string name)
{
    string? formValue = form is null ? null : form[name].FirstOrDefault();
    return string.IsNullOrWhiteSpace(formValue) ? request.Query[name].FirstOrDefault() : formValue;
}

static IReadOnlySet<string>? ReadSelectedProviders(
    string? providerValue,
    IReadOnlyList<string> availableProviders,
    out string[] unknownProviders,
    out string? error)
{
    unknownProviders = Array.Empty<string>();
    error = null;
    if (string.IsNullOrWhiteSpace(providerValue)) return null;

    string[] values = providerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    HashSet<string> requested = new(values, StringComparer.OrdinalIgnoreCase);
    if (requested.Count > 20 || requested.Any(provider => provider.Length > 64))
    {
        error = "Select no more than 20 engines; each engine name must be 64 characters or fewer.";
        return null;
    }

    HashSet<string> available = new(availableProviders, StringComparer.OrdinalIgnoreCase);
    unknownProviders = requested.Where(provider => !available.Contains(provider)).ToArray();
    return requested;
}

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<SearchResult> Results,
    IReadOnlyList<string> UnsupportedProviders,
    IReadOnlyList<string> CoolingDownProviders)
{
    public IReadOnlyList<ProviderOutcome> ProviderOutcomes { get; init; } = Array.Empty<ProviderOutcome>();
}
public partial class Program { }
