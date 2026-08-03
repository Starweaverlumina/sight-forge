using System.Windows;
using System.Windows.Controls;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class AccessibilityWizardWindow : Window
{
    private readonly AccessibleNarrationService _narrator;
    private readonly AccessibilityProfilePackStore _store = new();

    public AccessibilityWizardWindow(AccessibleNarrationService narrator)
    {
        InitializeComponent();
        _narrator = narrator;
    }

    private void TestSpeech_Click(object sender, RoutedEventArgs e)
    {
        _narrator.Rate = (int)Math.Round(SpeechRateSlider.Value);
        _narrator.Speak(new NarrationRequest("SightForge speech test. Twelve o'clock is directly ahead. Three o'clock is to your right.", NarrationPriority.Important));
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = new AccessibilityProfilePack
        {
            DisplayName = "My calibrated accessibility profile",
            ReviewStatus = "Local calibrated profile"
        };
        profile.Vision.Magnification = MagnificationSlider.Value;
        profile.Vision.EdgeEnhancementEnabled = EdgesCheck.IsChecked == true;
        profile.Vision.MotionEmphasisEnabled = MotionCheck.IsChecked == true;
        if ((ThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Contains("Color-Blind", StringComparison.OrdinalIgnoreCase) == true)
            profile.Vision.ApplyHighContrastColorBlindSafeTheme();
        profile.Narration.Enabled = SpeechCheck.IsChecked == true;
        profile.Narration.Rate = (int)Math.Round(SpeechRateSlider.Value);
        profile.Narration.SpeakClockDirections = ClockCheck.IsChecked == true;
        profile.Sound.AnnounceOnlyHighConfidence = HighConfidenceCheck.IsChecked == true;
        profile.Notes["controlPreference"] = (ControlCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Keyboard and mouse";
        await _store.SaveAsync(profile);
        StatusText.Text = "Starting profile saved.";
        _narrator.Speak(new NarrationRequest("Your starting accessibility profile has been saved.", NarrationPriority.Important));
    }
}
