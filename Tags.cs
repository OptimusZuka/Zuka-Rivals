using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>Icones vetoriais proprios (grelha 24x24, so tracos finos). Nao dependem de emojis nem de bibliotecas.</summary>
public static class TagIcons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["skull"] = "M12 3.2 C7.4 3.2 4.6 6.4 4.6 10.4 C4.6 12.9 5.8 14.6 7.6 15.6 V19 C7.6 19.4 7.9 19.7 8.3 19.7 H15.7 C16.1 19.7 16.4 19.4 16.4 19 V15.6 C18.2 14.6 19.4 12.9 19.4 10.4 C19.4 6.4 16.6 3.2 12 3.2 Z M8.1 10.8 a1.2 1.2 0 1 0 2.4 0 a1.2 1.2 0 1 0 -2.4 0 M13.5 10.8 a1.2 1.2 0 1 0 2.4 0 a1.2 1.2 0 1 0 -2.4 0 M10.5 19.7 V16.9 M13.5 19.7 V16.9",
        ["shield"] = "M12 3 L19 5.8 V11.4 C19 15.6 16.1 18.9 12 21 C7.9 18.9 5 15.6 5 11.4 V5.8 Z M8.8 12 L11 14.2 L15.4 9.6",
        ["bolt"] = "M13.2 2.8 L5.6 13.4 H11 L10.2 21.2 L18.4 10.2 H13 Z",
        ["warning"] = "M12 4 L21 19.5 H3 Z M12 9.8 V14 M12 16.9 V17",
        ["star"] = "M12 3.4 L14.6 9 L20.6 9.7 L16.1 13.8 L17.4 19.8 L12 16.7 L6.6 19.8 L7.9 13.8 L3.4 9.7 L9.4 9 Z",
        ["repeat"] = "M4 11 V9 A3 3 0 0 1 7 6 H19.5 M16.5 3 L19.5 6 L16.5 9 M20 13 V15 A3 3 0 0 1 17 18 H4.5 M7.5 15 L4.5 18 L7.5 21",
        ["flag"] = "M5 21 V4 M5 4 H17 L14.5 8 L17 12 H5",
        ["heart"] = "M12 20 C5 14.5 3 11 3 8.2 C3 5.6 5 4 7.2 4 C9.2 4 11 5.2 12 7 C13 5.2 14.8 4 16.8 4 C19 4 21 5.6 21 8.2 C21 11 19 14.5 12 20 Z",
        ["eye"] = "M2 12 C5 6.5 8.5 5 12 5 C15.5 5 19 6.5 22 12 C19 17.5 15.5 19 12 19 C8.5 19 5 17.5 2 12 Z M9 12 a3 3 0 1 0 6 0 a3 3 0 1 0 -6 0",
        ["clock"] = "M3 12 a9 9 0 1 0 18 0 a9 9 0 1 0 -18 0 M12 7 V12 L15.5 14",
        ["flame"] = "M12 3 C13 7 17 8.5 17 13.5 C17 17 14.8 20 12 20 C9.2 20 7 17.2 7 14 C7 11.5 8.5 10 9.5 8.5 C10 10 10.8 10.8 11.5 11 C11.8 8 11.2 5.5 12 3 Z",
        ["crown"] = "M3 18 H21 M4 18 L3 7 L8.5 12 L12 5 L15.5 12 L21 7 L20 18",
        ["lock"] = "M6 11 H18 V20 H6 Z M8.5 11 V8 a3.5 3.5 0 0 1 7 0 V11",
        ["target"] = "M3 12 a9 9 0 1 0 18 0 a9 9 0 1 0 -18 0 M7.5 12 a4.5 4.5 0 1 0 9 0 a4.5 4.5 0 1 0 -9 0 M11 12 a1 1 0 1 0 2 0 a1 1 0 1 0 -2 0",
        ["car"] = "M3 14 L5 9 H19 L21 14 V18 H3 Z M3 14 H21 M7 18 V20 M17 18 V20",
        ["handshake"] = "M2 9 H5 V16 H2 Z M22 9 H19 V16 H22 Z M5 10 L9 8.5 L12 10 L15 8.5 L19 10 M5 15 L9 15.5 L12 18 L15 15.5 L19 15 M8.5 12 L12 15.2 M11 11 L14 14",
    };

    private static readonly Dictionary<string, Geometry> Cache = new();

    public static Geometry Get(string key)
    {
        if (!Cache.TryGetValue(key, out Geometry? g))
        {
            string data = Paths.TryGetValue(key, out string? p) ? p : Paths["star"];
            g = Geometry.Parse(data);
            g.Freeze();
            Cache[key] = g;
        }
        return g;
    }
}

/// <summary>Uma tag (simbolo) que se pode atribuir a um piloto.</summary>
public sealed class TagDef
{
    public long Id { get; set; }
    public string Key { get; set; } = "";
    public string DbName { get; set; } = "";
    public string DbDescription { get; set; } = "";

    // as tags de origem mudam de idioma; as personalizadas ficam como foram escritas
    public string Name
    {
        get => IsBuiltin && Loc.Has("tag_" + Key + "_n") ? Loc.T("tag_" + Key + "_n") : DbName;
        set => DbName = value;
    }

    public string Description
    {
        get
        {
            if (IsBuiltin && Loc.Has("tag_" + Key + "_d")) return Loc.T("tag_" + Key + "_d");
            return DbDescription == "Tag personalizada" ? Loc.T("tag_custom_desc") : DbDescription;
        }
        set => DbDescription = value;
    }
    public int Color { get; set; }
    public int SortOrder { get; set; }

    public bool IsBuiltin { get; set; } = true;

    // tags personalizadas: Key = "c<numero>:<icone>"
    public string IconKey => Key.Contains(':') ? Key.Substring(Key.IndexOf(':') + 1) : Key;
    public Geometry Icon => TagIcons.Get(IconKey);
    public Brush Brush => BrushCache.Rgb(Color);
    public string Tooltip => Name + " - " + Description;
}

/// <summary>Botao de uma tag no painel do piloto (ligada/desligada).</summary>
public sealed class TagChipVm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public TagDef Def { get; }

    private bool _isOn;
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOn)));
        }
    }

    public TagChipVm(TagDef def, bool isOn)
    {
        Def = def;
        _isOn = isOn;
    }
}
