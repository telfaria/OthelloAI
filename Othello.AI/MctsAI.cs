using System.Diagnostics;
using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects a move using Monte Carlo Tree Search (MCTS).
/// </summary>
/// <remarks>
/// MCTS repeats four phases for a fixed iteration budget:
///
///   1. Selection   – Descend the tree by choosing the child with the highest UCB1 score
///                    until a node with unexpanded children or a terminal state is found.
///
///   2. Expansion   – Add one unvisited child of the selected node to the tree.
///
///   3. Simulation  – Play out the game from the new node with random moves
///                    until the game ends (rollout / playout).
///
///   4. Backpropagation – Walk back up the path and update visit count and win count
///                         for every ancestor with the result of the simulation.
///
/// After all iterations the child of the root with the highest visit count is chosen
/// (most-visited is more robust than best win rate for small sample sizes).
///
/// UCB1 formula used during Selection:
///   UCB1 = winRate + C * sqrt(ln(parentVisits) / childVisits)
/// where C (exploration constant) balances exploitation vs exploration.
/// </remarks>
public sealed class MctsAI : IOthelloAI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MctsAI"/> class.
    /// </summary>
    /// <param name="iterations">Number of MCTS iterations per move.</param>
    /// <param name="explorationConstant">UCB1 exploration constant C. Higher values explore more broadly.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel workers for root-level parallelization.</param>
    public MctsAI(int iterations = 1000, double explorationConstant = 1.414, int maxDegreeOfParallelism = 1)
    {
        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be greater than or equal to 1.");
        }

        if (explorationConstant < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(explorationConstant), "Exploration constant must be >= 0.");
        }

        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "MaxDegreeOfParallelism must be >= 1.");
        }

        Iterations = iterations;
        ExplorationConstant = explorationConstant;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Gets the number of MCTS iterations per move.
    /// </summary>
    public int Iterations { get; }

    /// <summary>
    /// Gets the UCB1 exploration constant.
    /// </summary>
    public double ExplorationConstant { get; }

    /// <summary>
    /// Gets the maximum parallel workers for root-level search.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <inheritdoc/>
    public string Name => $"MctsAI(iter={Iterations}, C={ExplorationConstant:F3}, p={MaxDegreeOfParallelism})";

    /// <inheritdoc/>
    public void Reset() { }

    /// <inheritdoc/>
    public AIDecision DecideMove(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        IReadOnlyList<Move> legalMoves = board.GetLegalMoves();
        if (legalMoves.Count == 0)
        {
            return new AIDecision(null, "No legal moves.");
        }

        Disc rootPlayer = board.CurrentPlayer;
        var stopwatch = Stopwatch.StartNew();

        if (MaxDegreeOfParallelism == 1 || legalMoves.Count == 1)
        {
            var root = new MctsNode(board.Clone(), parentMove: null, parent: null);
            RunIterations(root, Iterations, rootPlayer);

            stopwatch.Stop();
            return BuildDecision(root.Children, legalMoves, stopwatch.Elapsed.TotalMilliseconds, parallelMode: false);
        }

        int workerCount = Math.Min(Math.Min(MaxDegreeOfParallelism, legalMoves.Count), Iterations);
        var childResults = new ChildResult[legalMoves.Count];
        int baseIterations = Iterations / legalMoves.Count;
        int remainder = Iterations % legalMoves.Count;

        Parallel.For(
            0,
            legalMoves.Count,
            new ParallelOptions { MaxDegreeOfParallelism = workerCount },
            moveIndex =>
            {
                Move move = legalMoves[moveIndex];
                int localIterations = baseIterations + (moveIndex < remainder ? 1 : 0);

                if (localIterations == 0)
                {
                    childResults[moveIndex] = new ChildResult(move, 0, 0);
                    return;
                }

                Board childBoard = board.Clone();
                childBoard.TryApplyMove(move);

                var childRoot = new MctsNode(childBoard, parentMove: move, parent: null);
                RunIterations(childRoot, localIterations, rootPlayer);

                childResults[moveIndex] = new ChildResult(move, childRoot.Visits, childRoot.Wins);
            });

        stopwatch.Stop();
        return BuildDecision(childResults, legalMoves, stopwatch.Elapsed.TotalMilliseconds, parallelMode: true);
    }

    /// <summary>
    /// Executes full MCTS iterations from a root node.
    /// </summary>
    private void RunIterations(MctsNode root, int iterations, Disc rootPlayer)
    {
        for (int i = 0; i < iterations; i++)
        {
            MctsNode selected = Select(root);
            MctsNode expanded = Expand(selected);
            double result = Simulate(expanded, rootPlayer);
            Backpropagate(expanded, result);
        }
    }

    /// <summary>
    /// Builds the final decision from sequential children results.
    /// </summary>
    private AIDecision BuildDecision(List<MctsNode> children, IReadOnlyList<Move> legalMoves, double elapsedMilliseconds, bool parallelMode)
    {
        if (children.Count == 0)
        {
            return new AIDecision(legalMoves[0], "Fallback to first legal move.");
        }

        MctsNode bestChild = children[0];
        for (int i = 1; i < children.Count; i++)
        {
            if (children[i].Visits > bestChild.Visits)
            {
                bestChild = children[i];
            }
        }

        if (bestChild.ParentMove is null)
        {
            return new AIDecision(legalMoves[0], "Fallback to first legal move.");
        }

        double winRate = bestChild.Visits > 0 ? bestChild.Wins / bestChild.Visits : 0.0;
        string thought = $"Iterations={Iterations}, Mode={(parallelMode ? "ParallelRoot" : "SingleTree")}, Parallelism={MaxDegreeOfParallelism}, BestVisits={bestChild.Visits}, WinRate={winRate:P1}, Selected={ToCoordinate(bestChild.ParentMove.Value)}, Time={elapsedMilliseconds:F1}ms";
        return new AIDecision(bestChild.ParentMove.Value, thought);
    }

    /// <summary>
    /// Builds the final decision from root-parallel aggregated results.
    /// </summary>
    private AIDecision BuildDecision(ChildResult[] results, IReadOnlyList<Move> legalMoves, double elapsedMilliseconds, bool parallelMode)
    {
        if (results.Length == 0)
        {
            return new AIDecision(legalMoves[0], "Fallback to first legal move.");
        }

        ChildResult best = results[0];
        double bestWinRate = best.Visits > 0 ? best.Wins / best.Visits : 0.0;

        for (int i = 1; i < results.Length; i++)
        {
            ChildResult candidate = results[i];
            double candidateWinRate = candidate.Visits > 0 ? candidate.Wins / candidate.Visits : 0.0;

            if (candidateWinRate > bestWinRate)
            {
                best = candidate;
                bestWinRate = candidateWinRate;
                continue;
            }

            if (candidateWinRate == bestWinRate)
            {
                if (candidate.Wins > best.Wins)
                {
                    best = candidate;
                    bestWinRate = candidateWinRate;
                    continue;
                }

                if (candidate.Wins == best.Wins && candidate.Visits > best.Visits)
                {
                    best = candidate;
                    bestWinRate = candidateWinRate;
                }
            }
        }

        string thought = $"Iterations={Iterations}, Mode={(parallelMode ? "ParallelRoot" : "SingleTree")}, Parallelism={MaxDegreeOfParallelism}, BestVisits={best.Visits}, WinRate={bestWinRate:P1}, Selected={ToCoordinate(best.Move)}, Time={elapsedMilliseconds:F1}ms";
        return new AIDecision(best.Move, thought);
    }

    /// <summary>
    /// Descends the tree by UCB1 until a node with unexpanded moves or a terminal is found.
    /// </summary>
    private MctsNode Select(MctsNode node)
    {
        while (node.IsFullyExpanded && !node.IsPassNode && node.Children.Count > 0)
        {
            node = SelectBestUcb(node);
        }

        return node;
    }

    /// <summary>
    /// Expands one unvisited child from the selected node.
    /// Returns the selected node itself when it is terminal.
    /// </summary>
    private MctsNode Expand(MctsNode node)
    {
        if (node.IsTerminal)
        {
            return node;
        }

        // パス局面（合法手なし・終局でない）はパス後の盤面を1つの子として展開する
        if (node.UntriedCount == 0 && node.IsPassNode)
        {
            if (node.Children.Count == 0)
            {
                Board passBoard = node.Board.Clone();
                passBoard.TryPass();
                var passChild = new MctsNode(passBoard, parentMove: null, parent: node);
                node.Children.Add(passChild);
                return passChild;
            }

            return node.Children[0];
        }

        Move nextMove = node.PopUntriedMove();

        Board childBoard = node.Board.Clone();
        childBoard.TryApplyMove(nextMove);

        var child = new MctsNode(childBoard, parentMove: nextMove, parent: node);
        node.Children.Add(child);
        return child;
    }

    /// <summary>
    /// Plays out the game from the given node using random moves.
    /// Returns 1.0 for root player win, 0.5 for draw, 0.0 for loss.
    /// </summary>
    private double Simulate(MctsNode node, Disc rootPlayer)
    {
        Board simulation = node.Board.Clone();

        // stackalloc: 64 = 8x8 、全マス分の容量で十分
        Span<Move> moveBuffer = stackalloc Move[64];

        while (!simulation.IsGameOver())
        {
            int moveCount = simulation.EnumerateLegalMoves(moveBuffer);
            if (moveCount == 0)
            {
                simulation.TryPass();
                continue;
            }

            int index = Random.Shared.Next(moveCount);
            simulation.TryApplyMove(moveBuffer[index]);
        }

        int rootDiscs = simulation.CountDiscs(rootPlayer);
        Disc opponent = rootPlayer == Disc.Black ? Disc.White : Disc.Black;
        int opponentDiscs = simulation.CountDiscs(opponent);

        if (rootDiscs > opponentDiscs) return 1.0;
        if (rootDiscs < opponentDiscs) return 0.0;
        return 0.5;
    }

    /// <summary>
    /// Walks the path back to the root and updates wins and visits for each ancestor.
    /// </summary>
    private static void Backpropagate(MctsNode node, double result)
    {
        MctsNode? current = node;
        while (current is not null)
        {
            current.Visits++;
            current.Wins += result;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Selects the child with the highest UCB1 score.
    /// </summary>
    private MctsNode SelectBestUcb(MctsNode node)
    {
        MctsNode? best = null;
        double bestScore = double.NegativeInfinity;
        double logParentVisits = Math.Log(node.Visits);

        for (int i = 0; i < node.Children.Count; i++)
        {
            MctsNode child = node.Children[i];
            double winRate = child.Wins / child.Visits;
            double exploration = ExplorationConstant * Math.Sqrt(logParentVisits / child.Visits);
            double ucb = winRate + exploration;

            if (ucb > bestScore)
            {
                bestScore = ucb;
                best = child;
            }
        }

        return best!;
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }

    private readonly record struct ChildResult(Move Move, int Visits, double Wins);

    /// <summary>
    /// Represents a single node in the MCTS tree.
    /// </summary>
    private sealed class MctsNode
    {
        private int _untriedCount;
        private Move[] _untriedMoves;

        public MctsNode(Board board, Move? parentMove, MctsNode? parent)
        {
            Board = board;
            ParentMove = parentMove;
            Parent = parent;
            Children = new List<MctsNode>();

            Span<Move> buffer = stackalloc Move[64];
            int count = board.EnumerateLegalMoves(buffer);

            IsTerminal = board.IsGameOver();
            IsPassNode = !IsTerminal && count == 0;

            _untriedMoves = count > 0 ? buffer[..count].ToArray() : [];
            _untriedCount = _untriedMoves.Length;
        }

        public Board Board { get; }
        public Move? ParentMove { get; }
        public MctsNode? Parent { get; }
        public List<MctsNode> Children { get; }
        public int Visits { get; set; }
        public double Wins { get; set; }
        public bool IsTerminal { get; }
        public bool IsPassNode { get; }

        /// <summary>Gets remaining unexpanded moves.</summary>
        public int UntriedCount => _untriedCount;

        public Move PopUntriedMove()
        {
            Move move = _untriedMoves[--_untriedCount];
            return move;
        }

        public bool IsFullyExpanded => _untriedCount == 0 && !IsPassNode;
    }
}
