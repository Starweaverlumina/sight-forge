using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SightForge.Services;

namespace SightForge;

public partial class AccessibilityHubWindow : Window
{
    private readonly AccessibleNarrationService _narrator = new();
    private bool _spokenFocus = true;

    public AccessibilityHubWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            VisionButton.Focus();
            _narrator.Speak(new NarrationRequest("SightForge accessibility hub ready. Use Tab or arrow keys and press Enter to open a feature.", NarrationPriority.Important));
        };
    }

    private void VisionButton_Click(object sender, RoutedEventArgs e) => OpenChild(new MainWindow(), "VisionForge opened.");
    private void SoundButton_Click(object sender, RoutedEventArgs e) => OpenChild(new SoundForgeWindow(), "SoundForge opened.");
    private void OcrButton_Click(object sender, RoutedEventArgs e) => OpenChild(new OcrReaderWindow(_narrator), "Screen reader opened.");
    private void GuideButton_Click(object sender, RoutedEventArgs e) => OpenChild(new GuideForgeWindow(_narrator), "GuideForge opened.");
    private void ProfilesButton_Click(object sender, RoutedEventArgs e) => OpenChild(new ProfileManagerWindow(_narrator), "Profile manager opened.");
    private void SetupButton_Click(object sender, RoutedEventArgs e) => OpenChild(new AccessibilityWizardWindow(_narrator), "Accessibility setup opened.");

    private void OpenChild(Window window, string announcement)
    {
        window.Owner = this;
        window.Show();
        StatusText.Text = announcement;
        _narrator.Speak(new NarrationRequest(announcement, NarrationPriority.Important));
    }

    private void Control_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_spokenFocus || sender is not Button button) return;
        var name = System.Windows.Automation.AutomationProperties.GetName(button);
        if (string.IsNullOrWhiteSpace(name)) name = button.Content?.ToString();
        if (!string.IsNullOrWhiteSpace(name))
            _narrator.Speak(new NarrationRequest(name, NarrationPriority.Normal, true, "focus:" + name));
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            _narrator.Speak(new NarrationRequest("Use Tab or arrow keys to move. Press Enter to open. Press F2 to turn spoken focus on or off.", NarrationPriority.Important));
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            _spokenFocus = !_spokenFocus;
            ModeText.Text = $"Spoken focus: {(_spokenFocus ? "on" : "off")}";
            _narrator.Speak(new NarrationRequest($"Spoken focus {(_spokenFocus ? "on" : "off")}", NarrationPriority.Important));
            e.Handled = true;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => _narrator.Dispose();
}
