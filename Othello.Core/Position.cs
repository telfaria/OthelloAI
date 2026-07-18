namespace Othello.Core;

/// <summary>
/// Represents a board position.
/// </summary>
/// <param name="Row">Zero-based row index.</param>
/// <param name="Col">Zero-based column index.</param>
public readonly record struct Position(int Row, int Col);
