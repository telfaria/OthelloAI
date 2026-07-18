using System.Collections.Concurrent;
using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects a move using the minimax algorithm.
/// </summary>
public sealed class MinimaxAI : IOthelloAI
{
    private const int DiscDifferenceWeight = 10;
    private const int MobilityWeight = 6;
    private const int CornerWeight = 25;
    private const int PositionalWeight = 2;

    private static readonly int[,] PositionalTable =
    {
        { 120, -20, 20, 5, 5, 20, -20, 120 },
        { -20, -40, -5, -5, -5, -5, -40, -20 },
        { 20, -5, 15, 3, 3, 15, -5, 20 },
        { 5, -5, 3, 3, 3, 3, -5, 5 },
        { 5, -5, 3, 3, 3, 3, -5, 5 },
        { 20, -5, 15, 3, 3, 15, -5, 20 },
        { -20, -40, -5, -5, -5, -5, -40, -20 },
        { 120, -20, 20, 5, 5, 20, -20, 120 }
    };

    private readonly ConcurrentDictionary<CacheKey, int> _transpositionTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinimaxAI"/> class.
    /// </summary>
    /// <param name="searchDepth">Search depth in plies.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of parallel workers for root move evaluation.</param>
    /// <param name="transpositionTableCapacity">Approximate maximum number of cached nodes.</param>
    public MinimaxAI(int searchDepth = 3, int? maxDegreeOfParallelism = null, int transpositionTableCapacity = 1_000_000)
    {
        if (searchDepth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(searchDepth), "Search depth must be greater than or equal to 1.");
        }

        if (maxDegreeOfParallelism is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Max degree of parallelism must be greater than 0.");
        }

        if (transpositionTableCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(transpositionTableCapacity), "Transposition table capacity must be greater than 0.");
        }

        SearchDepth = searchDepth;
        MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount;
        TranspositionTableCapacity = transpositionTableCapacity;
        _transpositionTable = new ConcurrentDictionary<CacheKey, int>(Environment.ProcessorCount, TranspositionTableCapacity);
    }

    /// <summary>
    /// Gets the search depth in plies.
    /// </summary>
    public int SearchDepth { get; }

    /// <summary>
    /// Gets maximum degree of parallelism used for root move evaluation.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>
    /// Gets transposition table capacity.
    /// </summary>
    public int TranspositionTableCapacity { get; }

    /// <inheritdoc/>
    public string Name => $"MinimaxAI(d={SearchDepth}, p={MaxDegreeOfParallelism}, tt={TranspositionTableCapacity})";

    /// <inheritdoc/>
    public AIDecision DecideMove(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        IReadOnlyList<Move> legalMoves = board.GetLegalMoves();
        if (legalMoves.Count == 0)
        {
            return new AIDecision(null, "No legal moves.");
        }

        if (_transpositionTable.Count >= TranspositionTableCapacity)
        {
            _transpositionTable.Clear();
        }

        Disc rootPlayer = board.CurrentPlayer;
        long nodes = 0;
        int bestScore = int.MinValue;
        Move bestMove = legalMoves[0];
        int bestIndex = 0;
        object gate = new object();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        Parallel.For(0, legalMoves.Count, options, i =>
        {
            Move candidate = legalMoves[i];
            Board simulation = board.Clone();
            bool applied = simulation.TryApplyMove(candidate);
            if (!applied)
            {
                return;
            }

            int score = EvaluateMinimax(simulation, SearchDepth - 1, rootPlayer, ref nodes);

            lock (gate)
            {
                if (score > bestScore || (score == bestScore && i < bestIndex))
                {
                    bestScore = score;
                    bestMove = candidate;
                    bestIndex = i;
                }
            }
        });

        string thought = $"Depth={SearchDepth}, Nodes={nodes}, Score={bestScore}, Selected={ToCoordinate(bestMove)}, TT={_transpositionTable.Count}";
        return new AIDecision(bestMove, thought);
    }

    /// <summary>
    /// Recursively evaluates a game tree with minimax.
    /// </summary>
    /// <param name="board">Current simulation board.</param>
    /// <param name="depth">Remaining search depth.</param>
    /// <param name="rootPlayer">Player who started the search.</param>
    /// <param name="nodes">Visited node counter.</param>
    /// <returns>Evaluation score from root player perspective.</returns>
    private int EvaluateMinimax(Board board, int depth, Disc rootPlayer, ref long nodes)
    {
        Interlocked.Increment(ref nodes);

        if (depth == 0 || board.IsGameOver())
        {
            return EvaluateBoard(board, rootPlayer);
        }

        CacheKey cacheKey = new CacheKey(ComputeBoardHash(board), depth);
        if (_transpositionTable.TryGetValue(cacheKey, out int cached))
        {
            return cached;
        }

        IReadOnlyList<Move> legalMoves = board.GetLegalMoves();
        int result;

        if (legalMoves.Count == 0)
        {
            bool passed = board.TryPass();
            result = !passed ? EvaluateBoard(board, rootPlayer) : EvaluateMinimax(board, depth - 1, rootPlayer, ref nodes);
            TryCache(cacheKey, result);
            return result;
        }

        bool isMaxTurn = board.CurrentPlayer == rootPlayer;
        if (isMaxTurn)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                Board child = board.Clone();
                bool applied = child.TryApplyMove(legalMoves[i]);
                if (!applied)
                {
                    continue;
                }

                int score = EvaluateMinimax(child, depth - 1, rootPlayer, ref nodes);
                if (score > bestScore)
                {
                    bestScore = score;
                }
            }

            result = bestScore;
        }
        else
        {
            int minScore = int.MaxValue;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                Board child = board.Clone();
                bool applied = child.TryApplyMove(legalMoves[i]);
                if (!applied)
                {
                    continue;
                }

                int score = EvaluateMinimax(child, depth - 1, rootPlayer, ref nodes);
                if (score < minScore)
                {
                    minScore = score;
                }
            }

            result = minScore;
        }

        TryCache(cacheKey, result);
        return result;
    }

    private void TryCache(CacheKey key, int score)
    {
        if (_transpositionTable.Count < TranspositionTableCapacity)
        {
            _transpositionTable.TryAdd(key, score);
        }
    }

    private static ulong ComputeBoardHash(Board board)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offset;
        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                hash ^= (ulong)board.GetDisc(new Position(row, col));
                hash *= prime;
            }
        }

        hash ^= (ulong)board.CurrentPlayer;
        hash *= prime;
        return hash;
    }

    private static int EvaluateBoard(Board board, Disc rootPlayer)
    {
        Disc opponent = rootPlayer == Disc.Black ? Disc.White : Disc.Black;

        int discDifference = board.CountDiscs(rootPlayer) - board.CountDiscs(opponent);
        int mobilityDifference = CountLegalMovesForPlayer(board, rootPlayer) - CountLegalMovesForPlayer(board, opponent);
        int cornerDifference = CountCornerDiscs(board, rootPlayer) - CountCornerDiscs(board, opponent);
        int positionalDifference = CalculatePositionalScore(board, rootPlayer, opponent);

        return (discDifference * DiscDifferenceWeight)
            + (mobilityDifference * MobilityWeight)
            + (cornerDifference * CornerWeight)
            + (positionalDifference * PositionalWeight);
    }

    private static int CountLegalMovesForPlayer(Board board, Disc player)
    {
        Board playerView = CreateBoardWithCurrentPlayer(board, player);
        return playerView.GetLegalMoves().Count;
    }

    private static int CountCornerDiscs(Board board, Disc player)
    {
        int last = Board.BoardSize - 1;
        int count = 0;

        if (board.GetDisc(new Position(0, 0)) == player)
        {
            count++;
        }

        if (board.GetDisc(new Position(0, last)) == player)
        {
            count++;
        }

        if (board.GetDisc(new Position(last, 0)) == player)
        {
            count++;
        }

        if (board.GetDisc(new Position(last, last)) == player)
        {
            count++;
        }

        return count;
    }

    private static int CalculatePositionalScore(Board board, Disc player, Disc opponent)
    {
        int score = 0;

        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                Disc disc = board.GetDisc(new Position(row, col));
                if (disc == player)
                {
                    score += PositionalTable[row, col];
                }
                else if (disc == opponent)
                {
                    score -= PositionalTable[row, col];
                }
            }
        }

        return score;
    }

    private static Board CreateBoardWithCurrentPlayer(Board board, Disc player)
    {
        var cells = new Disc[Board.BoardSize, Board.BoardSize];

        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                cells[row, col] = board.GetDisc(new Position(row, col));
            }
        }

        return Board.FromCells(cells, player);
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }

    private readonly record struct CacheKey(ulong Hash, int Depth);
}
