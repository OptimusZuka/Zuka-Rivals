using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace IRacingTracker;

public partial class MainWindow : Window
{
    private readonly IrsdkReader _reader = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(7) };
    private readonly ObservableCollection<RowVm> _rows = new();
    private readonly Dictionary<int, RowVm> _rowByCar = new();
    private readonly AppSettings _settings;

    private HistoryDb? _history;
    private string _historyError = "";

    private object? _yamlRef;
    private ParsedSession _session = new();
    private Dictionary<int, int> _known = new();
    private long _knownForSub = long.MinValue;
    private bool _knownTest;
    private long _subId;
    private long _offlineEpoch;
    private bool _needRecord;
    private int _lastState = -1;
    private int _lastSessionNum = -1;
    private double _lastSessionTime = -1;
    private string _recKey = "";
    private int _recordedThisSession;
    private bool _loading = true;
    private bool? _lastConnected;
    private byte _rowAlpha = 255;
    private readonly Dictionary<int, int> _posMap = new();
    private readonly Dictionary<int, int> _clsMap = new();
    private readonly Dictionary<int, double> _classBest = new();

    private static void SplitLicense(string lic, out string letter, out string sr)
    {
        string s = (lic ?? "").Trim();
        int i = s.IndexOf(' ');
        if (i < 0) { letter = s; sr = ""; return; }
        letter = s.Substring(0, i);
        sr = s.Substring(i + 1).Trim();
    }
    private RowVm? _detailRow;

    // tags (Fase 4)
    private List<TagDef> _tags = new();
    private Dictionary<int, HashSet<long>> _driverTags = new();
    private readonly ObservableCollection<TagChipVm> _chips = new();

    // escrita de notas: o overlay so recebe o teclado durante a escrita
    private bool _typing;
    private IntPtr _prevForeground = IntPtr.Zero;

    public MainWindow()
    {
        _settings = AppSettings.Load();
        Loc.Set(_settings.Language);
        InitializeComponent();
        RowsList.ItemsSource = _rows;
        ApplySettingsToWindow();
        TestModeCheck.IsChecked = _settings.TestMode;
        InitOverlay();
        InitSwatches();
        ApplyTheme(_settings.Theme);
        ApplyLanguage();
        _loading = false;

        ChipsList.ItemsSource = _chips;

        try
        {
            _history = new HistoryDb(AppPaths.DbPath);
            _tags = _history.LoadTags();
            _driverTags = _history.LoadAllDriverTags();
            AutoBackupIfNeeded();
            _settings.TestMode = false;      // versao final: so corridas oficiais
            try { _history?.DeleteTestData(); } catch { }
        }
        catch (Exception ex)
        {
            _historyError = ex.Message;
            AppPaths.Log("Base de dados: " + ex);
        }

        Deactivated += (_, _) =>
        {
            CancelCapture();
            EndTyping(false);
        };
        _timer.Tick += (_, _) => Tick();
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveSettings();
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastText.Text = "";
        };
        LocationChanged += (_, _) => QueueSave();
        SizeChanged += (_, _) => QueueSave();
        Closed += OnClosed;
        _timer.Start();
    }

    // ------------------------------------------------------------------
    //  Janela: nao roubar o foco ao iRacing, posicao/tamanho guardados
    // ------------------------------------------------------------------

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    private void SetNoActivate(bool on)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex = on ? (ex | WS_EX_NOACTIVATE) : (ex & ~WS_EX_NOACTIVATE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }

    /// <summary>Para escrever uma nota o overlay tem de receber o teclado (o iRacing deixa de o receber).</summary>
    private void BeginTyping()
    {
        if (_typing) return;
        _typing = true;
        _prevForeground = GetForegroundWindow();
        SetNoActivate(false);
        Activate();
    }

    /// <summary>Volta ao modo normal (sem roubar o foco) e, se pedido, devolve o teclado ao iRacing.</summary>
    private void EndTyping(bool returnFocus)
    {
        if (!_typing) return;
        _typing = false;
        SetNoActivate(true);
        IntPtr mine = new WindowInteropHelper(this).Handle;
        if (returnFocus && _prevForeground != IntPtr.Zero && _prevForeground != mine)
            SetForegroundWindow(_prevForeground);
        _prevForeground = IntPtr.Zero;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        DwmClean.Apply(hwnd);
        HookAndRegister(hwnd);
    }

    private void ApplySettingsToWindow()
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (_settings.Width >= MinWidth && _settings.Height >= MinHeight)
        {
            Width = _settings.Width;
            Height = _settings.Height;
        }
        if (_settings.HasPosition && IsOnScreen(_settings.Left, _settings.Top, Width, Height))
        {
            Left = _settings.Left;
            Top = _settings.Top;
        }
        else
        {
            Left = 80;
            Top = 80;
        }
    }

    private static bool IsOnScreen(double l, double t, double w, double h)
    {
        double vl = SystemParameters.VirtualScreenLeft;
        double vt = SystemParameters.VirtualScreenTop;
        double vr = vl + SystemParameters.VirtualScreenWidth;
        double vb = vt + SystemParameters.VirtualScreenHeight;
        return l + w > vl + 40 && l < vr - 40 && t + h > vt + 40 && t < vb - 40;
    }

    private void QueueSave()
    {
        if (_loading) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveSettings()
    {
        if (WindowState == WindowState.Normal)
        {
            _settings.Left = Left;
            _settings.Top = Top;
            _settings.Width = Width;
            _settings.Height = Height;
            _settings.HasPosition = true;
        }
        _settings.Save();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _saveTimer.Stop();
        SaveSettings();
        ReleaseHotkeys();
        _pill?.Close();
        _history?.Dispose();
        _reader.Dispose();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
    }

    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
    }

    private void ResizeCorner_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
        Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Toast(string message)
    {
        ToastText.Text = "   " + message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    // ------------------------------------------------------------------
    //  Ciclo principal (5 vezes por segundo)
    // ------------------------------------------------------------------

    private void SetStatus(string text, Brush brush)
    {
        StatusText.Text = text;
        StatusText.Foreground = brush;
    }

    private void Tick()
    {
        bool ok = _reader.Poll();

        // "Mostrar so quando o iRacing esta ligado": esconde/mostra quando a ligacao muda
        bool conn = _reader.Connected;
        if (_lastConnected != conn)
        {
            _lastConnected = conn;
            if (_settings.OnlyWhenConnected)
            {
                if (conn)
                {
                    if (!IsVisible)
                    {
                        Show();
                        Topmost = _settings.AlwaysOnTop;
                    }
                }
                else
                {
                    EndTyping(false);
                    Hide();
                }
            }
        }

        if (!_reader.Connected)
        {
            SetStatus(Loc.T("st_notconn"), Brushes.IndianRed);
            InfoText.Text = "";
            if (_yamlRef != null || _rows.Count > 0) ResetSessionState();
            return;
        }

        if (!ok)
        {
            SetStatus(Loc.T("st_waiting"), Brushes.Goldenrod);
            return;
        }

        SetStatus(Loc.T("st_ok"), Brushes.LimeGreen);
        try
        {
            Update();
        }
        catch (Exception ex)
        {
            AppPaths.Log("Update: " + ex);
            InfoText.Text = Loc.T("err_update", ex.Message);
        }
    }

    private void ResetSessionState()
    {
        _yamlRef = null;
        _session = new ParsedSession();
        _known = new Dictionary<int, int>();
        _knownForSub = long.MinValue;
        _needRecord = false;
        _lastState = -1;
        _lastSessionNum = -1;
        _lastSessionTime = -1;
        _offlineEpoch = 0;
        _recKey = "";
        _centerKey = "";
        _recordedThisSession = 0;
        _rows.Clear();
        _rowByCar.Clear();
        _greenTime = -1;
        SofText.Text = "";
        CloseDetail();
    }

    private int FirstInt(string name, int fallback)
    {
        int[]? a = _reader.Ints(name);
        return a != null && a.Length > 0 ? a[0] : fallback;
    }

    private double FirstDouble(string name, double fallback)
    {
        double[]? a = _reader.Doubles(name);
        return a != null && a.Length > 0 ? a[0] : fallback;
    }

    private static int At(int[]? a, int i)
    {
        return a != null && i >= 0 && i < a.Length ? a[i] : 0;
    }

    private static int AtOr(int[]? a, int i, int fallback)
    {
        return a != null && i >= 0 && i < a.Length ? a[i] : fallback;
    }

    private static float AtFOr(float[]? a, int i, float fallback)
    {
        return a != null && i >= 0 && i < a.Length ? a[i] : fallback;
    }

    // ---- manter a tua linha sempre visivel, a meio da tabela ----

    private string _centerKey = "";

    private readonly Dictionary<int, DateTime> _lastSeen = new();
    private readonly Dictionary<int, (bool Shown, bool Cand, DateTime Since)> _vis = new();
    private readonly Dictionary<int, (int User, int Seq)> _joinSeq = new();
    private int _seqCounter;

    /// <summary>Nome ilegivel: o iRacing grava "??????" quando o alfabeto (ex.: cirilico) nao existe na pagina de codigos.</summary>
    private static bool UnresolvedName(string n)
    {
        foreach (char c in n)
            if (c != '?' && !char.IsWhiteSpace(c)) return false;
        return true;
    }

    /// <summary>Mostrar: so depois de 1 s estavel. Esconder: so depois de 2 s estavel. A 1.a vez e imediata.</summary>
    private bool StableVisible(int car, bool want)
    {
        DateTime now = DateTime.UtcNow;
        if (!_vis.TryGetValue(car, out var st))
        {
            _vis[car] = (want, want, now);
            return want;
        }
        if (want == st.Shown)
        {
            _vis[car] = (st.Shown, want, now);
            return st.Shown;
        }
        if (want != st.Cand) st = (st.Shown, want, now);
        double need = want ? 1.0 : 2.0;
        if ((now - st.Since).TotalSeconds >= need) st = (want, want, now);
        _vis[car] = st;
        return st.Shown;
    }

    /// <summary>Ordem de chegada: quem entra fica no fim da lista, sem posicao ainda (nao salta para o topo).</summary>
    private int JoinSeq(int car, int userId)
    {
        if (!_joinSeq.TryGetValue(car, out var js) || js.User != userId)
        {
            js = (userId, ++_seqCounter);
            _joinSeq[car] = js;
        }
        return js.Seq;
    }

    private readonly Dictionary<int, (int Shown, int Cand)> _posShown = new();
    private readonly Dictionary<int, (int Shown, int Cand)> _cpShown = new();

    private static int Debounce(Dictionary<int, (int Shown, int Cand)> map, int key, int value)
    {
        if (!map.TryGetValue(key, out var st)) st = (value, value);
        if (value != st.Shown)
        {
            if (value == st.Cand) st.Shown = value;
            else st.Cand = value;
        }
        else st.Cand = value;
        map[key] = st;
        return st.Shown;
    }

    private void ScheduleCenter()
    {
        if (!_settings.AutoCenter) return;
        int idx = -1;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].CarIdx == _session.DriverCarIdx)
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return;

        string key = idx + ":" + _rows.Count + ":" + (int)RowsScroll.ViewportHeight;
        if (key == _centerKey) return;
        _centerKey = key;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(CenterOnMe));
    }

    private void CenterOnMe()
    {
        RowVm? me = null;
        foreach (RowVm r in _rows)
            if (r.CarIdx == _session.DriverCarIdx) { me = r; break; }
        if (me == null) return;

        RowsList.UpdateLayout();
        var c = RowsList.ItemContainerGenerator.ContainerFromItem(me) as FrameworkElement;
        double view = RowsScroll.ViewportHeight;
        if (c == null || view <= 0 || c.ActualHeight <= 0)
        {
            _centerKey = "";   // a tabela ainda nao foi desenhada: tenta outra vez no proximo ciclo
            return;
        }

        double top = c.TransformToAncestor(RowsList).Transform(new Point(0, 0)).Y;
        double h = c.ActualHeight;
        double rel = top - RowsScroll.VerticalOffset;   // onde a minha linha esta dentro da vista

        // se ja esta na zona central, nao mexe (evita saltos quando alguem entra ou sai)
        if (rel >= view * 0.25 && rel + h <= view * 0.75) return;

        double target = top + h / 2.0 - view / 2.0;
        RowsScroll.ScrollToVerticalOffset(Math.Max(0.0, Math.Min(target, RowsScroll.ScrollableHeight)));
    }

    private void Update()
    {
        // ---- informacao da sessao (texto YAML, muda raramente) ----
        if (!ReferenceEquals(_yamlRef, _reader.SessionYaml))
        {
            _yamlRef = _reader.SessionYaml;
            _session = SessionParser.Parse(_reader.SessionYaml);
            _needRecord = true;
        }

        int sessionNum = FirstInt("SessionNum", _session.CurrentSessionNum);
        int state = FirstInt("SessionState", 0);
        double sessionTime = FirstDouble("SessionTime", 0);
        bool[]? replayArr = _reader.Bools("IsReplayPlaying");
        bool replay = replayArr != null && replayArr.Length > 0 && replayArr[0];

        // Sessoes offline (contra IA) nao tem SubSessionID: crio um identificador proprio por sessao
        if (_offlineEpoch == 0 || sessionNum != _lastSessionNum || sessionTime < _lastSessionTime - 5.0)
        {
            _offlineEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _needRecord = true;
            _greenTime = state == 4 ? sessionTime : -1;
        }
        _lastSessionNum = sessionNum;
        _lastSessionTime = sessionTime;
        if (state != _lastState)
        {
            _lastState = state;
            _needRecord = true;
            if (state == 4) _greenTime = sessionTime;
        }
        _subId = _session.SubSessionId != 0 ? _session.SubSessionId : -_offlineEpoch;

        bool testMode = _settings.TestMode;
        if (_history != null && (_knownForSub != _subId || _knownTest != testMode))
        {
            _known = _history.LoadKnown(_subId, testMode);
            _knownForSub = _subId;
            _knownTest = testMode;
        }

        SessionEntry? cur = _session.FindSession(sessionNum);
        string sType = cur != null && cur.SessionType.Length > 0 ? cur.SessionType : _session.EventType;

        // ---- telemetria por carro ----
        int[]? pos = _reader.Ints("CarIdxPosition");
        int[]? cpos = _reader.Ints("CarIdxClassPosition");
        int[]? lapsDone = _reader.Ints("CarIdxLapCompleted");
        int[]? tire = _reader.Ints("CarIdxTireCompound");
        float[]? f2 = _reader.Floats("CarIdxF2Time");
        float[]? bestLap = _reader.Floats("CarIdxBestLapTime");
        float[]? lastLap = _reader.Floats("CarIdxLastLapTime");

        var active = new List<DriverEntry>();
        foreach (var d in _session.Drivers)
            if (!d.IsPaceCar && !d.IsSpectator && d.CarIdx >= 0) active.Add(d);

        UpdateSof(active);

        // Dados para o tempo relativo a ti e para esconder carros nos boxes
        float[]? pct = _reader.Floats("CarIdxLapDistPct");
        int[]? surface = _reader.Ints("CarIdxTrackSurface");
        bool[]? onPit = _reader.Bools("CarIdxOnPitRoad");

        DriverEntry? me = _session.FindDriver(_session.DriverCarIdx);
        int myClass = me != null ? me.CarClassId : 0;
        float myPct = AtFOr(pct, _session.DriverCarIdx, -1f);
        double myLap = me != null && me.EstLapTime > 0 ? me.EstLapTime : _session.MyEstLapTime;

        SessionMode mode = ModeOf(sType);
        ApplyMode(mode);
        bool ours = ComputeOrder(active, mode, state, sessionTime, pct, lapsDone, bestLap, surface,
                                 _posMap, _clsMap, _classBest);

        // ---- linhas da tabela ----
        var wanted = new List<RowVm>();
        var seen = new HashSet<int>();
        foreach (var d in active)
        {
            bool isMe = d.CarIdx == _session.DriverCarIdx;

            // Carros nos boxes (ou fora do mundo) nao aparecem; a tua linha aparece sempre
            int surf = AtOr(surface, d.CarIdx, 3);
            bool inPit = onPit != null && d.CarIdx < onPit.Length && onPit[d.CarIdx];
            bool hide = !isMe && (surf < 0 || (_settings.HidePitCars && (inPit || surf == 1 || surf == 2)));
            if (!StableVisible(d.CarIdx, !hide)) continue;

            if (!_rowByCar.TryGetValue(d.CarIdx, out RowVm? r))
            {
                r = new RowVm(d.CarIdx);
                _rowByCar[d.CarIdx] = r;
            }

            int p = At(pos, d.CarIdx);
            int cp = At(cpos, d.CarIdx);
            int laps = AtOr(lapsDone, d.CarIdx, -1);

            // Tempo relativo a ti, em tempo real (como o "relative" do iRacing):
            // + = esta a frente de ti na pista, - = esta atras. Usa a distancia na pista (nunca mais de meia volta).
            // A frente: tempo que tu demoras a chegar la (o teu ritmo). Atras: tempo que ele demora a chegar a ti (ritmo dele).
            string gapText = "-";
            float other = AtFOr(pct, d.CarIdx, -1f);
            if (!isMe && myPct >= 0f && other >= 0f)
            {
                double dp = other - myPct;
                if (dp > 0.5) dp -= 1.0;
                else if (dp < -0.5) dp += 1.0;
                double lap = dp >= 0 ? myLap : (d.EstLapTime > 0 ? d.EstLapTime : myLap);
                if (lap > 0)
                    gapText = (dp * lap).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
            }

            // Posicao: ao vivo (corrida) ou pela melhor volta (treino/qualificacao); senao a do iRacing
            int shownPos = p, shownCp = cp;
            if (ours)
            {
                _posMap.TryGetValue(d.CarIdx, out int lp);
                _clsMap.TryGetValue(d.CarIdx, out int lc);
                if (mode == SessionMode.Race)
                {
                    if (lp > 0) { shownPos = lp; shownCp = lc; }
                }
                else
                {
                    shownPos = lp;
                    shownCp = lc;
                }
            }

            // um valor novo so aparece se se repetir no ciclo seguinte (tira os saltos de 1 ciclo quando alguem entra/sai)
            shownPos = Debounce(_posShown, d.CarIdx, shownPos);
            shownCp = Debounce(_cpShown, d.CarIdx, shownCp);

            double bl = AtFOr(bestLap, d.CarIdx, -1f);
            string bestText = bl > 1.0 ? FormatLap(bl) : "-";
            string extraText = "";
            if (mode == SessionMode.Qualify)
            {
                extraText = "-";
                if (bl > 1.0 && _classBest.TryGetValue(d.CarClassId, out double cb) && bl - cb >= 0.0005)
                    extraText = "+" + (bl - cb).ToString("0.000", CultureInfo.InvariantCulture);
            }
            else if (mode == SessionMode.Practice)
            {
                double ll = AtFOr(lastLap, d.CarIdx, -1f);
                extraText = ll > 1.0 ? FormatLap(ll) : "-";
            }

            r.Driver = d;
            r.SortKey = shownPos > 0 ? shownPos : 1000 + JoinSeq(d.CarIdx, d.UserId);
            r.Pos = shownPos > 0 ? shownPos.ToString(CultureInfo.InvariantCulture) : "-";
            r.ClassPos = shownCp > 0 ? shownCp.ToString(CultureInfo.InvariantCulture) : "-";
            r.Num = d.CarNumber;
            r.NumText = d.CarNumber.Length > 0 ? "#" + d.CarNumber : "";
            r.Flag = FlagArt.Get(d.Flair);
            r.Best = bestText;
            r.Extra = extraText;
            r.ClassName = CarNames.Short(d);
            r.ClassTip = d.CarScreenName.Length > 0 ? d.CarScreenName : d.CarClassShortName;
            r.ClassBrush = d.CarClassColor >= 0 ? BrushCache.Rgb(d.CarClassColor) : BrushCache.Rgb(0x777777);
            r.Name = UnresolvedName(d.DisplayName) ? Loc.T("name_unreadable", d.UserId) : d.DisplayName;
            r.Name = ShortName(r.Name);
            if (_theme.UpperNames) r.Name = r.Name.ToUpperInvariant();
            r.IRating = d.IRating > 0 ? d.IRating.ToString(CultureInfo.InvariantCulture) : "-";
            r.Lic = d.LicString;
            SplitLicense(d.LicString, out string licLetter, out string licSr);
            r.LicLetter = licLetter;
            r.LicSr = licSr;
            r.LicBrush = d.LicColor >= 0 ? BrushCache.Rgb(d.LicColor) : BrushCache.Rgb(0xA0A0A0);
            r.LicBg = d.LicColor >= 0 ? BrushCache.Argb(0x33, (byte)((d.LicColor >> 16) & 0xFF), (byte)((d.LicColor >> 8) & 0xFF), (byte)(d.LicColor & 0xFF)) : Brushes.Transparent;
            r.ClassPillBg = d.CarClassColor >= 0 ? BrushCache.Argb(0x40, (byte)((d.CarClassColor >> 16) & 0xFF), (byte)((d.CarClassColor >> 8) & 0xFF), (byte)(d.CarClassColor & 0xFF)) : BrushCache.Argb(0x40, 0x77, 0x77, 0x77);
            r.PosBg = isMe ? BrushCache.Rgb(_accRgb) : _posIdleBrush;
            r.PosFg = isMe ? BrushCache.Rgb(0x111111) : BrushCache.Rgb(0xF0F0F0);
            r.Laps = laps >= 0 ? laps.ToString(CultureInfo.InvariantCulture) : "-";
            r.Gap = gapText;
            r.SetTags(TagsFor(d.UserId));
            r.RowBg = isMe ? MeRowBrush() : _idleBrush;

            if (isMe)
            {
                r.HistBrush = BrushCache.Argb(0, 0, 0, 0);
                r.HistTip = Loc.T("hist_me");
            }
            else if (_known.TryGetValue(d.UserId, out int n))
            {
                r.HistBrush = BrushCache.Rgb(0x3FB950);
                r.HistTip = Loc.T(n == 1 ? "hist_known1" : "hist_knownN", n);
            }
            else
            {
                r.HistBrush = BrushCache.Rgb(0x5A5A5A);
                r.HistTip = Loc.T("hist_never");
            }

            TireInfo(AtOr(tire, d.CarIdx, -1), d.CarClassId == myClass, out string tireText, out string tireTip);
            r.Tire = tireText;
            r.TireTip = tireTip;
            int tireIdx = AtOr(tire, d.CarIdx, -1);
            string? tireName = null;
            if (tireIdx >= 0) _session.Tires.TryGetValue(tireIdx, out tireName);
            r.TireBrush = TireBrushFor(tireName, tireIdx);
            r.TireMark = tireIdx < 0 ? "-" : tireText.StartsWith("#") ? tireText : "";
            bool purple = mode != SessionMode.Race && bl > 1.0
                          && _classBest.TryGetValue(d.CarClassId, out double pb) && Math.Abs(bl - pb) < 0.0005;
            r.BestBrush = purple ? BrushCache.Rgb(0xC77DFF) : BrushCache.Rgb(0xF0F0F0);

            wanted.Add(r);
            seen.Add(d.CarIdx);
        }

        if (FilterActive)
        {
            wanted = wanted.Where(PassFilter).ToList();
            seen = new HashSet<int>(wanted.Select(w => w.CarIdx));
        }

        DateTime nowT = DateTime.UtcNow;
        foreach (int k in seen) _lastSeen[k] = nowT;
        foreach (int idx in _rowByCar.Keys.Where(k => !seen.Contains(k)).ToList())
        {
            // quem sai da sessao fica mais 2 s (a nao ser que seja o filtro a esconder): evita piscar
            if (!FilterActive && !active.Any(x => x.CarIdx == idx) && _lastSeen.TryGetValue(idx, out DateTime ls) && (nowT - ls).TotalSeconds < 2.0)
            {
                if (!wanted.Contains(_rowByCar[idx])) wanted.Add(_rowByCar[idx]);
                continue;
            }
            _rows.Remove(_rowByCar[idx]);
            _rowByCar.Remove(idx);
        }

        wanted = wanted.OrderBy(x => x.SortKey).ThenBy(x => x.CarIdx).ToList();
        for (int i = 0; i < wanted.Count; i++)
        {
            if (i < _rows.Count)
            {
                if (!ReferenceEquals(_rows[i], wanted[i]))
                {
                    int j = _rows.IndexOf(wanted[i]);
                    if (j >= 0) _rows.Move(j, i);
                    else _rows.Insert(i, wanted[i]);
                }
            }
            else
            {
                _rows.Add(wanted[i]);
            }
        }

        ScheduleCenter();

        // ---- gravar encontros ----
        bool real =sType == "Race" && _session.Official;
        string recKey = _subId.ToString(CultureInfo.InvariantCulture) + ":" + sessionNum.ToString(CultureInfo.InvariantCulture);
        if (recKey != _recKey)
        {
            _recKey = recKey;
            _recordedThisSession = 0;
        }

        string recInfo;
        int recState = 1;   // 0 verde (a gravar), 1 amarelo, 2 vermelho
        if (_history == null) { recInfo = Loc.T("rec_unavail", _historyError); recState = 2; }
        else if (real) { recInfo = Loc.T("rec_official"); recState = 0; }
        else if (testMode) recInfo = Loc.T("rec_test");
        else if (sType == "Race") recInfo = Loc.T("rec_unofficial");
        else recInfo = Loc.T("rec_notcount", sType);

        if (_history != null && _needRecord && state >= 4 && !replay && (real || testMode))
        {
            var ctx = new EncounterContext
            {
                SubSessionId = _subId,
                SessionNum = sessionNum,
                SessionType = sType,
                Official = _session.Official,
                SeriesId = _session.SeriesId,
                Track = _session.TrackName,
                MyUserId = _session.DriverUserId
            };
            _recordedThisSession += _history.RecordEncounters(ctx, _session.Drivers, testMode);
            _needRecord = false;
        }

        SetInfo(sType + (_session.Official ? Loc.T("info_official") : Loc.T("info_unofficial")),
                _session.TrackName,
                Loc.T("info_incars", _rows.Count),
                recInfo + (_recordedThisSession > 0 ? "  " + Loc.T("info_session", _recordedThisSession) : ""),
                recState);
    }

    /// <summary>Tipo de pneu. So mostra o que o iRacing diz: o nome vem da tabela de pneus do TEU carro.</summary>
    private void TireInfo(int idx, bool sameClassAsMe, out string text, out string tip)
    {
        if (idx < 0)
        {
            text = "-";
            tip = Loc.T("tire_none");
            return;
        }

        if (_session.Tires.TryGetValue(idx, out string? name) && name.Length > 0)
        {
            text = name.Substring(0, 1).ToUpperInvariant();
            tip = name;
            return;
        }

        text = "#" + idx.ToString(CultureInfo.InvariantCulture);
        tip = Loc.T("tire_idx", idx);
    }

    // ------------------------------------------------------------------
    //  Detalhe do piloto e botoes
    // ------------------------------------------------------------------

    private List<TagDef> TagsFor(int custId)
    {
        var list = new List<TagDef>();
        if (_driverTags.TryGetValue(custId, out HashSet<long>? ids))
        {
            foreach (TagDef t in _tags)
                if (ids.Contains(t.Id)) list.Add(t);
        }
        return list;
    }

    private void Row_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RowVm row) return;
        if (ReferenceEquals(_detailRow, row))
        {
            CloseDetail();
            return;
        }
        ShowDetail(row);
    }

    // Botao direito: abre o painel do piloto com as tags no topo (um clique liga/desliga cada uma)
    private void Row_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RowVm row) return;
        ShowDetail(row);
    }

    private bool IsMe(DriverEntry d) => d.CarIdx >= 0 && d.CarIdx == _session.DriverCarIdx;

    private void ShowDetail(RowVm row)
    {
        if (!ReferenceEquals(_detailRow, row)) EndTyping(true);
        _detailRow = row;
        DriverEntry d = row.Driver;
        bool isMe = IsMe(d);

        DetailTitle.Text = d.DisplayName + "   ·   Customer ID " + d.UserId;

        _chips.Clear();
        if (!isMe && _history != null)
        {
            _driverTags.TryGetValue(d.UserId, out HashSet<long>? ids);
            foreach (TagDef t in _tags)
                _chips.Add(new TagChipVm(t, ids != null && ids.Contains(t.Id)));
        }
        ChipsList.Visibility = isMe || _history == null ? Visibility.Collapsed : Visibility.Visible;
        NoteArea.Visibility = isMe || _history == null ? Visibility.Collapsed : Visibility.Visible;
        NoteBox.Text = "";

        DetailText.Text = BuildDetailText(row);
        DetailPanel.Visibility = Visibility.Visible;
    }

    private string BuildDetailText(RowVm row)
    {
        DriverEntry d = row.Driver;
        var sb = new StringBuilder();
        if (d.CarIdx >= 0)
            sb.AppendLine(Loc.T("det_car", d.CarScreenName, d.CarClassShortName, d.IRating, d.LicString));

        if (IsMe(d))
        {
            sb.AppendLine(Loc.T("det_me"));
            return sb.ToString().TrimEnd();
        }
        if (_history == null)
        {
            sb.AppendLine(Loc.T("det_unavail", _historyError));
            return sb.ToString().TrimEnd();
        }

        try
        {
            DriverHistory h = _history.GetHistory(d.UserId, _subId, _settings.TestMode);
            if (h.Count == 0)
            {
                sb.AppendLine(Loc.T("det_none"));
            }
            else
            {
                sb.AppendLine(Loc.T("det_prev", h.Count, h.LastSeen));
                foreach (EncounterRow er in h.Recent)
                {
                    string kind = er.IsTest ? Loc.T("det_test") : (er.SessionType == "Race" ? Loc.T("det_race") : er.SessionType);
                    sb.AppendLine("  " + er.When + "  " + kind + "  " + er.Track + "  " + er.Car
                                  + (er.IRating > 0 ? "  iR " + er.IRating : ""));
                }
            }
            if (h.Names.Count > 1) sb.AppendLine(Loc.T("det_names", string.Join(", ", h.Names)));

            List<string> notes = _history.GetNotes(d.UserId, 6);
            if (notes.Count > 0)
            {
                sb.AppendLine(Loc.T("det_notes"));
                foreach (string n in notes) sb.AppendLine("  " + n);
            }

            List<string> log = _history.GetTagLog(d.UserId, 6);
            if (log.Count > 0)
            {
                sb.AppendLine(Loc.T("det_tagchanges"));
                foreach (string l in log) sb.AppendLine("  " + l);
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log("Detalhe: " + ex);
            sb.AppendLine(Loc.T("det_err", ex.Message));
        }

        return sb.ToString().TrimEnd();
    }

    private void Chip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TagChipVm chip) return;
        if (_detailRow == null || _history == null) return;
        DriverEntry d = _detailRow.Driver;
        if (IsMe(d)) return;

        try
        {
            chip.IsOn = _history.ToggleTag(d.UserId, d.DisplayName, chip.Def.Id, _subId, _session.TrackName);
            _driverTags = _history.LoadAllDriverTags();
            DetailText.Text = BuildDetailText(_detailRow);
        }
        catch (Exception ex)
        {
            AppPaths.Log("Tag: " + ex);
            Toast(Loc.T("tag_err_save", ex.Message));
        }
    }

    private void NoteBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginTyping();
    }

    private void NoteBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SaveNote();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            NoteBox.Text = "";
            EndTyping(true);
        }
    }

    private void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        SaveNote();
    }

    private void SaveNote()
    {
        if (_detailRow == null || _history == null) return;
        DriverEntry d = _detailRow.Driver;
        string text = NoteBox.Text.Trim();
        if (text.Length == 0)
        {
            Toast(Loc.T("note_first"));
            return;
        }

        try
        {
            _history.AddNote(d.UserId, d.DisplayName, text, _subId, _session.TrackName);
            NoteBox.Text = "";
            DetailText.Text = BuildDetailText(_detailRow);
            Toast(Loc.T("note_saved"));
        }
        catch (Exception ex)
        {
            AppPaths.Log("Nota: " + ex);
            Toast(Loc.T("note_err", ex.Message));
        }
        EndTyping(true);
    }

    private void CloseDetail()
    {
        EndTyping(true);
        _detailRow = null;
        _chips.Clear();
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        CloseDetail();
    }

    private void TestModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.TestMode = TestModeCheck.IsChecked == true;
        SaveSettings();
        _knownForSub = long.MinValue;
        _needRecord = true;
    }

    private void DeleteTest_Click(object sender, RoutedEventArgs e)
    {
        if (_history == null)
        {
            Toast(Loc.T("hist_unavail"));
            return;
        }
        try
        {
            int n = _history.DeleteTestData();
            _knownForSub = long.MinValue;
            _recordedThisSession = 0;
            _needRecord = false;
            Toast(Loc.T("test_deleted", n));
        }
        catch (Exception ex)
        {
            AppPaths.Log("Apagar testes: " + ex);
            Toast(Loc.T("err_generic", ex.Message));
        }
    }

    private void Dump_Click(object sender, RoutedEventArgs e)
    {
        if (!_reader.Connected || _reader.Vars.Count == 0)
        {
            Toast(Loc.T("dump_notconn"));
            return;
        }

        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string dir = Path.Combine(desktop, "iRacingTracker_dump", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(dir);

            var csv = new StringBuilder("nome;tipo;quantidade;unidade;descricao\n");
            foreach (var v in _reader.Vars.Values.OrderBy(x => x.Name))
                csv.AppendLine(v.Name + ";" + v.Type + ";" + v.Count + ";" + v.Unit + ";" + v.Desc);
            File.WriteAllText(Path.Combine(dir, "variaveis.csv"), csv.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(dir, "sessao.yaml"), _reader.SessionYaml, Encoding.UTF8);

            var tab = new StringBuilder();
            foreach (RowVm r in _rows)
                tab.AppendLine(r.Pos + "\t" + r.ClassPos + "\t" + r.Num + "\t" + r.ClassName + "\t" + r.Name
                               + "\t" + r.IRating + "\t" + r.Lic + "\t" + r.Laps + "\t" + r.Gap + "\t" + r.Tire
                               + "\t" + r.HistTip);
            File.WriteAllText(Path.Combine(dir, "tabela_atual.txt"), tab.ToString(), Encoding.UTF8);

            Toast(Loc.T("dump_saved"));
        }
        catch (Exception ex)
        {
            AppPaths.Log("Dump: " + ex);
            Toast(Loc.T("dump_err", ex.Message));
        }
    }
}
