using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace IRacingTracker;

/// <summary>Copias de seguranca do historico, tags e notas (tracker.db).</summary>
public partial class MainWindow
{
    private static string Stamp() => DateTime.Now.ToString("yyyyMMdd_HHmmss");

    /// <summary>Uma copia automatica por dia, ao abrir o programa; guarda as 10 mais recentes.</summary>
    private void AutoBackupIfNeeded()
    {
        if (!_settings.AutoBackup || _history == null) return;
        try
        {
            string path = Path.Combine(AppPaths.BackupDir, "auto_" + DateTime.Now.ToString("yyyyMMdd") + ".db");
            if (!File.Exists(path)) _history.BackupTo(path);
            CopyExtra(path, true);

            var old = new DirectoryInfo(AppPaths.BackupDir).GetFiles("auto_*.db")
                .OrderByDescending(f => f.Name).Skip(10).ToList();
            foreach (var f in old) f.Delete();
        }
        catch (Exception ex)
        {
            AppPaths.Log("Backup automatico: " + ex.Message);
        }
    }

    /// <summary>Copia o backup para a pasta extra (ex.: OneDrive), se estiver escolhida.</summary>
    private void CopyExtra(string path, bool prune)
    {
        string dir = _settings.ExtraBackupDir;
        if (string.IsNullOrWhiteSpace(dir)) return;
        try
        {
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, Path.GetFileName(path));
            if (!File.Exists(dest)) File.Copy(path, dest, false);
            if (prune)
            {
                var old = new DirectoryInfo(dir).GetFiles("auto_*.db")
                    .OrderByDescending(f => f.Name).Skip(10).ToList();
                foreach (var f in old) f.Delete();
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log("Copia extra: " + ex.Message);
            Toast(Loc.T("bk_extra_err", ex.Message));
        }
    }

    private void UpdateExtraDirUi()
    {
        ExtraDirText.Text = string.IsNullOrWhiteSpace(_settings.ExtraBackupDir)
            ? Loc.T("txt_extra_none") : Loc.T("txt_extra_set", _settings.ExtraBackupDir);
    }

    private void ExtraDirChoose_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BeginTyping();
            var dlg = new Microsoft.Win32.OpenFolderDialog { Title = Loc.T("btn_extra_choose") };
            bool ok = dlg.ShowDialog(this) == true;
            EndTyping(false);
            if (!ok || string.IsNullOrWhiteSpace(dlg.FolderName)) return;
            _settings.ExtraBackupDir = dlg.FolderName;
            SaveSettings();
            UpdateExtraDirUi();
            if (_history != null)
            {
                string path = Path.Combine(AppPaths.BackupDir, "backup_" + Stamp() + ".db");
                _history.BackupTo(path);
                CopyExtra(path, false);
            }
            Toast(Loc.T("txt_extra_set", dlg.FolderName));
        }
        catch (Exception ex)
        {
            EndTyping(false);
            AppPaths.Log("Pasta extra: " + ex);
            Toast(Loc.T("err_generic", ex.Message));
        }
    }

    private void ExtraDirClear_Click(object sender, RoutedEventArgs e)
    {
        _settings.ExtraBackupDir = "";
        SaveSettings();
        UpdateExtraDirUi();
    }

    private void BackupNow_Click(object sender, RoutedEventArgs e)
    {
        if (_history == null) { Toast(Loc.T("hist_unavail")); return; }
        try
        {
            string path = Path.Combine(AppPaths.BackupDir, "backup_" + Stamp() + ".db");
            _history.BackupTo(path);
            CopyExtra(path, false);
            Toast(Loc.T("bk_created", path, _history.Summary()));
        }
        catch (Exception ex)
        {
            AppPaths.Log("Backup: " + ex);
            Toast(Loc.T("bk_err", ex.Message));
        }
    }

    private void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_history == null) { Toast(Loc.T("hist_unavail")); return; }
        try
        {
            BeginTyping();   // o dialogo precisa de receber o rato e o teclado
            var dlg = new SaveFileDialog
            {
                Title = Loc.T("bk_export_title"),
                Filter = "iRacing Tracker (*.db)|*.db",
                FileName = "iRacingTracker_backup_" + Stamp() + ".db",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            bool ok = dlg.ShowDialog(this) == true;
            EndTyping(false);
            if (!ok) return;
            _history.BackupTo(dlg.FileName);
            Toast(Loc.T("bk_exported", dlg.FileName, _history.Summary()));
        }
        catch (Exception ex)
        {
            EndTyping(false);
            AppPaths.Log("Exportar: " + ex);
            Toast(Loc.T("bk_err_export", ex.Message));
        }
    }

    private void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        string? chosen = null;
        string tmp = Path.Combine(AppPaths.Dir, "importar_tmp.db");
        try
        {
            BeginTyping();
            var dlg = new OpenFileDialog
            {
                Title = Loc.T("bk_import_title"),
                Filter = "iRacing Tracker (*.db)|*.db",
                InitialDirectory = AppPaths.BackupDir
            };
            bool ok = dlg.ShowDialog(this) == true;
            if (ok) chosen = dlg.FileName;

            if (chosen == null) { EndTyping(false); return; }

            // 1) validar uma copia do ficheiro escolhido
            File.Copy(chosen, tmp, true);
            string summary;
            using (var test = new HistoryDb(tmp)) summary = test.Summary();

            var answer = MessageBox.Show(this,
                Loc.T("bk_import_q", summary),
                Loc.T("bk_import_dlg"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            EndTyping(false);
            if (answer != MessageBoxResult.Yes) { File.Delete(tmp); return; }

            // 2) copia de seguranca do estado atual
            if (_history != null)
            {
                string before = Path.Combine(AppPaths.BackupDir, "antes_de_importar_" + Stamp() + ".db");
                _history.BackupTo(before);
            }

            // 3) trocar a base de dados
            _history?.Dispose();
            _history = null;
            foreach (string suffix in new[] { "", "-wal", "-shm", "-journal" })
            {
                string f = AppPaths.DbPath + suffix;
                if (File.Exists(f)) File.Delete(f);
            }
            File.Move(tmp, AppPaths.DbPath);

            _history = new HistoryDb(AppPaths.DbPath);
            _tags = _history.LoadTags();
            _driverTags = _history.LoadAllDriverTags();
            _knownForSub = long.MinValue;
            _needRecord = false;
            CloseDetail();
            Toast(Loc.T("bk_imported", _history.Summary()));
        }
        catch (Exception ex)
        {
            EndTyping(false);
            AppPaths.Log("Importar: " + ex);
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            if (_history == null)
            {
                try
                {
                    _history = new HistoryDb(AppPaths.DbPath);
                    _tags = _history.LoadTags();
                    _driverTags = _history.LoadAllDriverTags();
                }
                catch (Exception ex2) { _historyError = ex2.Message; }
            }
            Toast(Loc.T("bk_err_import", ex.Message));
        }
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + AppPaths.BackupDir + "\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Toast(Loc.T("bk_err_folder", ex.Message));
        }
    }
}
