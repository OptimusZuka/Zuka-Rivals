using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Ecra de definicoes: separadores, aparencia, comportamento.</summary>
public partial class MainWindow
{
    private FrameworkElement[] _tabPanels = Array.Empty<FrameworkElement>();
    private Button[] _tabButtons = Array.Empty<Button>();

    private void InitSettingsUi()
    {
        _tabPanels = new FrameworkElement[] { TabLook, TabTable, TabBehavior, TabData, TabTags };
        _tabButtons = new[] { TabBtn0, TabBtn1, TabBtn2, TabBtn3, TabBtn4 };

        FontSlider.Value = Math.Min(FontSlider.Maximum, Math.Max(FontSlider.Minimum, _settings.FontSize));
        RowHeightSlider.Value = Math.Min(RowHeightSlider.Maximum, Math.Max(RowHeightSlider.Minimum, _settings.RowHeight));
        ApplyTableSizes();

        TopmostCheck.IsChecked = _settings.AlwaysOnTop;
        HidePitCheck.IsChecked = _settings.HidePitCars;
        CenterCheck.IsChecked = _settings.AutoCenter;
        AutoBackupCheck.IsChecked = _settings.AutoBackup;
        OnlyConnectedCheck.IsChecked = _settings.OnlyWhenConnected;
        AutostartCheck.IsChecked = IsAutostartOn();
        RemoveLegacyWatcher();
        InitTagManager();
        UpdateFilterUi();
        Topmost = _settings.AlwaysOnTop;

        ShowTab(0);
    }

    private int _tabIdx;

    private void ShowTab(int index)
    {
        _tabIdx = index;
        for (int i = 0; i < _tabPanels.Length; i++)
        {
            _tabPanels[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
            SetBtnState(_tabButtons[i], i == index);
        }
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag as string, out int i)) ShowTab(i);
    }

    private void ApplyTableSizes()
    {
        double f = Math.Round(FontSlider.Value);
        double rowH = Math.Max(Math.Round(RowHeightSlider.Value), f + 12);
        _scale = f / 14.0;
        Resources["BaseFont"] = f;
        Resources["SmallFont"] = Math.Max(9.0, f - 2);
        Resources["HdrFont"] = Math.Max(10.0, f - 2);
        Resources["IconSz"] = f + 1;
        Resources["BadgeSz"] = f + 8;
        Resources["FlagW"] = Math.Round(f * 1.6);
        Resources["FlagH"] = Math.Round(f * 1.1);
        Resources["HistSz"] = Math.Round(f * 0.8);
        Resources["NameMin"] = 172.0 * _scale;
        Resources["NameMax"] = 125.0 * _scale;
        Resources["NumMin"] = 40.0 * _scale;
        Resources["RowH"] = rowH;
        Resources["W_Pos"] = new GridLength(42.0 * _scale);
        FontText.Text = f + " pt";
        RowHeightText.Text = Math.Round(RowHeightSlider.Value) + " px";
        _centerKey = "";
        RefreshColumns();
    }

    private void FontSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _settings.FontSize = Math.Round(FontSlider.Value);
        ApplyTableSizes();
        QueueSave();
    }

    private void RowHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _settings.RowHeight = Math.Round(RowHeightSlider.Value);
        ApplyTableSizes();
        QueueSave();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.AlwaysOnTop = TopmostCheck.IsChecked == true;
        _settings.HidePitCars = HidePitCheck.IsChecked == true;
        _settings.AutoCenter = CenterCheck.IsChecked == true;
        _settings.AutoBackup = AutoBackupCheck.IsChecked == true;
        _settings.OnlyWhenConnected = OnlyConnectedCheck.IsChecked == true;
        Topmost = _settings.AlwaysOnTop;
        _centerKey = "";
        SaveSettings();
    }
}
