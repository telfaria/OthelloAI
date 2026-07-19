from __future__ import annotations

from pathlib import Path

from kifu_recorder import KifuRecorder, normalize_board_snapshot
from kifu_store import KifuStore


def main() -> None:
    """棋譜保存システムの最小利用例。"""
    store = KifuStore(Path("data") / "selfplay_games.jsonl")
    recorder = KifuRecorder(
        black_ai="MctsAI",
        white_ai="MctsAI",
        metadata={"source": "sample", "format": "jsonl"},
    )

    board_before = normalize_board_snapshot(
        [
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", "W", "B", ".", ".", "."],
            [".", ".", ".", "B", "W", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
        ]
    )

    board_after = normalize_board_snapshot(
        [
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", "B", ".", ".", ".", "."],
            [".", ".", ".", "B", "B", ".", ".", "."],
            [".", ".", ".", "B", "W", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
            [".", ".", ".", ".", ".", ".", ".", "."],
        ]
    )

    recorder.append_move(
        ply=1,
        player="Black",
        move=(2, 3),
        board_before=board_before,
        board_after=board_after,
        thought={"ai": "MctsAI", "iterations": 500, "selected": "(2,3)"},
    )

    record = recorder.finalize(black_count=34, white_count=30)
    store.append(record)


if __name__ == "__main__":
    main()
