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
    private readonly DispatcherTimer _captureTimer;
    private readonly Stopwatch _framesPerSecondClock = Stopwatch.StartNew();

    private VisualSettings _settings = new();
    private bool _previewRunning;
    private bool _frameInProgress;
    private bool _isInitialized;
    private int _framesSinceMeasurement;

    public MainWindow()
    {
        InitializeComponent();

        _captureTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / _settings.TargetFramesPerSecond)
        };
        _captureTimer.Tick += CaptureTimer_Tick;

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
            StopPreview("Status: Compatibility preview stopped.");
            return;
        }

        _previewRunning = true;
        _visualProcessor.ResetMotionHistory();
        _framesPerSecondClock.Restart();
        _framesSinceMeasurement = 0;
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        StartButton.Content = "Stop Compatibility Preview";
        StatusText.Text = "Status: Capturing visible pixels from the primary display in compatibility mode.";
        _captureTimer.Start();
    }

    private async void CaptureTimer_Tick(object? sender, EventArgs e)
    {
        if (!_previewRunning || _frameInProgress)
        {
            return;
        }

        _frameInProgress = true;
        var settingsSnapshot = _settings.Clone();

        try
        {
            var bitmapSource = await Task.Run(() => CaptureAndProcess(settingsSnapshot));
            PreviewImage.Source = bitmapSource;
            UpdateFrameRate();
        }
        catch (Exception exception)
        {
            StopPreview($"Status: Preview stopped after a capture error. {exception.Message}");
        }
        finally
        {
            _frameInProgress = false;
        }
    }

    private BitmapSource CaptureAndProcess(VisualSettings settings)
    {
        var frame = _captureService.CapturePrimaryDisplay(
            settings.PreviewWidth,
            settings.PreviewHeight,
            settings.Magnification);

        _visualProcessor.Process(frame, settings);

        var bitmap = BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            frame.Pixels,
            frame.Stride);

        bitmap.Freeze();
        return bitmap;
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ReadSettingsFromControls();
    }

    private void SettingsControl_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ReadSettingsFromControls();
    }

    private void ReadSettingsFromControls()
    {
        _settings.Magnification = MagnificationSlider.Value;
        _settings.Contrast = ContrastSlider.Value;
        _settings.Gamma = GammaSlider.Value;
        _settings.EdgeEnhancementEnabled = EdgeEnhancementCheckBox.IsChecked == true;
        _settings.MotionEmphasisEnabled = MotionEmphasisCheckBox.IsChecked == true;
        _settings.CenterGuideEnabled = CrosshairCheckBox.IsChecked == true;
        _settings.MotionThreshold = (byte)Math.Clamp(
            (int)Math.Round(MotionThresholdSlider.Value),
            byte.MinValue,
            byte.MaxValue);

        MagnificationValue.Text = $"{_settings.Magnification:0.00}×";
        ContrastValue.Text = $"{_settings.Contrast:0.00}";
        GammaValue.Text = $"{_settings.Gamma:0.00}";
        MotionThresholdValue.Text = $"{_settings.MotionThreshold}";
        CenterGuide.Visibility = _settings.CenterGuideEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplySettingsToControls(VisualSettings settings)
    {
        MagnificationSlider.Value = Math.Clamp(settings.Magnification, 1.0, 4.0);
        ContrastSlider.Value = Math.Clamp(settings.Contrast, 0.5, 2.5);
        GammaSlider.Value = Math.Clamp(settings.Gamma, 0.4, 2.2);
        MotionThresholdSlider.Value = Math.Clamp(settings.MotionThreshold, (byte)8, (byte)90);
        EdgeEnhancementCheckBox.IsChecked = settings.EdgeEnhancementEnabled;
        MotionEmphasisCheckBox.IsChecked = settings.MotionEmphasisEnabled;
        CrosshairCheckBox.IsChecked = settings.CenterGuideEnabled;
        ReadSettingsFromControls();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _settings = new VisualSettings();
        _visualProcessor.ResetMotionHistory();
        ApplySettingsToControls(_settings);
        StatusText.Text = "Status: Visual settings reset to accessible defaults.";
    }

    private void UpdateFrameRate()
    {
        _framesSinceMeasurement++;

        if (_framesPerSecondClock.ElapsedMilliseconds < 1000)
        {
            return;
        }

        var framesPerSecond = _framesSinceMeasurement /
                              Math.Max(_framesPerSecondClock.Elapsed.TotalSeconds, 0.001);
        PerformanceText.Text = $"{framesPerSecond:0.0} FPS · {_settings.PreviewWidth}×{_settings.PreviewHeight}";
        _framesSinceMeasurement = 0;
        _framesPerSecondClock.Restart();
    }

    private void StopPreview(string status)
    {
        _previewRunning = false;
        _captureTimer.Stop();
        _visualProcessor.ResetMotionHistory();
        StartButton.Content = "Start Compatibility Preview";
        PerformanceText.Text = "0 FPS";
        StatusText.Text = status;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _captureTimer.Stop();

        try
        {
            _profileStore
                .SaveAsync(DefaultProfileName, _settings)
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // The application must still close if the local profile cannot be saved.
        }
    }
}
