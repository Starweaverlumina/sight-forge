using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SightForge.Services;

public sealed class DesktopCaptureService
{
    public CapturedFrame CapturePrimaryDisplay(int outputWidth, int outputHeight, double magnification)
    {
        if (outputWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputWidth));
        }

        if (outputHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputHeight));
        }

        var screen = Screen.PrimaryScreen
            ?? throw new InvalidOperationException("No primary display is available.");

        var bounds = screen.Bounds;
        magnification = Math.Clamp(magnification, 1.0, 4.0);

        using var desktopBitmap = new Bitmap(
            bounds.Width,
            bounds.Height,
            PixelFormat.Format32bppPArgb);

        using (var desktopGraphics = Graphics.FromImage(desktopBitmap))
        {
            desktopGraphics.CopyFromScreen(
                bounds.Left,
                bounds.Top,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        var cropWidth = Math.Max(1, (int)Math.Round(bounds.Width / magnification));
        var cropHeight = Math.Max(1, (int)Math.Round(bounds.Height / magnification));
        var cropX = Math.Max(0, (bounds.Width - cropWidth) / 2);
        var cropY = Math.Max(0, (bounds.Height - cropHeight) / 2);
        var crop = new Rectangle(cropX, cropY, cropWidth, cropHeight);

        using var outputBitmap = new Bitmap(
            outputWidth,
            outputHeight,
            PixelFormat.Format32bppPArgb);

        using (var outputGraphics = Graphics.FromImage(outputBitmap))
        {
            outputGraphics.CompositingMode = CompositingMode.SourceCopy;
            outputGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            outputGraphics.InterpolationMode = InterpolationMode.Bilinear;
            outputGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            outputGraphics.SmoothingMode = SmoothingMode.None;
            outputGraphics.DrawImage(
                desktopBitmap,
                new Rectangle(0, 0, outputWidth, outputHeight),
                crop,
                GraphicsUnit.Pixel);
        }

        return CopyPixels(outputBitmap);
    }

    private static CapturedFrame CopyPixels(Bitmap bitmap)
    {
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(
            rectangle,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

        try
        {
            var stride = Math.Abs(data.Stride);
            var pixels = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            return new CapturedFrame(bitmap.Width, bitmap.Height, stride, pixels);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}

public sealed record CapturedFrame(int Width, int Height, int Stride, byte[] Pixels);
