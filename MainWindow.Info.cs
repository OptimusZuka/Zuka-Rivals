using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Linha de informacao da sessao, com cores para ler de relance.</summary>
public partial class MainWindow
{
    private static readonly Brush InfoMain = BrushCache.Rgb(0xDADFE8);
    private static readonly Brush InfoSep = BrushCache.Rgb(0x6B7280);
    private static readonly Brush InfoTrack = BrushCache.Rgb(0xFFFFFF);

    private void SetInfo(string kind, string track, string count, string recording, int recState)
    {
        Brush recBrush = recState == 0 ? BrushCache.Rgb(0x3FB950)
                       : recState == 2 ? BrushCache.Rgb(0xE5534B)
                       : BrushCache.Rgb(0xE0B84C);   // sem gravacao / modo teste

        InfoText.Inlines.Clear();
        InfoText.Foreground = InfoMain;
        AddRun(kind, InfoMain, FontWeights.Normal);
        AddRun("  ·  ", InfoSep, FontWeights.Normal);
        AddRun(track, InfoTrack, FontWeights.SemiBold);
        AddRun("  ·  ", InfoSep, FontWeights.Normal);
        AddRun(count, InfoMain, FontWeights.Normal);
        AddRun("  ·  ", InfoSep, FontWeights.Normal);
        AddRun(recording, recBrush, FontWeights.Normal);
        if (_radarText != null)
        {
            AddRun("  ·  ", InfoSep, FontWeights.Normal);
            AddRun(_radarText, BrushCache.Rgb(0xF85149), FontWeights.Bold);
        }
    }

    private void AddRun(string text, Brush brush, FontWeight weight)
    {
        InfoText.Inlines.Add(new Run(text) { Foreground = brush, FontWeight = weight });
    }
}
