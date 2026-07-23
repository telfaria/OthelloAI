from __future__ import annotations

import argparse
import copy
import json
import random
from dataclasses import dataclass
from pathlib import Path

import torch
import torch.nn.functional as F
from torch import Tensor, nn
from torch.utils.data import DataLoader, Dataset, random_split

BOARD_SIZE = 8
POLICY_SIZE = BOARD_SIZE * BOARD_SIZE


@dataclass(slots=True)
class TrainingSample:
    features: Tensor
    policy_index: int
    value_target: float


class KifuPolicyValueDataset(Dataset[tuple[Tensor, Tensor, Tensor]]):
    """棋譜 JSONL から方策+価値学習サンプルを生成する Dataset。"""

    def __init__(self, kifu_paths: list[Path]) -> None:
        samples: list[TrainingSample] = []

        for kifu_path in kifu_paths:
            samples.extend(load_samples_from_jsonl(kifu_path))

        if not samples:
            raise ValueError("棋譜から有効な学習サンプルを作成できませんでした。")

        self._samples = samples

    def __len__(self) -> int:
        return len(self._samples)

    def __getitem__(self, index: int) -> tuple[Tensor, Tensor, Tensor]:
        sample = self._samples[index]
        policy = torch.tensor(sample.policy_index, dtype=torch.long)
        value = torch.tensor(sample.value_target, dtype=torch.float32)
        return sample.features, policy, value


def get_field(data: dict, camel_key: str, snake_key: str):
    if camel_key in data:
        return data[camel_key]

    return data.get(snake_key)


def load_samples_from_jsonl(kifu_path: Path) -> list[TrainingSample]:
    samples: list[TrainingSample] = []

    with kifu_path.open("r", encoding="utf-8") as stream:
        for line_number, line in enumerate(stream, start=1):
            payload = line.strip()
            if not payload:
                continue

            data = json.loads(payload)
            result = get_field(data, "Result", "result") or {}
            winner = str(get_field(result, "Winner", "winner") or "Draw")
            moves = get_field(data, "Moves", "moves") or []

            for move in moves:
                if not isinstance(move, dict):
                    continue

                is_pass = bool(get_field(move, "IsPass", "is_pass") or False)
                move_payload = get_field(move, "Move", "move")

                if is_pass or not isinstance(move_payload, dict):
                    continue

                row_value = get_field(move_payload, "Row", "row")
                col_value = get_field(move_payload, "Col", "col")
                board_before = get_field(move, "BoardBefore", "board_before")
                player = get_field(move, "Player", "player")

                if row_value is None or col_value is None or board_before is None or player is None:
                    continue

                row = int(row_value)
                col = int(col_value)
                if row < 0 or row >= BOARD_SIZE or col < 0 or col >= BOARD_SIZE:
                    continue

                features = encode_features(list(board_before), str(player))
                policy_index = row * BOARD_SIZE + col
                value_target = compute_value_target(winner, str(player))
                samples.append(TrainingSample(features, policy_index, value_target))

    return samples


def encode_features(board_before: list[str], player: str) -> Tensor:
    x = torch.zeros((3, BOARD_SIZE, BOARD_SIZE), dtype=torch.float32)

    for row_index, row_text in enumerate(board_before):
        if row_index >= BOARD_SIZE:
            break

        for col_index, cell in enumerate(row_text):
            if col_index >= BOARD_SIZE:
                break

            if cell == "B":
                x[0, row_index, col_index] = 1.0
            elif cell == "W":
                x[1, row_index, col_index] = 1.0

    is_black_turn = player.strip().lower().startswith("black")
    x[2].fill_(1.0 if is_black_turn else 0.0)
    return x


def compute_value_target(winner: str, player: str) -> float:
    if winner == "Draw":
        return 0.0

    player_is_black = player.strip().lower().startswith("black")
    winner_is_black = winner.strip().lower().startswith("black")
    return 1.0 if player_is_black == winner_is_black else -1.0


class SmallPolicyValueCnn(nn.Module):
    """8x8盤面向けの小型CNN (方策+価値 2ヘッド)。"""

    def __init__(self) -> None:
        super().__init__()

        self.trunk = nn.Sequential(
            nn.Conv2d(3, 32, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv2d(32, 64, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv2d(64, 64, kernel_size=3, padding=1),
            nn.ReLU(),
        )

        self.policy_head = nn.Sequential(
            nn.Conv2d(64, 2, kernel_size=1),
            nn.ReLU(),
            nn.Flatten(),
            nn.Linear(2 * POLICY_SIZE, POLICY_SIZE),
        )

        self.value_head = nn.Sequential(
            nn.Conv2d(64, 1, kernel_size=1),
            nn.ReLU(),
            nn.Flatten(),
            nn.Linear(POLICY_SIZE, 64),
            nn.ReLU(),
            nn.Linear(64, 1),
            nn.Tanh(),
        )

    def forward(self, x: Tensor) -> tuple[Tensor, Tensor]:
        features = self.trunk(x)
        policy_logits = self.policy_head(features)
        value = self.value_head(features)
        return policy_logits, value


def train_epoch(
    model: nn.Module,
    loader: DataLoader[tuple[Tensor, Tensor, Tensor]],
    optimizer: torch.optim.Optimizer,
    device: torch.device,
) -> tuple[float, float, float]:
    model.train()
    total_loss = 0.0
    total_policy_loss = 0.0
    total_value_loss = 0.0

    for boards, policies, values in loader:
        boards = boards.to(device, non_blocking=True)
        policies = policies.to(device, non_blocking=True)
        values = values.to(device, non_blocking=True)

        optimizer.zero_grad(set_to_none=True)

        policy_logits, value_pred = model(boards)
        policy_loss = F.cross_entropy(policy_logits, policies)
        value_loss = F.mse_loss(value_pred.squeeze(1), values)
        loss = policy_loss + value_loss

        loss.backward()
        optimizer.step()

        batch_size = boards.shape[0]
        total_loss += loss.item() * batch_size
        total_policy_loss += policy_loss.item() * batch_size
        total_value_loss += value_loss.item() * batch_size

    count = len(loader.dataset)
    return total_loss / count, total_policy_loss / count, total_value_loss / count


def validate_epoch(
    model: nn.Module,
    loader: DataLoader[tuple[Tensor, Tensor, Tensor]],
    device: torch.device,
) -> tuple[float, float, float]:
    model.eval()
    total_loss = 0.0
    total_policy_loss = 0.0
    total_value_loss = 0.0

    with torch.no_grad():
        for boards, policies, values in loader:
            boards = boards.to(device, non_blocking=True)
            policies = policies.to(device, non_blocking=True)
            values = values.to(device, non_blocking=True)

            policy_logits, value_pred = model(boards)
            policy_loss = F.cross_entropy(policy_logits, policies)
            value_loss = F.mse_loss(value_pred.squeeze(1), values)
            loss = policy_loss + value_loss

            batch_size = boards.shape[0]
            total_loss += loss.item() * batch_size
            total_policy_loss += policy_loss.item() * batch_size
            total_value_loss += value_loss.item() * batch_size

    count = len(loader.dataset)
    return total_loss / count, total_policy_loss / count, total_value_loss / count


def export_onnx(model: nn.Module, output_path: Path, device: torch.device) -> None:
    model.eval()
    dummy_input = torch.zeros((1, 3, BOARD_SIZE, BOARD_SIZE), dtype=torch.float32, device=device)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    # 新しいONNXエクスポータは onnxscript を要求するため、まず旧エクスポータを優先する。
    # これにより最小依存（torch + onnx）で動作させる。
    try:
        torch.onnx.export(
            model,
            dummy_input,
            output_path.as_posix(),
            export_params=True,
            opset_version=17,
            do_constant_folding=True,
            input_names=["board"],
            output_names=["policy_logits", "value"],
            dynamic_axes={
                "board": {0: "batch"},
                "policy_logits": {0: "batch"},
                "value": {0: "batch"},
            },
            dynamo=False,
        )
    except TypeError:
        # 古いPyTorchでは dynamo 引数がないためフォールバック
        torch.onnx.export(
            model,
            dummy_input,
            output_path.as_posix(),
            export_params=True,
            opset_version=17,
            do_constant_folding=True,
            input_names=["board"],
            output_names=["policy_logits", "value"],
            dynamic_axes={
                "board": {0: "batch"},
                "policy_logits": {0: "batch"},
                "value": {0: "batch"},
            },
        )


def split_dataset(dataset: Dataset[tuple[Tensor, Tensor, Tensor]], val_ratio: float, seed: int) -> tuple[Dataset[tuple[Tensor, Tensor, Tensor]], Dataset[tuple[Tensor, Tensor, Tensor]]]:
    if len(dataset) < 2:
        raise ValueError("学習には最低2サンプル以上が必要です。")

    val_count = max(1, int(len(dataset) * val_ratio))
    train_count = len(dataset) - val_count

    if train_count == 0:
        train_count = len(dataset) - 1
        val_count = 1

    generator = torch.Generator().manual_seed(seed)
    return random_split(dataset, [train_count, val_count], generator=generator)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="棋譜JSONLから方策+価値モデルを学習し、bestモデルをONNX出力する。")
    parser.add_argument("--kifu", type=str, required=True, help="入力棋譜（ファイル/ディレクトリ/ワイルドカード）。例: data/selfplay.jsonl, data/selfplay/, data/selfplay/*.jsonl")
    parser.add_argument("--onnx", type=Path, default=Path("models") / "policy_value_best.onnx", help="出力ONNXパス")
    parser.add_argument("--epochs", type=int, default=10, help="学習エポック数")
    parser.add_argument("--batch-size", type=int, default=256, help="バッチサイズ")
    parser.add_argument("--learning-rate", type=float, default=1e-3, help="学習率")
    parser.add_argument("--val-ratio", type=float, default=0.1, help="検証データ比率")
    parser.add_argument("--seed", type=int, default=42, help="乱数シード")
    parser.add_argument("--device", type=str, default="auto", choices=["auto", "cpu", "cuda"], help="学習デバイス")
    parser.add_argument("--cuda-strict", action="store_true", help="CUDA指定時は利用不可なら即エラーにする")
    return parser.parse_args()


def resolve_kifu_paths(kifu_arg: str) -> list[Path]:
    wildcard_chars = "*?[]"
    raw_path = Path(kifu_arg)

    if any(ch in kifu_arg for ch in wildcard_chars):
        paths = sorted(Path().glob(kifu_arg))
        files = [path for path in paths if path.is_file()]
        if not files:
            raise ValueError(f"--kifu パターンに一致するファイルがありません: {kifu_arg}")

        return files

    if raw_path.is_file():
        return [raw_path]

    if raw_path.is_dir():
        files = sorted(path for path in raw_path.glob("*.jsonl") if path.is_file())
        if not files:
            raise ValueError(f"--kifu ディレクトリ内に jsonl がありません: {raw_path}")

        return files

    raise ValueError(f"--kifu がファイル/ディレクトリ/ワイルドカードとして解決できません: {kifu_arg}")


def resolve_device(name: str) -> torch.device:
    if name == "cpu":
        return torch.device("cpu")

    if name == "cuda":
        ensure_cuda_available()
        return torch.device("cuda")

    if name == "auto":
        if torch.cuda.is_available():
            ensure_cuda_available()
            return torch.device("cuda")

        return torch.device("cpu")

    raise ValueError("--device は auto / cpu / cuda のいずれかを指定してください。")


def ensure_cuda_available() -> None:
    if not torch.cuda.is_available():
        raise RuntimeError(
            "CUDA が利用できません。GPU版PyTorchが入っているか、NVIDIAドライバ/CUDA環境を確認してください。"
        )

    cuda_version = getattr(torch.version, "cuda", None)
    if cuda_version is None:
        raise RuntimeError(
            "現在のPyTorchはCUDA非対応です。GPU版PyTorchをインストールしてください。"
        )


def main() -> None:
    args = parse_args()

    if args.epochs < 1:
        raise ValueError("--epochs は1以上を指定してください。")

    if args.batch_size < 1:
        raise ValueError("--batch-size は1以上を指定してください。")

    if args.learning_rate <= 0:
        raise ValueError("--learning-rate は正の値を指定してください。")

    if args.val_ratio <= 0 or args.val_ratio >= 1:
        raise ValueError("--val-ratio は 0 より大きく 1 未満を指定してください。")

    random.seed(args.seed)
    torch.manual_seed(args.seed)

    if args.cuda_strict and args.device != "cuda":
        raise ValueError("--cuda-strict は --device cuda と併用してください。")

    device = resolve_device(args.device)
    use_cuda = device.type == "cuda"

    kifu_paths = resolve_kifu_paths(args.kifu)
    print(f"kifu files: {len(kifu_paths)}")
    print(f"device: {device}")

    dataset = KifuPolicyValueDataset(kifu_paths)
    train_dataset, val_dataset = split_dataset(dataset, args.val_ratio, args.seed)

    train_loader = DataLoader(
        train_dataset,
        batch_size=args.batch_size,
        shuffle=True,
        pin_memory=use_cuda,
        num_workers=0,
    )
    val_loader = DataLoader(
        val_dataset,
        batch_size=args.batch_size,
        shuffle=False,
        pin_memory=use_cuda,
        num_workers=0,
    )

    model = SmallPolicyValueCnn().to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)

    best_state_dict: dict[str, Tensor] | None = None
    best_val_loss = float("inf")

    for epoch in range(1, args.epochs + 1):
        train_loss, train_policy_loss, train_value_loss = train_epoch(model, train_loader, optimizer, device)
        val_loss, val_policy_loss, val_value_loss = validate_epoch(model, val_loader, device)

        print(
            f"epoch={epoch} "
            f"train_loss={train_loss:.4f} "
            f"train_policy={train_policy_loss:.4f} "
            f"train_value={train_value_loss:.4f} "
            f"val_loss={val_loss:.4f} "
            f"val_policy={val_policy_loss:.4f} "
            f"val_value={val_value_loss:.4f}"
        )

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_state_dict = copy.deepcopy(model.state_dict())

    if best_state_dict is None:
        raise RuntimeError("best model の更新に失敗しました。")

    model.load_state_dict(best_state_dict)

    if use_cuda:
        model = model.to(torch.device("cpu"))

    export_onnx(model, args.onnx, torch.device("cpu") if use_cuda else device)
    print(f"best model exported to ONNX: {args.onnx}")


if __name__ == "__main__":
    main()
