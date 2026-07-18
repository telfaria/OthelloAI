using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class GreedyAITests
{
    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new GreedyAI();
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.False(string.IsNullOrWhiteSpace(decision.Thought));
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        var ai = new GreedyAI();
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
    }

    [Fact]
    public void DecideMove_SelectsMoveWithHighestImmediateDiscDifference()
    {
        var ai = new GreedyAI();
        var board = Board.CreateInitial();

        board.TryApplyMove(new Move(2, 3));
        board.TryApplyMove(new Move(2, 2));
        board.TryApplyMove(new Move(2, 1));

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);

        Disc player = board.CurrentPlayer;
        Disc opponent = player == Disc.Black ? Disc.White : Disc.Black;

        int selectedScore = Evaluate(board, decision.Move.Value, player, opponent);
        IReadOnlyList<Move> legalMoves = board.GetLegalMoves();

        int maxScore = int.MinValue;
        for (int i = 0; i < legalMoves.Count; i++)
        {
            int score = Evaluate(board, legalMoves[i], player, opponent);
            if (score > maxScore)
            {
                maxScore = score;
            }
        }

        Assert.Equal(maxScore, selectedScore);
    }

    private static int Evaluate(Board board, Move move, Disc player, Disc opponent)
    {
        Board simulation = board.Clone();
        bool applied = simulation.TryApplyMove(move);

        Assert.True(applied);
        return simulation.CountDiscs(player) - simulation.CountDiscs(opponent);
    }

    private static void Fill(Disc[,] cells, Disc value)
    {
        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                cells[row, col] = value;
            }
        }
    }
}
