using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SightForge.Services;

namespace SightForge;

public partial class AccessibilityHubWindow : Window
{
    private readonly AccessibleNarrationService _narrator = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private bool _spokenFocus = true;

    public AccessibilityHubWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => RegisterGlobalHotkeys();
        Loaded += (_, _) =>
        {
            VisionButton.Focus();
            _narrator.Speak(new NarrationRequest("SightForge accessibility hub ready. Use Tab or arrow keys and press Enter to open a feature. Control Shift 1 through 6 work globally.", NarrationPriority.Important));
        };
    }

    private void RegisterGlobalHotkeys()
    {
        try
        {
            _hotkeys.Attach(this);
            _hotkeys.RegisterCtrlShiftDigit(101, 1, () => Dispatcher.Invoke(OpenVision));
            _hotkeys.RegisterCtrlShiftDigit(102, 2, () => Dispatcher.Invoke(OpenSound));
            _hotkeys.RegisterCtrlShiftDigit(103, 3, () => Dispatcher.Invoke(OpenOcr));
            _hotkeys.RegisterCtrlShiftDigit(104, 4, () => Dispatcher.Invoke(OpenGuide));
            _hotkeys.RegisterCtrlShiftDigit(105, 5, () => Dispatcher.Invoke(OpenProfiles));
            _hotkeys.RegisterCtrlShiftDigit(106, 6, () => Dispatcher.Invoke(OpenSetup));
            StatusText.Text = "Global shortcuts active: Control Shift 1 through 6.";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Global shortcuts unavailable: " + exception.Message;
        }
    }

    private void VisionButton_Click(object sender, RoutedEventArgs e) => OpenVision();
    private void SoundButton_Click(object sender, RoutedEventArgs e) => OpenSound();
    private void OcrButton_Click(object sender, RoutedEventArgs e) => OpenOcr();
    private void GuideButton_Click(object sender, RoutedEventArgs e) => OpenGuide();
    private void ProfilesButton_Click(object sender, RoutedEventArgs e) => OpenProfiles();
    private void SetupButton_Click(object sender, RoutedEventArgs e) => OpenSetup();

    private void OpenVision() => OpenChild(new MainWindow(), "VisionForge opened.");
    private void OpenSound() => OpenChild(new SoundForgeWindow(), "SoundForge opened.");
    private void OpenOcr() => OpenChild(new OcrReaderWindow(_narrator), "Screen reader opened.");
    private void OpenGuide() => OpenChild(new GuideForgeWindow(_narrator), "GuideForge opened.");
    private void OpenProfiles() => OpenChild(new ProfileManagerWindow(_narrator), "Profile manager opened.");
    private void OpenSetup() => OpenChild(new AccessibilityWizardWindow(_narrator), "Accessibility setup opened.");

    private void OpenChild(Window window, string announcement)
    {
        window.Owner = this;
        window.Show();
        StatusText.Text = announcement;
        _narrator.Speak(new NarrationRequest(announcement, NarrationPriority.Important));
    }

    private void Control_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (!_spokenFocus || sender is not Button button) return;
        var name = System.Windows.Automation.AutomationProperties.GetName(button);
        if (string.IsNullOrWhiteSpace(name)) name = button.Content?.ToString();
        if (!string.IsNullOrWhiteSpace(name))
            _narrator.Speak(new NarrationRequest(name, NarrationPriority.Normal, true, "focus:" + name));
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1)
        {
            _narrator.Speak(new NarrationRequest("Use Tab or arrow keys to move. Press Enter to open. Press F2 to turn spoken focus on or off. Control Shift 1 opens vision. 2 opens sound. 3 reads the screen. 4 opens game help. 5 opens profiles. 6 opens setup.", NarrationPriority.Important));
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F2)
        {
            _spokenFocus = !_spokenFocus;
            ModeText.Text = $"Spoken focus: {(_spokenFocus ? "on" : "off")}";
            _narrator.Speak(new NarrationRequest($"Spoken focus {(_spokenFocus ? "on" : "off")}", NarrationPriority.Important));
            e.Handled = true;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _hotkeys.Dispose();
        _narrator.Dispose();
    }
}
