using CommunityToolkit.Mvvm.ComponentModel;

namespace Othello.WinUI.ViewModel;

/// <summary>
/// メインウィンドウに表示する情報を提供する ViewModel です。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// ウィンドウタイトルを取得します。
    /// </summary>
    public string WindowTitle { get; } = "Othello AI Research Platform";

    /// <summary>
    /// プラットフォームの説明文を取得します。
    /// </summary>
    public string PlatformDescription { get; } = "WinUI 3 GUI は表示専用レイヤーとして構成されています。";

    /// <summary>
    /// 初期ステータスメッセージを取得します。
    /// </summary>
    public string StatusMessage { get; } = "Core / AI / Training ライブラリ連携の土台を準備済みです。";
}
