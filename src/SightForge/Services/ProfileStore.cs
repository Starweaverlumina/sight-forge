using System.Text.Json;
using SightForge.Models;

namespace SightForge.Services;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _profileDirectory;

    public ProfileStore(string? profileDirectory = null)
    {
        _profileDirectory = profileDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SightForge",
            "Profiles");
    }

    public async Task<VisualSettings> LoadAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var path = GetProfilePath(profileName);

        if (!File.Exists(path))
        {
            return new VisualSettings();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<VisualSettings>(
                   stream,
                   SerializerOptions,
                   cancellationToken)
               ?? new VisualSettings();
    }

    public async Task SaveAsync(
        string profileName,
        VisualSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_profileDirectory);

        var path = GetProfilePath(profileName);
        var temporaryPath = path + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                SerializerOptions,
                cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetProfilePath(string profileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string(
            profileName
                .Trim()
                .Select(character => invalidCharacters.Contains(character) ? '_' : character)
                .ToArray());

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "Default";
        }

        return Path.Combine(_profileDirectory, safeName + ".json");
    }
}
