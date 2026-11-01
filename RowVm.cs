using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Reutiliza pinceis (um por cor) para evitar notificacoes desnecessarias.</summary>
public static class BrushCache
{
    private static readonly Dictionary<uint, SolidColorBrush> Cache = new();

    public static Brush Argb(byte a, byte r, byte g, byte b)
    {
        uint key = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        if (!Cache.TryGetValue(key, out var brush))
        {
            brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            brush.Freeze();
            Cache[key] = brush;
        }
        return brush;
    }

    public static Brush Rgb(int rgb)
    {
        return Argb(255, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
    }
}

/// <summary>Uma linha da tabela de pilotos.</summary>
public sealed class RowVm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public int CarIdx { get; }
    public int SortKey { get; set; }
    public DriverEntry Driver { get; set; } = new DriverEntry();

    public RowVm(int carIdx)
    {
        CarIdx = carIdx;
    }

    private string _pos = "-";
    public string Pos { get => _pos; set => Set(ref _pos, value); }

    private string _classPos = "-";
    public string ClassPos { get => _classPos; set => Set(ref _classPos, value); }

    private string _num = "";
    public string Num { get => _num; set => Set(ref _num, value); }

    private string _className = "";
    public string ClassName { get => _className; set => Set(ref _className, value); }

    private Brush _classBrush = Brushes.Gray;
    public Brush ClassBrush { get => _classBrush; set => Set(ref _classBrush, value); }

    private string _name = "";
    public string Name { get => _name; set => Set(ref _name, value); }

    private Brush _histBrush = Brushes.Gray;
    public Brush HistBrush { get => _histBrush; set => Set(ref _histBrush, value); }

    private string _histTip = "";
    public string HistTip { get => _histTip; set => Set(ref _histTip, value); }

    private string _tagsKey = "";
    private IReadOnlyList<TagDef> _tags = new List<TagDef>();
    public IReadOnlyList<TagDef> Tags => _tags;

    /// <summary>Atualiza os simbolos do piloto (so notifica a interface se a lista mudou).</summary>
    public void SetTags(List<TagDef> list)
    {
        string key = string.Join(",", list.Select(t => t.Id));
        if (key == _tagsKey) return;
        _tagsKey = key;
        _tags = list;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tags)));
    }

    private string _iRating = "";
    public string IRating { get => _iRating; set => Set(ref _iRating, value); }

    private string _lic = "";
    public string Lic { get => _lic; set => Set(ref _lic, value); }

    private Brush _licBrush = Brushes.Gray;
    public Brush LicBrush { get => _licBrush; set => Set(ref _licBrush, value); }

    private string _laps = "-";
    public string Laps { get => _laps; set => Set(ref _laps, value); }

    private string _gap = "-";
    public string Gap { get => _gap; set => Set(ref _gap, value); }

    private string _tire = "-";
    public string Tire { get => _tire; set => Set(ref _tire, value); }

    private string _tireTip = "";
    public string TireTip { get => _tireTip; set => Set(ref _tireTip, value); }

    private Brush _rowBg = Brushes.Transparent;
    public Brush RowBg { get => _rowBg; set => Set(ref _rowBg, value); }

    private string _numText = "";
    public string NumText { get => _numText; set => Set(ref _numText, value); }

    private ImageSource? _flag;
    public ImageSource? Flag { get => _flag; set => Set(ref _flag, value); }

    private Brush _posBg = Brushes.Transparent;
    public Brush PosBg { get => _posBg; set => Set(ref _posBg, value); }

    private Brush _posFg = Brushes.White;
    public Brush PosFg { get => _posFg; set => Set(ref _posFg, value); }

    private Brush _classPillBg = Brushes.Transparent;
    public Brush ClassPillBg { get => _classPillBg; set => Set(ref _classPillBg, value); }

    private string _licLetter = "";
    public string LicLetter { get => _licLetter; set => Set(ref _licLetter, value); }

    private string _licSr = "";
    public string LicSr { get => _licSr; set => Set(ref _licSr, value); }

    private Brush _licBg = Brushes.Transparent;
    public Brush LicBg { get => _licBg; set => Set(ref _licBg, value); }

    private Brush _tireBrush = Brushes.Gray;
    public Brush TireBrush { get => _tireBrush; set => Set(ref _tireBrush, value); }

    private string _tireMark = "";
    public string TireMark { get => _tireMark; set => Set(ref _tireMark, value); }

    private Brush _bestBrush = Brushes.White;
    public Brush BestBrush { get => _bestBrush; set => Set(ref _bestBrush, value); }

    private string _classTip = "";
    public string ClassTip { get => _classTip; set => Set(ref _classTip, value); }

    private string _best = "-";
    public string Best { get => _best; set => Set(ref _best, value); }

    private string _extra = "";
    public string Extra { get => _extra; set => Set(ref _extra, value); }
}
