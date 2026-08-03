namespace Othello.WinUI.Models;

/// <summary>
/// JSONL から読み込む1ゲーム分の棋譜モデルです。
/// </summary>
public sealed class ReplayGameRecordModel
{
    /// <summary>
    /// ゲームIDを取得または設定します。
    /// </summary>
    public required string GameId { get; init; }

    /// <summary>
    /// 先手AI名を取得または設定します。
    /// </summary>
    public required string BlackAI { get; init; }

    /// <summary>
    /// 後手AI名を取得または設定します。
    /// </summary>
    public required string WhiteAI { get; init; }

    /// <summary>
    /// 棋譜手順を取得または設定します。
    /// </summary>
    public required List<ReplayMoveRecordModel> Moves { get; init; }

    /// <summary>
    /// 一覧表示用のラベルを取得します。
    /// </summary>
    public string DisplayName => $"{GameId} ({BlackAI} vs {WhiteAI})";
}
