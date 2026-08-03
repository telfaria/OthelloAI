using Othello.AI;
using Othello.Core;
using Othello.WinUI.Models;

namespace Othello.WinUI.Services;

/// <summary>
/// Core / AI ライブラリ接続情報を表示用モデルへ変換します。
/// </summary>
public sealed class PlatformSummaryService : IPlatformSummaryService
{
    /// <summary>
    /// 表示用のプラットフォーム概要を取得します。
    /// </summary>
    /// <returns>表示用概要モデルです。</returns>
    public PlatformSummaryModel GetSummary()
    {
        string coreAssembly = typeof(Board).Assembly.GetName().Name ?? "Othello.Core";
        string aiAssembly = typeof(IOthelloAI).Assembly.GetName().Name ?? "Othello.AI";

        return new PlatformSummaryModel
        {
            Title = "Platform Scope",
            Description = "このGUIは研究ワークフローの可視化を担当し、ゲームロジックは既存ライブラリへ委譲します。",
            ConnectedLibraries = $"Connected: {coreAssembly}, {aiAssembly} (Training は後続フェーズで接続)",
            Note = "MainWindow は表示専用であり、対局処理や探索処理は実装しません。"
        };
    }
}
