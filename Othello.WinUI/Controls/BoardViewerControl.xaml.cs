using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Othello.WinUI.ViewModel;

namespace Othello.WinUI.Controls;

/// <summary>
/// BoardViewModel を表示する 8x8 盤面ビューアーです。
/// </summary>
public sealed partial class BoardViewerControl : UserControl
{
    /// <summary>
    /// <see cref="BoardViewerControl"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public BoardViewerControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 盤面表示に利用する ViewModel を取得または設定します。
    /// </summary>
    public BoardViewModel? ViewModel
    {
        get => (BoardViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// <see cref="ViewModel"/> の依存関係プロパティです。
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(BoardViewModel),
        typeof(BoardViewerControl),
        new PropertyMetadata(null));
}
