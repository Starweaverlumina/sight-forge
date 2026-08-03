using System.Net.Http.Json;
using System.Text.Json;
using SightForge.Models;

namespace SightForge.Services;

public interface IGameResearchProvider
{
    Task<GameResearchResult> ResearchAsync(
        GameResearchRequest request,
        GameResearchPolicy policy,
        CancellationToken cancellationToken = default);
}

public sealed class GameResearchService
{
    private readonly IGameResearchProvider _provider;
    private readonly string _cacheDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public GameResearchService(IGameResearchProvider provider, string? cacheDirectory = null)
    {
        _provider = provider;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SightForge",
            "GameResearchCache");
    }

    public async Task<GameResearchResult> ResearchAsync(
        GameResearchRequest request,
        GameResearchPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.WebResearchEnabled)
        {
            throw new InvalidOperationException(
                "Web research is disabled. The user must enable it in SightForge settings.");
        }

        Directory.CreateDirectory(_cacheDirectory);
        var cachePath = GetCachePath(request);
        var cached = await TryReadCacheAsync(cachePath, policy.CacheHours, cancellationToken);
        if (cached is not null)
        {
            return new GameResearchResult
            {
                GameTitle = cached.GameTitle,
                Summary = cached.Summary,
                Sources = cached.Sources,
                SuggestedAccessibilitySettings = cached.SuggestedAccessibilitySettings,
                RetrievedAt = cached.RetrievedAt,
                FromCache = true
            };
        }

        var result = await _provider.ResearchAsync(request, policy, cancellationToken);
        ValidateSources(result, policy);
        await File.WriteAllTextAsync(
            cachePath,
            JsonSerializer.Serialize(result, _jsonOptions),
            cancellationToken);
        return result;
    }

    private async Task<GameResearchResult?> TryReadCacheAsync(
        string path,
        int cacheHours,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        var age = DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(path);
        if (age > TimeSpan.FromHours(Math.Clamp(cacheHours, 1, 168))) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<GameResearchResult>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ValidateSources(GameResearchResult result, GameResearchPolicy policy)
    {
        foreach (var source in result.Sources)
        {
            if (source.Uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Research sources must use HTTPS.");
            }

            var allowed = policy.AllowedDomains.Any(domain =>
                source.Uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                source.Uri.Host.EndsWith('.' + domain, StringComparison.OrdinalIgnoreCase));

            if (!allowed && policy.RequireUserConfirmationForNewDomains)
            {
                throw new InvalidOperationException(
                    $"Research returned an unapproved domain: {source.Uri.Host}");
            }
        }
    }

    private static string GetCachePath(GameResearchRequest request)
    {
        var key = string.Join('-', new[] { request.GameTitle, request.Platform, request.Topic }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var safe = string.Concat(key.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return safe + ".json";
    }
}

public sealed class OpenAiWebResearchProvider : IGameResearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAiWebResearchProvider(HttpClient httpClient, string model = "gpt-5")
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<GameResearchResult> ResearchAsync(
        GameResearchRequest request,
        GameResearchPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Research the game {request.GameTitle} for an accessibility assistant.
            Platform: {request.Platform ?? "unknown"}.
            Topic: {request.Topic ?? "general accessibility and gameplay help"}.
            User question: {request.UserQuestion ?? "none"}.
            Prefer official documentation and reputable guides. Cite every factual answer.
            Do not provide hidden-player detection, cheating instructions, automation,
            anti-cheat evasion, or information unavailable to an ordinary player.
            Return concise guidance and suggested SightForge accessibility settings.
            """;

        using var response = await _httpClient.PostAsJsonAsync(
            "v1/responses",
            new
            {
                model = _model,
                tools = new[] { new { type = "web_search" } },
                input = prompt
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var summary = document.RootElement.TryGetProperty("output_text", out var outputText)
            ? outputText.GetString()
            : null;

        return new GameResearchResult
        {
            GameTitle = request.GameTitle,
            Summary = summary ?? "The research provider returned no readable answer.",
            Sources = Array.Empty<GameResearchSource>(),
            SuggestedAccessibilitySettings = new Dictionary<string, string>()
        };
    }
}
