# OthelloAI

OthelloAI is an AI learning project built around Othello (Reversi).  
It implements the game engine and search-based AIs in C#/.NET 10, then trains a policy+value model from self-play records in Python, exports ONNX, and runs inference back in C#.

## Overview

This repository focuses on the following goals:

- Implementing an Othello rules engine
- Comparing multiple AIs (Random / Greedy / Minimax / AlphaBeta / MCTS / ONNX inference)
- Generating self-play data in parallel on CPU (JSONL)
- Training in Python and exporting ONNX models

Dependency direction (architecture rule):

- `Othello.Console` → `Othello.AI` → `Othello.Core`
- `Othello.Core` does not depend on upper layers

## Solution Structure

- `Othello.Core`
  - Board state, legal move checks, flipping, pass, game-over detection
  - Board representation uses a 2D array (designed for future BitBoard migration)
- `Othello.AI`
  - Shared interface: `IOthelloAI`
  - `RandomAI`, `GreedyAI`, `MinimaxAI`, `AlphaBetaAI`, `MctsAI`, `NeuralOnnxAI`
- `Othello.Console`
  - Runs AI-vs-AI self-play simulations
  - Persists game records in JSON Lines format
- `Othello.Python`
  - Kifu model definitions and JSONL I/O
  - `train_policy_value_onnx.py` trains a policy+value CNN and exports ONNX
- `Othello.Tests`
  - Tests for rules, AIs, self-play persistence, and performance regression (including allocation checks)

## Requirements

### .NET side

- .NET SDK 10
- Visual Studio 2026 (optional; CLI works as well)

### Python side

- Python 3.11
- CPU training: `torch>=2.3.0`, `onnx>=1.16.0`
- CUDA training (optional): above + `torchvision` (`requirements-cuda.txt`)

## Build Instructions

Run from repository root:

1. Restore  
   `dotnet restore`
2. Build  
   `dotnet build OthelloAI.slnx -c Debug`
3. Run tests  
   `dotnet test OthelloAI.slnx -c Debug`

## Usage (C# Self-Play)

Basic run:

`dotnet run --project Othello.Console -- --games 100`

Main options:

- `--games <N>` or `--games=<N>`: number of games (default: 1000)
- `--thinking` / `--thinking=on` / `--thinking=off`: show per-turn AI thought
- `--black-ai <name>` / `--white-ai <name>`: AI selection
  - `random`, `greedy`, `minimax`, `alphabeta`, `mcts`, `neural`
- `--onnx-model <path>`: required when using `neural`

Examples:

- MCTS vs AlphaBeta  
  `dotnet run --project Othello.Console -- --games 200 --black-ai mcts --white-ai alphabeta`
- Use ONNX inference AI as Black  
  `dotnet run --project Othello.Console -- --games 50 --black-ai neural --white-ai mcts --onnx-model Othello.Python/models/policy_value_best.onnx`

By default, both sides use `MctsAI(iter=3000, p=30)`.

## Output Data (Kifu JSONL)

Self-play results are saved under `data/selfplay` using date-based sequence files.

- Format: `yyyyMMdd_NNN.jsonl`
- 1 line = 1 game (JSON object)
- Each move stores:
  - Player turn and move coordinate (or pass)
  - Board snapshots before/after move (8x8)
  - Optional AI thought text

## Python Training Flow

### Install dependencies

CPU:

`pip install -r Othello.Python/requirements.txt`

CUDA:

`pip install -r Othello.Python/requirements-cuda.txt`

### Train and export ONNX

Example:

`python Othello.Python/train_policy_value_onnx.py --kifu data/selfplay --onnx Othello.Python/models/policy_value_best.onnx --epochs 16 --batch-size 256 --device auto`

Notes:

- `--kifu` accepts file / directory / wildcard
- `--device` supports `auto|cpu|cuda`
- Model input shape is `1x3x8x8` (black plane, white plane, turn plane)
- Outputs are `policy_logits(64)` and `value(1)`

On Windows, you can also use `Othello.Python/run_train_venv.bat` for preset execution.

## AI Algorithm Summary

- `RandomAI`: random legal move
- `GreedyAI`: maximizes immediate disc difference after one move
- `MinimaxAI`: minimax with evaluation function (parallel root eval + transposition table)
- `AlphaBetaAI`: minimax with alpha-beta pruning
- `MctsAI`: MCTS with UCB1 (single-tree or root-parallel)
- `NeuralOnnxAI`: ONNX Runtime policy+value inference

## Tests

`Othello.Tests` includes:

- Rule correctness for board operations
- Legal move selection by each AI
- Node-count comparison between AlphaBeta and Minimax
- Parallel-root MCTS regression checks
- Self-play kifu persistence format checks
- Allocation-focused performance regression checks for hot paths

## Libraries

### C# / .NET

- .NET 10 (`net10.0`)
- `Microsoft.ML.OnnxRuntime` 1.22.1
- Test packages:
  - `xunit` 2.9.3
  - `xunit.runner.visualstudio` 3.1.4
  - `Microsoft.NET.Test.Sdk` 17.14.1
  - `coverlet.collector` 6.0.4

### Python

- `torch>=2.3.0`
- `onnx>=1.16.0`
- (CUDA setup) `torchvision`

## License

MIT License

## Future Plan (excerpt)

- Stronger search stack (e.g., Negamax / Iterative Deepening)
- More advanced self-play + training loop (AlphaZero-style)
- WinUI frontend integration

## Related Documents

- `Architecture.md`
- `AIAlgorithms.md`
- `Roadmap.md`
- `CodingRules.md`
- `AGENTS.md`
