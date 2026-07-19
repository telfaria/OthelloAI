from __future__ import annotations

import json
from pathlib import Path
from typing import Iterable

from kifu_models import GameRecord


class KifuStore:
    """`GameRecord` を JSON Lines 形式で永続化する。"""

    def __init__(self, file_path: str | Path) -> None:
        self._path = Path(file_path)

    @property
    def path(self) -> Path:
        return self._path

    def append(self, record: GameRecord) -> None:
        """1ゲーム分を JSONL の1行として追記する。"""
        self._path.parent.mkdir(parents=True, exist_ok=True)
        line = json.dumps(record.to_dict(), ensure_ascii=False, separators=(",", ":"))

        with self._path.open("a", encoding="utf-8") as stream:
            stream.write(line)
            stream.write("\n")

    def append_many(self, records: Iterable[GameRecord]) -> None:
        """複数ゲームをまとめて追記する。"""
        self._path.parent.mkdir(parents=True, exist_ok=True)

        with self._path.open("a", encoding="utf-8") as stream:
            for record in records:
                line = json.dumps(record.to_dict(), ensure_ascii=False, separators=(",", ":"))
                stream.write(line)
                stream.write("\n")

    def load_all(self) -> list[GameRecord]:
        """JSONL ファイル全件を読み込んで `GameRecord` 配列で返す。"""
        if not self._path.exists():
            return []

        records: list[GameRecord] = []

        with self._path.open("r", encoding="utf-8") as stream:
            for line in stream:
                payload = line.strip()
                if not payload:
                    continue

                data = json.loads(payload)
                records.append(GameRecord.from_dict(dict(data)))

        return records
