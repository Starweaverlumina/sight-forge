using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace SightForge.Services;

public sealed record OcrReadResult(
    string Text,
    IReadOnlyList<string> Lines,
    DateTimeOffset CapturedAt,
    string LanguageTag);

public sealed class WindowsOcrService
{
    private readonly OcrEngine _engine;

    public WindowsOcrService()
    {
        _engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new NotSupportedException("Windows OCR is unavailable for the installed user languages.");
    }

    public async Task<OcrReadResult> RecognizeBgra32Async(
        byte[] pixels,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        var expected = checked(width * height * 4);
        if (pixels.Length < expected) throw new ArgumentException("Pixel buffer is smaller than the BGRA image dimensions.", nameof(pixels));

        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            pixels.AsBuffer(),
            BitmapPixelFormat.Bgra8,
            width,
            height,
            BitmapAlphaMode.Premultiplied);

        var result = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        var lines = result.Lines
            .Select(line => line.Text?.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Cast<string>()
            .ToArray();

        return new OcrReadResult(
            string.Join(Environment.NewLine, lines),
            lines,
            DateTimeOffset.UtcNow,
            _engine.RecognizerLanguage.LanguageTag);
    }
}
