using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>
/// Botaozinho no canto do painel que continua clicavel mesmo com o click-through ligado
/// (e uma janela propria: a janela principal deixa o rato passar, esta nao).
/// </summary>
public sealed class ControlPill : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly Border _border;
    private readonly TextBlock _label;

    public event Action? Clicked;

    public ControlPill()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight;

        _label = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

        _border = new Border
        {
            Background = BrushCache.Rgb(0x1E1E1E),
            BorderBrush = BrushCache.Rgb(0x3A3A3A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 2, 8, 2),
            Cursor = Cursors.Hand,
            Child = _label
        };
        _border.MouseLeftButtonUp += (_, _) => Clicked?.Invoke();
        Content = _border;

        SetState(false);

        SourceInitialized += (_, _) =>
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
            DwmClean.Apply(hwnd);
        };
    }

    public void SetState(bool clickThrough)
    {
        if (clickThrough)
        {
            _label.Text = Loc.T("pill_on");
            _label.Foreground = BrushCache.Rgb(0xE3B341);
            _border.BorderBrush = BrushCache.Rgb(0xE3B341);
        }
        else
        {
            _label.Text = Loc.T("pill_off");
            _label.Foreground = BrushCache.Rgb(0x9A9A9A);
            _border.BorderBrush = BrushCache.Rgb(0x3A3A3A);
        }
    }
}
