using System.Text.Json;

namespace SightForge.Platform;

public sealed class ModulePermissionStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private Dictionary<string, ModulePermissionDecision> _decisions =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public ModulePermissionStore(string? rootDirectory = null)
    {
        var directory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SightForge");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "module-permissions.json");
    }

    public async Task<IReadOnlyCollection<ModulePermissionDecision>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _decisions.Values.ToArray();
    }

    public async Task<ModuleCapability> GetGrantedAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        await EnsureLoadedAsync(cancellationToken);
        return _decisions.TryGetValue(moduleId, out var decision)
            ? decision.GrantedCapabilities
            : ModuleCapability.None;
    }

    public async Task SetAsync(
        ModuleDescriptor module,
        ModuleCapability grantedCapabilities,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(module);
        await EnsureLoadedAsync(cancellationToken);

        var sanitized = grantedCapabilities & module.RequestedCapabilities;
        _decisions[module.Id] = new ModulePermissionDecision(
            module.Id,
            sanitized,
            DateTimeOffset.UtcNow,
            reason);

        await SaveAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        await EnsureLoadedAsync(cancellationToken);
        _decisions[moduleId] = new ModulePermissionDecision(
            moduleId,
            ModuleCapability.None,
            DateTimeOffset.UtcNow,
            "Permissions revoked by user.");
        await SaveAsync(cancellationToken);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded) return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_loaded) return;
            if (File.Exists(_filePath))
            {
                await using var stream = File.OpenRead(_filePath);
                var values = await JsonSerializer.DeserializeAsync<ModulePermissionDecision[]>(
                    stream,
                    _jsonOptions,
                    cancellationToken) ?? Array.Empty<ModulePermissionDecision>();
                _decisions = values.ToDictionary(value => value.ModuleId, StringComparer.OrdinalIgnoreCase);
            }
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var tempPath = _filePath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    _decisions.Values.OrderBy(value => value.ModuleId).ToArray(),
                    _jsonOptions,
                    cancellationToken);
            }
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
