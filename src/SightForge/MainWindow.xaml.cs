using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SightForge.Models;
using SightForge.Services;

namespace SightForge;

public partial class MainWindow : Window
{
    private const string DefaultProfileName = "Default";
    private readonly DesktopCaptureService _captureService = new();
    private readonly VisualProcessor _visualProcessor = new();
    private readonly ProfileStore _profileStore = new();
    private readonly ReplayBufferService _replayBuffer = new();
    private readonly AdaptiveVisualTuner _adaptiveTuner = new();
    private readonly DirectionalCueEngine _directionalCueEngine = new();
    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _playbackTimer;
    private readonly Stopwatch _fpsClock = Stopwatch.StartNew();

    private VisualSettings _settings = new();
    private IReadOnlyList<RecordedFrame> _playbackFrames = Array.Empty<RecordedFrame>();
    private byte[]? _previousAnalysisPixels;
    private bool _previewRunning;
    private bool _playbackRunning;
    private bool _frameInProgress;
    private bool _isInitialized;
    private bool _isApplyingSettings;
    private int _playbackIndex;
    private int _framesSinceMeasurement;

    public MainWindow()
    {
        InitializeComponent();
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render);
        _captureTimer.Tick += CaptureTimer_Tick;
        _playbackTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 15.0)
        };
        _playbackTimer.Tick += PlaybackTimer_Tick;
        Loaded += MainWindow_Loaded;
        _isInitialized = true;
        ApplySettingsToControls(_settings);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = await _profileStore.LoadAsync(DefaultProfileName);
            NormalizeAudioSettings(_settings);
            ApplySettingsToControls(_settings);
            StatusText.Text = "Status: Accessibility profile loaded. Ready.";
        }
        catch (Exception exception)
        {
            _settings = new VisualSettings();
            ApplySettingsToControls(_settings);
            StatusText.Text = $"Status: Using defaults because the profile could not load. {exception.Message}";
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_previewRunning)
        {
            StopPreview("Status: Live feed stopped. Rolling replay remains available.");
            return;
        }

        StopPlayback();
        _previewRunning = true;
        _visualProcessor.ResetMotionHistory();
        _adaptiveTuner.Reset();
        _directionalCueEngine.Reset();
        _previousAnalysisPixels = null;
        _fpsClock.Restart();
        _framesSinceMeasurement = 0;
        _captureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / Math.Clamp(_settings.TargetFramesPerSecond, 5, 60));
        _replayBuffer.Retention = TimeSpan.FromSeconds(_settings.ReplaySeconds);
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        StartButton.Content = "Stop Live Feed";
        StatusText.Text = "Status: Live feed active. Spatial cues represent general motion regions only.";
        _captureTimer.Start();
    }

    private async void CaptureTimer_Tick(object? sender, EventArgs e)
    {
        if (!_previewRunning || _frameInProgress) return;
        _frameInProgress = true;
        var snapshot = _settings.Clone();

        try
        {
            var result = await Task.Run(() => CaptureProcessAndAnalyze(snapshot));
            PreviewImage.Source = result.Bitmap;
            if (_settings.RollingReplayEnabled) _replayBuffer.Add(result.Frame);

            if (_settings.AdaptiveTuningEnabled && _adaptiveTuner.ApplySafeAdaptation(_settings))
                ApplySettingsToControls(_settings);

            var gridResult = _directionalCueEngine.Process(result.Frame, _settings);
            AudioCueStatusText.Text = gridResult is null
                ? $"Spatial cue wheel: {(_settings.DirectionalAudioEnabled ? "listening" : "off")}" 
                : $"Spatial cue wheel: {gridResult.Direction} · {gridResult.Strength:P0}";

            var replayMb = _replayBuffer.StoredBytes / (1024.0 * 1024.0);
            ReplayStatusText.Text = $"Replay: {_replayBuffer.Count} frames · {replayMb:0} MB · {_settings.ReplaySeconds}s target";
            UpdateFrameRate();
        }
        catch (Exception exception)
        {
            StopPreview($"Status: Live feed stopped after a capture error. {exception.Message}");
        }
        finally
        {
            _frameInProgress = false;
        }
    }

    private (BitmapSource Bitmap, RecordedFrame Frame) CaptureProcessAndAnalyze(VisualSettings settings)
    {
        var frame = _captureService.CapturePrimaryDisplay(settings.PreviewWidth, settings.PreviewHeight, settings.Magnification);
        _visualProcessor.Process(frame, settings);
        var recorded = _adaptiveTuner.AnalyzeAndRecord(frame.Pixels, frame.Width, frame.Height, frame.Stride, _previousAnalysisPixels);
        _previousAnalysisPixels = (byte[])frame.Pixels.Clone();
        return (CreateBitmap(recorded), recorded);
    }

    private static BitmapSource CreateBitmap(RecordedFrame frame)
    {
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, frame.Pixels, frame.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private void PlaybackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_playbackRunning)
        {
            StopPlayback();
            StatusText.Text = "Status: Playback stopped.";
            return;
        }

        _playbackFrames = _replayBuffer.Snapshot();
        if (_playbackFrames.Count == 0)
        {
            StatusText.Text = "Status: No replay frames yet. Start the live feed first.";
            return;
        }

        if (_previewRunning) StopPreview("Status: Live feed paused for replay.");
        _playbackIndex = 0;
        _playbackRunning = true;
        PlaybackButton.Content = "Stop Playback";
        StatusText.Text = $"Status: Playing {_playbackFrames.Count} local frames.";
        _playbackTimer.Start();
    }

    private void PlaybackTimer_Tick(object? sender, EventArgs e)
    {
        if (!_playbackRunning || _playbackFrames.Count == 0) return;
        if (_playbackIndex >= _playbackFrames.Count)
        {
            StopPlayback();
            StatusText.Text = "Status: Replay complete.";
            return;
        }

        var frame = _playbackFrames[_playbackIndex++];
        PreviewImage.Source = CreateBitmap(frame);
        ReplayStatusText.Text = $"Playback: {_playbackIndex}/{_playbackFrames.Count} · motion {frame.MotionRatio:P0}";
    }

    private void ClearReplayButton_Click(object sender, RoutedEventArgs e)
    {
        StopPlayback();
        _replayBuffer.Clear();
        ReplayStatusText.Text = "Replay: empty";
        StatusText.Text = "Status: Rolling replay cleared.";
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitialized && !_isApplyingSettings) ReadSettingsFromControls();
    }

    private void SettingsControl_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized && !_isApplyingSettings) ReadSettingsFromControls();
    }

    private void CueFrequency_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized && !_isApplyingSettings) ReadCueFrequenciesFromControls();
    }

    private void PreviewCueButton_Click(object sender, RoutedEventArgs e)
    {
        ReadSettingsFromControls();
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out var index) && index is >= 0 and < 9)
        {
            _directionalCueEngine.Preview((GridDirection)index, _settings);
            AudioCueStatusText.Text = $"Spatial preview: {(GridDirection)index}";
        }
    }

    private void ReadSettingsFromControls()
    {
        if (_isApplyingSettings) return;
        _settings.Magnification = MagnificationSlider.Value;
        _settings.Contrast = ContrastSlider.Value;
        _settings.Gamma = GammaSlider.Value;
        _settings.EdgeEnhancementEnabled = EdgeEnhancementCheckBox.IsChecked == true;
        _settings.MotionEmphasisEnabled = MotionEmphasisCheckBox.IsChecked == true;
        _settings.CenterGuideEnabled = CrosshairCheckBox.IsChecked == true;
        _settings.AdaptiveTuningEnabled = AdaptiveTuningCheckBox.IsChecked == true;
        _settings.RollingReplayEnabled = RollingReplayCheckBox.IsChecked == true;
        _settings.DirectionalAudioEnabled = DirectionalAudioCheckBox.IsChecked == true;
        _settings.DirectionalAudioVolume = CueVolumeSlider.Value / 100.0;
        _settings.DirectionalAudioCooldownMilliseconds = (int)Math.Round(CueCooldownSlider.Value);
        _settings.MotionThreshold = (byte)Math.Clamp((int)Math.Round(MotionThresholdSlider.Value), 0, 255);
        _settings.ReplaySeconds = (int)Math.Round(ReplaySecondsSlider.Value);
        _replayBuffer.Retention = TimeSpan.FromSeconds(_settings.ReplaySeconds);
        ReadCueFrequenciesFromControls();

        MagnificationValue.Text = $"{_settings.Magnification:0.00}×";
        ContrastValue.Text = $"{_settings.Contrast:0.00}";
        GammaValue.Text = $"{_settings.Gamma:0.00}";
        MotionThresholdValue.Text = _settings.MotionThreshold.ToString();
        ReplaySecondsValue.Text = $"{_settings.ReplaySeconds}s";
        CueVolumeValue.Text = $"{_settings.DirectionalAudioVolume:P0}";
        CueCooldownValue.Text = $"{_settings.DirectionalAudioCooldownMilliseconds} ms";
        CenterGuide.Visibility = _settings.CenterGuideEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ReadCueFrequenciesFromControls()
    {
        var boxes = GetCueBoxes();
        var frequencies = new int[9];
        for (var index = 0; index < boxes.Length; index++)
        {
            frequencies[index] = int.TryParse(boxes[index].Text, out var value)
                ? Math.Clamp(value, 120, 2400)
                : _settings.DirectionalAudioFrequencies.ElementAtOrDefault(index) is var fallback && fallback > 0 ? fallback : 440;
        }
        _settings.DirectionalAudioFrequencies = frequencies;
    }

    private TextBox[] GetCueBoxes() =>
    [Cue0TextBox, Cue1TextBox, Cue2TextBox, Cue3TextBox, Cue4TextBox, Cue5TextBox, Cue6TextBox, Cue7TextBox, Cue8TextBox];

    private void ApplySettingsToControls(VisualSettings settings)
    {
        NormalizeAudioSettings(settings);
        _isApplyingSettings = true;
        try
        {
            MagnificationSlider.Value = Math.Clamp(settings.Magnification, 1.0, 4.0);
            ContrastSlider.Value = Math.Clamp(settings.Contrast, 0.5, 2.5);
            GammaSlider.Value = Math.Clamp(settings.Gamma, 0.4, 2.2);
            MotionThresholdSlider.Value = Math.Clamp(settings.MotionThreshold, (byte)8, (byte)90);
            ReplaySecondsSlider.Value = Math.Clamp(settings.ReplaySeconds, 5, 120);
            CueVolumeSlider.Value = Math.Clamp(settings.DirectionalAudioVolume * 100.0, 2, 65);
            CueCooldownSlider.Value = Math.Clamp(settings.DirectionalAudioCooldownMilliseconds, 100, 2000);
            EdgeEnhancementCheckBox.IsChecked = settings.EdgeEnhancementEnabled;
            MotionEmphasisCheckBox.IsChecked = settings.MotionEmphasisEnabled;
            CrosshairCheckBox.IsChecked = settings.CenterGuideEnabled;
            AdaptiveTuningCheckBox.IsChecked = settings.AdaptiveTuningEnabled;
            RollingReplayCheckBox.IsChecked = settings.RollingReplayEnabled;
            DirectionalAudioCheckBox.IsChecked = settings.DirectionalAudioEnabled;
            var boxes = GetCueBoxes();
            for (var index = 0; index < boxes.Length; index++) boxes[index].Text = settings.DirectionalAudioFrequencies[index].ToString();
        }
        finally
        {
            _isApplyingSettings = false;
        }
        ReadSettingsFromControls();
    }

    private static void NormalizeAudioSettings(VisualSettings settings)
    {
        if (settings.DirectionalAudioFrequencies is { Length: 9 }) return;
        settings.DirectionalAudioFrequencies = new VisualSettings().DirectionalAudioFrequencies;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _settings = new VisualSettings();
        _visualProcessor.ResetMotionHistory();
        _adaptiveTuner.Reset();
        _directionalCueEngine.Reset();
        ApplySettingsToControls(_settings);
        StatusText.Text = "Status: Settings reset to accessible defaults.";
    }

    private void UpdateFrameRate()
    {
        _framesSinceMeasurement++;
        if (_fpsClock.ElapsedMilliseconds < 1000) return;
        var fps = _framesSinceMeasurement / Math.Max(_fpsClock.Elapsed.TotalSeconds, 0.001);
        PerformanceText.Text = $"{fps:0.0} FPS · {_settings.PreviewWidth}×{_settings.PreviewHeight}";
        _framesSinceMeasurement = 0;
        _fpsClock.Restart();
    }

    private void StopPreview(string status)
    {
        _previewRunning = false;
        _captureTimer.Stop();
        _visualProcessor.ResetMotionHistory();
        _directionalCueEngine.Reset();
        StartButton.Content = "Start Live Feed";
        PerformanceText.Text = "0 FPS";
        StatusText.Text = status;
    }

    private void StopPlayback()
    {
        _playbackRunning = false;
        _playbackTimer.Stop();
        PlaybackButton.Content = "Play Rolling Replay";
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _captureTimer.Stop();
        _playbackTimer.Stop();
        _directionalCueEngine.Dispose();
        try { _profileStore.SaveAsync(DefaultProfileName, _settings).GetAwaiter().GetResult(); }
        catch { }
    }
}
