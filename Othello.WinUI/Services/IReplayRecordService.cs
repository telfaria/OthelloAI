using Othello.WinUI.Models;

namespace Othello.WinUI.Services;

/// <summary>
/// JSONL 棋譜の読み込み機能を提供します。
/// </summary>
public interface IReplayRecordService
{
    /// <summary>
    /// JSON Lines ファイルから棋譜一覧を読み込みます。
    /// </summary>
    /// <param name="jsonlFilePath">読み込み対象ファイルパスです。</param>
    /// <returns>読み込んだゲーム一覧です。</returns>
    IReadOnlyList<ReplayGameRecordModel> LoadGames(string jsonlFilePath);
}
