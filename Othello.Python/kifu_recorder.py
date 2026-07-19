from __future__ import annotations

from datetime import UTC, datetime
from typing import Any
from uuid import uuid4

from kifu_models import GameRecord, GameResult, MoveRecord


class KifuRecorder:
    """Self Play 実行中に棋譜を蓄積し、`GameRecord` を生成する。"""

    def __init__(self, black_ai: str, white_ai: str, metadata: dict[str, Any] | None = None) -> None:
        self._game_id = str(uuid4())
        self._started_at = datetime.now(UTC)
        self._black_ai = black_ai
        self._white_ai = white_ai
        self._metadata = dict(metadata) if metadata is not None else {}
        self._moves: list[MoveRecord] = []

    @property
    def game_id(self) -> str:
        return self._game_id

    def append_move(
        self,
        *,
        ply: int,
        player: str,
        board_before: list[str],
        board_after: list[str],
        move: tuple[int, int] | None,
        thought: dict[str, Any] | None = None,
    ) -> None:
        """1手の情報を追加する。`move is None` はパスとして扱う。"""
        move_payload: dict[str, int] | None = None
        is_pass = move is None

        if move is not None:
            move_payload = {"row": int(move[0]), "col": int(move[1])}

        self._moves.append(
            MoveRecord(
                ply=ply,
                player=player,
                move=move_payload,
                is_pass=is_pass,
                board_before=list(board_before),
                board_after=list(board_after),
                thought=thought,
            )
        )

    def finalize(self, *, black_count: int, white_count: int) -> GameRecord:
        """ゲーム終了時に最終結果を含む `GameRecord` を返す。"""
        finished_at = datetime.now(UTC)

        if black_count > white_count:
            winner = "Black"
        elif white_count > black_count:
            winner = "White"
        else:
            winner = "Draw"

        return GameRecord(
            game_id=self._game_id,
            started_at_utc=self._started_at.isoformat(),
            finished_at_utc=finished_at.isoformat(),
            black_ai=self._black_ai,
            white_ai=self._white_ai,
            result=GameResult(
                winner=winner,
                black_count=int(black_count),
                white_count=int(white_count),
            ),
            moves=list(self._moves),
            metadata=dict(self._metadata),
        )


def normalize_board_snapshot(board: list[list[Any]]) -> list[str]:
    """2次元配列の盤面を JSON 保存向けの文字列配列へ正規化する。"""
    normalized: list[str] = []

    for row in board:
        cells: list[str] = []
        for cell in row:
            text = str(cell)
            upper = text.upper()
            if upper in {"BLACK", "B", "●", "X", "1"}:
                cells.append("B")
            elif upper in {"WHITE", "W", "○", "O", "2"}:
                cells.append("W")
            else:
                cells.append(".")

        normalized.append("".join(cells))

    return normalized
