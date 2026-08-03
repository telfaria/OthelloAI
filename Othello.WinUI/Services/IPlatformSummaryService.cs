using Othello.WinUI.Models;

namespace Othello.WinUI.Services;

/// <summary>
/// メイン画面に表示するプラットフォーム概要情報を提供します。
/// </summary>
public interface IPlatformSummaryService
{
    /// <summary>
    /// 表示用のプラットフォーム概要を取得します。
    /// </summary>
    /// <returns>表示用概要モデルです。</returns>
    PlatformSummaryModel GetSummary();
}
