using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SightForge.Models;

namespace SightForge;

public partial class CueStyleEditorWindow : Window
{
    private static readonly Regex HexColorPattern = new("^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);
    private readonly VisualSettings _workingSettings;
    private readonly ObservableCollection<CueStyleEditorItem> _items = new();
    private bool _loadingTheme;

    public CueStyleEditorWindow(VisualSettings settings)
    {
        InitializeComponent();
        _workingSettings = settings.Clone();
        LoadStyles(_workingSettings.EventCueStyles);
        SelectTheme(_workingSettings.CueThemeName);
    }

    public VisualSettings EditedSettings => _workingSettings;

    private void LoadStyles(IEnumerable<EventCueStyle> styles)
    {
        _items.Clear();
        foreach (var style in styles)
        {
            _items.Add(new CueStyleEditorItem(style.Clone()));
        }
        CueItemsControl.ItemsSource = _items;
        ValidateAll();
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingTheme || ThemeComboBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var selected = item.Content?.ToString();
        if (selected == "Standard")
        {
            LoadStyles(EventCueStyleDefaults.CreateStandard());
        }
        else if (selected == "High Contrast Color-Blind Safe")
        {
            LoadStyles(EventCueStyleDefaults.CreateHighContrastColorBlindSafe());
        }
    }

    private void RestoreStock_Click(object sender, RoutedEventArgs e)
    {
        LoadStyles(EventCueStyleDefaults.CreateStandard());
        SelectTheme("Standard");
    }

    private void HighContrast_Click(object sender, RoutedEventArgs e)
    {
        LoadStyles(EventCueStyleDefaults.CreateHighContrastColorBlindSafe());
        SelectTheme("High Contrast Color-Blind Safe");
    }

    private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is CueStyleEditorItem item)
        {
            item.RefreshBrush();
            SelectTheme("Custom");
            ValidateAll();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateAll())
        {
            MessageBox.Show(this,
                "One or more colors are invalid. Use #RRGGBB or #AARRGGBB.",
                "Invalid cue color",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _workingSettings.EventCueStyles = _items.Select(item => item.Style.Clone()).ToArray();
        _workingSettings.CueThemeName = GetSelectedThemeName();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool ValidateAll()
    {
        var valid = _items.All(item => HexColorPattern.IsMatch(item.Style.ColorHex ?? string.Empty));
        ValidationText.Text = valid
            ? "All cue colors are valid. Color is reinforced by shape, pulse, and sound."
            : "Invalid color found. Use #RRGGBB or #AARRGGBB.";
        ValidationText.Foreground = valid
            ? new SolidColorBrush(Color.FromRgb(167, 243, 177))
            : new SolidColorBrush(Color.FromRgb(255, 120, 120));
        return valid;
    }

    private void SelectTheme(string themeName)
    {
        _loadingTheme = true;
        foreach (var candidate in ThemeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(candidate.Content?.ToString(), themeName, StringComparison.OrdinalIgnoreCase))
            {
                ThemeComboBox.SelectedItem = candidate;
                _loadingTheme = false;
                return;
            }
        }
        ThemeComboBox.SelectedIndex = 2;
        _loadingTheme = false;
    }

    private string GetSelectedThemeName() =>
        (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Custom";
}

public sealed class CueStyleEditorItem : System.ComponentModel.INotifyPropertyChanged
{
    private Brush _previewBrush = Brushes.Transparent;

    public CueStyleEditorItem(EventCueStyle style)
    {
        Style = style;
        RefreshBrush();
    }

    public EventCueStyle Style { get; }

    public Brush PreviewBrush
    {
        get => _previewBrush;
        private set
        {
            _previewBrush = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(PreviewBrush)));
        }
    }

    public void RefreshBrush()
    {
        try
        {
            PreviewBrush = (Brush)new BrushConverter().ConvertFromString(Style.ColorHex)!;
        }
        catch
        {
            PreviewBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
