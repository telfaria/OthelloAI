namespace Othello.WinUI.Models;

/// <summary>
/// 盤面セルの表示情報を表します。
/// </summary>
public sealed class BoardCellDisplayModel
{
    /// <summary>
    /// 行インデックスを取得または設定します。
    /// </summary>
    public required int Row { get; init; }

    /// <summary>
    /// 列インデックスを取得または設定します。
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// 石表示用のシンボルを取得または設定します。
    /// </summary>
    public required string DiscSymbol { get; init; }

    /// <summary>
    /// セル内容の説明を取得または設定します。
    /// </summary>
    public required string DiscName { get; init; }
}
