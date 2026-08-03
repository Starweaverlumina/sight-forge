using System.Windows;
using System.Windows.Controls;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class ProfileManagerWindow : Window
{
    private readonly AccessibleNarrationService _narrator;
    private readonly AccessibilityProfilePackStore _store = new();
    private AccessibilityProfilePack? _selected;

    public ProfileManagerWindow(AccessibleNarrationService narrator)
    {
        InitializeComponent();
        _narrator = narrator;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        ProfilesList.ItemsSource = await _store.ListAsync();
        StatusText.Text = $"Loaded {ProfilesList.Items.Count} profiles.";
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _selected = new AccessibilityProfilePack();
        NameText.Text = _selected.DisplayName;
        GameText.Text = string.Empty;
        PlatformText.Text = string.Empty;
        HighContrastCheck.IsChecked = false;
        SpeakClockCheck.IsChecked = false;
        StatusText.Text = "New profile ready to edit.";
        NameText.Focus();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _selected ??= new AccessibilityProfilePack();
        _selected.DisplayName = string.IsNullOrWhiteSpace(NameText.Text) ? "Accessibility profile" : NameText.Text.Trim();
        _selected.GameTitle = GameText.Text.Trim();
        _selected.Platform = PlatformText.Text.Trim();
        _selected.Narration.SpeakClockDirections = SpeakClockCheck.IsChecked == true;
        if (HighContrastCheck.IsChecked == true) _selected.Vision.ApplyHighContrastColorBlindSafeTheme();
        await _store.SaveAsync(_selected);
        await RefreshAsync();
        StatusText.Text = "Profile saved.";
        _narrator.Speak(new NarrationRequest("Profile saved.", NarrationPriority.Important));
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not AccessibilityProfilePack profile) return;
        _selected = profile;
        NameText.Text = profile.DisplayName;
        GameText.Text = profile.GameTitle ?? string.Empty;
        PlatformText.Text = profile.Platform ?? string.Empty;
        HighContrastCheck.IsChecked = profile.Vision.CueThemeName.Contains("Color-Blind", StringComparison.OrdinalIgnoreCase);
        SpeakClockCheck.IsChecked = profile.Narration.SpeakClockDirections;
        DetailText.Text = $"Schema {profile.SchemaVersion} · {profile.ReviewStatus} · updated {profile.UpdatedAt.LocalDateTime:g}";
        _narrator.Speak(new NarrationRequest(profile.DisplayName, NarrationPriority.Normal));
    }
}
