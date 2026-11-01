using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Fase 6: click-through, atalhos globais, transparencia e janela de definicoes.</summary>
public partial class MainWindow
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_TOGGLE = 1;
    private const int HOTKEY_CLICK = 2;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private ControlPill? _pill;
    private bool _clickThrough;
    private int _capturing;            // 0 = nenhum, 1 = mostrar/ocultar, 2 = click-through
    private bool _toggleRegistered;
    private bool _clickRegistered;

    private void InitOverlay()
    {
        double op = Math.Min(1.0, Math.Max(0.1, _settings.Opacity));
        OpacitySlider.Value = op;
        ApplyBackgroundOpacity(op);
        InitColumns();
        InitSettingsUi();

        _pill = new ControlPill();
        _pill.Clicked += () => SetClickThrough(!_clickThrough);
        _pill.SizeChanged += (_, _) => PositionPill();
        LocationChanged += (_, _) => PositionPill();
        SizeChanged += (_, _) => PositionPill();
        ContentRendered += (_, _) =>
        {
            if (_pill != null && !_pill.IsVisible)
            {
                _pill.Owner = this;
                _pill.Show();
                PositionPill();
            }
        };

        PreviewKeyDown += Window_PreviewKeyDown;
        UpdateHotkeyLabels();
    }

    private void PositionPill()
    {
        if (_pill == null || _pill.ActualWidth <= 0) return;
        _pill.Left = Left + Width - _pill.ActualWidth - 8;
        _pill.Top = Top + 3;
    }

    // ------------------------------------------------------------------
    //  Atalhos globais (funcionam com o iRacing em primeiro plano)
    // ------------------------------------------------------------------

    private void HookAndRegister(IntPtr hwnd)
    {
        HwndSource? src = HwndSource.FromHwnd(hwnd);
        if (src != null) src.AddHook(WndProc);

        _toggleRegistered = RegisterHotKey(hwnd, HOTKEY_TOGGLE, (uint)_settings.ToggleMods | MOD_NOREPEAT, (uint)_settings.ToggleVk);
        _clickRegistered = RegisterHotKey(hwnd, HOTKEY_CLICK, (uint)_settings.ClickMods | MOD_NOREPEAT, (uint)_settings.ClickVk);
        UpdateHotkeyLabels();
    }

    private void ReleaseHotkeys()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        UnregisterHotKey(hwnd, HOTKEY_TOGGLE);
        UnregisterHotKey(hwnd, HOTKEY_CLICK);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == HOTKEY_TOGGLE)
            {
                ToggleVisible();
                handled = true;
            }
            else if (id == HOTKEY_CLICK)
            {
                SetClickThrough(!_clickThrough);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void ToggleVisible()
    {
        if (IsVisible)
        {
            EndTyping(false);
            Hide();
        }
        else
        {
            Show();
            Topmost = _settings.AlwaysOnTop;
        }
    }

    // ------------------------------------------------------------------
    //  Click-through
    // ------------------------------------------------------------------

    private void SetClickThrough(bool on)
    {
        if (on)
        {
            CancelCapture();
            EndTyping(true);
        }

        _clickThrough = on;
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex = on ? (ex | WS_EX_TRANSPARENT) : (ex & ~WS_EX_TRANSPARENT);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);

        if (_pill != null) _pill.SetState(on);
        ClickThroughButton.Content = on ? Loc.T("btn_ct_off") : Loc.T("btn_ct_on");
    }

    private void ClickThroughButton_Click(object sender, RoutedEventArgs e)
    {
        SetClickThrough(!_clickThrough);
    }

    // ------------------------------------------------------------------
    //  Definicoes: transparencia e teclas de atalho
    // ------------------------------------------------------------------

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        double v = Math.Min(1.0, Math.Max(0.1, e.NewValue));
        _settings.Opacity = v;
        ApplyBackgroundOpacity(v);
        QueueSave();
    }

    /// <summary>A transparencia afeta so o fundo; letras, icones e cores ficam sempre nitidos.</summary>
    private void ApplyBackgroundOpacity(double v)
    {
        byte a = (byte)Math.Round(255 * v * _theme.BgScale);
        RootBorder.Background = new SolidColorBrush(RgbColor(_rootRgb, a));
        HeaderBar.Background = new SolidColorBrush(RgbColor(_hdrRgb, _theme.ShowHeaderBg ? (byte)Math.Round(255 * v) : (byte)0));
        ColHeader.Background = new SolidColorBrush(RgbColor(_colHdrRgb, a));
        _rowAlpha = (byte)Math.Round(255 * v);
        OpacityText.Text = Math.Round(v * 100) + "%";
    }

    private void HotkeyToggleButton_Click(object sender, RoutedEventArgs e)
    {
        StartCapture(1);
    }

    private void HotkeyClickButton_Click(object sender, RoutedEventArgs e)
    {
        StartCapture(2);
    }

    private void StartCapture(int which)
    {
        CancelCapture();
        _capturing = which;
        Button b = which == 1 ? HotkeyToggleButton : HotkeyClickButton;
        b.Content = Loc.T("hk_press");
        BeginTyping();
        Focus();
    }

    private void CancelCapture()
    {
        if (_capturing == 0) return;
        _capturing = 0;
        UpdateHotkeyLabels();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturing == 0) return;
        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            CancelCapture();
            EndTyping(true);
            return;
        }
        if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt
            || key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin || key == Key.None)
            return;   // ainda so ha modificadores: espera pela tecla principal

        int mods = 0;
        ModifierKeys m = Keyboard.Modifiers;
        if ((m & ModifierKeys.Alt) != 0) mods |= 1;
        if ((m & ModifierKeys.Control) != 0) mods |= 2;
        if ((m & ModifierKeys.Shift) != 0) mods |= 4;
        if ((m & ModifierKeys.Windows) != 0) mods |= 8;
        if (mods == 0)
        {
            Toast(Loc.T("hk_needmod"));
            return;
        }

        int vk = KeyInterop.VirtualKeyFromKey(key);
        int which = _capturing;
        _capturing = 0;
        ApplyHotkey(which, mods, vk);
        UpdateHotkeyLabels();
        EndTyping(true);
    }

    private void ApplyHotkey(int which, int mods, int vk)
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int otherMods = which == 1 ? _settings.ClickMods : _settings.ToggleMods;
        int otherVk = which == 1 ? _settings.ClickVk : _settings.ToggleVk;
        if (mods == otherMods && vk == otherVk)
        {
            Toast(Loc.T("hk_dup"));
            return;
        }

        int id = which == 1 ? HOTKEY_TOGGLE : HOTKEY_CLICK;
        int oldMods = which == 1 ? _settings.ToggleMods : _settings.ClickMods;
        int oldVk = which == 1 ? _settings.ToggleVk : _settings.ClickVk;

        UnregisterHotKey(hwnd, id);
        bool ok = RegisterHotKey(hwnd, id, (uint)mods | MOD_NOREPEAT, (uint)vk);
        if (ok)
        {
            if (which == 1)
            {
                _settings.ToggleMods = mods;
                _settings.ToggleVk = vk;
                _toggleRegistered = true;
            }
            else
            {
                _settings.ClickMods = mods;
                _settings.ClickVk = vk;
                _clickRegistered = true;
            }
            SaveSettings();
            Toast(Loc.T("hk_saved", Describe(mods, vk)));
        }
        else
        {
            bool back = RegisterHotKey(hwnd, id, (uint)oldMods | MOD_NOREPEAT, (uint)oldVk);
            if (which == 1) _toggleRegistered = back;
            else _clickRegistered = back;
            Toast(Loc.T("hk_inuse"));
        }
    }

    private void UpdateHotkeyLabels()
    {
        HotkeyToggleButton.Content = Describe(_settings.ToggleMods, _settings.ToggleVk) + "   " + Loc.T("hk_change");
        HotkeyClickButton.Content = Describe(_settings.ClickMods, _settings.ClickVk) + "   " + Loc.T("hk_change");
        HotkeyToggleStatus.Text = _toggleRegistered ? Loc.T("hk_active") : Loc.T("hk_notreg");
        HotkeyClickStatus.Text = _clickRegistered ? Loc.T("hk_active") : Loc.T("hk_notreg");
    }

    private static string Describe(int mods, int vk)
    {
        var parts = new List<string>();
        if ((mods & 2) != 0) parts.Add("Ctrl");
        if ((mods & 1) != 0) parts.Add("Alt");
        if ((mods & 4) != 0) parts.Add("Shift");
        if ((mods & 8) != 0) parts.Add("Win");

        string name = KeyInterop.KeyFromVirtualKey(vk).ToString();
        if (name.Length == 2 && name[0] == 'D' && char.IsDigit(name[1])) name = name.Substring(1);
        parts.Add(name);
        return string.Join("+", parts);
    }
}
