namespace IRacingTracker;

/// <summary>Nomes de carro abreviados (so o suficiente para quem conhece o jogo reconhecer).</summary>
public static class CarNames
{
    // padrao (minusculas, contido no nome do carro) -> abreviatura; ganha a primeira regra que bater
    private static readonly (string Pattern, string Short)[] Rules =
    {
        ("mx-5", "MX-5"), ("mx5", "MX-5"), ("gr86", "GR86"), ("bmw m2", "M2"),
        ("gt3 cup", "911 Cup"), ("911 rsr", "911 RSR"), ("911 gt3", "911 GT3"), ("cayman", "Cayman"),
        ("porsche 963", "963"), ("bmw m hybrid", "M Hybrid"), ("m4 gt3", "M4 GT3"), ("m4 gt4", "M4 GT4"),
        ("296", "296 GT3"), ("488 gt3", "488 GT3"), ("488 gte", "488 GTE"),
        ("amg gt3", "AMG GT3"), ("amg gt4", "AMG GT4"), ("r8", "R8"), ("720s", "720S"),
        ("hurac", "Huracán"), ("vantage", "Vantage"), ("z06", "Z06"), ("c8.r", "C8.R"),
        ("mustang", "Mustang"), ("nsx", "NSX"), ("rc f", "RC F"), ("civic", "Civic"),
        ("v-series", "V-Series.R"), ("arx-06", "ARX-06"), ("p217", "P217"), ("p320", "P320"),
        ("dallara f3", "F3"), ("ir-18", "IR-18"), ("super formula", "Super Formula"),
        ("formula vee", "F.Vee"), ("skip barber", "Skippy"), ("ff1600", "FF1600"), ("sr10", "SR10"),
        ("next gen", "Next Gen"), ("late model", "Late Model"), ("street stock", "Street Stock"),
        ("legends", "Legends"), ("modified", "Modified"), ("truck", "Truck"),
    };

    private static readonly string[] Brands =
    {
        "Mercedes-AMG", "Aston Martin", "Mazda", "Toyota", "BMW", "Porsche", "Ferrari", "Audi", "McLaren",
        "Lamborghini", "Chevrolet", "Ford", "Acura", "Lexus", "Honda", "Dallara", "Cadillac", "Hyundai",
        "Nissan", "Ligier", "Radical", "Subaru", "Volkswagen", "Kia", "Ray",
    };

    public static string Short(DriverEntry d)
    {
        string full = (d.CarScreenNameShort + " | " + d.CarScreenName + " | " + d.CarClassShortName).ToLowerInvariant();
        foreach (var (pattern, abbr) in Rules)
            if (full.Contains(pattern)) return abbr;

        // sem regra: usa o mais curto dos nomes e tira a marca da frente
        string name = new[] { d.CarScreenNameShort, d.CarClassShortName, d.CarScreenName }
            .Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s.Length).FirstOrDefault() ?? "";
        foreach (string brand in Brands)
        {
            if (name.StartsWith(brand + " ", StringComparison.OrdinalIgnoreCase))
            {
                string rest = name.Substring(brand.Length + 1).Trim();
                if (rest.Length > 0) return rest;
            }
        }
        return name;
    }
}
