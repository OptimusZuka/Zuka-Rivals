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

    /// <summary>Caminho do proprio executavel instalado (ZukaRivals.exe).</summary>
    private static string? FindLauncher()
    {
        string? exe = Environment.ProcessPath;
        if (exe != null && File.Exists(exe)
            && !Path.GetFileName(exe).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            return exe;
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
                key.SetValue(RunValueName, "\"" + launcher + "\"");
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
