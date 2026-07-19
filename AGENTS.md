# AGENTS.md

## Project Overview

This project is an Othello(Reversi) AI learning project.

The goal is to implement:

- Othello game engine
- Traditional AI algorithms
- Search algorithms
- Self-play system
- Machine learning based AI

The project uses C#/.NET 10 for the game engine and Python for machine learning.

---

# Technology Stack

## C#

- .NET 10
- Nullable Reference Types enabled
- xUnit
- XML Documentation Comments

## Future

- WinUI frontend
- Python PyTorch training
- ONNX Runtime inference


---

# Solution Structure
```
src
├ Othello.Core
├ Othello.AI
├ Othello.Python
└ Othello.Console


tests
└ Othello.Tests
```
# Architecture Rules

## Dependency Direction

Allowed:


Console
|
v
AI
|
v
Core


Core must not depend on AI or Console.

---

# Othello.Core Rules

This project contains:

- Board representation
- Move handling
- Rule validation
- Game state

Do NOT implement AI logic here.

---

# Othello.AI Rules

All AI implementations must implement:

```
csharp
IOthelloAI
```

Examples:

- RandomAI
- GreedyAI
- MinimaxAI
- AlphaBetaAI
- MctsAI
- NeuralAI

AI implementations must not modify Board directly.

# Coding Rules
## General
- Use Nullable Reference Types.
- Avoid unnecessary allocations.
- Prefer immutable objects where reasonable.
- Avoid magic numbers.
- Use meaningful names.

# var usage

Use var only when the type is obvious.

Good:

var moves = board.GetLegalMoves();

Bad:

var x = GetValue();

## LINQ

Do not overuse LINQ.

Prefer readable loops in performance critical code.

## Async

Do not introduce async unless required.

## Exceptions

Never hide exceptions.

Do not use:

catch(Exception)
{
}

## Documentation

Public members require XML documentation.

Example:
```
/// <summary>
/// Returns legal moves for current player.
/// </summary>
public IReadOnlyList<Move> GetLegalMoves()
```

Complex algorithms require additional comments.

Especially:

 - Minimax
 - AlphaBeta pruning
 - MCTS
 - Neural network evaluation

---

## Testing Rules

Every feature requires tests.

Implementation order:

1. Write test
2. Implement feature
3. Refactor
4. Verify tests

## AI Development Roadmap

Implement in this order:

1. Random AI
2. Greedy AI
3. Minimax
4. AlphaBeta
5. Negamax
6. Iterative Deepening
7. Monte Carlo Tree Search
8. Self Play
9. Neural Network
10. AlphaZero style training

---

# Git Rules

Use Conventional Commits.

Format:
type(scope): message

Examples:
feat(core): add board representation

feat(ai): implement minimax search

fix(core): correct stone flipping

test(core): add move validation tests

refactor(ai): improve evaluation function

docs: update architecture document

# Copilot Behavior

Before writing code:

1. Understand architecture.
2. Check existing patterns.
3. Add tests.
4. Explain design decisions when complex.

Do not create unnecessary abstractions.

Prefer simple readable implementations.
