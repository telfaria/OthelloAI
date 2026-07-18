using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Defines the contract for Othello AI implementations.
/// </summary>
public interface IOthelloAI
{
    /// <summary>
    /// Gets the AI display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Selects a move for the given board state.
    /// </summary>
    /// <param name="board">Board state to evaluate.</param>
    /// <returns>Selected decision.</returns>
    AIDecision DecideMove(Board board);

    /// <summary>
    /// Resets the internal state (e.g. transposition table) of the AI.
    /// Call between games to start each trial with a fresh memory.
    /// </summary>
    void Reset();
}
