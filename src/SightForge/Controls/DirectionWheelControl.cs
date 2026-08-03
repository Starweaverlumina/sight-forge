using System.Windows;
using System.Windows.Media;
using SightForge.Models;
using WpfPoint = System.Windows.Point;

namespace SightForge.Controls;

public sealed class DirectionWheelControl : FrameworkElement
{
    private AudioDirectionSnapshot _audio = AudioDirectionSnapshot.Silence;
    private GridMotionResult? _motion;
    private EventCueStyle[] _styles = EventCueStyleDefaults.CreateStandard();

    public DirectionWheelControl()
    {
        Width = 260;
        Height = 260;
        SnapsToDevicePixels = true;
        IsHitTestVisible = false;
    }

    public void SetStyles(IEnumerable<EventCueStyle> styles)
    {
        _styles = styles.Select(style => style.Clone()).ToArray();
        InvalidateVisual();
    }

    public void UpdateAudio(AudioDirectionSnapshot snapshot)
    {
        _audio = snapshot;
        InvalidateVisual();
    }

    public void UpdateMotion(GridMotionResult? motion)
    {
        _motion = motion;
        InvalidateVisual();
    }

    public void Clear()
    {
        _audio = AudioDirectionSnapshot.Silence;
        _motion = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var center = new WpfPoint(width / 2.0, height / 2.0);
        var radius = Math.Max(20, Math.Min(width, height) / 2.0 - 12);

        drawingContext.DrawEllipse(
            new SolidColorBrush(Color.FromArgb(150, 10, 12, 18)),
            new Pen(new SolidColorBrush(Color.FromRgb(126, 137, 156)), 2),
            center,
            radius,
            radius);

        DrawClockTicks(drawingContext, center, radius);
        DrawAudioDirection(drawingContext, center, radius);
        DrawMotionDirection(drawingContext, center, radius);
        DrawCenterLabel(drawingContext, center);
    }

    private static void DrawClockTicks(DrawingContext context, WpfPoint center, double radius)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(190, 210, 215, 225)), 2);
        for (var hour = 0; hour < 12; hour++)
        {
            var angle = ((hour * 30.0) - 90.0) * Math.PI / 180.0;
            var outer = PointAt(center, radius - 5, angle);
            var inner = PointAt(center, hour % 3 == 0 ? radius - 22 : radius - 14, angle);
            context.DrawLine(pen, inner, outer);
        }
    }

    private void DrawAudioDirection(DrawingContext context, WpfPoint center, double radius)
    {
        if (_audio.Timestamp == DateTimeOffset.MinValue || _audio.CombinedLevel <= 0) return;

        var angle = _audio.Direction switch
        {
            HorizontalAudioDirection.Left => -Math.PI,
            HorizontalAudioDirection.Right => 0,
            _ => -Math.PI / 2.0
        };

        var style = FindStyle("general-motion");
        var brush = BrushFrom(style.ColorHex, 205);
        var thickness = Math.Clamp(style.Thickness, 2, 14);
        var confidenceRadius = radius * (0.55 + (0.35 * _audio.Confidence));
        var endpoint = PointAt(center, confidenceRadius, angle);
        drawingContext.DrawLine(new Pen(brush, thickness), center, endpoint);
        drawingContext.DrawEllipse(brush, null, endpoint, 8, 8);
    }

    private void DrawMotionDirection(DrawingContext context, WpfPoint center, double radius)
    {
        if (_motion is null || _motion.Strength <= 0) return;
        var angle = DirectionAngle(_motion.Direction);
        var style = FindStyle("general-motion");
        var brush = BrushFrom(style.ColorHex, 150);
        var inner = radius * 0.62;
        var outer = radius * 0.92;
        var start = PointAt(center, inner, angle);
        var end = PointAt(center, outer, angle);
        context.DrawLine(new Pen(brush, Math.Clamp(style.Thickness, 2, 14)), start, end);
    }

    private void DrawCenterLabel(DrawingContext context, WpfPoint center)
    {
        var label = _audio.Timestamp == DateTimeOffset.MinValue ? "READY" : _audio.ClockPosition.ToUpperInvariant();
        var text = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            14,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        context.DrawText(text, new WpfPoint(center.X - (text.Width / 2.0), center.Y - (text.Height / 2.0)));
    }

    private EventCueStyle FindStyle(string eventId) =>
        _styles.FirstOrDefault(style => style.EventId.Equals(eventId, StringComparison.OrdinalIgnoreCase))
        ?? EventCueStyleDefaults.CreateStandard().Last();

    private static SolidColorBrush BrushFrom(string value, byte fallbackAlpha)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value);
            if (color.A == 255) color.A = fallbackAlpha;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            var brush = new SolidColorBrush(Color.FromArgb(fallbackAlpha, 255, 255, 255));
            brush.Freeze();
            return brush;
        }
    }

    private static double DirectionAngle(GridDirection direction) => direction switch
    {
        GridDirection.TopLeft => -3 * Math.PI / 4,
        GridDirection.TopCenter => -Math.PI / 2,
        GridDirection.TopRight => -Math.PI / 4,
        GridDirection.MiddleLeft => Math.PI,
        GridDirection.Center => -Math.PI / 2,
        GridDirection.MiddleRight => 0,
        GridDirection.BottomLeft => 3 * Math.PI / 4,
        GridDirection.BottomCenter => Math.PI / 2,
        GridDirection.BottomRight => Math.PI / 4,
        _ => -Math.PI / 2
    };

    private static WpfPoint PointAt(WpfPoint center, double radius, double angle) =>
        new(center.X + (Math.Cos(angle) * radius), center.Y + (Math.Sin(angle) * radius));
}
