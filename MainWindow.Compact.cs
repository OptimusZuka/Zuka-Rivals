using System.Windows;

namespace IRacingTracker;

/// <summary>Modo discreto: esconde a barra de baixo e os paineis de filtros, pesquisa e definicoes.</summary>
public partial class MainWindow
{
    private void Compact_Click(object sender, RoutedEventArgs e)
    {
        _settings.Compact = !_settings.Compact;
        ApplyCompact();
        SaveSettings();
    }

    private void ApplyCompact()
    {
        bool c = _settings.Compact;
        FooterBar.Visibility = c ? Visibility.Collapsed : Visibility.Visible;
        if (c)
        {
            EndTyping(false);
            FilterPanel.Visibility = Visibility.Collapsed;
            HistSearchPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
        }
        CompactButton.Content = c ? "\u25B4" : "\u25BE";
        CompactButton.Opacity = c ? 0.35 : 0.6;
    }
}
