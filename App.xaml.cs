using System.Windows;

namespace IRacingTracker;

public partial class App : Application
{
    private System.Threading.Mutex? _single;

    protected override void OnStartup(StartupEventArgs e)
    {
        // So uma copia de cada vez (o arranque com o Windows e com o iRacing podem tentar abrir duas)
        _single = new System.Threading.Mutex(true, "IRacingTracker_SingleInstance", out bool first);
        if (!first)
        {
            Shutdown();
            return;
        }

        // Um erro inesperado nao deve fechar o overlay a meio de uma corrida: fica registado em erro.log
        DispatcherUnhandledException += (_, args) =>
        {
            AppPaths.Log(args.Exception.ToString());
            args.Handled = true;
        };
        base.OnStartup(e);
    }
}
