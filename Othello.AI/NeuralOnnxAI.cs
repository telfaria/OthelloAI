using System.Globalization;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Selects moves by running an ONNX policy/value model.
/// </summary>
public sealed class NeuralOnnxAI : IOthelloAI, IDisposable
{
    private const int BoardSize = 8;

    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string _policyOutputName;
    private readonly string _valueOutputName;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeuralOnnxAI"/> class.
    /// </summary>
    /// <param name="modelPath">Path to ONNX model.</param>
    /// <param name="inputName">Input tensor name. Default is <c>board</c>.</param>
    /// <param name="policyOutputName">Policy output tensor name. Default is <c>policy_logits</c>.</param>
    /// <param name="valueOutputName">Value output tensor name. Default is <c>value</c>.</param>
    public NeuralOnnxAI(
        string modelPath,
        string inputName = "board",
        string policyOutputName = "policy_logits",
        string valueOutputName = "value")
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Model path is required.", nameof(modelPath));
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("ONNX model file was not found.", modelPath);
        }

        if (string.IsNullOrWhiteSpace(inputName))
        {
            throw new ArgumentException("Input name is required.", nameof(inputName));
        }

        if (string.IsNullOrWhiteSpace(policyOutputName))
        {
            throw new ArgumentException("Policy output name is required.", nameof(policyOutputName));
        }

        if (string.IsNullOrWhiteSpace(valueOutputName))
        {
            throw new ArgumentException("Value output name is required.", nameof(valueOutputName));
        }

        _session = new InferenceSession(modelPath);
        _inputName = inputName;
        _policyOutputName = policyOutputName;
        _valueOutputName = valueOutputName;
    }

    /// <inheritdoc/>
    public string Name => "NeuralOnnxAI";

    /// <inheritdoc/>
    public void Reset()
    {
    }

    /// <inheritdoc/>
    public AIDecision DecideMove(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);
        ThrowIfDisposed();

        Span<Move> legalMoves = stackalloc Move[BoardSize * BoardSize];
        int moveCount = board.EnumerateLegalMoves(legalMoves);

        if (moveCount == 0)
        {
            return new AIDecision(null, "No legal moves.");
        }

        var inputTensor = new DenseTensor<float>(new[] { 1, 3, BoardSize, BoardSize });
        FillInputTensor(board, inputTensor);

        var inputs = new List<NamedOnnxValue>(1)
        {
            NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
        };

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run(inputs);

        Tensor<float> policyTensor = GetOutputTensor(outputs, _policyOutputName);
        Tensor<float> valueTensor = GetOutputTensor(outputs, _valueOutputName);

        Move selected = SelectBestMove(policyTensor, legalMoves[..moveCount]);
        float value = valueTensor[0, 0];

        string thought =
            $"Candidates={moveCount}, Selected={ToCoordinate(selected)}, Value={value.ToString("F3", CultureInfo.InvariantCulture)}";

        return new AIDecision(selected, thought);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }

    private static void FillInputTensor(Board board, DenseTensor<float> tensor)
    {
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                Disc disc = board.GetDisc(new Position(row, col));
                if (disc == Disc.Black)
                {
                    tensor[0, 0, row, col] = 1f;
                }
                else if (disc == Disc.White)
                {
                    tensor[0, 1, row, col] = 1f;
                }
            }
        }

        float turn = board.CurrentPlayer == Disc.Black ? 1f : 0f;
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                tensor[0, 2, row, col] = turn;
            }
        }
    }

    private static Move SelectBestMove(Tensor<float> policyTensor, ReadOnlySpan<Move> legalMoves)
    {
        Move bestMove = legalMoves[0];
        float bestScore = policyTensor[0, bestMove.Row * BoardSize + bestMove.Col];

        for (int i = 1; i < legalMoves.Length; i++)
        {
            Move move = legalMoves[i];
            float score = policyTensor[0, move.Row * BoardSize + move.Col];
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private static Tensor<float> GetOutputTensor(
        IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs,
        string outputName)
    {
        foreach (DisposableNamedOnnxValue output in outputs)
        {
            if (string.Equals(output.Name, outputName, StringComparison.Ordinal))
            {
                return output.AsTensor<float>();
            }
        }

        throw new InvalidOperationException($"Required ONNX output '{outputName}' was not found.");
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NeuralOnnxAI));
        }
    }
}
