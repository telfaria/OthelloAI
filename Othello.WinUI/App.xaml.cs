using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Othello.WinUI.Services;
using Othello.WinUI.View;
using Othello.WinUI.ViewModel;

namespace Othello.WinUI;

/// <summary>
/// WinUI アプリケーションのエントリポイントを表します。
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private Window? _mainWindow;

    /// <summary>
    /// <see cref="App"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// アプリケーション起動時にメインウィンドウを表示します。
    /// </summary>
    /// <param name="args">起動引数です。</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
    }

    /// <summary>
    /// DI コンテナへ依存関係を登録します。
    /// </summary>
    /// <param name="services">サービスコレクションです。</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPlatformSummaryService, PlatformSummaryService>();
        services.AddSingleton<IReplayRecordService, ReplayRecordService>();
        services.AddSingleton<BoardViewModel>();
        services.AddSingleton<ReplayViewerViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }
}
