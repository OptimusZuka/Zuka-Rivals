using System.Globalization;

namespace IRacingTracker;

public sealed class DriverEntry
{
    public int CarIdx = -1;
    public string UserName = "";
    public string Abbrev = "";       // AbbrevName: "Sobrenome, N" (vem em UTF-8 mesmo quando o UserName vem estragado)

    /// <summary>Nome para mostrar: se o UserName veio como "??????", usa o AbbrevName ("N. Sobrenome").</summary>
    public string DisplayName
    {
        get
        {
            if (!SessionParser.IsUnreadable(UserName)) return UserName;
            string a = Abbrev.Trim();
            if (a.Length == 0 || SessionParser.IsUnreadable(a)) return UserName;
            int c = a.IndexOf(',');
            if (c <= 0) return a;
            string sur = a.Substring(0, c).Trim();
            string ini = a.Substring(c + 1).Trim();
            return ini.Length > 0 ? ini + ". " + sur : sur;
        }
    }
    public int UserId;
    public string CarNumber = "";
    public int CarClassId;
    public string CarClassShortName = "";
    public string Flair = "";        // FlairName do iRacing (normalmente o pais)
    public string CarScreenName = "";
    public string CarScreenNameShort = "";
    public int CarClassColor = -1;   // 0xRRGGBB oficial do iRacing; -1 = desconhecida
    public int IRating;
    public double EstLapTime;        // tempo de volta estimado da classe (s), usado no tempo relativo
    public string LicString = "";    // ex.: "B 3.55"  (classe de licenca + SR)
    public int LicColor = -1;
    public bool IsSpectator;
    public bool IsPaceCar;
    public bool IsAI;
}

public sealed class SessionEntry
{
    public int SessionNum;
    public string SessionType = "";
    public string SessionName = "";
}

public sealed class ParsedSession
{
    public int SeriesId;
    public int SessionId;
    public int SubSessionId;
    public bool Official;
    public string EventType = "";
    public string TrackName = "";
    public int CurrentSessionNum;
    public int DriverCarIdx = -1;
    public int DriverUserId;
    public double MyEstLapTime;
    public List<SessionEntry> Sessions = new();
    public List<DriverEntry> Drivers = new();
    public Dictionary<int, string> Tires = new();   // TireIndex -> "Hard", "Wet", ...

    public SessionEntry? FindSession(int num)
    {
        foreach (var s in Sessions)
            if (s.SessionNum == num) return s;
        return null;
    }

    public DriverEntry? FindDriver(int carIdx)
    {
        foreach (var d in Drivers)
            if (d.CarIdx == carIdx) return d;
        return null;
    }
}

/// <summary>
/// Le o texto YAML da sessao do iRacing. Nao usa bibliotecas externas: o formato e simples
/// (indentacao fixa) e testei esta mesma logica contra um dump real de uma sessao.
/// </summary>
public static class SessionParser
{
    /// <summary>Texto so com "?" e espacos: o iRacing grava assim os alfabetos que a pagina de codigos nao tem.</summary>
    public static bool IsUnreadable(string s)
    {
        foreach (char c in s)
            if (c != '?' && !char.IsWhiteSpace(c)) return false;
        return true;
    }

    /// <summary>
    /// O YAML e lido byte a byte (Latin1). Alguns campos (ex.: AbbrevName) vem em UTF-8, outros na pagina de
    /// codigos do Windows: tenta UTF-8 estrito e, se falhar, usa a pagina de codigos.
    /// </summary>
    public static string FixText(string s)
    {
        bool high = false;
        foreach (char c in s)
        {
            if (c > 0xFF) return s;
            if (c > 0x7F) high = true;
        }
        if (!high) return s;
        byte[] bytes = System.Text.Encoding.Latin1.GetBytes(s);
        // O iRacing corta estes campos a meio de um caractere UTF-8: tira ate 3 bytes finais incompletos
        for (int cut = 0; cut <= 3 && cut < bytes.Length; cut++)
        {
            if (cut > 0 && bytes[bytes.Length - cut] < 0x80) break;
            try
            {
                string u = new System.Text.UTF8Encoding(false, true).GetString(bytes, 0, bytes.Length - cut);
                if (u.Length > 0 && (cut == 0 || u.Any(ch => ch > 0x7F))) return u;
            }
            catch (ArgumentException)
            {
                // nao e UTF-8 (ainda)
            }
        }
        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            return System.Text.Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage).GetString(bytes);
        }
        catch
        {
            return s;
        }
    }

    public static ParsedSession Parse(string yaml)
    {
        var res = new ParsedSession();
        if (string.IsNullOrEmpty(yaml)) return res;

        string section = "";
        string list = "";            // dentro de DriverInfo: "Drivers" ou "DriverTires"
        DriverEntry? driver = null;
        SessionEntry? sess = null;
        int tireIndex = -1;

        foreach (string rawLine in yaml.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0) continue;

            if (line[0] != ' ')
            {
                // chave de nivel 0 (WeekendInfo:, SessionInfo:, DriverInfo:...) ou "---"
                string t = line.Trim('﻿').Trim();
                section = t.EndsWith(":") ? t.Substring(0, t.Length - 1) : "";
                list = "";
                driver = null;
                sess = null;
                tireIndex = -1;
                continue;
            }

            int indent = 0;
            while (indent < line.Length && line[indent] == ' ') indent++;
            string body = line.Substring(indent);
            bool isItem = body.StartsWith("- ");
            if (isItem) body = body.Substring(2);
            SplitKeyValue(body, out string key, out string val);

            if (section == "WeekendInfo")
            {
                if (indent == 1 && !isItem) ApplyWeekend(res, key, val);
            }
            else if (section == "SessionInfo")
            {
                if (indent == 1 && !isItem && key == "CurrentSessionNum")
                {
                    res.CurrentSessionNum = ToInt(val);
                }
                else if (indent == 1 && isItem && key == "SessionNum")
                {
                    sess = new SessionEntry { SessionNum = ToInt(val) };
                    res.Sessions.Add(sess);
                }
                else if (sess != null && indent == 3 && !isItem)
                {
                    if (key == "SessionType") sess.SessionType = val;
                    else if (key == "SessionName") sess.SessionName = val;
                }
            }
            else if (section == "DriverInfo")
            {
                if (indent == 1 && !isItem)
                {
                    list = val.Length == 0 ? key : "";
                    driver = null;
                    tireIndex = -1;
                    if (key == "DriverCarIdx") res.DriverCarIdx = ToInt(val);
                    else if (key == "DriverUserID") res.DriverUserId = ToInt(val);
                    else if (key == "DriverCarEstLapTime") res.MyEstLapTime = ToDouble(val);
                }
                else if (list == "Drivers")
                {
                    if (indent == 1 && isItem && key == "CarIdx")
                    {
                        driver = new DriverEntry { CarIdx = ToInt(val) };
                        res.Drivers.Add(driver);
                    }
                    else if (driver != null && indent == 3 && !isItem)
                    {
                        ApplyDriver(driver, key, val);
                    }
                }
                else if (list == "DriverTires")
                {
                    if (indent == 1 && isItem && key == "TireIndex") tireIndex = ToInt(val);
                    else if (indent == 3 && !isItem && key == "TireCompoundType" && tireIndex >= 0)
                        res.Tires[tireIndex] = val;
                }
            }
        }

        return res;
    }

    private static void ApplyWeekend(ParsedSession r, string key, string val)
    {
        switch (key)
        {
            case "SeriesID": r.SeriesId = ToInt(val); break;
            case "SessionID": r.SessionId = ToInt(val); break;
            case "SubSessionID": r.SubSessionId = ToInt(val); break;
            case "Official": r.Official = ToInt(val) == 1; break;
            case "EventType": r.EventType = val; break;
            case "TrackDisplayName": r.TrackName = val; break;
            case "TrackName": if (r.TrackName.Length == 0) r.TrackName = val; break;
        }
    }

    private static void ApplyDriver(DriverEntry d, string key, string val)
    {
        switch (key)
        {
            case "UserName": d.UserName = FixText(val); break;
            case "AbbrevName": d.Abbrev = FixText(val); break;
            case "UserID": d.UserId = ToInt(val); break;
            case "CarNumber": d.CarNumber = val; break;
            case "CarClassID": d.CarClassId = ToInt(val); break;
            case "CarClassShortName": d.CarClassShortName = val; break;
            case "FlairName": d.Flair = val; break;
            case "CarScreenName": d.CarScreenName = val; break;
            case "CarScreenNameShort": d.CarScreenNameShort = val; break;
            case "CarClassColor": d.CarClassColor = ToHex(val); break;
            case "IRating": d.IRating = ToInt(val); break;
            case "CarClassEstLapTime": d.EstLapTime = ToDouble(val); break;
            case "LicString": d.LicString = val; break;
            case "LicColor": d.LicColor = ToHex(val); break;
            case "IsSpectator": d.IsSpectator = ToInt(val) == 1; break;
            case "CarIsPaceCar": d.IsPaceCar = ToInt(val) == 1; break;
            case "CarIsAI": d.IsAI = ToInt(val) == 1; break;
        }
    }

    private static void SplitKeyValue(string body, out string key, out string val)
    {
        int i = body.IndexOf(':');
        if (i < 0)
        {
            key = body.Trim();
            val = "";
            return;
        }
        key = body.Substring(0, i).Trim();
        val = body.Substring(i + 1).Trim();
        if (val.Length >= 2 && val[0] == '"' && val[val.Length - 1] == '"')
            val = val.Substring(1, val.Length - 2);
    }

    private static int ToInt(string s)
    {
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    }

    private static double ToDouble(string s)
    {
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;
    }

    private static int ToHex(string s)
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
        return int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v) ? v & 0xFFFFFF : -1;
    }
}
