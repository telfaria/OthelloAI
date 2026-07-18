using Othello.Core;

namespace Othello.Tests;

public class BoardTests
{
    [Fact]
    public void CreateInitial_SetsStandardStartingPosition()
    {
        var board = Board.CreateInitial();

        Assert.Equal(Disc.White, board.GetDisc(new Position(3, 3)));
        Assert.Equal(Disc.Black, board.GetDisc(new Position(3, 4)));
        Assert.Equal(Disc.Black, board.GetDisc(new Position(4, 3)));
        Assert.Equal(Disc.White, board.GetDisc(new Position(4, 4)));
        Assert.Equal(Disc.Black, board.CurrentPlayer);
    }

    [Fact]
    public void GetLegalMoves_ReturnsFourMoves_ForInitialBoard()
    {
        var board = Board.CreateInitial();

        var moves = board.GetLegalMoves();

        Assert.Equal(4, moves.Count);
        Assert.Contains(new Move(2, 3), moves);
        Assert.Contains(new Move(3, 2), moves);
        Assert.Contains(new Move(4, 5), moves);
        Assert.Contains(new Move(5, 4), moves);
    }

    [Fact]
    public void TryApplyMove_FlipsDiscs_AndChangesTurn_WhenMoveIsLegal()
    {
        var board = Board.CreateInitial();

        var applied = board.TryApplyMove(new Move(2, 3));

        Assert.True(applied);
        Assert.Equal(Disc.Black, board.GetDisc(new Position(2, 3)));
        Assert.Equal(Disc.Black, board.GetDisc(new Position(3, 3)));
        Assert.Equal(Disc.White, board.CurrentPlayer);
    }

    [Fact]
    public void TryApplyMove_ReturnsFalse_AndKeepsState_WhenMoveIsIllegal()
    {
        var board = Board.CreateInitial();

        var applied = board.TryApplyMove(new Move(0, 0));

        Assert.False(applied);
        Assert.Equal(Disc.Empty, board.GetDisc(new Position(0, 0)));
        Assert.Equal(Disc.Black, board.CurrentPlayer);
    }

    [Fact]
    public void Clone_CreatesIndependentBoardCopy()
    {
        var original = Board.CreateInitial();
        var copied = original.Clone();

        var copiedApplied = copied.TryApplyMove(new Move(2, 3));

        Assert.True(copiedApplied);
        Assert.Equal(Disc.Empty, original.GetDisc(new Position(2, 3)));
        Assert.Equal(Disc.Black, copied.GetDisc(new Position(2, 3)));
    }

    [Fact]
    public void CanPass_ReturnsTrue_WhenCurrentPlayerHasNoLegalMove()
    {
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        cells[0, 0] = Disc.White;
        var board = Board.FromCells(cells, Disc.White);

        var canPass = board.CanPass();

        Assert.True(canPass);
    }

    [Fact]
    public void TryPass_ChangesTurn_WhenCurrentPlayerHasNoLegalMove()
    {
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        cells[0, 0] = Disc.White;
        var board = Board.FromCells(cells, Disc.White);

        var passed = board.TryPass();

        Assert.True(passed);
        Assert.Equal(Disc.Black, board.CurrentPlayer);
    }

    [Fact]
    public void TryPass_ReturnsFalse_WhenCurrentPlayerHasLegalMove()
    {
        var board = Board.CreateInitial();

        var passed = board.TryPass();

        Assert.False(passed);
        Assert.Equal(Disc.Black, board.CurrentPlayer);
    }

    [Fact]
    public void IsGameOver_ReturnsTrue_WhenNeitherPlayerHasLegalMoves()
    {
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        var isGameOver = board.IsGameOver();

        Assert.True(isGameOver);
    }

    private static void Fill(Disc[,] cells, Disc value)
    {
        for (var row = 0; row < Board.BoardSize; row++)
        {
            for (var col = 0; col < Board.BoardSize; col++)
            {
                cells[row, col] = value;
            }
        }
    }
}
