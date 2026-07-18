using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class MinimaxAITests
{
    [Fact]
    public void Constructor_Throws_WhenDepthIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MinimaxAI(0));
    }

    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new MinimaxAI(2);
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.Contains("Depth=2", decision.Thought, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        var ai = new MinimaxAI();
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
    }

    [Fact]
    public void DecideMove_DepthOne_PrefersCorner_WhenCornerIsAvailable()
    {
        var ai = new MinimaxAI(1);
        var cells = new Disc[Board.BoardSize, Board.BoardSize];

        cells[0, 1] = Disc.White;
        cells[0, 2] = Disc.Black;
        cells[2, 0] = Disc.Black;
        cells[2, 1] = Disc.White;

        var board = Board.FromCells(cells, Disc.Black);

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.Equal(new Move(0, 0), decision.Move.Value);
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
