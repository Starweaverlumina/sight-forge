using System.Windows;

namespace SightForge;

public partial class MainWindow : Window
{
    private bool _previewRunning;

    private double _magnification = 1.5;
    private double _contrast = 1;
    private double _gamma = 1;
    private bool _edgeEnhancementEnabled;
    private bool _motionEmphasisEnabled;
    private bool _crosshairEnabled = true;

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
            ? "Status: UI shell active. Screen capture engine is not implemented yet, so no preview is rendered."
            : "Status: Ready. No game-memory access or input automation.";
    }

    private void MagnificationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _magnification = e.NewValue;
    }

    private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _contrast = e.NewValue;
    }

    private void GammaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _gamma = e.NewValue;
    }

    private void EdgeEnhancementCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _edgeEnhancementEnabled = EdgeEnhancementCheckBox.IsChecked == true;
    }

    private void MotionEmphasisCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _motionEmphasisEnabled = MotionEmphasisCheckBox.IsChecked == true;
    }

    private void CrosshairCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _crosshairEnabled = CrosshairCheckBox.IsChecked == true;
    }
}
