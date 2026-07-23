# OthelloAI

OthelloAI は、オセロ（リバーシ）を題材にした AI 学習プロジェクトです。  
C#/.NET 10 でゲームエンジンと探索 AI を実装し、Python で自己対戦棋譜から方策+価値モデルを学習して ONNX 化し、再び C# 側で推論利用できます。

## 概要

このリポジトリは次を主目的にしています。

- オセロのルールエンジン実装
- 複数 AI（Random / Greedy / Minimax / AlphaBeta / MCTS / ONNX 推論）の比較
- CPU 並列での自己対戦データ生成（JSONL）
- Python による学習と ONNX モデル出力

依存方向（設計ルール）は以下です。

- `Othello.Console` → `Othello.AI` → `Othello.Core`
- `Othello.Core` は上位層へ依存しない

## ソリューション構成

- `Othello.Core`
  - 盤面・合法手判定・反転処理・パス判定・終局判定
  - 盤面表現は 2 次元配列（将来 BitBoard 置換を意識）
- `Othello.AI`
  - `IOthelloAI` 共通インターフェイス
  - `RandomAI`, `GreedyAI`, `MinimaxAI`, `AlphaBetaAI`, `MctsAI`, `NeuralOnnxAI`
- `Othello.Console`
  - AI 同士の自己対戦を実行
  - 対局ログ（棋譜）を JSON Lines で保存
- `Othello.Python`
  - 棋譜モデル定義と JSONL 読み書き
  - `train_policy_value_onnx.py` で方策+価値 CNN を学習し ONNX 出力
- `Othello.Tests`
  - ルール・AI・自己対戦記録・性能回帰（割り当て監視含む）のテスト

## 要件

### .NET 側

- .NET SDK 10
- Visual Studio 2026（任意、CLI でも可）

### Python 側

- Python 3.11
- CPU 学習: `torch>=2.3.0`, `onnx>=1.16.0`
- CUDA 学習（任意）: 上記 + `torchvision`（`requirements-cuda.txt`）

## ビルド方法

リポジトリルートで実行します。

1. 復元  
   `dotnet restore`
2. ビルド  
   `dotnet build OthelloAI.slnx -c Debug`
3. テスト実行  
   `dotnet test OthelloAI.slnx -c Debug`

## 使用方法（C# 自己対戦）

基本実行:

`dotnet run --project Othello.Console -- --games 100`

主要オプション:

- `--games <N>` または `--games=<N>`: 対局数（省略時は既定 1000）
- `--thinking` / `--thinking=on` / `--thinking=off`: 各手の思考表示
- `--black-ai <name>` / `--white-ai <name>`: 使用 AI 指定
  - `random`, `greedy`, `minimax`, `alphabeta`, `mcts`, `neural`
- `--onnx-model <path>`: `neural` 指定時に必須

実行例:

- MCTS vs AlphaBeta  
  `dotnet run --project Othello.Console -- --games 200 --black-ai mcts --white-ai alphabeta`
- ONNX 推論 AI を黒番で利用  
  `dotnet run --project Othello.Console -- --games 50 --black-ai neural --white-ai mcts --onnx-model Othello.Python/models/policy_value_best.onnx`

既定では黒白とも `MctsAI(iter=3000, p=30)` が選択されます。

## 出力データ（棋譜 JSONL）

自己対戦結果は `data/selfplay` 配下へ日付連番ファイルとして保存されます。

- 形式: `yyyyMMdd_NNN.jsonl`
- 1 行 = 1 対局（JSON オブジェクト）
- 各手に以下を保持
  - 手番、着手座標（またはパス）
  - 手前/手後の盤面スナップショット（8x8）
  - AI 思考テキスト（任意）

## Python 学習フロー

### インストール

CPU:

`pip install -r Othello.Python/requirements.txt`

CUDA:

`pip install -r Othello.Python/requirements-cuda.txt`

### 学習と ONNX 出力

例:

`python Othello.Python/train_policy_value_onnx.py --kifu data/selfplay --onnx Othello.Python/models/policy_value_best.onnx --epochs 16 --batch-size 256 --device auto`

補足:

- `--kifu` はファイル / ディレクトリ / ワイルドカード対応
- `--device` は `auto|cpu|cuda`
- モデル入力は `1x3x8x8`（黒石面・白石面・手番面）
- 出力は `policy_logits(64)` と `value(1)`

Windows では `Othello.Python/run_train_venv.bat` で定型実行も可能です。

## AI アルゴリズム要約

- `RandomAI`: 合法手からランダム選択
- `GreedyAI`: 1 手先の石差を最大化
- `MinimaxAI`: 評価関数付きミニマックス探索（並列 root 評価 + TT）
- `AlphaBetaAI`: ミニマックスに αβ 枝刈りを追加
- `MctsAI`: UCB1 を用いた MCTS（単一木 / root 並列）
- `NeuralOnnxAI`: ONNX Runtime で方策+価値モデル推論

## テスト

`Othello.Tests` には以下の観点を含みます。

- 盤面ルールの正当性
- 各 AI の合法手選択
- AlphaBeta と Minimax の探索ノード比較
- MCTS の並列 root 回帰
- 自己対戦棋譜保存フォーマット
- 高頻度 API の割り当て抑制（性能回帰）

## 利用ライブラリ

### C# / .NET

- .NET 10（`net10.0`）
- `Microsoft.ML.OnnxRuntime` 1.22.1
- テスト:
  - `xunit` 2.9.3
  - `xunit.runner.visualstudio` 3.1.4
  - `Microsoft.NET.Test.Sdk` 17.14.1
  - `coverlet.collector` 6.0.4

### Python

- `torch>=2.3.0`
- `onnx>=1.16.0`
- （CUDA 用）`torchvision`

## ライセンス

MIT License

## 将来計画（抜粋）

- Negamax / Iterative Deepening など探索強化
- Self-Play と学習ループの高度化（AlphaZero スタイル）
- WinUI フロントエンド統合

## 関連ドキュメント

- `Architecture.md`
- `AIAlgorithms.md`
- `Roadmap.md`
- `CodingRules.md`
- `AGENTS.md`
