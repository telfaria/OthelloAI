from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class MoveRecord:
    """1手分の棋譜情報。"""

    ply: int
    player: str
    board_before: list[str]
    board_after: list[str]
    move: dict[str, int] | None = None
    is_pass: bool = False
    thought: dict[str, Any] | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "ply": self.ply,
            "player": self.player,
            "move": self.move,
            "is_pass": self.is_pass,
            "board_before": self.board_before,
            "board_after": self.board_after,
            "thought": self.thought,
        }

    @staticmethod
    def from_dict(data: dict[str, Any]) -> "MoveRecord":
        return MoveRecord(
            ply=int(data["ply"]),
            player=str(data["player"]),
            move=data.get("move"),
            is_pass=bool(data.get("is_pass", False)),
            board_before=list(data["board_before"]),
            board_after=list(data["board_after"]),
            thought=data.get("thought"),
        )


@dataclass(slots=True)
class GameResult:
    """1ゲームの最終結果。"""

    winner: str
    black_count: int
    white_count: int

    def to_dict(self) -> dict[str, Any]:
        return {
            "winner": self.winner,
            "black_count": self.black_count,
            "white_count": self.white_count,
        }

    @staticmethod
    def from_dict(data: dict[str, Any]) -> "GameResult":
        return GameResult(
            winner=str(data["winner"]),
            black_count=int(data["black_count"]),
            white_count=int(data["white_count"]),
        )


@dataclass(slots=True)
class GameRecord:
    """1ゲーム分の棋譜データ。JSONLの1行に対応する。"""

    game_id: str
    started_at_utc: str
    finished_at_utc: str
    black_ai: str
    white_ai: str
    result: GameResult
    moves: list[MoveRecord]
    metadata: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {
            "game_id": self.game_id,
            "started_at_utc": self.started_at_utc,
            "finished_at_utc": self.finished_at_utc,
            "black_ai": self.black_ai,
            "white_ai": self.white_ai,
            "result": self.result.to_dict(),
            "moves": [move.to_dict() for move in self.moves],
            "metadata": self.metadata,
        }

    @staticmethod
    def from_dict(data: dict[str, Any]) -> "GameRecord":
        return GameRecord(
            game_id=str(data["game_id"]),
            started_at_utc=str(data["started_at_utc"]),
            finished_at_utc=str(data["finished_at_utc"]),
            black_ai=str(data["black_ai"]),
            white_ai=str(data["white_ai"]),
            result=GameResult.from_dict(dict(data["result"])),
            moves=[MoveRecord.from_dict(dict(item)) for item in data["moves"]],
            metadata=dict(data.get("metadata", {})),
        )
