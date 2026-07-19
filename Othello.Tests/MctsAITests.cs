using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class MctsAITests
{
    [Fact]
    public void Constructor_Throws_WhenIterationsIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MctsAI(0));
    }

    [Fact]
    public void Constructor_Throws_WhenExplorationConstantIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MctsAI(explorationConstant: -1.0));
    }

    [Fact]
    public void Constructor_Throws_WhenMaxDegreeOfParallelismIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MctsAI(maxDegreeOfParallelism: 0));
    }

    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new MctsAI(iterations: 100);
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.Contains("Iterations=100", decision.Thought, StringComparison.Ordinal);
        Assert.Contains("Mode=SingleTree", decision.Thought, StringComparison.Ordinal);
        Assert.Contains("Time=", decision.Thought, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideMove_ParallelRootMode_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new MctsAI(iterations: 100, maxDegreeOfParallelism: 4);
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.Contains("Mode=ParallelRoot", decision.Thought, StringComparison.Ordinal);
        Assert.Contains("Parallelism=4", decision.Thought, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        var ai = new MctsAI(iterations: 10);
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
    }

    [Fact]
    public void DecideMove_CompletesFullGame_WithoutException()
    {
        var ai = new MctsAI(iterations: 50);
        var board = Board.CreateInitial();

        while (!board.IsGameOver())
        {
            if (board.CanPass())
            {
                board.TryPass();
                continue;
            }

            AIDecision decision = ai.DecideMove(board);
            Assert.NotNull(decision.Move);
            board.TryApplyMove(decision.Move!.Value);
        }

        int blackDiscs = board.CountDiscs(Disc.Black);
        int whiteDiscs = board.CountDiscs(Disc.White);
        Assert.Equal(64, blackDiscs + whiteDiscs);
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
