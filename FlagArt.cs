using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace IRacingTracker;

/// <summary>
/// Bandeiras desenhadas em vetor (versoes simplificadas, 30x20). O iRacing indica o pais em FlairName;
/// paises que nao estao nesta lista aparecem sem bandeira.
/// Formato: camadas separadas por ';'  ->  H/V cor[*peso],...  |  R x y w h cor  |  C x y r cor  |  P cor caminho
/// </summary>
public static class FlagArt
{
    private const string UnionJack =
        "R 0 0 30 20 012169; P FFFFFF M0,0 L4,0 L30,17.3 L30,20 L26,20 L0,2.7 Z M30,0 L26,0 L0,17.3 L0,20 L4,20 L30,2.7 Z; " +
        "P C8102E M0,0 L2,0 L30,18.7 L30,20 L28,20 L0,1.3 Z M30,0 L28,0 L0,18.7 L0,20 L2,20 L30,1.3 Z; " +
        "R 0 7 30 6 FFFFFF; R 12 0 6 20 FFFFFF; R 0 8.3 30 3.4 C8102E; R 13.3 0 3.4 20 C8102E";

    private const string Cross = "R 0 0 30 20 {0}; R 0 {1} 30 {2} {3}; R {4} 0 {2} 20 {3}";

    private static readonly Dictionary<string, string> Specs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Portugal"] = "V 006600*2,FF0000*3; C 12 10 5.1 FFD700; C 12 10 4.1 E8B800; P C8102E M9.2,7.2 L14.8,7.2 L14.8,11.2 Q14.8,13.7 12,14.8 Q9.2,13.7 9.2,11.2 Z; P FFFFFF M10,8 L14,8 L14,11.1 Q14,12.9 12,13.8 Q10,12.9 10,11.1 Z; R 10.9 8.9 2.2 2.2 1C3F94",
        ["Spain"] = "H C60B1E,FFC400*2,C60B1E; R 5 7.4 4.4 5.4 8B1A1A; R 5.4 7.8 3.6 4.6 C9A227; R 6.2 8.8 2 2.6 C60B1E",
        ["France"] = "V 0055A4,FFFFFF,EF4135",
        ["Germany"] = "H 000000,DD0000,FFCE00",
        ["Italy"] = "V 009246,FFFFFF,CE2B37",
        ["Netherlands"] = "H AE1C28,FFFFFF,21468B",
        ["Belgium"] = "V 000000,FAE042,ED2939",
        ["Austria"] = "H ED2939,FFFFFF,ED2939",
        ["Switzerland"] = "R 0 0 30 20 FF0000; R 12.5 4 5 12 FFFFFF; R 9 7.5 12 5 FFFFFF",
        ["Ireland"] = "V 169B62,FFFFFF,FF883E",
        ["Luxembourg"] = "H EF3340,FFFFFF,00A3E0",
        ["Poland"] = "H FFFFFF,DC143C",
        ["Ukraine"] = "H 0057B7,FFD700",
        ["Russia"] = "H FFFFFF,0039A6,D52B1E",
        ["Finland"] = "R 0 0 30 20 FFFFFF; R 0 7.5 30 5 003580; R 8 0 5 20 003580",
        ["Sweden"] = "R 0 0 30 20 006AA7; R 0 8 30 4 FECC00; R 9 0 4 20 FECC00",
        ["Norway"] = "R 0 0 30 20 BA0C2F; R 0 7.5 30 5 FFFFFF; R 7.5 0 5 20 FFFFFF; R 0 8.5 30 3 00205B; R 8.5 0 3 20 00205B",
        ["Denmark"] = "R 0 0 30 20 C8102E; R 0 8 30 4 FFFFFF; R 9 0 4 20 FFFFFF",
        ["Iceland"] = "R 0 0 30 20 02529C; R 0 7.5 30 5 FFFFFF; R 7.5 0 5 20 FFFFFF; R 0 8.5 30 3 DC1E35; R 8.5 0 3 20 DC1E35",
        ["United Kingdom"] = UnionJack,
        ["England"] = "R 0 0 30 20 FFFFFF; R 0 8 30 4 CE1124; R 13 0 4 20 CE1124",
        ["Scotland"] = "R 0 0 30 20 0065BD; P FFFFFF M0,0 L4,0 L30,17.3 L30,20 L26,20 L0,2.7 Z M30,0 L26,0 L0,17.3 L0,20 L4,20 L30,2.7 Z",
        ["United States"] = "H B22234,FFFFFF,B22234,FFFFFF,B22234,FFFFFF,B22234,FFFFFF,B22234,FFFFFF,B22234,FFFFFF,B22234; R 0 0 13 10.8 3C3B6E; C 1.7 1.8 0.55 FFFFFF; C 4.1 1.8 0.55 FFFFFF; C 6.5 1.8 0.55 FFFFFF; C 8.9 1.8 0.55 FFFFFF; C 11.3 1.8 0.55 FFFFFF; C 1.7 4.1 0.55 FFFFFF; C 4.1 4.1 0.55 FFFFFF; C 6.5 4.1 0.55 FFFFFF; C 8.9 4.1 0.55 FFFFFF; C 11.3 4.1 0.55 FFFFFF; C 1.7 6.4 0.55 FFFFFF; C 4.1 6.4 0.55 FFFFFF; C 6.5 6.4 0.55 FFFFFF; C 8.9 6.4 0.55 FFFFFF; C 11.3 6.4 0.55 FFFFFF; C 1.7 8.7 0.55 FFFFFF; C 4.1 8.7 0.55 FFFFFF; C 6.5 8.7 0.55 FFFFFF; C 8.9 8.7 0.55 FFFFFF; C 11.3 8.7 0.55 FFFFFF",
        ["Canada"] = "V FF0000,FFFFFF*2,FF0000; P FF0000 M15,3.2 L16.3,6.2 L18.6,5.4 L17.6,9 L19.6,9.8 L16.4,12.4 L16.7,14.4 L15.4,14.1 L15.4,16.8 L14.6,16.8 L14.6,14.1 L13.3,14.4 L13.6,12.4 L10.4,9.8 L12.4,9 L11.4,5.4 L13.7,6.2 Z",
        ["Brazil"] = "R 0 0 30 20 009C3B; P FFDF00 M15,2 L28,10 L15,18 L2,10 Z; C 15 10 4.5 002776; P FFFFFF M10.8,9.3 Q15,8.1 19.4,11.1 L19.2,12 Q15,9.4 10.9,10.2 Z",
        ["Argentina"] = "H 74ACDF,FFFFFF,74ACDF; C 15 10 2 F6B40E",
        ["Chile"] = "H FFFFFF,D52B1E; R 0 0 10 10 0039A6",
        ["Colombia"] = "H FCD116*2,003893,CE1126",
        ["Mexico"] = "V 006847,FFFFFF,CE1126; C 15 10 2.5 8B6B3B",
        ["Venezuela"] = "H FFCC00,00247D,CF142B",
        ["Uruguay"] = "H FFFFFF,0038A8,FFFFFF,0038A8,FFFFFF,0038A8,FFFFFF,0038A8,FFFFFF; R 0 0 11 11 FFFFFF; C 5.5 5.5 2.5 FCD116",
        ["Peru"] = "V D91023,FFFFFF,D91023",
        ["Ecuador"] = "H FFDD00*2,034EA2,ED1C24",
        ["Bolivia"] = "H D52B1E,F9E300,007934",
        ["Paraguay"] = "H D52B1E,FFFFFF,0038A8",
        ["Costa Rica"] = "H 002B7F,FFFFFF,CE1126*2,FFFFFF,002B7F",
        ["Guatemala"] = "V 4997D0,FFFFFF,4997D0",
        ["Cuba"] = "H 002A8F,FFFFFF,002A8F,FFFFFF,002A8F; P CF142B M0,0 L15,10 L0,20 Z",
        ["Puerto Rico"] = "H ED0000,FFFFFF,ED0000,FFFFFF,ED0000; P 0050F0 M0,0 L13,10 L0,20 Z",
        ["Japan"] = "R 0 0 30 20 FFFFFF; C 15 10 6 BC002D",
        ["China"] = "R 0 0 30 20 DE2910; C 6 6 3 FFDE00",
        ["Taiwan"] = "R 0 0 30 20 FE0000; R 0 0 15 10 000095; C 7.5 5 3 FFFFFF",
        ["South Korea"] = "R 0 0 30 20 FFFFFF; C 15 10 5 CD2E3A; P 0047A0 M10,10 A5,5 0 0 0 20,10 Z",
        ["India"] = "H FF9933,FFFFFF,138808; C 15 10 2 000080",
        ["Indonesia"] = "H FF0000,FFFFFF",
        ["Monaco"] = "H CE1126,FFFFFF",
        ["Singapore"] = "H ED2939,FFFFFF; C 6 5 3 FFFFFF; C 7.4 5 2.6 ED2939",
        ["Thailand"] = "H A51931,F4F5F8,2D2A4A*2,F4F5F8,A51931",
        ["Malaysia"] = "H CC0001,FFFFFF,CC0001,FFFFFF,CC0001,FFFFFF,CC0001; R 0 0 15 10 010066",
        ["Philippines"] = "H 0038A8,CE1126; P FFFFFF M0,0 L15,10 L0,20 Z",
        ["Vietnam"] = "R 0 0 30 20 DA251D; C 15 10 4 FFFF00",
        ["Australia"] = "R 0 0 30 20 00247D; R 0 4 15 2 FFFFFF; R 6.5 0 2 10 FFFFFF; R 0 4.6 15 0.8 CF142B; R 7.1 0 0.8 10 CF142B; C 22 6 1.2 FFFFFF; C 22 15 1.2 FFFFFF; C 26 10 1.2 FFFFFF; C 18 11 1.2 FFFFFF",
        ["New Zealand"] = "R 0 0 30 20 00247D; R 0 4 15 2 FFFFFF; R 6.5 0 2 10 FFFFFF; R 0 4.6 15 0.8 CC142B; R 7.1 0 0.8 10 CC142B; C 22 6 1.2 CC142B; C 22 15 1.2 CC142B; C 26 10 1.2 CC142B; C 18 11 1.2 CC142B",
        ["South Africa"] = "H DE3831,002395; P 007749 M0,0 L10,10 L0,20 Z",
        ["Turkey"] = "R 0 0 30 20 E30A17; C 11 10 5 FFFFFF; C 12.5 10 4 E30A17",
        ["Greece"] = "H 0D5EAF,FFFFFF,0D5EAF,FFFFFF,0D5EAF,FFFFFF,0D5EAF,FFFFFF,0D5EAF; R 0 0 11 11 0D5EAF; R 4.5 0 2 11 FFFFFF; R 0 4.5 11 2 FFFFFF",
        ["Czech Republic"] = "H FFFFFF,D7141A; P 11457E M0,0 L15,10 L0,20 Z",
        ["Slovakia"] = "H FFFFFF,0B4EA2,EE1C25",
        ["Hungary"] = "H CE2939,FFFFFF,477050",
        ["Romania"] = "V 002B7F,FCD116,CE1126",
        ["Bulgaria"] = "H FFFFFF,00966E,D62612",
        ["Serbia"] = "H C6363C,0C4076,FFFFFF",
        ["Croatia"] = "H FF0000,FFFFFF,171796",
        ["Slovenia"] = "H FFFFFF,005DA4,ED1C24",
        ["Lithuania"] = "H FDB913,006A44,C1272D",
        ["Latvia"] = "H 9E3039*2,FFFFFF,9E3039*2",
        ["Estonia"] = "H 0072CE,000000,FFFFFF",
        ["Moldova"] = "V 0046AE,FFD200,CC092F",
        ["Malta"] = "V FFFFFF,CF142B",
        ["Georgia"] = "R 0 0 30 20 FFFFFF; R 0 8 30 4 FF0000; R 13 0 4 20 FF0000",
        ["Armenia"] = "H D90012,0033A0,F2A800",
        ["Azerbaijan"] = "H 00B5E2,EF3340,509E2F",
        ["Kazakhstan"] = "R 0 0 30 20 00AFCA; C 15 10 4 FEC50C",
        ["Israel"] = "R 0 0 30 20 FFFFFF; R 0 2.5 30 2.5 0038B8; R 0 15 30 2.5 0038B8; C 15 10 3 0038B8; C 15 10 2 FFFFFF",
        ["Palestine"] = "H 000000,FFFFFF,009736; P E4312B M0,0 L12,10 L0,20 Z",
        ["Saudi Arabia"] = "R 0 0 30 20 006C35; R 7 11 16 1.5 FFFFFF",
        ["United Arab Emirates"] = "H 00732F,FFFFFF,000000; R 0 0 7.5 20 FF0000",
        ["Qatar"] = "R 0 0 30 20 8A1538; P FFFFFF M0,0 L9,0 L11,2 L9,4 L11,6 L9,8 L11,10 L9,12 L11,14 L9,16 L11,18 L9,20 L0,20 Z",
        ["Kuwait"] = "H 007A3D,FFFFFF,CE1126; P 000000 M0,0 L8,6.5 L8,13.5 L0,20 Z",
        ["Egypt"] = "H CE1126,FFFFFF,000000",
        ["Morocco"] = "R 0 0 30 20 C1272D; C 15 10 4 006233",
        ["Nigeria"] = "V 008751,FFFFFF,008751",
        ["Iran"] = "H 239F40,FFFFFF,DA0000",
        ["Iraq"] = "H CE1126,FFFFFF,000000",
        ["Pakistan"] = "R 0 0 30 20 01411C; R 0 0 7.5 20 FFFFFF",
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Czechia"] = "Czech Republic",
        ["Türkiye"] = "Turkey",
        ["Turkiye"] = "Turkey",
        ["Korea"] = "South Korea",
        ["Republic of Korea"] = "South Korea",
        ["USA"] = "United States",
        ["UK"] = "United Kingdom",
        ["Great Britain"] = "United Kingdom",
        ["UAE"] = "United Arab Emirates",
        ["Russian Federation"] = "Russia",
    };

    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bandeira do pais (nome em ingles, como o iRacing) ou null se nao houver desenho.</summary>
    public static ImageSource? Get(string country)
    {
        if (string.IsNullOrWhiteSpace(country)) return null;
        if (Cache.TryGetValue(country, out var cached)) return cached;

        ImageSource? img = null;
        string key = Aliases.TryGetValue(country.Trim(), out string? a) ? a : country.Trim();
        if (Specs.TryGetValue(key, out string? spec))
        {
            try { img = Build(spec); }
            catch (Exception ex) { AppPaths.Log("Bandeira " + country + ": " + ex.Message); }
        }
        Cache[country] = img;
        return img;
    }

    private static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    private static Brush B(string hex)
    {
        var c = (Color)ColorConverter.ConvertFromString("#" + hex);
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static ImageSource Build(string spec)
    {
        var group = new DrawingGroup();
        foreach (string raw in spec.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string layer = raw.Trim();
            if (layer.Length == 0) continue;
            string[] tk = layer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (tk[0])
            {
                case "H":
                case "V":
                {
                    var items = tk[1].Split(',').Select(s =>
                    {
                        string[] p = s.Split('*');
                        return (Color: p[0], W: p.Length > 1 ? D(p[1]) : 1.0);
                    }).ToList();
                    double total = items.Sum(i => i.W);
                    double pos = 0;
                    foreach (var it in items)
                    {
                        double len = (tk[0] == "H" ? 20.0 : 30.0) * it.W / total;
                        Rect r = tk[0] == "H" ? new Rect(0, pos, 30, len) : new Rect(pos, 0, len, 20);
                        group.Children.Add(new GeometryDrawing(B(it.Color), null, new RectangleGeometry(r)));
                        pos += len;
                    }
                    break;
                }
                case "R":
                    group.Children.Add(new GeometryDrawing(B(tk[5]), null,
                        new RectangleGeometry(new Rect(D(tk[1]), D(tk[2]), D(tk[3]), D(tk[4])))));
                    break;
                case "C":
                    group.Children.Add(new GeometryDrawing(B(tk[4]), null,
                        new EllipseGeometry(new Point(D(tk[1]), D(tk[2])), D(tk[3]), D(tk[3]))));
                    break;
                case "P":
                    group.Children.Add(new GeometryDrawing(B(tk[1]), null,
                        Geometry.Parse(string.Join(" ", tk.Skip(2)))));
                    break;
            }
        }
        group.ClipGeometry = new RectangleGeometry(new Rect(0, 0, 30, 20));
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
