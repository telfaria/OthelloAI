using Microsoft.UI.Xaml;
using Othello.WinUI.ViewModel;

namespace Othello.WinUI.View;

/// <summary>
/// AI研究・学習プラットフォームのメインウィンドウを表します。
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// <see cref="MainWindow"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
