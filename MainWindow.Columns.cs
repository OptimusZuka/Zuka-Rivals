using System.Windows;
using System.Windows.Controls;

namespace IRacingTracker;

/// <summary>Colunas que podem ser ligadas/desligadas em Definicoes (POS e PILOTO ficam sempre).</summary>
public partial class MainWindow
{
    private static readonly (string Key, string Label, double Width)[] ColumnDefs =
    {
        ("ClassPos", "col_classpos", 34),
        ("Class", "col_class", 68),
        ("Hist", "col_hist", 28),
        ("IRating", "col_ir", 54),
        ("Lic", "col_lic", 84),
        ("Laps", "col_laps", 42),
        ("Gap", "col_gap", 64),
        ("Best", "col_best", 80),
        ("Extra", "col_extra", 72),
        ("Tire", "col_tire", 44),
    };

    /// <summary>Colunas que so fazem sentido num modo: Gap na corrida; Melhor e Dif./Ultima em treino/qualificacao.</summary>
    private bool ModeAllows(string key)
    {
        return key switch
        {
            "Gap" => _mode == SessionMode.Race && ThemeAllows(key),
            "Best" or "Extra" => _mode != SessionMode.Race && ThemeAllows(key),
            _ => ThemeAllows(key)
        };
    }

    // quando a janela encolhe, estas colunas desaparecem por esta ordem (as principais ficam)
    private static readonly string[] HideOrder = { "Laps", "Hist", "Tire", "ClassPos", "IRating", "Class", "Lic", "Extra" };
    private double _scale = 1.0;

    private void RefreshColumns()
    {
        double avail = ActualWidth > 0 ? ActualWidth - 30 : 100000;
        var wanted = ColumnDefs.Where(c => !_settings.HiddenColumns.Contains(c.Key) && ModeAllows(c.Key)).ToList();
        double sum = (5 + 42 + 172) * _scale + wanted.Sum(c => c.Width * _scale);
        var auto = new HashSet<string>();
        foreach (string k in HideOrder)
        {
            if (sum <= avail) break;
            var c = wanted.FirstOrDefault(w => w.Key == k);
            if (c.Key == null) continue;
            sum -= c.Width * _scale;
            auto.Add(k);
        }
        foreach (var c in ColumnDefs)
            ApplyColumn(c.Key, c.Width * _scale,
                !_settings.HiddenColumns.Contains(c.Key) && ModeAllows(c.Key) && !auto.Contains(c.Key));
    }

    private void InitColumns()
    {
        foreach (var c in ColumnDefs)
        {
            string key = c.Key;
            bool visible = !_settings.HiddenColumns.Contains(key);

            var cb = new CheckBox { Content = Loc.T(c.Label), IsChecked = visible, Margin = new Thickness(0, 0, 12, 2) };
            cb.Checked += (_, _) => ColumnToggled(key, true);
            cb.Unchecked += (_, _) => ColumnToggled(key, false);
            ColumnsPanel.Children.Add(cb);
        }
        SizeChanged += (_, _) => { RefreshColumns(); FitHistScroll(); };
        RefreshColumns();
    }

    private void ApplyColumn(string key, double width, bool visible)
    {
        Resources["W_" + key] = new GridLength(visible ? width : 0);
        Resources["V_" + key] = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ColumnToggled(string key, bool visible)
    {
        if (_loading) return;
        if (visible) _settings.HiddenColumns.Remove(key);
        else if (!_settings.HiddenColumns.Contains(key)) _settings.HiddenColumns.Add(key);
        RefreshColumns();
        QueueSave();
    }
}
