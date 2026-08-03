using System.Text.Json;
using SightForge.Models;

namespace SightForge.Services;

public sealed class AccessibilityProfilePackStore
{
    private readonly string _directory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AccessibilityProfilePackStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SightForge",
            "Profiles");
    }

    public async Task<IReadOnlyList<AccessibilityProfilePack>> ListAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        var results = new List<AccessibilityProfilePack>();
        foreach (var file in Directory.EnumerateFiles(_directory, "*.sightforge.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var profile = JsonSerializer.Deserialize<AccessibilityProfilePack>(json, _jsonOptions);
                if (profile is not null) results.Add(profile);
            }
            catch (JsonException)
            {
                // Ignore damaged profiles here; validation UI can surface them separately later.
            }
        }

        return results.OrderBy(profile => profile.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public async Task SaveAsync(AccessibilityProfilePack profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);
        Directory.CreateDirectory(_directory);
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        var path = Path.Combine(_directory, SafeFileName(profile.Id) + ".sightforge.json");
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(profile, _jsonOptions), cancellationToken);
        File.Move(temporary, path, true);
    }

    public async Task<AccessibilityProfilePack> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Profile pack was not found.", sourcePath);
        var json = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var profile = JsonSerializer.Deserialize<AccessibilityProfilePack>(json, _jsonOptions)
            ?? throw new InvalidDataException("Profile pack did not contain a valid profile.");
        Validate(profile);
        profile.Id = Guid.NewGuid().ToString("N");
        profile.ReviewStatus = "Imported — unreviewed";
        profile.CommunityReviewed = false;
        await SaveAsync(profile, cancellationToken);
        return profile;
    }

    public async Task ExportAsync(AccessibilityProfilePack profile, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(destinationPath, JsonSerializer.Serialize(profile, _jsonOptions), cancellationToken);
    }

    public Task DeleteAsync(string profileId)
    {
        var path = Path.Combine(_directory, SafeFileName(profileId) + ".sightforge.json");
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static void Validate(AccessibilityProfilePack profile)
    {
        if (profile.SchemaVersion != 1) throw new InvalidDataException($"Unsupported profile schema: {profile.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(profile.Id)) throw new InvalidDataException("Profile id is required.");
        if (string.IsNullOrWhiteSpace(profile.DisplayName)) throw new InvalidDataException("Profile name is required.");
        if (profile.OcrRegions.Length > 32) throw new InvalidDataException("A profile may contain at most 32 OCR regions.");
        foreach (var region in profile.OcrRegions)
        {
            if (region.LeftPercent is < 0 or > 1 || region.TopPercent is < 0 or > 1 ||
                region.WidthPercent is <= 0 or > 1 || region.HeightPercent is <= 0 or > 1)
                throw new InvalidDataException($"OCR region '{region.DisplayName}' has invalid normalized bounds.");
        }
    }

    private static string SafeFileName(string value) =>
        string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
}
