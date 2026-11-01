using System.Globalization;

namespace IRacingTracker;

/// <summary>SOF (forca media do grid) calculado com o iRating dos pilotos da sessao.</summary>
public partial class MainWindow
{
    // Formula de iRacing usada pela comunidade: SOF = k * ln( N / soma(exp(-iR / k)) ), k = 1600 / ln 2
    private static double Sof(IEnumerable<int> ratings)
    {
        var l = ratings.Where(x => x > 0).ToList();
        if (l.Count == 0) return 0;
        double k = 1600.0 / Math.Log(2.0);
        double sum = l.Sum(r => Math.Exp(-r / k));
        return k * Math.Log(l.Count / sum);
    }

    private void UpdateSof(List<DriverEntry> active)
    {
        var real = active.Where(d => !d.IsAI && d.IRating > 0).ToList();
        if (real.Count == 0)
        {
            SofText.Text = "";
            return;
        }

        double all = Sof(real.Select(d => d.IRating));
        string text = "SOF " + Math.Round(all).ToString(CultureInfo.InvariantCulture);

        int myClass = _session.FindDriver(_session.DriverCarIdx)?.CarClassId ?? 0;
        bool multi = real.Select(d => d.CarClassId).Distinct().Count() > 1;
        if (multi && myClass != 0)
        {
            double mine = Sof(real.Where(d => d.CarClassId == myClass).Select(d => d.IRating));
            if (mine > 0) text += Loc.T("sof_class", Math.Round(mine).ToString(CultureInfo.InvariantCulture));
        }
        SofText.Text = text;
    }
}
