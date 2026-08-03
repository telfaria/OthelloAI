using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Othello.Core;
using Othello.WinUI.Models;
using Othello.WinUI.Services;

namespace Othello.WinUI.ViewModel;

/// <summary>
/// JSONL 棋譜の読込と再生を管理する ViewModel です。
/// </summary>
public sealed class ReplayViewerViewModel : ObservableObject
{
    private readonly IReplayRecordService _replayRecordService;
    private readonly DispatcherQueueTimer _playbackTimer;

    private ReplayGameRecordModel? _selectedGame;
    private string _jsonlPath = Path.Combine("data", "selfplay");
    private string _playbackStatus = "停止";
    private string _currentMoveLabel = "0 / 0";
    private int _currentMoveIndex = -1;

    /// <summary>
    /// <see cref="ReplayViewerViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="replayRecordService">棋譜読込サービスです。</param>
    /// <param name="boardViewer">盤面表示用 ViewModel です。</param>
    public ReplayViewerViewModel(IReplayRecordService replayRecordService, BoardViewModel boardViewer)
    {
        _replayRecordService = replayRecordService ?? throw new ArgumentNullException(nameof(replayRecordService));
        BoardViewer = boardViewer ?? throw new ArgumentNullException(nameof(boardViewer));

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("DispatcherQueue is not available on current thread.");

        _playbackTimer = dispatcherQueue.CreateTimer();
        _playbackTimer.Interval = TimeSpan.FromMilliseconds(800);
        _playbackTimer.IsRepeating = true;
        _playbackTimer.Tick += (_, _) => MoveNextInternal();

        LoadJsonlCommand = new RelayCommand(LoadJsonl);
        PlayCommand = new RelayCommand(StartPlayback);
        PauseCommand = new RelayCommand(PausePlayback);
        NextMoveCommand = new RelayCommand(MoveNextInternal);
        PreviousMoveCommand = new RelayCommand(MovePreviousInternal);
        FirstMoveCommand = new RelayCommand(MoveFirstInternal);
        LastMoveCommand = new RelayCommand(MoveLastInternal);
    }

    /// <summary>
    /// 棋譜一覧を取得します。
    /// </summary>
    public ObservableCollection<ReplayGameRecordModel> Games { get; } = [];

    /// <summary>
    /// Board Viewer の表示データを取得します。
    /// </summary>
    public BoardViewModel BoardViewer { get; }

    /// <summary>
    /// JSONL パスを取得または設定します。
    /// </summary>
    public string JsonlPath
    {
        get => _jsonlPath;
        set => SetProperty(ref _jsonlPath, value);
    }

    /// <summary>
    /// 選択中ゲームを取得または設定します。
    /// </summary>
    public ReplayGameRecordModel? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetProperty(ref _selectedGame, value))
            {
                MoveFirstInternal();
            }
        }
    }

    /// <summary>
    /// 再生状態を取得します。
    /// </summary>
    public string PlaybackStatus
    {
        get => _playbackStatus;
        private set => SetProperty(ref _playbackStatus, value);
    }

    /// <summary>
    /// 現在手数表示を取得します。
    /// </summary>
    public string CurrentMoveLabel
    {
        get => _currentMoveLabel;
        private set => SetProperty(ref _currentMoveLabel, value);
    }

    /// <summary>
    /// JSONL を読み込むコマンドを取得します。
    /// </summary>
    public IRelayCommand LoadJsonlCommand { get; }

    /// <summary>
    /// 再生開始コマンドを取得します。
    /// </summary>
    public IRelayCommand PlayCommand { get; }

    /// <summary>
    /// 一時停止コマンドを取得します。
    /// </summary>
    public IRelayCommand PauseCommand { get; }

    /// <summary>
    /// 次の手へ進むコマンドを取得します。
    /// </summary>
    public IRelayCommand NextMoveCommand { get; }

    /// <summary>
    /// 前の手へ戻るコマンドを取得します。
    /// </summary>
    public IRelayCommand PreviousMoveCommand { get; }

    /// <summary>
    /// 最初の手へ移動するコマンドを取得します。
    /// </summary>
    public IRelayCommand FirstMoveCommand { get; }

    /// <summary>
    /// 最後の手へ移動するコマンドを取得します。
    /// </summary>
    public IRelayCommand LastMoveCommand { get; }

    private void LoadJsonl()
    {
        try
        {
            string filePath = ResolveJsonlPath(JsonlPath);

            IReadOnlyList<ReplayGameRecordModel> games = _replayRecordService.LoadGames(filePath);

            Games.Clear();
            foreach (ReplayGameRecordModel game in games)
            {
                Games.Add(game);
            }

            SelectedGame = Games.FirstOrDefault();
            PlaybackStatus = $"読込完了: {Path.GetFileName(filePath)} ({Games.Count} games)";
        }
        catch (Exception ex)
        {
            PlaybackStatus = $"読込失敗: {ex.Message}";
        }
    }

    private void StartPlayback()
    {
        if (SelectedGame is null)
        {
            PlaybackStatus = "ゲーム未選択";
            return;
        }

        if (_currentMoveIndex >= SelectedGame.Moves.Count - 1)
        {
            _currentMoveIndex = -1;
        }

        _playbackTimer.Start();
        PlaybackStatus = "再生中";
    }

    private void PausePlayback()
    {
        _playbackTimer.Stop();
        PlaybackStatus = "一時停止";
    }

    private void MoveNextInternal()
    {
        if (SelectedGame is null || SelectedGame.Moves.Count == 0)
        {
            return;
        }

        if (_currentMoveIndex >= SelectedGame.Moves.Count - 1)
        {
            PausePlayback();
            return;
        }

        _currentMoveIndex++;
        ApplyBoardForCurrentMove();
    }

    private void MovePreviousInternal()
    {
        if (SelectedGame is null)
        {
            return;
        }

        if (_currentMoveIndex <= -1)
        {
            return;
        }

        _currentMoveIndex--;
        ApplyBoardForCurrentMove();
    }

    private void MoveFirstInternal()
    {
        _playbackTimer.Stop();
        _currentMoveIndex = -1;
        ApplyBoardForCurrentMove();
    }

    private void MoveLastInternal()
    {
        if (SelectedGame is null || SelectedGame.Moves.Count == 0)
        {
            return;
        }

        _playbackTimer.Stop();
        _currentMoveIndex = SelectedGame.Moves.Count - 1;
        ApplyBoardForCurrentMove();
    }

    private void ApplyBoardForCurrentMove()
    {
        if (SelectedGame is null || SelectedGame.Moves.Count == 0)
        {
            BoardViewer.SetBoard(Board.CreateInitial());
            CurrentMoveLabel = "0 / 0";
            return;
        }

        ReplayMoveRecordModel referenceMove = _currentMoveIndex < 0
            ? SelectedGame.Moves[0]
            : SelectedGame.Moves[_currentMoveIndex];

        string[] boardRows = _currentMoveIndex < 0 ? referenceMove.BoardBefore : referenceMove.BoardAfter;
        Disc currentPlayer = ResolveCurrentPlayer(referenceMove.Player, _currentMoveIndex < 0);

        Disc[,] cells = ConvertSnapshotToCells(boardRows);
        Board board = Board.FromCells(cells, currentPlayer);
        BoardViewer.SetBoard(board);

        int current = Math.Max(0, _currentMoveIndex + 1);
        CurrentMoveLabel = $"{current} / {SelectedGame.Moves.Count}";
    }

    private static Disc ResolveCurrentPlayer(string movePlayer, bool beforeMove)
    {
        Disc player = string.Equals(movePlayer, "Black", StringComparison.OrdinalIgnoreCase)
            ? Disc.Black
            : Disc.White;

        if (beforeMove)
        {
            return player;
        }

        return player == Disc.Black ? Disc.White : Disc.Black;
    }

    private static Disc[,] ConvertSnapshotToCells(IReadOnlyList<string> boardRows)
    {
        if (boardRows.Count != Board.BoardSize)
        {
            throw new InvalidDataException("Board snapshot row count must be 8.");
        }

        var cells = new Disc[Board.BoardSize, Board.BoardSize];

        for (int row = 0; row < Board.BoardSize; row++)
        {
            if (boardRows[row].Length != Board.BoardSize)
            {
                throw new InvalidDataException("Board snapshot column count must be 8.");
            }

            for (int col = 0; col < Board.BoardSize; col++)
            {
                cells[row, col] = boardRows[row][col] switch
                {
                    'B' => Disc.Black,
                    'W' => Disc.White,
                    _ => Disc.Empty
                };
            }
        }

        return cells;
    }

    private static string ResolveJsonlPath(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("JSONL path is required.", nameof(inputPath));
        }

        if (Directory.Exists(inputPath))
        {
            string? latestFile = Directory
                .GetFiles(inputPath, "*.jsonl", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (latestFile is null)
            {
                throw new FileNotFoundException("JSONL file not found in directory.", inputPath);
            }

            return latestFile;
        }

        return inputPath;
    }
}
