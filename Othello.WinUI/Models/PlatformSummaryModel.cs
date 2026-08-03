namespace Othello.WinUI.Models;

/// <summary>
/// AI研究・学習プラットフォームの表示概要を表します。
/// </summary>
public sealed class PlatformSummaryModel
{
    /// <summary>
    /// セクション見出しを取得または設定します。
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 概要説明を取得または設定します。
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 連携ライブラリ表示を取得または設定します。
    /// </summary>
    public required string ConnectedLibraries { get; init; }

    /// <summary>
    /// 補足メモを取得または設定します。
    /// </summary>
    public required string Note { get; init; }
}
