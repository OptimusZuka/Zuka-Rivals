using System.Threading.Tasks;
using System.Windows;

namespace IRacingTracker;

public partial class App : Application
{
    private System.Threading.Mutex? _single;
    private bool _started;

    public App()
    {
        // Qualquer erro, em qualquer ponto, fica gravado em erro.log (e, no arranque, aparece numa mensagem)
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ReportFatal(args.ExceptionObject as Exception, true);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppPaths.Log("Tarefa: " + args.Exception);
            args.SetObserved();
        };
    }

    private static void ReportFatal(Exception? ex, bool show)
    {
        string text = ex != null ? ex.ToString() : "Erro desconhecido";
        AppPaths.Log("ERRO FATAL: " + text);
        if (!show) return;
        try
        {
            MessageBox.Show(
                "O Zuka Rivals encontrou um erro e vai fechar.\n\n" +
                (ex != null ? ex.GetType().Name + ": " + ex.Message : text) +
                "\n\nO detalhe completo ficou em:\n" + AppPaths.LogPath,
                "Zuka Rivals", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // So uma copia de cada vez (o arranque com o Windows e com o iRacing podem tentar abrir duas).
        // Se o Windows nao deixar criar o bloqueio, o programa abre na mesma em vez de falhar.
        try
        {
            _single = new System.Threading.Mutex(true, "IRacingTracker_SingleInstance", out bool first);
            if (!first)
            {
                Shutdown();
                return;
            }
        }
        catch (Exception ex)
        {
            AppPaths.Log("Bloqueio de instancia unica: " + ex.Message);
        }

        // Um erro inesperado nao deve fechar o overlay a meio de uma corrida: fica registado em erro.log.
        // Se acontecer ainda no arranque (antes de a janela aparecer), mostra o motivo.
        DispatcherUnhandledException += (_, args) =>
        {
            AppPaths.Log(args.Exception.ToString());
            args.Handled = true;
            if (!_started)
            {
                ReportFatal(args.Exception, true);
                Shutdown();
            }
        };

        try
        {
            base.OnStartup(e);
            // A janela e criada aqui (e nao por StartupUri) para qualquer erro ao abrir aparecer numa mensagem
            var win = new IRacingTracker.MainWindow();
            MainWindow = win;
            win.Show();
            _started = true;
        }
        catch (Exception ex)
        {
            ReportFatal(ex, true);
            Shutdown();
        }
    }
}
