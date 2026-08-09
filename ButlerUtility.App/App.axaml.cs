using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ItchioButlerUtility.Services;
using ItchioButlerUtility.ViewModels;
using ItchioButlerUtility.Views;

namespace ItchioButlerUtility;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var butler = new ButlerService();
            var config = ConfigStore.Load();

            var viewModel = new MainWindowViewModel(config, butler, http);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };

            // Last-chance net for faulted tasks nobody awaited — surface instead of vanishing.
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                e.SetObserved();
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    viewModel.ReportProblem($"Unhandled background error: {e.Exception.GetBaseException().Message}"));
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
