namespace SightForge.Models;

public sealed class GameResearchRequest
{
    public required string GameTitle { get; init; }
    public string? Platform { get; init; }
    public string? Topic { get; init; }
    public string? UserQuestion { get; init; }
    public bool AccessibilityOnly { get; init; } = true;
}

public sealed class GameResearchResult
{
    public required string GameTitle { get; init; }
    public required string Summary { get; init; }
    public IReadOnlyList<GameResearchSource> Sources { get; init; } = Array.Empty<GameResearchSource>();
    public IReadOnlyDictionary<string, string> SuggestedAccessibilitySettings { get; init; }
        = new Dictionary<string, string>();
    public DateTimeOffset RetrievedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool FromCache { get; init; }
}

public sealed class GameResearchSource
{
    public required string Title { get; init; }
    public required Uri Uri { get; init; }
    public string? Publisher { get; init; }
    public string? Excerpt { get; init; }
}

public sealed class GameResearchPolicy
{
    public bool WebResearchEnabled { get; set; }
    public bool RequireUserConfirmationForNewDomains { get; set; } = true;
    public bool PreferOfficialSources { get; set; } = true;
    public int CacheHours { get; set; } = 24;
    public string[] AllowedDomains { get; set; } =
    [
        "support.xbox.com",
        "playstation.com",
        "steamcommunity.com",
        "pcgamingwiki.com",
        "ign.com",
        "gamepressure.com"
    ];
}
