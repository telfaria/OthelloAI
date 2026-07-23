using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class NeuralOnnxAITests
{
    private const string DefaultModelPath = @"C:\Users\tvb_e\source\repos\OthelloAI\Othello.Python\models\policy_value_best.onnx";

    [Fact]
    public void Constructor_Throws_WhenModelPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new NeuralOnnxAI(string.Empty));
    }

    [Fact]
    public void Constructor_Throws_WhenModelFileDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => new NeuralOnnxAI("missing_model.onnx"));
    }

    [Fact]
    public void DecideMove_ReturnsLegalMove_WhenLegalMovesExist()
    {
        if (!File.Exists(DefaultModelPath))
        {
            return;
        }

        using var ai = new NeuralOnnxAI(DefaultModelPath);
        var board = Board.CreateInitial();

        AIDecision decision = ai.DecideMove(board);

        Assert.NotNull(decision.Move);
        Assert.True(board.IsLegalMove(decision.Move!.Value));
        Assert.False(string.IsNullOrWhiteSpace(decision.Thought));
    }

    [Fact]
    public void DecideMove_ReturnsNullMove_WhenNoLegalMovesExist()
    {
        if (!File.Exists(DefaultModelPath))
        {
            return;
        }

        using var ai = new NeuralOnnxAI(DefaultModelPath);
        var cells = new Disc[Board.BoardSize, Board.BoardSize];
        Fill(cells, Disc.Black);
        var board = Board.FromCells(cells, Disc.White);

        AIDecision decision = ai.DecideMove(board);

        Assert.Null(decision.Move);
        Assert.Equal("No legal moves.", decision.Thought);
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
