using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IRacingTracker;

/// <summary>Filtros da tabela: pesquisa por nome/numero, historico, tag e classe.</summary>
public partial class MainWindow
{
    private string _search = "";
    private int _fHist;            // 0 = todos, 1 = ja corri contra eles, 2 = nunca
    private long _fTag = -2;       // -2 = todas, -1 = qualquer tag, >0 = id de uma tag
    private int _fClass;           // 0 = todas, senao o id da classe

    private bool FilterActive => _search.Length > 0 || _fHist != 0 || _fTag != -2 || _fClass != 0;

    private bool PassFilter(RowVm r)
    {
        if (r.CarIdx == _session.DriverCarIdx) return true;   // a tua linha aparece sempre
        DriverEntry d = r.Driver;

        if (_search.Length > 0)
        {
            string q = _search.Trim();
            bool byName = d.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase);
            bool byNum = ("#" + d.CarNumber).Contains(q, StringComparison.OrdinalIgnoreCase);
            if (!byName && !byNum) return false;
        }

        if (_fHist != 0)
        {
            bool known = _known.ContainsKey(d.UserId);
            if (_fHist == 1 && !known) return false;
            if (_fHist == 2 && known) return false;
        }

        if (_fTag != -2)
        {
            _driverTags.TryGetValue(d.UserId, out HashSet<long>? ids);
            if (_fTag == -1) { if (ids == null || ids.Count == 0) return false; }
            else if (ids == null || !ids.Contains(_fTag)) return false;
        }

        if (_fClass != 0 && d.CarClassId != _fClass) return false;
        return true;
    }

    private void UpdateFilterUi()
    {
        FilterHistButton.Content = Loc.T("filt_hist", _fHist == 0 ? Loc.T("filt_all_m") : _fHist == 1 ? Loc.T("filt_known") : Loc.T("filt_never"));

        string tagText = Loc.T("filt_all_f");
        if (_fTag == -1) tagText = Loc.T("filt_any");
        else if (_fTag > 0) tagText = _tags.FirstOrDefault(t => t.Id == _fTag)?.Name ?? Loc.T("filt_all_f");
        FilterTagButton.Content = Loc.T("filt_tag", tagText);

        string classText = Loc.T("filt_all_f");
        if (_fClass != 0)
        {
            DriverEntry? any = _session.Drivers.FirstOrDefault(d => d.CarClassId == _fClass);
            classText = any != null ? CarNames.Short(any) : Loc.T("filt_all_f");
        }
        FilterClassButton.Content = Loc.T("filt_class", classText);

        FilterToggleButton.Content = FilterActive ? Loc.T("filters_on") : Loc.T("filters");
    }

    private void FilterToggle_Click(object sender, RoutedEventArgs e)
    {
        if (FilterPanel.Visibility == Visibility.Visible)
        {
            FilterPanel.Visibility = Visibility.Collapsed;
            EndTyping(false);
            return;
        }
        HistSearchPanel.Visibility = Visibility.Collapsed;
        EnsureRoomForPanel();
        FilterPanel.Visibility = Visibility.Visible;
    }

    private void FilterClose_Click(object sender, RoutedEventArgs e)
    {
        FilterPanel.Visibility = Visibility.Collapsed;
        EndTyping(false);
    }

    private void FilterHist_Click(object sender, RoutedEventArgs e)
    {
        _fHist = (_fHist + 1) % 3;
        UpdateFilterUi();
    }

    private void FilterTag_Click(object sender, RoutedEventArgs e)
    {
        var order = new List<long> { -2, -1 };
        order.AddRange(_tags.Select(t => t.Id));
        int i = order.IndexOf(_fTag);
        _fTag = order[(i + 1) % order.Count];
        UpdateFilterUi();
    }

    private void FilterClass_Click(object sender, RoutedEventArgs e)
    {
        var ids = _session.Drivers.Where(d => !d.IsPaceCar && !d.IsSpectator && d.CarClassId != 0)
            .Select(d => d.CarClassId).Distinct().OrderBy(x => x).ToList();
        ids.Insert(0, 0);
        int i = ids.IndexOf(_fClass);
        _fClass = ids[(i + 1) % ids.Count];
        UpdateFilterUi();
    }

    private void FilterClear_Click(object sender, RoutedEventArgs e)
    {
        _fHist = 0;
        _fTag = -2;
        _fClass = 0;
        FilterSearchBox.Text = "";   // dispara TextChanged, que limpa a pesquisa
        _search = "";
        UpdateFilterUi();
    }

    private void FilterSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _search = FilterSearchBox.Text ?? "";
        UpdateFilterUi();
    }

    private void FilterSearch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginTyping();
    }

    private void FilterSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            e.Handled = true;
            EndTyping(true);
        }
    }
}
