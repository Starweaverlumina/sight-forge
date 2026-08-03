using System.ComponentModel;
using System.Windows;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class SoundForgeWindow : Window
{
    private readonly WasapiLoopbackDirectionService _audioDirection = new();
    private SoundForgeSettings _settings = new();

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
            return;
        }

        _settings = new SoundForgeSettings
        {
            MinimumAudibleLevel = MinimumLevelSlider.Value,
            DirectionDeadZone = DeadZoneSlider.Value,
            MinimumEventIntervalMilliseconds = (int)Math.Round(IntervalSlider.Value),
            SpeakClockDirection = ClockSpeechCheckBox.IsChecked == true,
            CriticalOnlySpeech = CriticalOnlyCheckBox.IsChecked == true
        };

        try
        {
            _audioDirection.Start(_settings);
            StartStopButton.Content = "Stop Audio Direction";
            StatusText.Text = "Status: Listening to system output through WASAPI loopback.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Status: Audio capture could not start. {exception.Message}";
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

            if (_settings.SpeakClockDirection)
            {
                var shouldAnnounce = !_settings.CriticalOnlySpeech || snapshot.Confidence >= 0.78;
                StatusText.Text = shouldAnnounce
                    ? $"Clock callout ready: {snapshot.ClockPosition}. Speech output is the next integration layer."
                    : "Status: Direction visible; speech suppressed below confidence threshold.";
            }
        });
    }

    private void AudioDirection_CaptureFailed(object? sender, Exception exception)
    {
        Dispatcher.BeginInvoke(() =>
        {
            StartStopButton.Content = "Start Audio Direction";
            StatusText.Text = $"Status: Audio capture stopped. {exception.Message}";
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
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _audioDirection.DirectionChanged -= AudioDirection_DirectionChanged;
        _audioDirection.CaptureFailed -= AudioDirection_CaptureFailed;
        _audioDirection.Dispose();
    }
}
