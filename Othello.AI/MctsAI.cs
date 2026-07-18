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
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="MctsAI"/> class.
    /// </summary>
    /// <param name="iterations">Number of MCTS iterations per move.</param>
    /// <param name="explorationConstant">UCB1 exploration constant C. Higher values explore more broadly.</param>
    public MctsAI(int iterations = 1000, double explorationConstant = 1.414)
    {
        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be greater than or equal to 1.");
        }

        if (explorationConstant < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(explorationConstant), "Exploration constant must be >= 0.");
        }

        Iterations = iterations;
        ExplorationConstant = explorationConstant;
        _random = new Random();
    }

    /// <summary>
    /// Gets the number of MCTS iterations per move.
    /// </summary>
    public int Iterations { get; }

    /// <summary>
    /// Gets the UCB1 exploration constant.
    /// </summary>
    public double ExplorationConstant { get; }

    /// <inheritdoc/>
    public string Name => $"MctsAI(iter={Iterations}, C={ExplorationConstant:F3})";

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
        var root = new MctsNode(board.Clone(), parentMove: null, parent: null);

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < Iterations; i++)
        {
            // 1. Selection
            MctsNode selected = Select(root);

            // 2. Expansion
            MctsNode expanded = Expand(selected);

            // 3. Simulation
            double result = Simulate(expanded, rootPlayer);

            // 4. Backpropagation
            Backpropagate(expanded, result);
        }

        stopwatch.Stop();

        // 最も訪問回数が多い子を選択する（勝率ではなく訪問数が選択基準として安定）
        MctsNode? bestChild = null;
        int bestVisits = -1;

        for (int i = 0; i < root.Children.Count; i++)
        {
            MctsNode child = root.Children[i];
            if (child.Visits > bestVisits)
            {
                bestVisits = child.Visits;
                bestChild = child;
            }
        }

        if (bestChild?.ParentMove is null)
        {
            return new AIDecision(legalMoves[0], "Fallback to first legal move.");
        }

        double winRate = bestChild.Visits > 0 ? bestChild.Wins / bestChild.Visits : 0.0;
        string thought = $"Iterations={Iterations}, BestVisits={bestVisits}, WinRate={winRate:P1}, Selected={ToCoordinate(bestChild.ParentMove.Value)}, Time={stopwatch.Elapsed.TotalMilliseconds:F1}ms";
        return new AIDecision(bestChild.ParentMove.Value, thought);
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
        if (node.UntriedMoves.Count == 0 && node.IsPassNode)
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

        Move nextMove = node.UntriedMoves[^1];
        node.UntriedMoves.RemoveAt(node.UntriedMoves.Count - 1);

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

        while (!simulation.IsGameOver())
        {
            IReadOnlyList<Move> moves = simulation.GetLegalMoves();
            if (moves.Count == 0)
            {
                simulation.TryPass();
                continue;
            }

            int index = _random.Next(moves.Count);
            simulation.TryApplyMove(moves[index]);
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

    /// <summary>
    /// Represents a single node in the MCTS tree.
    /// </summary>
    private sealed class MctsNode
    {
        /// <summary>
        /// Initializes a new MCTS node.
        /// </summary>
        public MctsNode(Board board, Move? parentMove, MctsNode? parent)
        {
            Board = board;
            ParentMove = parentMove;
            Parent = parent;
            Children = new List<MctsNode>();

            IReadOnlyList<Move> legalMoves = board.GetLegalMoves();
            IsTerminal = board.IsGameOver();
            IsPassNode = !IsTerminal && legalMoves.Count == 0;
            UntriedMoves = new List<Move>(legalMoves);
        }

        public Board Board { get; }

        /// <summary>Gets the move that led to this node from the parent.</summary>
        public Move? ParentMove { get; }

        public MctsNode? Parent { get; }

        public List<MctsNode> Children { get; }

        /// <summary>Gets remaining unexpanded moves.</summary>
        public List<Move> UntriedMoves { get; }

        public int Visits { get; set; }

        public double Wins { get; set; }

        public bool IsTerminal { get; }

        /// <summary>Gets whether this node represents a pass-only state (no legal moves, not game over).</summary>
        public bool IsPassNode { get; }

        /// <summary>Gets whether all legal moves have been expanded into children.</summary>
        public bool IsFullyExpanded => UntriedMoves.Count == 0 && !IsPassNode;
    }
}
