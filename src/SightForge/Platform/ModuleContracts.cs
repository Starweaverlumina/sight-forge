namespace SightForge.Platform;

[Flags]
public enum ModuleCapability
{
    None = 0,
    ScreenCapture = 1 << 0,
    AudioCapture = 1 << 1,
    Microphone = 1 << 2,
    WebAccess = 1 << 3,
    LocalStorage = 1 << 4,
    SpeechOutput = 1 << 5,
    ControllerInput = 1 << 6,
    ClipExport = 1 << 7,
    ProviderReporting = 1 << 8
}

public enum ModuleState
{
    Disabled,
    AwaitingPermission,
    Ready,
    Running,
    Paused,
    Faulted
}

public enum AssistancePriority
{
    Background = 0,
    Informational = 1,
    Guidance = 2,
    Important = 3,
    Critical = 4
}

public sealed record ModuleDescriptor(
    string Id,
    string DisplayName,
    string Category,
    string Description,
    Version Version,
    ModuleCapability RequestedCapabilities,
    bool EnabledByDefault = false);

public sealed record ModulePermissionDecision(
    string ModuleId,
    ModuleCapability GrantedCapabilities,
    DateTimeOffset UpdatedAt,
    string? Reason = null);

public sealed record AssistanceMessage(
    string SourceModuleId,
    AssistancePriority Priority,
    string Text,
    DateTimeOffset CreatedAt,
    TimeSpan? ExpiresAfter = null,
    string? DeduplicationKey = null);

public interface ISightForgeModule : IAsyncDisposable
{
    ModuleDescriptor Descriptor { get; }
    ModuleState State { get; }
    string? LastError { get; }

    Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task PauseAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public sealed class ModuleContext
{
    public ModuleContext(
        Func<ModuleCapability, bool> hasCapability,
        Func<AssistanceMessage, CancellationToken, ValueTask> publishAssistance,
        string dataDirectory)
    {
        HasCapability = hasCapability;
        PublishAssistance = publishAssistance;
        DataDirectory = dataDirectory;
    }

    public Func<ModuleCapability, bool> HasCapability { get; }
    public Func<AssistanceMessage, CancellationToken, ValueTask> PublishAssistance { get; }
    public string DataDirectory { get; }

    public void Demand(ModuleCapability capability)
    {
        if (!HasCapability(capability))
        {
            throw new UnauthorizedAccessException($"SightForge module capability not granted: {capability}.");
        }
    }
}
