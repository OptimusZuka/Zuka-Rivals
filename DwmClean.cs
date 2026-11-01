using System.Runtime.InteropServices;

namespace IRacingTracker;

/// <summary>Tira a moldura/borda que o Windows 11 desenha em volta das janelas (cantos e contorno).</summary>
internal static class DwmClean
{
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void Apply(IntPtr hwnd)
    {
        try
        {
            int none = unchecked((int)0xFFFFFFFE);   // DWMWA_COLOR_NONE
            DwmSetWindowAttribute(hwnd, 34, ref none, 4);   // DWMWA_BORDER_COLOR
            int noRound = 1;                                 // DWMWCP_DONOTROUND (o painel ja tem cantos proprios)
            DwmSetWindowAttribute(hwnd, 33, ref noRound, 4);
        }
        catch
        {
            // Windows antigo: nao ha moldura para tirar
        }
    }
}
