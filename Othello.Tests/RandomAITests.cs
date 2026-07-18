using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class RandomAITests
{
    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new RandomAI(new Random(1));
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.False(string.IsNullOrWhiteSpace(decision.Thought));
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        var ai = new RandomAI(new Random(1));
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
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
