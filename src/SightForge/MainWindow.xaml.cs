using System.Windows;

namespace SightForge;

public partial class MainWindow : Window
{
    private bool _previewRunning;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _previewRunning = !_previewRunning;

        StartButton.Content = _previewRunning
            ? "Stop Compatibility Preview"
            : "Start Compatibility Preview";

        StatusText.Text = _previewRunning
            ? "Status: Preview shell active. Screen capture engine is the next implementation step."
            : "Status: Ready. No game-memory access or input automation.";
    }
}
