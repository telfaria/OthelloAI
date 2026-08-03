namespace Othello.WinUI.Models;

/// <summary>
/// 棋譜の1手分を表す表示モデルです。
/// </summary>
public sealed class ReplayMoveRecordModel
{
    /// <summary>
    /// 手数を取得または設定します。
    /// </summary>
    public int Ply { get; init; }

    /// <summary>
    /// 着手プレイヤーを取得または設定します。
    /// </summary>
    public required string Player { get; init; }

    /// <summary>
    /// 着手前盤面を取得または設定します。
    /// </summary>
    public required string[] BoardBefore { get; init; }

    /// <summary>
    /// 着手後盤面を取得または設定します。
    /// </summary>
    public required string[] BoardAfter { get; init; }
}
