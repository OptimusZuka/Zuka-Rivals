using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace IRacingTracker;

/// <summary>Arrancar com o Windows (so para o utilizador atual, sem direitos de administrador).</summary>
public partial class MainWindow
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "iRacingTracker";
    private const string RunWatcherName = "iRacingTrackerWatcher";

    private static bool IsAutostartOn()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(RunValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Versoes de teste antigas criavam um vigia em PowerShell; se existir, e removido (o antivirus costuma desconfiar disso).</summary>
    private static void RemoveLegacyWatcher()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(RunWatcherName, false);
        }
        catch
        {
            // sem permissao ou chave inexistente: ignora
        }
    }

    /// <summary>Procura o executar.bat subindo a partir da pasta do programa.</summary>
    private static string? FindLauncher()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            string vbs = Path.Combine(dir, "launcher.vbs");
            if (File.Exists(vbs)) return vbs;
            string candidate = Path.Combine(dir, "executar.bat");
            if (File.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    private void AutostartCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        bool want = AutostartCheck.IsChecked == true;
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (want)
            {
                string? launcher = FindLauncher();
                if (launcher == null)
                {
                    Toast(Loc.T("as_nolauncher"));
                    _loading = true;
                    AutostartCheck.IsChecked = false;
                    _loading = false;
                    return;
                }
                key.SetValue(RunValueName, launcher.EndsWith(".vbs", StringComparison.OrdinalIgnoreCase)
                    ? "wscript.exe \"" + launcher + "\""
                    : "cmd.exe /c start \"\" /min \"" + launcher + "\"");
                Toast(Loc.T("as_on"));
            }
            else
            {
                key.DeleteValue(RunValueName, false);
                Toast(Loc.T("as_off"));
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log("Autostart: " + ex);
            Toast(Loc.T("err_generic", ex.Message));
        }
    }
}
