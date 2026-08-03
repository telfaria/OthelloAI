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
    /// <param name="viewModel">画面表示データを提供する ViewModel です。</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        if (Content is FrameworkElement root)
        {
            root.DataContext = viewModel;
        }

        Title = viewModel.WindowTitle;
    }
}
