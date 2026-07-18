using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects a legal move randomly.
/// </summary>
public sealed class RandomAI : IOthelloAI
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomAI"/> class.
    /// </summary>
    public RandomAI()
        : this(Random.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomAI"/> class with a custom random source.
    /// </summary>
    /// <param name="random">Random source.</param>
    public RandomAI(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <inheritdoc/>
    public string Name => "RandomAI";

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

        int selectedIndex = _random.Next(legalMoves.Count);
        Move selectedMove = legalMoves[selectedIndex];
        string thought = $"Candidates={legalMoves.Count}, Selected={ToCoordinate(selectedMove)}";

        return new AIDecision(selectedMove, thought);
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }
}
