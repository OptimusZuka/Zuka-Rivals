using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace IRacingTracker;

/// <summary>Idiomas: pt, en, es, fr. Muda em tempo real (os textos do XAML atualizam sozinhos).</summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static readonly Loc Instance = new Loc();
    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly Dictionary<string, string[]> Table = Build();
    private static int _lang;   // 0 pt, 1 en, 2 es, 3 fr
    public static string[] Codes { get; } = { "pt", "en", "es", "fr" };
    public static string Current => Codes[_lang];

    private static Dictionary<string, string[]> Build()
    {
        var d = new Dictionary<string, string[]>();
        foreach (var r in Strings.All) d[r.Key] = new[] { r.Pt, r.En, r.Es, r.Fr };
        return d;
    }

    /// <summary>"auto" usa o idioma do Windows (pt/en/es/fr, senão inglês).</summary>
    public static void Set(string code)
    {
        if (string.IsNullOrEmpty(code) || code == "auto")
        {
            string two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            code = Array.IndexOf(Codes, two) >= 0 ? two : "en";
        }
        int i = Array.IndexOf(Codes, code);
        _lang = i < 0 ? 1 : i;
        Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] => T(key);

    public static string T(string key)
    {
        if (Table.TryGetValue(key, out var v))
        {
            string s = v[_lang];
            return string.IsNullOrEmpty(s) ? v[0] : s;
        }
        return key;
    }

    public static string T(string key, params object?[] args)
    {
        try { return string.Format(T(key), args); }
        catch { return T(key); }
    }

    public static bool Has(string key) => Table.ContainsKey(key);
}

/// <summary>Uso no XAML: Text="{loc:Tr chave}"</summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var b = new Binding("[" + Key + "]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay
        };
        return b.ProvideValue(serviceProvider);
    }
}
