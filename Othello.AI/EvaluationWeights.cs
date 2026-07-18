namespace Othello.AI;

/// <summary>
/// Holds evaluation weights used by the minimax evaluation function.
/// </summary>
public sealed class EvaluationWeights
{
    /// <summary>
    /// Gets the default weights.
    /// </summary>
    public static readonly EvaluationWeights Default = new EvaluationWeights();

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationWeights"/> class with default values.
    /// </summary>
    public EvaluationWeights()
        : this(discDifference: 10, mobility: 6, corner: 25, positional: 2)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationWeights"/> class.
    /// </summary>
    /// <param name="discDifference">Weight for disc count difference.</param>
    /// <param name="mobility">Weight for legal move count difference.</param>
    /// <param name="corner">Weight for corner disc difference.</param>
    /// <param name="positional">Weight for positional table score.</param>
    public EvaluationWeights(int discDifference, int mobility, int corner, int positional)
    {
        DiscDifference = discDifference;
        Mobility = mobility;
        Corner = corner;
        Positional = positional;
    }

    /// <summary>
    /// Gets the weight applied to disc count difference.
    /// </summary>
    public int DiscDifference { get; }

    /// <summary>
    /// Gets the weight applied to legal move count difference (mobility).
    /// </summary>
    public int Mobility { get; }

    /// <summary>
    /// Gets the weight applied to corner disc difference.
    /// </summary>
    public int Corner { get; }

    /// <summary>
    /// Gets the weight applied to positional table score.
    /// </summary>
    public int Positional { get; }

    /// <summary>
    /// Returns a string representation of the weights.
    /// </summary>
    public override string ToString()
        => $"disc={DiscDifference}, mob={Mobility}, corner={Corner}, pos={Positional}";
}
