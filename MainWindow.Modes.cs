using System.Globalization;

namespace IRacingTracker;

/// <summary>Modos automaticos (treino, qualificacao, corrida) e ordem em tempo real.</summary>
public partial class MainWindow
{
    private enum SessionMode { Practice, Qualify, Race }

    private SessionMode _mode = SessionMode.Race;
    private bool _modeInit;
    private double _greenTime = -1;

    private static SessionMode ModeOf(string sessionType)
    {
        string s = sessionType ?? "";
        if (s.Contains("Race", StringComparison.OrdinalIgnoreCase)) return SessionMode.Race;
        if (s.Contains("Qual", StringComparison.OrdinalIgnoreCase)) return SessionMode.Qualify;
        return SessionMode.Practice;
    }

    private bool _forceMode;

    private void ApplyMode(SessionMode m)
    {
        if (_modeInit && m == _mode && !_forceMode) return;
        _forceMode = false;
        _mode = m;
        _modeInit = true;
        ModeText.Text = m == SessionMode.Race ? Loc.T("mode_race") : m == SessionMode.Qualify ? Loc.T("mode_qual") : Loc.T("mode_prac");
        HdrExtra.Text = m == SessionMode.Qualify ? Loc.T("hdr_diff") : Loc.T("hdr_last");
        HdrExtra.ToolTip = m == SessionMode.Qualify ? Loc.T("tip_diff") : Loc.T("tip_last");
        RefreshColumns();
    }

    /// <summary>
    /// Corrida: posicao ao vivo = voltas completas + distancia na volta atual (so depois de 20 s de bandeira verde;
    /// antes disso e depois da bandeira de xadrez usa a posicao do iRacing).
    /// Treino/qualificacao: ordem pela melhor volta.
    /// </summary>
    private bool ComputeOrder(List<DriverEntry> active, SessionMode mode, int state, double sessionTime,
        float[]? pct, int[]? lapsDone, float[]? best, int[]? surface,
        Dictionary<int, int> pos, Dictionary<int, int> cpos, Dictionary<int, double> classBest)
    {
        pos.Clear();
        cpos.Clear();
        classBest.Clear();

        List<DriverEntry> ordered;
        if (mode == SessionMode.Race)
        {
            bool live = state == 4 && _greenTime >= 0 && sessionTime - _greenTime >= 20.0 && pct != null && lapsDone != null;
            if (!live) return false;

            var list = new List<(DriverEntry D, double Prog)>();
            foreach (var d in active)
            {
                float pc = AtFOr(pct, d.CarIdx, -1f);
                int l = AtOr(lapsDone, d.CarIdx, -1);
                int surf = AtOr(surface, d.CarIdx, 3);
                if (pc < 0f || l < 0 || surf < 0) continue;
                list.Add((d, l + pc));
            }
            ordered = list.OrderByDescending(x => x.Prog).ThenBy(x => x.D.CarIdx).Select(x => x.D).ToList();
        }
        else
        {
            if (best == null) return false;
            var list = new List<(DriverEntry D, double Best)>();
            foreach (var d in active)
            {
                double b = AtFOr(best, d.CarIdx, -1f);
                if (b > 1.0) list.Add((d, b));
            }
            ordered = list.OrderBy(x => x.Best).ThenBy(x => x.D.CarIdx).Select(x => x.D).ToList();
            foreach (var x in list)
                if (!classBest.TryGetValue(x.D.CarClassId, out double cb) || x.Best < cb)
                    classBest[x.D.CarClassId] = x.Best;
        }

        var perClass = new Dictionary<int, int>();
        for (int i = 0; i < ordered.Count; i++)
        {
            DriverEntry d = ordered[i];
            pos[d.CarIdx] = i + 1;
            perClass.TryGetValue(d.CarClassId, out int n);
            perClass[d.CarClassId] = n + 1;
            cpos[d.CarIdx] = n + 1;
        }
        return true;
    }

    /// <summary>Cor do pneu pelo nome que o iRacing da; nomes desconhecidos ficam em cinzento neutro.</summary>
    private static System.Windows.Media.Brush TireBrushFor(string? name, int idx)
    {
        if (idx < 0 || string.IsNullOrEmpty(name)) return BrushCache.Rgb(0x8A8F99);
        string n = name.ToLowerInvariant();
        if (n.Contains("inter")) return BrushCache.Rgb(0x3FB950);                       // intermedio: verde
        if (n.Contains("wet") || n.Contains("rain")) return BrushCache.Rgb(0x3B9CFF);   // chuva: azul
        if (n.Contains("soft") || n.Contains("option") || n.Contains("alternate") || n.Contains("qualif")
            || n.Contains("ultra") || n.Contains("super")) return BrushCache.Rgb(0xE5413B);   // macio: vermelho
        if (n.Contains("medium")) return BrushCache.Rgb(0xF5C518);                      // medio: amarelo
        if (n.Contains("hard") || n.Contains("dry") || n.Contains("slick") || n.Contains("prime")
            || n.Contains("primary") || n.Contains("standard")) return BrushCache.Rgb(0xEDEDED);   // duro/seco: branco
        return BrushCache.Rgb(0x8A8F99);
    }

    private static string FormatLap(double s)
    {
        if (s <= 1.0) return "-";
        int m = (int)(s / 60.0);
        double sec = s - m * 60.0;
        return m.ToString(CultureInfo.InvariantCulture) + ":" + sec.ToString("00.000", CultureInfo.InvariantCulture);
    }
}
