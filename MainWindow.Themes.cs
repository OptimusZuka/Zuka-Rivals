using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Um tema: cores, tipo de letra, forma das linhas e que colunas opcionais mostra.</summary>
public sealed class ThemeDef
{
    public string Key = "";
    public string Font = "Segoe UI";
    public int RootRgb, HdrRgb, ColHdrRgb, AccentRgb, LineRgb, MeRgb;
    public byte MeA = 192;             // forca do destaque da tua linha (0-255)
    public byte IdleA;                 // fundo das outras linhas
    public int IdleRgb = 0xFFFFFF;
    public byte PosIdleA;              // fundo da caixa da posicao nas outras linhas
    public int PosIdleRgb = 0xFFFFFF;
    public byte GapA = 0x14;           // fundo da caixa do gap
    public double BgScale = 1.0;       // 1 = fundo normal; menos = mais transparente (HUD)
    public double RowCorner = 2;
    public Thickness RowMargin = new Thickness(0);
    public bool ShowHeaderBg = true, ShowHeaderLine = true, ShowColHeader = true, UpperNames;
    public string[]? Allowed;          // colunas opcionais permitidas (null = todas)
    public double PrefWidth = 940;

    // aspeto dos botoes, paineis e cabecalho (ARGB)
    public uint BtnBg = 0xFF262626, BtnFg = 0xFFD0D0D0, BtnBorder = 0xFF3A3A3A, BtnHoverBorder = 0xFF5A86B0, BtnHoverBg = 0xFF2E2E2E;
    public uint BtnSelBg = 0xFF2B4C7E, BtnSelFg = 0xFFFFFFFF;
    public double BtnCorner = 4, BtnBorderTh = 1, PanelCorner = 4, RootCorner = 8, PillCorner = 3;
    public Thickness BtnPad = new Thickness(10, 3, 10, 3), HdrPad = new Thickness(10, 6, 10, 6);
    public bool BtnBold;
    public uint PanelBg = 0xFF161A20, PanelItemBg = 0xFF1C2027, PanelBorder = 0xFF2A2A2A, TxtBg = 0xFF1C1C1C, TxtBorder = 0xFF3A3A3A;
    public uint FooterBg = 0x00000000, SofFg = 0xFF7FB2F0, PillBg = 0x00000000, PillFg = 0xFF9AA0AA;
}

public static class Themes
{
    public static readonly ThemeDef[] All =
    {
        // A: sobrio, profissional (ambar)
        new ThemeDef { Key = "pitwall", RootRgb = 0x0D0F12, HdrRgb = 0x12151A, ColHdrRgb = 0x1A1F26, AccentRgb = 0xF2B705,
            LineRgb = 0x1A1E24, MeRgb = 0x4A3E10, RowCorner = 2, PrefWidth = 940,
            BtnBg = 0xFF12151A, BtnFg = 0xFF9AA3B2, BtnBorder = 0xFF262B33, BtnHoverBorder = 0xFFF2B705, BtnHoverBg = 0xFF1A1F26,
            BtnSelBg = 0xFFF2B705, BtnSelFg = 0xFF0D0F12, BtnCorner = 3, BtnPad = new Thickness(12, 4, 12, 4), BtnBold = true,
            PanelBg = 0xFF12151A, PanelItemBg = 0xFF1A1F26, PanelBorder = 0xFF262B33, PanelCorner = 3, TxtBg = 0xFF0D0F12, TxtBorder = 0xFF262B33,
            FooterBg = 0xFF12151A, SofFg = 0xFF8FB7F2, PillBg = 0xFFF2B705, PillFg = 0xFF0D0F12, PillCorner = 3, HdrPad = new Thickness(12, 9, 12, 9), RootCorner = 8 },
        // B: cartoes arredondados
        new ThemeDef { Key = "cards", RootRgb = 0x0E1220, HdrRgb = 0x171D33, ColHdrRgb = 0x141A2C, AccentRgb = 0xFFC107,
            LineRgb = 0x0E1220, MeRgb = 0x6B5410, MeA = 205, IdleA = 0x12, PosIdleA = 0x14, GapA = 0x1C,
            RowCorner = 10, RowMargin = new Thickness(0, 2, 0, 2), PrefWidth = 940,
            BtnBg = 0x12FFFFFF, BtnFg = 0xFFE6E9F2, BtnBorder = 0x00FFFFFF, BtnHoverBorder = 0x66FFC107, BtnHoverBg = 0x24FFFFFF,
            BtnSelBg = 0xFF7C5CFF, BtnSelFg = 0xFFFFFFFF, BtnCorner = 15, BtnBorderTh = 0, BtnPad = new Thickness(15, 5, 15, 5), BtnBold = true,
            PanelBg = 0x14FFFFFF, PanelItemBg = 0x14FFFFFF, PanelBorder = 0x14FFFFFF, PanelCorner = 12, TxtBg = 0x14FFFFFF, TxtBorder = 0x24FFFFFF,
            FooterBg = 0x00000000, SofFg = 0xFFB9A8FF, PillBg = 0x2E7C5CFF, PillFg = 0xFFB9A8FF, PillCorner = 12, HdrPad = new Thickness(14, 12, 14, 12), RootCorner = 16 },
        // C: torre de tempos (vermelho escuro de corrida, acento ambar)
        new ThemeDef { Key = "tower", Font = "Bahnschrift SemiCondensed, Segoe UI", RootRgb = 0x101216, HdrRgb = 0x7A2430,
            ColHdrRgb = 0x181B21, AccentRgb = 0xF2B705, LineRgb = 0x101216, MeRgb = 0x4A3E10,
            IdleA = 255, IdleRgb = 0x1A1D23, PosIdleA = 255, PosIdleRgb = 0x2A2E37, GapA = 0x22,
            RowCorner = 0, RowMargin = new Thickness(0, 1, 0, 1), ShowColHeader = false, UpperNames = true,
            Allowed = new[] { "Gap", "Tire" }, PrefWidth = 430,
            BtnBg = 0xFF2A2E37, BtnFg = 0xFFFFFFFF, BtnBorder = 0x00000000, BtnHoverBorder = 0x00000000, BtnHoverBg = 0xFF7A2430,
            BtnSelBg = 0xFF7A2430, BtnSelFg = 0xFFFFFFFF, BtnCorner = 0, BtnBorderTh = 0, BtnPad = new Thickness(14, 5, 14, 5), BtnBold = true,
            PanelBg = 0xFF181B21, PanelItemBg = 0xFF1A1D23, PanelBorder = 0xFF2A2E37, PanelCorner = 0, TxtBg = 0xFF101216, TxtBorder = 0xFF2A2E37,
            FooterBg = 0xFF181B21, SofFg = 0xFFFFFFFF, PillBg = 0xFFFFFFFF, PillFg = 0xFF7A2430, PillCorner = 0, HdrPad = new Thickness(12, 10, 12, 10), RootCorner = 6 },
        // D: HUD minimalista, sem painel
        new ThemeDef { Key = "hud", RootRgb = 0x000000, HdrRgb = 0x000000, ColHdrRgb = 0x000000, AccentRgb = 0xF2B705,
            LineRgb = 0x2A2D32, MeRgb = 0x4A3E10, MeA = 200, BgScale = 0.42, RowCorner = 6, GapA = 0,
            ShowHeaderBg = false, ShowHeaderLine = false, ShowColHeader = false,
            Allowed = new[] { "IRating", "Lic", "Gap" }, PrefWidth = 700,
            BtnBg = 0x00000000, BtnFg = 0xB3FFFFFF, BtnBorder = 0x00000000, BtnHoverBorder = 0x00000000, BtnHoverBg = 0x1AFFFFFF,
            BtnSelBg = 0x33F2B705, BtnSelFg = 0xFFF2B705, BtnCorner = 6, BtnBorderTh = 0, BtnPad = new Thickness(10, 3, 10, 3),
            PanelBg = 0x99000000, PanelItemBg = 0x33FFFFFF, PanelBorder = 0x00000000, PanelCorner = 8, TxtBg = 0x66000000, TxtBorder = 0x33FFFFFF,
            FooterBg = 0x00000000, SofFg = 0xCCFFFFFF, PillBg = 0x00000000, PillFg = 0xFFF2B705, PillCorner = 3, HdrPad = new Thickness(6, 4, 6, 4), RootCorner = 12 },
        // E: telemetria densa, cantos retos, acento verde-agua
        new ThemeDef { Key = "data", RootRgb = 0x14181D, HdrRgb = 0x1B2128, ColHdrRgb = 0x1F262E, AccentRgb = 0x2DD4BF,
            LineRgb = 0x252C35, MeRgb = 0x103A36, MeA = 210, RowCorner = 0, GapA = 0x10, PrefWidth = 1000,
            BtnBg = 0xFF232B34, BtnFg = 0xFFCBD3DD, BtnBorder = 0xFF2F3945, BtnHoverBorder = 0xFF2DD4BF, BtnHoverBg = 0xFF2A333D,
            BtnSelBg = 0xFF1C4D48, BtnSelFg = 0xFF5EEAD4, BtnCorner = 2, BtnPad = new Thickness(10, 2, 10, 2),
            PanelBg = 0xFF1B2128, PanelItemBg = 0xFF232B34, PanelBorder = 0xFF2F3945, PanelCorner = 2, TxtBg = 0xFF14181D, TxtBorder = 0xFF2F3945,
            FooterBg = 0xFF1B2128, SofFg = 0xFF5EEAD4, PillBg = 0xFF232B34, PillFg = 0xFF2DD4BF, PillCorner = 2, HdrPad = new Thickness(10, 6, 10, 6), RootCorner = 4 },
    };

    public static ThemeDef Get(string? key)
    {
        foreach (ThemeDef t in All)
            if (t.Key == key) return t;
        return All[0];
    }
}

public partial class MainWindow
{
    private ThemeDef _theme = Themes.All[0];
    private int _accRgb = 0xF2B705, _rootRgb = 0x0D0F12, _hdrRgb = 0x12151A, _colHdrRgb = 0x1A1F26, _meRgb = 0x4A3E10;

    private static readonly string[] AccentChoices = { "F2B705", "FF8A3D", "F25555", "F472B6", "A78BFA", "5B8DEF", "2DD4BF", "3FB950", "FFFFFF" };
    private static readonly string[] HdrChoices = { "12151A", "7A2430", "8B1E1E", "1E3A8A", "14532D", "4C1D95", "92400E", "0F766E" };
    private static readonly string[] BgChoices = { "0D0F12", "000000", "0B1020", "101A14", "1A1412", "1A1220" };

    private static int ParseHex(string? h, int fallback) =>
        h != null && h.Length == 6 && int.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out int v) ? v : fallback;

    private static int Blend(int a, int b, double t)
    {
        int Ch(int sh) => (int)Math.Round(((a >> sh) & 0xFF) * (1 - t) + ((b >> sh) & 0xFF) * t);
        return (Ch(16) << 16) | (Ch(8) << 8) | Ch(0);
    }

    /// <summary>Caixinhas de cor em Definicoes > Aparencia.</summary>
    private void InitSwatches()
    {
        void Fill(Panel p, string[] list, string kind)
        {
            p.Children.Clear();
            var reset = new Button { Content = "\u21BA", Tag = kind + ":", Width = 24, Height = 24, Padding = new Thickness(0), Margin = new Thickness(0, 0, 6, 4),
                                     ToolTip = Loc.T("tip_color_reset") };
            reset.Click += Swatch_Click;
            p.Children.Add(reset);
            foreach (string hx in list)
            {
                var b = new Button { Tag = kind + ":" + hx, Width = 24, Height = 24, Padding = new Thickness(0), Margin = new Thickness(0, 0, 6, 4),
                                     Background = BrushCache.Rgb(ParseHex(hx, 0)), BorderBrush = BrushCache.Rgb(0x555555) };
                b.Click += Swatch_Click;
                p.Children.Add(b);
            }
        }
        Fill(AccentSwatches, AccentChoices, "a");
        Fill(BgSwatches, BgChoices, "b");
        Fill(HdrSwatches, HdrChoices, "h");
    }

    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string t || t.Length < 2) return;
        string val = t.Substring(2);
        if (t[0] == 'a') _settings.AccentHex = val; else if (t[0] == 'h') _settings.HdrHex = val; else _settings.BgHex = val;
        ApplyTheme(_settings.Theme);
        QueueSave();
    }

    /// <summary>Abrevia o primeiro nome quando o nome e longo (Christopher Raphose -> C. Raphose).</summary>
    private static string ShortName(string n)
    {
        if (n.Length <= 17) return n;
        int i = n.IndexOf(' ');
        return i > 1 && i < n.Length - 2 ? n[0] + ". " + n.Substring(i + 1) : n;
    }
    private Brush _idleBrush = Brushes.Transparent;
    private Brush _posIdleBrush = Brushes.Transparent;

    private static Color RgbColor(int rgb, byte a = 255) =>
        Color.FromArgb(a, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

    private bool ThemeAllows(string key) => _theme.Allowed == null || Array.IndexOf(_theme.Allowed, key) >= 0;

    private Brush MeRowBrush() =>
        BrushCache.Argb((byte)(_rowAlpha * _theme.MeA / 255), (byte)((_meRgb >> 16) & 0xFF),
                        (byte)((_meRgb >> 8) & 0xFF), (byte)(_meRgb & 0xFF));

    private void ApplyTheme(string? key)
    {
        _theme = Themes.Get(key);
        _settings.Theme = _theme.Key;

        // cores personalizadas por cima do tema
        bool ownAcc = ParseHex(_settings.AccentHex, -1) >= 0, ownBg = ParseHex(_settings.BgHex, -1) >= 0, ownHdr = ParseHex(_settings.HdrHex, -1) >= 0;
        _accRgb = ownAcc ? ParseHex(_settings.AccentHex, 0) : _theme.AccentRgb;
        _rootRgb = ownBg ? ParseHex(_settings.BgHex, 0) : _theme.RootRgb;
        _hdrRgb = ownHdr ? ParseHex(_settings.HdrHex, 0) : ownBg ? Blend(_rootRgb, 0xFFFFFF, 0.06) : _theme.HdrRgb;
        _colHdrRgb = ownBg ? Blend(_rootRgb, 0xFFFFFF, 0.09) : _theme.ColHdrRgb;
        _meRgb = ownAcc || ownBg ? Blend(_rootRgb, _accRgb, 0.30) : _theme.MeRgb;
        // contraste do texto sobre a cor de destaque
        double lum = (0.299 * ((_accRgb >> 16) & 0xFF) + 0.587 * ((_accRgb >> 8) & 0xFF) + 0.114 * (_accRgb & 0xFF)) / 255.0;
        uint accSolid = 0xFF000000u | (uint)_accRgb;
        uint onAcc = lum > 0.55 ? 0xFF0D0F12u : 0xFFFFFFFFu;
        uint hdrSolid = 0xFF000000u | (uint)_hdrRgb;

        try { Resources["FontMain"] = new FontFamily(_theme.Font); }
        catch { Resources["FontMain"] = new FontFamily("Segoe UI"); }
        Resources["AccentBrush"] = BrushCache.Rgb(_accRgb);
        Resources["LineBrush"] = BrushCache.Rgb(ownBg ? _rootRgb : _theme.LineRgb);
        Resources["GapBg"] = BrushCache.Argb(_theme.GapA, 255, 255, 255);
        Resources["RowCorner"] = new CornerRadius(_theme.RowCorner);
        Resources["RowMargin"] = _theme.RowMargin;

        _idleBrush = _theme.IdleA == 0 ? Brushes.Transparent
            : BrushCache.Argb(_theme.IdleA, (byte)((_theme.IdleRgb >> 16) & 0xFF), (byte)((_theme.IdleRgb >> 8) & 0xFF), (byte)(_theme.IdleRgb & 0xFF));
        _posIdleBrush = _theme.PosIdleA == 0 ? Brushes.Transparent
            : BrushCache.Argb(_theme.PosIdleA, (byte)((_theme.PosIdleRgb >> 16) & 0xFF), (byte)((_theme.PosIdleRgb >> 8) & 0xFF), (byte)(_theme.PosIdleRgb & 0xFF));

        static Brush B(uint c) => BrushCache.Argb((byte)(c >> 24), (byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
        Resources["BtnBg"] = B(_theme.BtnBg); Resources["BtnFg"] = B(_theme.BtnFg); Resources["BtnBorder"] = B(_theme.BtnBorder);
        Resources["BtnHoverBorder"] = B(ownAcc ? accSolid : _theme.BtnHoverBorder); Resources["BtnHoverBg"] = B(ownHdr && _theme.Key == "tower" ? hdrSolid : _theme.BtnHoverBg);
        Resources["BtnSelBg"] = B(ownAcc ? accSolid : _theme.BtnSelBg); Resources["BtnSelFg"] = B(ownAcc ? onAcc : _theme.BtnSelFg);
        Resources["BtnCorner"] = new CornerRadius(_theme.BtnCorner);
        Resources["BtnBorderTh"] = new Thickness(_theme.BtnBorderTh);
        Resources["BtnPad"] = _theme.BtnPad;
        Resources["BtnWeight"] = _theme.BtnBold ? FontWeights.SemiBold : FontWeights.Normal;
        Resources["PanelBg"] = ownBg ? B(0xFF000000u | (uint)Blend(_rootRgb, 0xFFFFFF, 0.05)) : B(_theme.PanelBg); Resources["PanelItemBg"] = B(_theme.PanelItemBg); Resources["PanelBorder"] = B(_theme.PanelBorder);
        Resources["PanelCorner"] = new CornerRadius(_theme.PanelCorner);
        Resources["TxtBg"] = B(_theme.TxtBg); Resources["TxtBorder"] = B(_theme.TxtBorder);
        Resources["FooterBg"] = B(_theme.FooterBg); Resources["SofFg"] = B(_theme.SofFg);
        Resources["PillBg"] = B(ownAcc ? (0x33000000u | (uint)_accRgb) : _theme.PillBg); Resources["PillFg"] = B(ownAcc ? accSolid : _theme.PillFg);
        Resources["PillCorner"] = new CornerRadius(_theme.PillCorner);
        Resources["HdrPad"] = _theme.HdrPad;
        Resources["RootCorner"] = new CornerRadius(_theme.RootCorner);

        HeaderLine.Visibility = _theme.ShowHeaderLine ? Visibility.Visible : Visibility.Collapsed;
        ColHeader.Visibility = _theme.ShowColHeader ? Visibility.Visible : Visibility.Collapsed;

        ApplyBackgroundOpacity(Math.Min(1.0, Math.Max(0.1, _settings.Opacity)));
        UpdateThemeButtons();
        if (_tabButtons.Length > 0) ShowTab(_tabIdx);
        Button[] lb = { LangBtn0, LangBtn1, LangBtn2, LangBtn3 };
        for (int i = 0; i < lb.Length; i++) SetBtnState(lb[i], Loc.Codes[i] == Loc.Current);
        _centerKey = "";
        RefreshColumns();
    }

    private void UpdateThemeButtons()
    {
        Button[] tb = { ThemeBtn0, ThemeBtn1, ThemeBtn2, ThemeBtn3, ThemeBtn4 };
        foreach (Button b in tb)
        {
            string k = b.Tag as string ?? "";
            b.Content = Loc.T("th_" + k);
            SetBtnState(b, k == _theme.Key);
        }
    }

    /// <summary>Botao ativo/inativo seguindo as cores do tema (atualiza sozinho ao trocar de tema).</summary>
    private static void SetBtnState(Button b, bool on)
    {
        b.SetResourceReference(Control.BackgroundProperty, on ? "BtnSelBg" : "BtnBg");
        b.SetResourceReference(Control.ForegroundProperty, on ? "BtnSelFg" : "BtnFg");
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string key) return;
        ApplyTheme(key);
        if (_theme.PrefWidth > 0) Width = Math.Max(MinWidth, _theme.PrefWidth);
        QueueSave();
    }
}
