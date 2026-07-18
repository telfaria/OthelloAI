using System.Collections.Concurrent;
using System.Diagnostics;
using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects a move using the minimax algorithm with alpha-beta pruning.
/// </summary>
/// <remarks>
/// Alpha-beta pruning eliminates branches that cannot influence the final decision.
///
/// Two bounds are maintained at each node:
///   alpha: best score the maximizer (root player) is guaranteed so far.
///   beta:  best score the minimizer (opponent) is guaranteed so far.
///
/// When alpha >= beta, the remaining siblings can never be chosen by the opponent (beta cutoff)
/// or by us (alpha cutoff), so the search stops early.
///
/// This reduces the effective branching factor from B to roughly sqrt(B) in the best case,
/// allowing roughly twice the search depth compared to plain minimax for the same time budget.
/// </remarks>
public sealed class AlphaBetaAI : IOthelloAI
{
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

    private readonly EvaluationWeights _weights;
    private readonly ConcurrentDictionary<CacheKey, int> _transpositionTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlphaBetaAI"/> class.
    /// </summary>
    /// <param name="searchDepth">Search depth in plies.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel workers for root move evaluation.</param>
    /// <param name="transpositionTableCapacity">Approximate maximum number of cached nodes.</param>
    /// <param name="weights">Evaluation weights. Uses default when null.</param>
    public AlphaBetaAI(int searchDepth = 4, int? maxDegreeOfParallelism = null, int transpositionTableCapacity = 1_000_000, EvaluationWeights? weights = null)
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
        _weights = weights ?? EvaluationWeights.Default;
        _transpositionTable = new ConcurrentDictionary<CacheKey, int>(Environment.ProcessorCount, TranspositionTableCapacity);
    }

    /// <summary>
    /// Gets the search depth in plies.
    /// </summary>
    public int SearchDepth { get; }

    /// <summary>
    /// Gets maximum degree of parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>
    /// Gets the transposition table capacity.
    /// </summary>
    public int TranspositionTableCapacity { get; }

    /// <inheritdoc/>
    public string Name => $"AlphaBetaAI(d={SearchDepth}, p={MaxDegreeOfParallelism}, tt={TranspositionTableCapacity}, w=[{_weights}])";

    /// <inheritdoc/>
    public void Reset()
    {
        _transpositionTable.Clear();
    }

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

        var stopwatch = Stopwatch.StartNew();

        // Root moves are evaluated in parallel.
        // Each worker starts with full alpha/beta window so pruning between workers is not applied
        // at the root level, but sub-trees are pruned independently per worker.
        Parallel.For(0, legalMoves.Count, options, i =>
        {
            Move candidate = legalMoves[i];
            Board simulation = board.Clone();
            bool applied = simulation.TryApplyMove(candidate);
            if (!applied)
            {
                return;
            }

            int score = EvaluateAlphaBeta(simulation, SearchDepth - 1, int.MinValue, int.MaxValue, rootPlayer, ref nodes);

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

        stopwatch.Stop();

        string thought = $"Depth={SearchDepth}, Nodes={nodes}, Score={bestScore}, Selected={ToCoordinate(bestMove)}, TT={_transpositionTable.Count}, Time={stopwatch.Elapsed.TotalMilliseconds:F1}ms";
        return new AIDecision(bestMove, thought);
    }

    /// <summary>
    /// Recursively evaluates a node using alpha-beta pruning.
    /// </summary>
    /// <param name="board">Current simulation board.</param>
    /// <param name="depth">Remaining search depth.</param>
    /// <param name="alpha">Best score the maximizer can guarantee so far.</param>
    /// <param name="beta">Best score the minimizer can guarantee so far.</param>
    /// <param name="rootPlayer">Player who initiated the search.</param>
    /// <param name="nodes">Visited node counter.</param>
    /// <returns>Evaluation score from root player perspective.</returns>
    private int EvaluateAlphaBeta(Board board, int depth, int alpha, int beta, Disc rootPlayer, ref long nodes)
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
            result = !passed
                ? EvaluateBoard(board, rootPlayer)
                : EvaluateAlphaBeta(board, depth - 1, alpha, beta, rootPlayer, ref nodes);
            TryCache(cacheKey, result);
            return result;
        }

        bool isMaxTurn = board.CurrentPlayer == rootPlayer;
        if (isMaxTurn)
        {
            // Maximizer: raise alpha, prune when alpha >= beta (beta cutoff).
            int bestScore = int.MinValue;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                Board child = board.Clone();
                bool applied = child.TryApplyMove(legalMoves[i]);
                if (!applied)
                {
                    continue;
                }

                int score = EvaluateAlphaBeta(child, depth - 1, alpha, beta, rootPlayer, ref nodes);
                if (score > bestScore)
                {
                    bestScore = score;
                }

                if (bestScore > alpha)
                {
                    alpha = bestScore;
                }

                // Beta cutoff: minimizer would avoid this node.
                if (alpha >= beta)
                {
                    break;
                }
            }

            result = bestScore;
        }
        else
        {
            // Minimizer: lower beta, prune when alpha >= beta (alpha cutoff).
            int minScore = int.MaxValue;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                Board child = board.Clone();
                bool applied = child.TryApplyMove(legalMoves[i]);
                if (!applied)
                {
                    continue;
                }

                int score = EvaluateAlphaBeta(child, depth - 1, alpha, beta, rootPlayer, ref nodes);
                if (score < minScore)
                {
                    minScore = score;
                }

                if (minScore < beta)
                {
                    beta = minScore;
                }

                // Alpha cutoff: maximizer would avoid this node.
                if (alpha >= beta)
                {
                    break;
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

    private int EvaluateBoard(Board board, Disc rootPlayer)
    {
        Disc opponent = rootPlayer == Disc.Black ? Disc.White : Disc.Black;

        int discDifference = board.CountDiscs(rootPlayer) - board.CountDiscs(opponent);
        int mobilityDifference = CountLegalMovesForPlayer(board, rootPlayer) - CountLegalMovesForPlayer(board, opponent);
        int cornerDifference = CountCornerDiscs(board, rootPlayer) - CountCornerDiscs(board, opponent);
        int positionalDifference = CalculatePositionalScore(board, rootPlayer, opponent);

        return (discDifference * _weights.DiscDifference)
            + (mobilityDifference * _weights.Mobility)
            + (cornerDifference * _weights.Corner)
            + (positionalDifference * _weights.Positional);
    }

    private static int CountLegalMovesForPlayer(Board board, Disc player)
    {
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                cells[row, col] = board.GetDisc(new Position(row, col));
            }
        }

        return Board.FromCells(cells, player).GetLegalMoves().Count;
    }

    private static int CountCornerDiscs(Board board, Disc player)
    {
        int last = Board.BoardSize - 1;
        int count = 0;

        if (board.GetDisc(new Position(0, 0)) == player) count++;
        if (board.GetDisc(new Position(0, last)) == player) count++;
        if (board.GetDisc(new Position(last, 0)) == player) count++;
        if (board.GetDisc(new Position(last, last)) == player) count++;

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

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }

    private readonly record struct CacheKey(ulong Hash, int Depth);
}
