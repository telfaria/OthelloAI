using System.Collections.Concurrent;
using System.Diagnostics;
using Othello.AI;
using Othello.Core;

namespace Othello.Console;

/// <summary>
/// Manages self-play execution lifecycle including game generation, worker orchestration, and cancellation.
/// </summary>
public sealed class SelfPlayManager
{
    private readonly int _gameCount;
    private readonly bool _showThinking;
    private readonly int _maxDegreeOfParallelism;
    private readonly Func<IOthelloAI> _blackAIFactory;
    private readonly Func<IOthelloAI> _whiteAIFactory;
    private readonly GameWriter _gameWriter;
    private readonly Action<string> _writeLine;
    private readonly Action<Board> _renderBoard;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfPlayManager"/> class.
    /// </summary>
    public SelfPlayManager(
        int gameCount,
        bool showThinking,
        int maxDegreeOfParallelism,
        Func<IOthelloAI> blackAIFactory,
        Func<IOthelloAI> whiteAIFactory,
        GameWriter gameWriter,
        Action<string> writeLine,
        Action<Board> renderBoard)
    {
        if (gameCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gameCount), "Game count must be >= 1.");
        }

        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "MaxDegreeOfParallelism must be >= 1.");
        }

        _gameCount = gameCount;
        _showThinking = showThinking;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _blackAIFactory = blackAIFactory ?? throw new ArgumentNullException(nameof(blackAIFactory));
        _whiteAIFactory = whiteAIFactory ?? throw new ArgumentNullException(nameof(whiteAIFactory));
        _gameWriter = gameWriter ?? throw new ArgumentNullException(nameof(gameWriter));
        _writeLine = writeLine ?? throw new ArgumentNullException(nameof(writeLine));
        _renderBoard = renderBoard ?? throw new ArgumentNullException(nameof(renderBoard));
    }

    /// <summary>
    /// Runs all configured games with parallel workers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated simulation summary.</returns>
    public SelfPlaySimulationSummary Run(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        int progressInterval = _gameCount >= 10 ? Math.Max(1, _gameCount / 10) : int.MaxValue;

        int blackWins = 0;
        int whiteWins = 0;
        int draws = 0;
        long totalBlackDiscs = 0;
        long totalWhiteDiscs = 0;
        long totalMoves = 0;
        long totalPasses = 0;
        long totalMargin = 0;
        int maxMargin = 0;
        int completedGames = 0;

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _maxDegreeOfParallelism
        };

        Parallel.ForEach(Partitioner.Create(1, _gameCount + 1), options, range =>
        {
            var worker = new SelfPlayWorker(
                _blackAIFactory,
                _whiteAIFactory,
                _showThinking,
                _writeLine,
                _renderBoard);

            for (int gameIndex = range.Item1; gameIndex < range.Item2; gameIndex++)
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                SelfPlayWorkerResult result = worker.RunGame(gameIndex, options.CancellationToken);
                _gameWriter.Enqueue(result.GameRecord);

                Interlocked.Add(ref totalBlackDiscs, result.BlackCount);
                Interlocked.Add(ref totalWhiteDiscs, result.WhiteCount);
                Interlocked.Add(ref totalMoves, result.MovesInGame);
                Interlocked.Add(ref totalPasses, result.PassesInGame);
                Interlocked.Add(ref totalMargin, result.Margin);

                if (result.BlackCount > result.WhiteCount)
                {
                    Interlocked.Increment(ref blackWins);
                }
                else if (result.WhiteCount > result.BlackCount)
                {
                    Interlocked.Increment(ref whiteWins);
                }
                else
                {
                    Interlocked.Increment(ref draws);
                }

                UpdateMax(ref maxMargin, result.Margin);

                if (!_showThinking)
                {
                    int done = Interlocked.Increment(ref completedGames);
                    if (done % progressInterval == 0 || done == _gameCount)
                    {
                        _writeLine($"Progress: {done}/{_gameCount}");
                    }
                }
            }
        });

        _gameWriter.Flush(cancellationToken);

        stopwatch.Stop();

        return new SelfPlaySimulationSummary
        {
            BlackWins = blackWins,
            WhiteWins = whiteWins,
            Draws = draws,
            TotalBlackDiscs = totalBlackDiscs,
            TotalWhiteDiscs = totalWhiteDiscs,
            TotalMoves = totalMoves,
            TotalPasses = totalPasses,
            TotalMargin = totalMargin,
            MaxMargin = maxMargin,
            Elapsed = stopwatch.Elapsed
        };
    }

    private static void UpdateMax(ref int target, int candidate)
    {
        int current;
        do
        {
            current = target;
            if (candidate <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, candidate, current) != current);
    }
}

/// <summary>
/// Executes one full self-play game and generates one game record.
/// </summary>
public sealed class SelfPlayWorker
{
    private readonly Func<IOthelloAI> _blackAIFactory;
    private readonly Func<IOthelloAI> _whiteAIFactory;
    private readonly bool _showThinking;
    private readonly Action<string> _writeLine;
    private readonly Action<Board> _renderBoard;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfPlayWorker"/> class.
    /// </summary>
    public SelfPlayWorker(
        Func<IOthelloAI> blackAIFactory,
        Func<IOthelloAI> whiteAIFactory,
        bool showThinking,
        Action<string> writeLine,
        Action<Board> renderBoard)
    {
        _blackAIFactory = blackAIFactory ?? throw new ArgumentNullException(nameof(blackAIFactory));
        _whiteAIFactory = whiteAIFactory ?? throw new ArgumentNullException(nameof(whiteAIFactory));
        _showThinking = showThinking;
        _writeLine = writeLine ?? throw new ArgumentNullException(nameof(writeLine));
        _renderBoard = renderBoard ?? throw new ArgumentNullException(nameof(renderBoard));
    }

    /// <summary>
    /// Runs one game.
    /// </summary>
    /// <param name="gameIndex">Game index for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Game execution result.</returns>
    public SelfPlayWorkerResult RunGame(int gameIndex, CancellationToken cancellationToken)
    {
        IOthelloAI blackAI = _blackAIFactory();
        IOthelloAI whiteAI = _whiteAIFactory();

        blackAI.Reset();
        whiteAI.Reset();

        Board board = Board.CreateInitial();
        int movesInGame = 0;
        int passesInGame = 0;
        int ply = 1;
        var recorder = new SelfPlayKifuRecorder(blackAI.Name, whiteAI.Name);

        while (!board.IsGameOver())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_showThinking)
            {
                _writeLine($"[Game {gameIndex}] Turn={GetPlayerName(board.CurrentPlayer)}");
                _renderBoard(board);
            }

            string[] boardBefore = SelfPlayKifuRecorder.CaptureSnapshot(board);
            Disc currentPlayer = board.CurrentPlayer;
            IOthelloAI currentAI = currentPlayer == Disc.Black ? blackAI : whiteAI;
            AIDecision decision = currentAI.DecideMove(board.Clone());

            if (decision.Move is null)
            {
                bool passed = board.TryPass();
                if (!passed)
                {
                    throw new InvalidOperationException("AI returned no move while legal moves exist.");
                }

                string[] boardAfterPass = SelfPlayKifuRecorder.CaptureSnapshot(board);
                recorder.AppendMove(ply, currentPlayer, null, boardBefore, boardAfterPass, decision.Thought);
                ply++;

                passesInGame++;
                if (_showThinking)
                {
                    _writeLine($"{currentAI.Name} ({GetPlayerName(board.CurrentPlayer == Disc.Black ? Disc.White : Disc.Black)}): pass, {decision.Thought}");
                }

                continue;
            }

            bool applied = board.TryApplyMove(decision.Move.Value);
            if (!applied)
            {
                throw new InvalidOperationException($"AI selected an illegal move: {ToCoordinate(decision.Move.Value)}");
            }

            string[] boardAfterMove = SelfPlayKifuRecorder.CaptureSnapshot(board);
            recorder.AppendMove(ply, currentPlayer, decision.Move, boardBefore, boardAfterMove, decision.Thought);
            ply++;

            movesInGame++;
            if (_showThinking)
            {
                _writeLine($"{currentAI.Name} played {ToCoordinate(decision.Move.Value)} | {decision.Thought}");
            }
        }

        int blackCount = board.CountDiscs(Disc.Black);
        int whiteCount = board.CountDiscs(Disc.White);
        int margin = Math.Abs(blackCount - whiteCount);

        return new SelfPlayWorkerResult(
            recorder.Finalize(blackCount, whiteCount),
            blackCount,
            whiteCount,
            margin,
            movesInGame,
            passesInGame);
    }

    private static string GetPlayerName(Disc player)
    {
        return player == Disc.Black ? "Black" : "White";
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }
}

/// <summary>
/// Writes game records into persistent storage through an in-memory queue.
/// </summary>
public sealed class GameWriter
{
    private readonly ConcurrentQueue<SelfPlayGameRecord> _queue;
    private readonly SelfPlayKifuStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameWriter"/> class.
    /// </summary>
    public GameWriter(SelfPlayKifuStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _queue = new ConcurrentQueue<SelfPlayGameRecord>();
    }

    /// <summary>
    /// Gets output JSONL file path.
    /// </summary>
    public string FilePath => _store.FilePath;

    /// <summary>
    /// Enqueues one game record.
    /// </summary>
    public void Enqueue(SelfPlayGameRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _queue.Enqueue(record);
    }

    /// <summary>
    /// Flushes all queued records into JSONL storage.
    /// </summary>
    public void Flush(CancellationToken cancellationToken)
    {
        while (_queue.TryDequeue(out SelfPlayGameRecord? record))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _store.Append(record);
        }
    }
}

/// <summary>
/// Aggregated simulation statistics.
/// </summary>
public sealed class SelfPlaySimulationSummary
{
    public int BlackWins { get; set; }
    public int WhiteWins { get; set; }
    public int Draws { get; set; }
    public long TotalBlackDiscs { get; set; }
    public long TotalWhiteDiscs { get; set; }
    public long TotalMoves { get; set; }
    public long TotalPasses { get; set; }
    public long TotalMargin { get; set; }
    public int MaxMargin { get; set; }
    public TimeSpan Elapsed { get; set; }
}

/// <summary>
/// Represents one completed game result from a worker.
/// </summary>
/// <param name="GameRecord">Generated game record.</param>
/// <param name="BlackCount">Final black disc count.</param>
/// <param name="WhiteCount">Final white disc count.</param>
/// <param name="Margin">Final absolute margin.</param>
/// <param name="MovesInGame">Number of applied moves.</param>
/// <param name="PassesInGame">Number of pass turns.</param>
public readonly record struct SelfPlayWorkerResult(
    SelfPlayGameRecord GameRecord,
    int BlackCount,
    int WhiteCount,
    int Margin,
    int MovesInGame,
    int PassesInGame);
