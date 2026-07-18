using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class AlphaBetaAITests
{
    [Fact]
    public void Constructor_Throws_WhenDepthIsLessThanOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AlphaBetaAI(0));
    }

    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        var ai = new AlphaBetaAI(2);
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.Contains("Depth=2", decision.Thought, StringComparison.Ordinal);
        Assert.Contains("Time=", decision.Thought, StringComparison.Ordinal);
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        var ai = new AlphaBetaAI();
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
    }

    [Fact]
    public void DecideMove_PrefersCorner_WhenCornerIsAvailable()
    {
        var ai = new AlphaBetaAI(1);
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

    [Fact]
    public void DecideMove_ProducesFewerNodes_ThanMinimax_AtSameDepth()
    {
        var alphaBeta = new AlphaBetaAI(3, maxDegreeOfParallelism: 1);
        var minimax = new MinimaxAI(3, maxDegreeOfParallelism: 1);
        var board = Board.CreateInitial();

        AIDecision abDecision = alphaBeta.DecideMove(board.Clone());
        AIDecision mmDecision = minimax.DecideMove(board.Clone());

        long abNodes = ParseNodes(abDecision.Thought);
        long mmNodes = ParseNodes(mmDecision.Thought);

        Assert.True(abNodes <= mmNodes, $"AlphaBeta nodes ({abNodes}) should be <= Minimax nodes ({mmNodes})");
    }

    private static long ParseNodes(string thought)
    {
        foreach (string part in thought.Split(','))
        {
            string trimmed = part.Trim();
            if (trimmed.StartsWith("Nodes=", StringComparison.Ordinal))
            {
                return long.Parse(trimmed[6..]);
            }
        }

        throw new InvalidOperationException($"Nodes not found in thought: {thought}");
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
