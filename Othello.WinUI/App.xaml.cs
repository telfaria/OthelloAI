using Microsoft.UI.Xaml;
using Othello.WinUI.View;

namespace Othello.WinUI;

/// <summary>
/// WinUI アプリケーションのエントリポイントを表します。
/// </summary>
public partial class App : Application
{
    private Window? _mainWindow;

    /// <summary>
    /// <see cref="App"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// アプリケーション起動時にメインウィンドウを表示します。
    /// </summary>
    /// <param name="args">起動引数です。</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
