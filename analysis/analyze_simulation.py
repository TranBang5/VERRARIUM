import argparse
import glob
import json
import os
from dataclasses import dataclass
from datetime import datetime
from typing import List, Dict, Any, Tuple

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


@dataclass
class SaveSummary:
    file_path: str
    save_time: datetime
    simulation_time: float
    avg_age: float
    max_age: float
    species_count: int


def load_save_file(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def parse_datetime(value: str) -> datetime:
    # Unity JsonUtility serializes DateTime với định dạng ISO 8601
    # Ví dụ: "2025-12-26T10:30:45.1234567"
    # Dùng fromisoformat nếu có thể; nếu lỗi thì fallback.
    try:
        return datetime.fromisoformat(value)
    except Exception:
        try:
            # Cắt bớt nếu có quá nhiều chữ số mili/tiểu giây
            if "." in value:
                date_part, frac_part = value.split(".", 1)
                frac_part = "".join(ch for ch in frac_part if ch.isdigit())
                frac_part = frac_part[:6]  # microseconds
                return datetime.strptime(f"{date_part}.{frac_part}", "%Y-%m-%dT%H:%M:%S.%f")
            else:
                return datetime.strptime(value, "%Y-%m-%dT%H:%M:%S")
        except Exception:
            # Cuối cùng: trả về thời điểm hiện tại để không vỡ phân tích
            return datetime.now()


def summarize_save(path: str) -> SaveSummary:
    data = load_save_file(path)

    save_time_raw = data.get("saveTime")
    simulation_time = float(data.get("simulationTime", 0.0))
    creatures = data.get("creatures", [])

    ages = [float(c.get("age", 0.0)) for c in creatures]
    if ages:
        avg_age = float(np.mean(ages))
        max_age = float(np.max(ages))
    else:
        avg_age = max_age = 0.0

    species_ids = [c.get("speciesId", -1) for c in creatures if c.get("speciesId", -1) is not None]
    # Bỏ speciesId = -1 (chưa phân loại) khỏi thống kê đa dạng loài
    species_ids = [sid for sid in species_ids if isinstance(sid, int) and sid >= 0]
    species_count = len(set(species_ids))

    save_time = parse_datetime(save_time_raw) if isinstance(save_time_raw, str) else datetime.now()

    return SaveSummary(
        file_path=path,
        save_time=save_time,
        simulation_time=simulation_time,
        avg_age=avg_age,
        max_age=max_age,
        species_count=species_count,
    )


def summarize_multiple(files: List[str], out_dir: str) -> None:
    if not files:
        print("Không tìm thấy file JSON nào để phân tích.")
        return

    os.makedirs(out_dir, exist_ok=True)

    summaries = [summarize_save(f) for f in files]
    # Sắp xếp theo thời gian lưu
    summaries.sort(key=lambda s: s.save_time)

    df = pd.DataFrame(
        [
            {
                "file": os.path.basename(s.file_path),
                "save_time": s.save_time,
                "simulation_time": s.simulation_time,
                "avg_age": s.avg_age,
                "max_age": s.max_age,
                "species_count": s.species_count,
            }
            for s in summaries
        ]
    )

    csv_path = os.path.join(out_dir, "summary_stats.csv")
    df.to_csv(csv_path, index=False, encoding="utf-8-sig")
    # Dùng log ASCII để tránh lỗi Unicode trên Windows console
    print(f"Saved summary stats to: {csv_path}")

    # Vẽ biểu đồ tuổi (avg/max) theo thời gian (simulation_time)
    plt.figure(figsize=(10, 6))
    x = df["simulation_time"]
    plt.plot(x, df["avg_age"], label="Tuổi trung bình")
    plt.plot(x, df["max_age"], label="Tuổi lớn nhất")
    plt.xlabel("Thời gian giả lập (simulationTime)")
    plt.ylabel("Tuổi")
    plt.title("Thay đổi tuổi tác theo thời gian giả lập")
    plt.legend()
    plt.grid(True, alpha=0.3)
    age_plot_path = os.path.join(out_dir, "age_over_time.png")
    plt.tight_layout()
    plt.savefig(age_plot_path, dpi=150)
    plt.close()
    print(f"Saved age plot to: {age_plot_path}")

    # Vẽ biểu đồ số loài theo thời gian
    plt.figure(figsize=(10, 6))
    plt.plot(x, df["species_count"], marker="o", label="Số loài")
    plt.xlabel("Thời gian giả lập (simulationTime)")
    plt.ylabel("Số loài (distinct speciesId >= 0)")
    plt.title("Thay đổi đa dạng chủng loài theo thời gian giả lập")
    plt.grid(True, alpha=0.3)
    plt.legend()
    species_plot_path = os.path.join(out_dir, "species_over_time.png")
    plt.tight_layout()
    plt.savefig(species_plot_path, dpi=150)
    plt.close()
    print(f"Saved species plot to: {species_plot_path}")


def build_population_matrix(
    data: Dict[str, Any], rows: int, cols: int
) -> Tuple[np.ndarray, Tuple[float, float, float, float]]:
    """
    Tạo ma trận dân số m x n từ một file save.
    - worldSize: [ -worldSize.x/2, worldSize.x/2 ] x [ -worldSize.y/2, worldSize.y/2 ]
    - Làm tròn vị trí sinh vật đến ô gần nhất.
    """
    world_size = data.get("worldSize", {})
    world_x = float(world_size.get("x", 20.0))
    world_y = float(world_size.get("y", 20.0))

    # Giả định map đối xứng quanh gốc (như trong SimulationSupervisor.ClampToWorldBounds)
    x_min, x_max = -world_x / 2.0, world_x / 2.0
    y_min, y_max = -world_y / 2.0, world_y / 2.0

    matrix = np.zeros((rows, cols), dtype=np.int32)

    creatures = data.get("creatures", [])
    for c in creatures:
        pos = c.get("position", {})
        x = float(pos.get("x", 0.0))
        y = float(pos.get("y", 0.0))

        # Chuẩn hóa vào [0, 1]
        if x_max == x_min or y_max == y_min:
            continue
        u = (x - x_min) / (x_max - x_min)
        v = (y - y_min) / (y_max - y_min)

        # Chuyển sang index [0..cols-1], [0..rows-1]
        col = int(round(u * (cols - 1)))
        row = int(round(v * (rows - 1)))

        # Clamp để tránh out-of-bounds
        col = max(0, min(cols - 1, col))
        row = max(0, min(rows - 1, row))

        # Thường biểu đồ ma trận ở toạ độ (row 0) là phía trên,
        # nên đảo trục y để hợp với hiển thị (tùy chọn).
        row_display = rows - 1 - row

        matrix[row_display, col] += 1

    return matrix, (x_min, x_max, y_min, y_max)


def plot_population_heatmap(
    file_path: str, rows: int, cols: int, out_dir: str
) -> None:
    os.makedirs(out_dir, exist_ok=True)

    data = load_save_file(file_path)
    matrix, (x_min, x_max, y_min, y_max) = build_population_matrix(data, rows, cols)

    # Lưu ma trận ra CSV để có thể phân tích thêm
    base_name = os.path.splitext(os.path.basename(file_path))[0]
    csv_path = os.path.join(out_dir, f"{base_name}_population_matrix_{rows}x{cols}.csv")
    pd.DataFrame(matrix).to_csv(csv_path, index=False)
    print(f"Đã lưu ma trận dân số tại: {csv_path}")

    # Vẽ heatmap grayscale
    plt.figure(figsize=(8, 6))
    # cmap="gray_r" -> ô nhiều sinh vật hơn thì màu đậm hơn
    plt.imshow(matrix, cmap="gray_r", interpolation="nearest", aspect="auto")
    plt.colorbar(label="Số sinh vật")
    plt.title(f"Ma trận dân số {rows}x{cols} - {base_name}")
    plt.xlabel("X")
    plt.ylabel("Y")
    heatmap_path = os.path.join(out_dir, f"{base_name}_population_heatmap_{rows}x{cols}.png")
    plt.tight_layout()
    plt.savefig(heatmap_path, dpi=150)
    plt.close()
    print(f"Đã lưu heatmap dân số: {heatmap_path}")


def expand_input_patterns(patterns: List[str]) -> List[str]:
    files: List[str] = []
    for p in patterns:
        if os.path.isdir(p):
            files.extend(glob.glob(os.path.join(p, "*.json")))
        else:
            # Cho phép dùng wildcard như D:/Games/Verrarium/Saves/autosave_*.json
            matched = glob.glob(p)
            if matched:
                files.extend(matched)
            elif os.path.isfile(p):
                files.append(p)
    # Loại trùng và sort cho ổn định
    return sorted(set(files))


def main():
    parser = argparse.ArgumentParser(description="Phân tích dữ liệu save Verrarium (offline, độc lập với Unity).")
    parser.add_argument(
        "--mode",
        choices=["summary", "heatmap"],
        required=True,
        help="Chế độ phân tích: 'summary' (tuổi + loài, nhiều file) hoặc 'heatmap' (ma trận dân số cho 1 file).",
    )
    parser.add_argument(
        "--inputs",
        nargs="*",
        default=[],
        help="Danh sách file/thư mục/pattern (*.json) cho mode 'summary'.",
    )
    parser.add_argument(
        "--input",
        type=str,
        help="File JSON duy nhất cho mode 'heatmap'.",
    )
    parser.add_argument(
        "--matrix-size",
        nargs=2,
        type=int,
        metavar=("ROWS", "COLS"),
        default=[20, 20],
        help="Kích thước ma trận dân số m x n (m hàng, n cột) cho mode 'heatmap'.",
    )
    parser.add_argument(
        "--out-dir",
        type=str,
        default="results",
        help="Thư mục con để lưu kết quả phân tích (CSV, PNG).",
    )

    args = parser.parse_args()

    out_dir = args.out_dir
    os.makedirs(out_dir, exist_ok=True)

    if args.mode == "summary":
        files = expand_input_patterns(args.inputs)
        summarize_multiple(files, out_dir)
    elif args.mode == "heatmap":
        if not args.input:
            print("Mode 'heatmap' yêu cầu --input chỉ định file JSON.")
            return
        rows, cols = args.matrix_size
        plot_population_heatmap(args.input, rows, cols, out_dir)


if __name__ == "__main__":
    main()

