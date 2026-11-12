using SMP_SCADA.ViewModels;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace SMP_SCADA;

public partial class App : Application
{
    readonly CancellationTokenSource _closing = new();
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var service = new ServiceConnection(SynchronizationContext.Current!);
        var main = new MainViewModel(service, TimeProvider.System);

        var clock = new DispatcherTimer { Interval = MainViewModel.FlashPeriod / 2 };
        clock.Tick += (_, _) => main.OnTimer();
        clock.Start();

        new MainWindow { DataContext = main }.Show();
        _ = service.RunAsync(_closing.Token);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _closing.Cancel();
        base.OnExit(e);
    }
}

