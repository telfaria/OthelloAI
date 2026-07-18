using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects the move with the best immediate board score.
/// </summary>
public sealed class GreedyAI : IOthelloAI
{
    /// <inheritdoc/>
    public string Name => "GreedyAI";

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

        Disc player = board.CurrentPlayer;
        Disc opponent = player == Disc.Black ? Disc.White : Disc.Black;

        Move bestMove = legalMoves[0];
        int bestScore = int.MinValue;

        for (int i = 0; i < legalMoves.Count; i++)
        {
            Move candidate = legalMoves[i];
            Board simulation = board.Clone();

            bool applied = simulation.TryApplyMove(candidate);
            if (!applied)
            {
                continue;
            }

            int score = simulation.CountDiscs(player) - simulation.CountDiscs(opponent);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = candidate;
            }
        }

        string thought = $"Candidates={legalMoves.Count}, BestScore={bestScore}, Selected={ToCoordinate(bestMove)}";
        return new AIDecision(bestMove, thought);
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }
}
