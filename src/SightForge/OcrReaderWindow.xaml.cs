using System.Windows;
using System.Windows.Input;
using SightForge.Services;

namespace SightForge;

public partial class OcrReaderWindow : Window
{
    private readonly AccessibleNarrationService _narrator;
    private readonly DesktopCaptureService _capture = new();
    private readonly WindowsOcrService _ocr = new();

    public OcrReaderWindow(AccessibleNarrationService narrator)
    {
        InitializeComponent();
        _narrator = narrator;
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    private async Task ScanAsync()
    {
        try
        {
            StatusText.Text = "Capturing and recognizing visible text...";
            var frame = await Task.Run(() => _capture.CapturePrimaryDisplay(1280, 720, 1.0));
            var result = await _ocr.RecognizeBgra32Async(frame.Pixels, frame.Width, frame.Height);
            ResultText.Text = string.IsNullOrWhiteSpace(result.Text) ? "No readable text was found." : result.Text;
            StatusText.Text = $"Read {result.Lines.Count} lines using {result.LanguageTag}.";
            if (!string.IsNullOrWhiteSpace(result.Text))
                _narrator.Speak(new NarrationRequest(result.Text, NarrationPriority.Important));
        }
        catch (Exception exception)
        {
            StatusText.Text = "Screen reading failed: " + exception.Message;
            _narrator.Speak(new NarrationRequest("Screen reading failed.", NarrationPriority.Important));
        }
    }

    private void SpeakButton_Click(object sender, RoutedEventArgs e) =>
        _narrator.Speak(new NarrationRequest(ResultText.Text, NarrationPriority.Important));

    private void StopButton_Click(object sender, RoutedEventArgs e) => _narrator.Stop();

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            await ScanAsync();
            e.Handled = true;
        }
    }
}
