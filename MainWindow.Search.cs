using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IRacingTracker;

/// <summary>Resultado da pesquisa no historico.</summary>
public sealed class HistHit
{
    public int CustId { get; set; }
    public string Name { get; set; } = "";
    public string LastSeen { get; set; } = "";
    public int Count { get; set; }
    public string Info { get; set; } = "";
}

/// <summary>Pesquisa no historico: acha pilotos que ja sairam da sessao para lhes pores tags e notas.</summary>
public partial class MainWindow
{
    private void HistSearchToggle_Click(object sender, RoutedEventArgs e)
    {
        if (HistSearchPanel.Visibility == Visibility.Visible)
        {
            HistSearchPanel.Visibility = Visibility.Collapsed;
            EndTyping(false);
            return;
        }
        FilterPanel.Visibility = Visibility.Collapsed;
        EnsureRoomForPanel();
        HistSearchPanel.Visibility = Visibility.Visible;
        RunHistSearch();
    }

    private void HistClose_Click(object sender, RoutedEventArgs e)
    {
        HistSearchPanel.Visibility = Visibility.Collapsed;
        EndTyping(false);
    }

    /// <summary>Garante que a janela tem altura para o rodape continuar visivel com um painel aberto.</summary>
    private void EnsureRoomForPanel()
    {
        if (Height < 440) Height = 440;
    }

    /// <summary>A lista de resultados encolhe com a janela, para o rodape nunca sair do ecra.</summary>
    private void FitHistScroll()
    {
        HistScroll.MaxHeight = Math.Max(48, Math.Min(150, (ActualHeight - 330) / 2));
    }

    private void HistSearch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginTyping();
    }

    private void HistSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            e.Handled = true;
            EndTyping(true);
        }
    }

    private void HistSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        RunHistSearch();
    }

    private void RunHistSearch()
    {
        if (HistSearchPanel.Visibility != Visibility.Visible) return;
        if (_history == null)
        {
            HistResults.ItemsSource = null;
            HistSearchHint.Text = Loc.T("hist_unavail");
            return;
        }
        try
        {
            _driverTags = _history.LoadAllDriverTags();
            string q = HistSearchBox.Text ?? "";
            List<HistHit> hits = _history.SearchDrivers(q, 30);
            foreach (HistHit h in hits)
            {
                string last = h.LastSeen.Length >= 10 ? h.LastSeen.Substring(0, 10) : h.LastSeen;
                string info = Loc.T("hs_info", h.Count, last);
                var tags = TagsFor(h.CustId);
                if (tags.Count > 0) info += "  ·  " + string.Join(", ", tags.Select(t => t.Name));
                h.Info = info + "  ·  #" + h.CustId;
            }
            HistResults.ItemsSource = hits;
            HistSearchHint.Text = hits.Count == 0 ? Loc.T("hs_none")
                                : q.Trim().Length == 0 ? Loc.T("hs_hint") : "";
        }
        catch (Exception ex)
        {
            AppPaths.Log("Pesquisa historico: " + ex);
            HistSearchHint.Text = Loc.T("err_generic", ex.Message);
        }
    }

    private void HistResult_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not HistHit h) return;
        var d = new DriverEntry { CarIdx = -1, UserId = h.CustId, UserName = h.Name };
        var row = new RowVm(-1) { Driver = d };
        ShowDetail(row);
    }
}
