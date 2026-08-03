using System.ComponentModel;
using System.Windows;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class SoundForgeWindow : Window
{
    private readonly WasapiLoopbackDirectionService _audioDirection = new();
    private readonly AccessibleNarrationService _narrator = new();
    private SoundForgeSettings _settings = new();
    private string? _lastSpokenClock;
    private DateTimeOffset _lastSpokenAt = DateTimeOffset.MinValue;

    public SoundForgeWindow()
    {
        InitializeComponent();
        DirectionWheel.SetStyles(EventCueStyleDefaults.CreateHighContrastColorBlindSafe());
        _audioDirection.DirectionChanged += AudioDirection_DirectionChanged;
        _audioDirection.CaptureFailed += AudioDirection_CaptureFailed;
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_audioDirection.IsRunning)
        {
            _audioDirection.Stop();
            StartStopButton.Content = "Start Audio Direction";
            StatusText.Text = "Status: Audio analysis stopped.";
            _narrator.Speak(new NarrationRequest("Audio direction stopped.", NarrationPriority.Important));
            return;
        }

        _settings = new SoundForgeSettings
        {
            Enabled = true,
            MinimumAudibleLevel = MinimumLevelSlider.Value,
            DirectionDeadZone = DeadZoneSlider.Value,
            MinimumEventIntervalMilliseconds = (int)Math.Round(IntervalSlider.Value),
            SpeakClockDirection = ClockSpeechCheckBox.IsChecked == true,
            CriticalOnlySpeech = CriticalOnlyCheckBox.IsChecked == true,
            AnnounceOnlyHighConfidence = CriticalOnlyCheckBox.IsChecked == true
        };

        try
        {
            _audioDirection.Start(_settings);
            StartStopButton.Content = "Stop Audio Direction";
            StatusText.Text = "Status: Listening to system output through WASAPI loopback.";
            _narrator.Speak(new NarrationRequest("SoundForge listening.", NarrationPriority.Important));
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Status: Audio capture could not start. {exception.Message}";
            _narrator.Speak(new NarrationRequest("Audio capture could not start.", NarrationPriority.Important));
        }
    }

    private void AudioDirection_DirectionChanged(object? sender, AudioDirectionSnapshot snapshot)
    {
        Dispatcher.BeginInvoke(() =>
        {
            DirectionWheel.UpdateAudio(snapshot);
            DirectionText.Text = snapshot.ClockPosition.ToUpperInvariant();
            ConfidenceText.Text = $"Confidence: {snapshot.Confidence:P0} · balance {snapshot.Balance:+0.00;-0.00;0.00}";
            LeftLevelBar.Value = Math.Clamp(snapshot.LeftLevel * 5.0, 0, 1);
            RightLevelBar.Value = Math.Clamp(snapshot.RightLevel * 5.0, 0, 1);
            LeftLevelText.Text = $"Left: {snapshot.LeftLevel:P1}";
            RightLevelText.Text = $"Right: {snapshot.RightLevel:P1}";

            if (!_settings.SpeakClockDirection) return;
            var shouldAnnounce = !_settings.CriticalOnlySpeech || snapshot.Confidence >= 0.78;
            if (!shouldAnnounce)
            {
                StatusText.Text = "Status: Direction visible; speech suppressed below confidence threshold.";
                return;
            }

            var now = DateTimeOffset.UtcNow;
            if (_lastSpokenClock == snapshot.ClockPosition && now - _lastSpokenAt < TimeSpan.FromSeconds(1.2)) return;
            _lastSpokenClock = snapshot.ClockPosition;
            _lastSpokenAt = now;
            StatusText.Text = $"Spoken direction: {snapshot.ClockPosition}.";
            _narrator.Speak(new NarrationRequest(
                snapshot.ClockPosition,
                snapshot.Confidence >= 0.9 ? NarrationPriority.Critical : NarrationPriority.Important,
                true,
                "clock:" + snapshot.ClockPosition));
        });
    }

    private void AudioDirection_CaptureFailed(object? sender, Exception exception)
    {
        Dispatcher.BeginInvoke(() =>
        {
            StartStopButton.Content = "Start Audio Direction";
            StatusText.Text = $"Status: Audio capture stopped. {exception.Message}";
            _narrator.Speak(new NarrationRequest("Audio capture stopped.", NarrationPriority.Critical));
        });
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        DirectionWheel.Clear();
        DirectionText.Text = "READY";
        ConfidenceText.Text = "Confidence: 0%";
        LeftLevelBar.Value = 0;
        RightLevelBar.Value = 0;
        LeftLevelText.Text = "Left: 0%";
        RightLevelText.Text = "Right: 0%";
        _lastSpokenClock = null;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _audioDirection.DirectionChanged -= AudioDirection_DirectionChanged;
        _audioDirection.CaptureFailed -= AudioDirection_CaptureFailed;
        _audioDirection.Dispose();
        _narrator.Dispose();
    }
}
