using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SightForge.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private readonly Dictionary<int, Action> _actions = new();
    private HwndSource? _source;
    private IntPtr _handle;
    private bool _disposed;

    public void Attach(Window window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _handle = new WindowInteropHelper(window).Handle;
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("The window handle is not ready.");
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
    }

    public bool RegisterCtrlShiftDigit(int id, int digit, Action action)
    {
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("Attach the hotkey service after the window is shown.");
        if (digit is < 1 or > 9) throw new ArgumentOutOfRangeException(nameof(digit));
        ArgumentNullException.ThrowIfNull(action);
        var virtualKey = (uint)('0' + digit);
        if (!RegisterHotKey(_handle, id, ModControl | ModShift, virtualKey)) return false;
        _actions[id] = action;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var id in _actions.Keys) UnregisterHotKey(_handle, id);
        _actions.Clear();
        _source?.RemoveHook(WndProc);
        _source = null;
        _handle = IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
}
