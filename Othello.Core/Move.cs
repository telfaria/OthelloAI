namespace Othello.Core;

/// <summary>
/// Represents a move on the Othello board.
/// </summary>
/// <param name="Position">Move position.</param>
public readonly record struct Move(Position Position)
{
    /// <summary>
    /// Initializes a move from row and column values.
    /// </summary>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="col">Zero-based column index.</param>
    public Move(int row, int col)
        : this(new Position(row, col))
    {
    }

    /// <summary>
    /// Gets the move row index.
    /// </summary>
    public int Row => Position.Row;

    /// <summary>
    /// Gets the move column index.
    /// </summary>
    public int Col => Position.Col;
}
