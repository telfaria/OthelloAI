using CommunityToolkit.Mvvm.ComponentModel;
using Othello.WinUI.Services;

namespace Othello.WinUI.ViewModel;

/// <summary>
/// メインウィンドウに表示する情報を提供する ViewModel です。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// <see cref="MainWindowViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="platformSummaryService">プラットフォーム概要情報サービスです。</param>
    /// <param name="replayViewer">棋譜再生 ViewModel です。</param>
    public MainWindowViewModel(IPlatformSummaryService platformSummaryService, ReplayViewerViewModel replayViewer)
    {
        ArgumentNullException.ThrowIfNull(platformSummaryService);
        ReplayViewer = replayViewer ?? throw new ArgumentNullException(nameof(replayViewer));
        BoardViewer = ReplayViewer.BoardViewer;

        var summary = platformSummaryService.GetSummary();
        PlatformDescription = summary.Description;
    }

    /// <summary>
    /// ウィンドウタイトルを取得します。
    /// </summary>
    public string WindowTitle { get; } = "Othello AI Research Platform";

    /// <summary>
    /// プラットフォームの説明文を取得します。
    /// </summary>
    public string PlatformDescription { get; }

    /// <summary>
    /// 左ナビゲーションの項目一覧を取得します。
    /// </summary>
    public IReadOnlyList<string> NavigationItems { get; } =
    [
        "Dashboard",
        "SelfPlay",
        "Experiments",
        "Models",
        "Settings"
    ];

    /// <summary>
    /// Board Viewer の表示データを取得します。
    /// </summary>
    public BoardViewModel BoardViewer { get; }

    /// <summary>
    /// Replay Viewer の表示データを取得します。
    /// </summary>
    public ReplayViewerViewModel ReplayViewer { get; }

    /// <summary>
    /// 現在の状態を取得します。
    /// </summary>
    public string CurrentStatus { get; } = "待機中";

    /// <summary>
    /// SelfPlay の実行状態を取得します。
    /// </summary>
    public string SelfPlayStatus { get; } = "停止";

    /// <summary>
    /// 総ゲーム数を取得します。
    /// </summary>
    public int TotalGames { get; } = 10000;

    /// <summary>
    /// 現在のゲーム番号を取得します。
    /// </summary>
    public int CurrentGame { get; } = 128;

    /// <summary>
    /// CPU 使用率（ダミー値）を取得します。
    /// </summary>
    public string CpuUsage { get; } = "37%";

    /// <summary>
    /// GPU 使用率（ダミー値）を取得します。
    /// </summary>
    public string GpuUsage { get; } = "22%";

    /// <summary>
    /// 経過時間を取得します。
    /// </summary>
    public string ElapsedTime { get; } = "00:14:32";
}
