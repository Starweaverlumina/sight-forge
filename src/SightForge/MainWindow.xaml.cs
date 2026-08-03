using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
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
    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _playbackTimer;
    private readonly Stopwatch _framesPerSecondClock = Stopwatch.StartNew();

    private VisualSettings _settings = new();
    private IReadOnlyList<RecordedFrame> _playbackFrames = Array.Empty<RecordedFrame>();
    private byte[]? _previousAnalysisPixels;
    private bool _previewRunning;
    private bool _playbackRunning;
    private bool _frameInProgress;
    private bool _isInitialized;
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
            ApplySettingsToControls(_settings);
            StatusText.Text = "Status: Default accessibility profile loaded. Ready to start.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Status: Profile could not be loaded. Using defaults. {exception.Message}";
            _settings = new VisualSettings();
            ApplySettingsToControls(_settings);
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
        _previousAnalysisPixels = null;
        _framesPerSecondClock.Restart();
        _framesSinceMeasurement = 0;
        _captureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / Math.Clamp(_settings.TargetFramesPerSecond, 5, 60));
        _replayBuffer.Retention = TimeSpan.FromSeconds(_settings.ReplaySeconds);
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        StartButton.Content = "Stop Live Feed";
        StatusText.Text = "Status: Live screen feed active. Visible pixels are recorded into a local rolling memory buffer.";
        _captureTimer.Start();
    }

    private async void CaptureTimer_Tick(object? sender, EventArgs e)
    {
        if (!_previewRunning || _frameInProgress) return;

        _frameInProgress = true;
        var settingsSnapshot = _settings.Clone();

        try
        {
            var result = await Task.Run(() => CaptureProcessAndAnalyze(settingsSnapshot));
            PreviewImage.Source = result.Bitmap;

            if (_settings.RollingReplayEnabled)
            {
                _replayBuffer.Add(result.Frame);
            }

            if (_settings.AdaptiveTuningEnabled && _adaptiveTuner.ApplySafeAdaptation(_settings))
            {
                ApplySettingsToControls(_settings);
            }

            ReplayStatusText.Text = $"Replay: {_replayBuffer.Count} frames · {_settings.ReplaySeconds}s rolling window";
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
        var frame = _captureService.CapturePrimaryDisplay(
            settings.PreviewWidth,
            settings.PreviewHeight,
            settings.Magnification);

        _visualProcessor.Process(frame, settings);
        var recorded = _adaptiveTuner.AnalyzeAndRecord(
            frame.Pixels, frame.Width, frame.Height, frame.Stride, _previousAnalysisPixels);
        _previousAnalysisPixels = (byte[])frame.Pixels.Clone();

        return (CreateBitmap(recorded), recorded);
    }

    private static BitmapSource CreateBitmap(RecordedFrame frame)
    {
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96,
            PixelFormats.Bgra32, null, frame.Pixels, frame.Stride);
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
            StatusText.Text = "Status: No replay frames are available yet. Start the live feed first.";
            return;
        }

        if (_previewRunning) StopPreview("Status: Live feed paused for replay.");
        _playbackIndex = 0;
        _playbackRunning = true;
        PlaybackButton.Content = "Stop Playback";
        StatusText.Text = $"Status: Playing back {_playbackFrames.Count} locally recorded frames.";
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
        StatusText.Text = "Status: Rolling replay memory cleared.";
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) ReadSettingsFromControls();
    }

    private void SettingsControl_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitialized) ReadSettingsFromControls();
    }

    private void ReadSettingsFromControls()
    {
        _settings.Magnification = MagnificationSlider.Value;
        _settings.Contrast = ContrastSlider.Value;
        _settings.Gamma = GammaSlider.Value;
        _settings.EdgeEnhancementEnabled = EdgeEnhancementCheckBox.IsChecked == true;
        _settings.MotionEmphasisEnabled = MotionEmphasisCheckBox.IsChecked == true;
        _settings.CenterGuideEnabled = CrosshairCheckBox.IsChecked == true;
        _settings.AdaptiveTuningEnabled = AdaptiveTuningCheckBox.IsChecked == true;
        _settings.RollingReplayEnabled = RollingReplayCheckBox.IsChecked == true;
        _settings.MotionThreshold = (byte)Math.Clamp((int)Math.Round(MotionThresholdSlider.Value), 0, 255);
        _settings.ReplaySeconds = (int)Math.Round(ReplaySecondsSlider.Value);
        _replayBuffer.Retention = TimeSpan.FromSeconds(_settings.ReplaySeconds);

        MagnificationValue.Text = $"{_settings.Magnification:0.00}×";
        ContrastValue.Text = $"{_settings.Contrast:0.00}";
        GammaValue.Text = $"{_settings.Gamma:0.00}";
        MotionThresholdValue.Text = $"{_settings.MotionThreshold}";
        ReplaySecondsValue.Text = $"{_settings.ReplaySeconds}s";
        CenterGuide.Visibility = _settings.CenterGuideEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplySettingsToControls(VisualSettings settings)
    {
        MagnificationSlider.Value = Math.Clamp(settings.Magnification, 1.0, 4.0);
        ContrastSlider.Value = Math.Clamp(settings.Contrast, 0.5, 2.5);
        GammaSlider.Value = Math.Clamp(settings.Gamma, 0.4, 2.2);
        MotionThresholdSlider.Value = Math.Clamp(settings.MotionThreshold, (byte)8, (byte)90);
        ReplaySecondsSlider.Value = Math.Clamp(settings.ReplaySeconds, 5, 120);
        EdgeEnhancementCheckBox.IsChecked = settings.EdgeEnhancementEnabled;
        MotionEmphasisCheckBox.IsChecked = settings.MotionEmphasisEnabled;
        CrosshairCheckBox.IsChecked = settings.CenterGuideEnabled;
        AdaptiveTuningCheckBox.IsChecked = settings.AdaptiveTuningEnabled;
        RollingReplayCheckBox.IsChecked = settings.RollingReplayEnabled;
        ReadSettingsFromControls();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _settings = new VisualSettings();
        _visualProcessor.ResetMotionHistory();
        _adaptiveTuner.Reset();
        ApplySettingsToControls(_settings);
        StatusText.Text = "Status: Visual settings reset to accessible defaults.";
    }

    private void UpdateFrameRate()
    {
        _framesSinceMeasurement++;
        if (_framesPerSecondClock.ElapsedMilliseconds < 1000) return;

        var framesPerSecond = _framesSinceMeasurement / Math.Max(_framesPerSecondClock.Elapsed.TotalSeconds, 0.001);
        PerformanceText.Text = $"{framesPerSecond:0.0} FPS · {_settings.PreviewWidth}×{_settings.PreviewHeight}";
        _framesSinceMeasurement = 0;
        _framesPerSecondClock.Restart();
    }

    private void StopPreview(string status)
    {
        _previewRunning = false;
        _captureTimer.Stop();
        _visualProcessor.ResetMotionHistory();
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
        try
        {
            _profileStore.SaveAsync(DefaultProfileName, _settings).GetAwaiter().GetResult();
        }
        catch
        {
            // Closing must not be blocked by profile persistence errors.
        }
    }
}
