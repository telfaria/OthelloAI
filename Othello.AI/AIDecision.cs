using Othello.Core;

namespace Othello.AI;

/// <summary>
/// Represents an AI decision output.
/// </summary>
/// <param name="Move">Selected move. Null when no legal move exists.</param>
/// <param name="Thought">Optional thought summary for display.</param>
public readonly record struct AIDecision(Move? Move, string Thought);
