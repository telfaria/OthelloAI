# Copilot Instructions

## プロジェクト ガイドライン
- Board実装は2次元配列を使用し、将来的にBitBoardへ置換しやすい設計を優先する。
- WinUI実装ではMVVMパターンとCommunityToolkit.Mvvmを使用し、Nullable有効・XMLコメント付与・GUIは表示専用でロジックはCore/AI/Training側へ委譲する。
- Board表示機能ではBoardクラスを変更せず、MVVMでBoardViewModelを作成し、GUIは表示のみ担当し将来リアルタイム更新可能な構造にする。