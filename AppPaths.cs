using System.IO;
using System.Text.Json;

namespace IRacingTracker;

/// <summary>Pastas e ficheiros do programa (em %AppData%\iRacingTracker).</summary>
public static class AppPaths
{
    public static string Dir
    {
        get
        {
            string p = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iRacingTracker");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string DbPath => Path.Combine(Dir, "tracker.db");
    public static string SettingsPath => Path.Combine(Dir, "settings.json");
    public static string LogPath => Path.Combine(Dir, "erro.log");
    public static string BackupDir
    {
        get
        {
            string p = Path.Combine(Dir, "backups");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
        }
        catch
        {
            // o registo de erros nunca pode causar outro erro
        }
    }
}

/// <summary>Definições guardadas automaticamente (posição, tamanho, modo de teste).</summary>
public sealed class AppSettings
{
    public bool HasPosition { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; } = 760;
    public double Height { get; set; } = 460;
    public bool TestMode { get; set; }

    // Fase 6: transparencia e atalhos globais (modificadores: Alt=1, Ctrl=2, Shift=4, Win=8)
    public double Opacity { get; set; } = 0.94;     // opacidade so do fundo
    public List<string> HiddenColumns { get; set; } = new();

    // Fase 7
    public int SettingsVersion { get; set; }
    public double FontSize { get; set; } = 14;
    public double RowHeight { get; set; } = 28;
    public bool AlwaysOnTop { get; set; } = true;
    public bool HidePitCars { get; set; } = true;
    public bool AutoCenter { get; set; } = true;
    public bool AutoBackup { get; set; } = true;
    public bool OnlyWhenConnected { get; set; }
    public int ToggleMods { get; set; } = 3;      // Ctrl+Alt
    public int ToggleVk { get; set; } = 0x4F;     // O
    public int ClickMods { get; set; } = 3;       // Ctrl+Alt
    public int ClickVk { get; set; } = 0x43;      // C
    public string ExtraBackupDir { get; set; } = "";   // copia extra (ex.: OneDrive)
    public bool Compact { get; set; }               // barra de baixo recolhida
    public string AccentHex { get; set; } = "";       // cor de destaque personalizada (vazio = a do tema)
    public string HdrHex { get; set; } = "";          // cor do cabecalho personalizada
    public string BgHex { get; set; } = "";           // cor de fundo personalizada (vazio = a do tema)
    public string Theme { get; set; } = "pitwall";    // pitwall, cards, tower, hud, data
    public string Language { get; set; } = "auto";   // auto, pt, en, es, fr

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsPath))
            {
                string json = File.ReadAllText(AppPaths.SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null)
                {
                    if (s.SettingsVersion < 3)
                    {
                        // layout novo (Fase 7b): letras e linhas maiores
                        s.SettingsVersion = 3;
                        s.FontSize = 14;
                        s.RowHeight = 28;
                        s.Width = Math.Max(s.Width, 940);
                        s.Height = Math.Max(s.Height, 560);
                    }
                    return s;
                }
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log("Erro a ler definicoes: " + ex.Message);
        }
        return new AppSettings { SettingsVersion = 3, Width = 940, Height = 560 };
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppPaths.SettingsPath, json);
        }
        catch (Exception ex)
        {
            AppPaths.Log("Erro a guardar definicoes: " + ex.Message);
        }
    }
}
