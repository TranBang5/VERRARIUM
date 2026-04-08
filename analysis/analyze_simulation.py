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
    current_population: int
    data: Dict[str, Any]


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
    current_population = int(data.get("currentPopulation", len(creatures)))

    save_time = parse_datetime(save_time_raw) if isinstance(save_time_raw, str) else datetime.now()

    return SaveSummary(
        file_path=path,
        save_time=save_time,
        simulation_time=simulation_time,
        avg_age=avg_age,
        max_age=max_age,
        species_count=species_count,
        current_population=current_population,
        data=data,
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
                "current_population": s.current_population,
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

    # Carrying capacity stability: population over time
    plt.figure(figsize=(10, 6))
    plt.plot(x, df["current_population"], marker="o", label="Population")
    plt.xlabel("Thời gian giả lập (simulationTime)")
    plt.ylabel("Số lượng cá thể")
    plt.title("Carrying Capacity Stability - Population over time")
    plt.grid(True, alpha=0.3)
    plt.legend()
    pop_plot_path = os.path.join(out_dir, "population_over_time.png")
    plt.tight_layout()
    plt.savefig(pop_plot_path, dpi=150)
    plt.close()
    print(f"Saved carrying capacity plot to: {pop_plot_path}")

    run_advanced_metrics(summaries, out_dir)


def build_bedau_timeseries_from_files(files: List[str]) -> pd.DataFrame:
    if not files:
        return pd.DataFrame()
    summaries = [summarize_save(f) for f in files]
    summaries.sort(key=lambda s: s.save_time)
    latest = summaries[-1].data
    innovation_samples = latest.get("innovationActivitySamples", [])
    if not isinstance(innovation_samples, list) or not innovation_samples:
        return pd.DataFrame()

    rows = []
    for s in innovation_samples:
        if not isinstance(s, dict):
            continue
        rows.append(
            {
                "simulation_time": float(s.get("simulationTime", 0.0)),
                "a_active_all": float(s.get("totalActivityActive", 0.0)),
                "a_active_adaptive": float(s.get("totalActivityAdaptive", 0.0)),
                "a_cum_all": float(s.get("cumulativeActivityAllTime", 0.0)),
                "a_cum_adaptive": float(s.get("cumulativeActivityAdaptive", 0.0)),
                "diversity": float(s.get("diversity", 0.0)),
                "adaptive_count": float(s.get("adaptiveInnovationCount", 0.0)),
                "threshold_l": float(s.get("adaptiveThresholdL", 0.0)),
                "max_top_innovation_activity": float(
                    max([float(e.get("activity", 0.0)) for e in s.get("topInnovations", [])], default=0.0)
                ),
            }
        )
    if not rows:
        return pd.DataFrame()
    df = pd.DataFrame(rows).sort_values("simulation_time").reset_index(drop=True)
    return df


def compare_neutral_runs(
    normal_files: List[str],
    neutral_files: List[str],
    out_dir: str,
    neutral_threshold_policy: str,
) -> None:
    os.makedirs(out_dir, exist_ok=True)
    normal_df = build_bedau_timeseries_from_files(normal_files)
    neutral_df = build_bedau_timeseries_from_files(neutral_files)

    if normal_df.empty or neutral_df.empty:
        print("Missing innovation activity samples for normal or neutral run.")
        return

    if neutral_threshold_policy == "max_top_innovation":
        suggested_l = float(neutral_df["max_top_innovation_activity"].max())
    else:
        suggested_l = float(np.percentile(neutral_df["max_top_innovation_activity"], 95))

    # Overlay using runtime-computed adaptive cumulative curves.
    plt.figure(figsize=(10, 6))
    plt.plot(normal_df["simulation_time"], normal_df["a_cum_adaptive"], label="Normal A_cum(adaptive)")
    plt.plot(neutral_df["simulation_time"], neutral_df["a_cum_adaptive"], label="Neutral A_cum(adaptive)")
    plt.xlabel("Thời gian giả lập")
    plt.ylabel("A_cum(adaptive)")
    plt.title(f"Normal vs Neutral Cumulative Activity (suggested L~{suggested_l:.1f})")
    plt.grid(True, alpha=0.3)
    plt.legend()
    out_path = os.path.join(out_dir, "cumulative_activity_normal_vs_neutral.png")
    plt.tight_layout()
    plt.savefig(out_path, dpi=150)
    plt.close()
    print(f"Saved normal-vs-neutral cumulative comparison: {out_path}")

    compare_df = pd.DataFrame(
        {
            "normal_final_a_cum_adaptive": [float(normal_df["a_cum_adaptive"].iloc[-1])],
            "neutral_final_a_cum_adaptive": [float(neutral_df["a_cum_adaptive"].iloc[-1])],
            "ratio_normal_over_neutral": [
                float(normal_df["a_cum_adaptive"].iloc[-1] / max(1e-9, neutral_df["a_cum_adaptive"].iloc[-1]))
            ],
            "suggested_threshold_l_from_neutral": [suggested_l],
            "threshold_policy": [neutral_threshold_policy],
        }
    )
    out_path = os.path.join(out_dir, "normal_vs_neutral_summary.csv")
    compare_df.to_csv(out_path, index=False)
    print(f"Saved normal-vs-neutral summary: {out_path}")


def run_advanced_metrics(summaries: List[SaveSummary], out_dir: str) -> None:
    if not summaries:
        return

    latest = summaries[-1].data
    latest_creatures = latest.get("creatures", [])
    latest_deaths = latest.get("deathRecords", [])
    latest_mutations = latest.get("mutationEvents", [])
    innovation_samples = latest.get("innovationActivitySamples", [])

    # Morphological diversity (histograms)
    trait_specs = [
        ("size", "size"),
        ("speed", "speed"),
        ("visionRange", "vision_range"),
    ]
    for trait_key, trait_name in trait_specs:
        values = []
        for c in latest_creatures:
            genome = c.get("genome", {})
            try:
                values.append(float(genome.get(trait_key, 0.0)))
            except Exception:
                continue
        if not values:
            continue
        plt.figure(figsize=(8, 5))
        plt.hist(values, bins=20, color="#4C78A8", alpha=0.85)
        plt.xlabel(trait_key)
        plt.ylabel("Count")
        plt.title(f"Morphological Diversity - {trait_key}")
        plt.grid(True, alpha=0.25)
        out_path = os.path.join(out_dir, f"morphology_hist_{trait_name}.png")
        plt.tight_layout()
        plt.savefig(out_path, dpi=150)
        plt.close()
        print(f"Saved morphology histogram: {out_path}")

    if isinstance(innovation_samples, list) and innovation_samples:
        # Bedau-lite adaptive activity from runtime innovation registry summaries.
        valid_samples = [
            s for s in innovation_samples
            if isinstance(s, dict) and "simulationTime" in s
        ]
        valid_samples.sort(key=lambda s: float(s.get("simulationTime", 0.0)))

        if valid_samples:
            xs = [float(s.get("simulationTime", 0.0)) for s in valid_samples]
            ys_activity = [float(s.get("totalActivityActive", 0.0)) for s in valid_samples]
            ys_diversity = [float(s.get("diversity", 0.0)) for s in valid_samples]
            ys_adaptive = [float(s.get("totalActivityAdaptive", 0.0)) for s in valid_samples]
            threshold_l = float(valid_samples[-1].get("adaptiveThresholdL", 0.0))
            ys_adaptive_count = [float(s.get("adaptiveInnovationCount", 0.0)) for s in valid_samples]

            plt.figure(figsize=(10, 6))
            plt.plot(xs, ys_activity, label="Adaptive activity (active usage)")
            plt.plot(xs, ys_diversity, label="Diversity D")
            plt.plot(xs, ys_adaptive, label="Adaptive activity (>=L)")
            plt.xlabel("Thời gian giả lập")
            plt.ylabel("Giá trị")
            plt.title(f"Adaptive Activity (Bedau-lite, L={threshold_l:.1f})")
            plt.grid(True, alpha=0.3)
            plt.legend()
            out_path = os.path.join(out_dir, "adaptive_activity_bedau_lite.png")
            plt.tight_layout()
            plt.savefig(out_path, dpi=150)
            plt.close()
            print(f"Saved adaptive activity bedau-lite: {out_path}")

            adaptive_summary_df = pd.DataFrame(
                {
                    "simulation_time": xs,
                    "total_activity_active": ys_activity,
                    "total_activity_adaptive": ys_adaptive,
                    "diversity": ys_diversity,
                    "adaptive_innovation_count": ys_adaptive_count,
                    "adaptive_threshold_l": [threshold_l] * len(xs),
                }
            )
            out_path = os.path.join(out_dir, "adaptive_activity_timeseries.csv")
            adaptive_summary_df.to_csv(out_path, index=False)
            print(f"Saved adaptive activity timeseries: {out_path}")

            # Evolutionary activity wave (stacked area from top innovations)
            innovation_totals = {}
            for s in valid_samples:
                for e in s.get("topInnovations", []):
                    iid = e.get("innovationId", "")
                    val = float(e.get("activity", 0.0))
                    if not iid or val < threshold_l:
                        continue
                    innovation_totals[iid] = innovation_totals.get(iid, 0.0) + val

            top_ids = [k for k, _ in sorted(innovation_totals.items(), key=lambda kv: kv[1], reverse=True)[:8]]
            if top_ids:
                stacked = {iid: [] for iid in top_ids}
                other_vals = []
                for s in valid_samples:
                    per_sample = {}
                    for e in s.get("topInnovations", []):
                        iid = e.get("innovationId", "")
                        if iid:
                            v = float(e.get("activity", 0.0))
                            if v >= threshold_l:
                                per_sample[iid] = v
                    used = 0.0
                    for iid in top_ids:
                        v = per_sample.get(iid, 0.0)
                        stacked[iid].append(v)
                        used += v
                    total_active = float(s.get("totalActivityAdaptive", 0.0))
                    other_vals.append(max(0.0, total_active - used))

                labels = top_ids + ["other"]
                arrays = [stacked[iid] for iid in top_ids] + [other_vals]
                plt.figure(figsize=(12, 6))
                plt.stackplot(xs, *arrays, labels=labels, alpha=0.85)
                plt.xlabel("Thời gian giả lập")
                plt.ylabel("Activity đóng góp")
                plt.title("Evolutionary Activity Wave (Adaptive Innovations)")
                plt.legend(loc="upper left", ncol=3, fontsize=8)
                plt.grid(True, alpha=0.2)
                out_path = os.path.join(out_dir, "adaptive_activity_wave.png")
                plt.tight_layout()
                plt.savefig(out_path, dpi=150)
                plt.close()
                print(f"Saved adaptive activity wave: {out_path}")

                # Export wave matrix for reproducible reports.
                wave_df = pd.DataFrame({"simulation_time": xs})
                for iid in top_ids:
                    wave_df[f"innovation_{iid}"] = stacked[iid]
                wave_df["other"] = other_vals
                out_path = os.path.join(out_dir, "adaptive_activity_wave_matrix.csv")
                wave_df.to_csv(out_path, index=False)
                print(f"Saved adaptive activity wave matrix: {out_path}")
    else:
        # Fallback for older saves: mutation-atom persistence across snapshots.
        sim_times = []
        persistence_scores = []
        seen_at = {}
        for summary in summaries:
            sim_time = summary.simulation_time
            sim_times.append(sim_time)
            atoms = set()
            for c in summary.data.get("creatures", []):
                atom_ids = c.get("mutationAtomIds", [])
                if isinstance(atom_ids, list) and atom_ids:
                    for atom in atom_ids:
                        if isinstance(atom, str) and atom:
                            atoms.add(atom)
                else:
                    h = c.get("genotypeHash")
                    if isinstance(h, str) and h:
                        atoms.add(f"legacy_{h}")
            for atom in atoms:
                seen_at.setdefault(atom, []).append(sim_time)

            current_count = {}
            for c in summary.data.get("creatures", []):
                atom_ids = c.get("mutationAtomIds", [])
                if isinstance(atom_ids, list) and atom_ids:
                    for atom in atom_ids:
                        if isinstance(atom, str) and atom:
                            current_count[atom] = current_count.get(atom, 0) + 1
                else:
                    h = c.get("genotypeHash")
                    if isinstance(h, str) and h:
                        key = f"legacy_{h}"
                        current_count[key] = current_count.get(key, 0) + 1

            score = 0.0
            lifetime_threshold = 2 * metric_dt_guess(summaries)
            for atom in atoms:
                times = seen_at.get(atom, [])
                if len(times) >= 2 and (times[-1] - times[0]) >= lifetime_threshold:
                    score += float(current_count.get(atom, 1))
            persistence_scores.append(score)

        if sim_times:
            plt.figure(figsize=(10, 6))
            plt.plot(sim_times, persistence_scores, marker="o", label="Adaptive atom activity")
            plt.xlabel("Thời gian giả lập")
            plt.ylabel("Điểm activity (weighted carriers)")
            plt.title("Adaptive Activity (Mutation-Atom Proxy)")
            plt.grid(True, alpha=0.3)
            plt.legend()
            out_path = os.path.join(out_dir, "adaptive_activity_proxy.png")
            plt.tight_layout()
            plt.savefig(out_path, dpi=150)
            plt.close()
            print(f"Saved adaptive activity proxy: {out_path}")

    # Cumulative evolutionary activity
    if isinstance(innovation_samples, list) and innovation_samples:
        sorted_samples = sorted(
            [s for s in innovation_samples if isinstance(s, dict)],
            key=lambda s: float(s.get("simulationTime", 0.0)),
        )
        mutation_times = [float(s.get("simulationTime", 0.0)) for s in sorted_samples]
        cum_all = [float(s.get("cumulativeActivityAllTime", 0.0)) for s in sorted_samples]
        cum_adaptive = [float(s.get("cumulativeActivityAdaptive", 0.0)) for s in sorted_samples]
        threshold_l = float(sorted_samples[-1].get("adaptiveThresholdL", 0.0)) if sorted_samples else 0.0
        if mutation_times:
            plt.figure(figsize=(10, 6))
            plt.plot(mutation_times, cum_all, label="A_cum(all innovations)")
            plt.plot(mutation_times, cum_adaptive, label="A_cum(adaptive, activity>=L)")
            plt.xlabel("Thời gian giả lập")
            plt.ylabel("A_cum")
            plt.title(f"Cumulative Evolutionary Activity (Bedau-lite, L={threshold_l:.1f})")
            plt.grid(True, alpha=0.3)
            plt.legend()
            out_path = os.path.join(out_dir, "cumulative_evolutionary_activity_bedau_lite.png")
            plt.tight_layout()
            plt.savefig(out_path, dpi=150)
            plt.close()
            print(f"Saved cumulative evolutionary activity bedau-lite: {out_path}")
    elif latest_mutations:
        mutation_times = []
        cum = []
        count = 0
        sorted_events = sorted(latest_mutations, key=lambda e: float(e.get("simulationTime", 0.0)))
        for e in sorted_events:
            count += 1
            mutation_times.append(float(e.get("simulationTime", 0.0)))
            cum.append(count)
        plt.figure(figsize=(10, 6))
        plt.plot(mutation_times, cum, label="Cumulative mutation events")
        plt.xlabel("Thời gian giả lập")
        plt.ylabel("Sự kiện đột biến tích lũy")
        plt.title("Cumulative Evolutionary Activity (Proxy)")
        plt.grid(True, alpha=0.3)
        plt.legend()
        out_path = os.path.join(out_dir, "cumulative_evolutionary_activity_proxy.png")
        plt.tight_layout()
        plt.savefig(out_path, dpi=150)
        plt.close()
        print(f"Saved cumulative evolutionary activity proxy: {out_path}")

    # Brain complexity vs fitness (chỉ lấy cá thể trưởng thành để giảm nhiễu từ newborns)
    mature_threshold = 0.85
    xs = []
    ys = []
    death_rows = []
    if latest_deaths:
        for d in latest_deaths:
            maturity_at_death = d.get("maturityAtDeath", None)
            if maturity_at_death is None:
                # Save cũ chưa có maturityAtDeath -> bỏ qua để giữ tính nhất quán.
                continue
            if float(maturity_at_death) < mature_threshold:
                continue
            neuron_count = float(d.get("neuronCount", 0.0))
            connection_count = float(d.get("connectionCount", 0.0))
            lifespan = float(d.get("lifespan", 0.0))
            complexity = neuron_count + connection_count
            if complexity > 0:
                xs.append(complexity)
                ys.append(lifespan)
                death_rows.append(
                    {
                        "complexity": complexity,
                        "lifespan": lifespan,
                        "neuron_count": neuron_count,
                        "connection_count": connection_count,
                        "death_time": float(d.get("deathTime", 0.0)),
                        "offspring_count": float(d.get("offspringCount", 0.0)),
                        "total_energy_gained": float(d.get("totalEnergyGained", 0.0)),
                    }
                )
    else:
        for c in latest_creatures:
            if float(c.get("maturity", 0.0)) < mature_threshold:
                continue
            brain = c.get("brain", {})
            complexity = float(len(brain.get("neurons", [])) + len(brain.get("connections", [])))
            age = float(c.get("age", 0.0))
            if complexity > 0:
                xs.append(complexity)
                ys.append(age)
    if xs:
        x_arr = np.asarray(xs, dtype=float)
        y_arr = np.asarray(ys, dtype=float)

        plt.figure(figsize=(8, 6))
        plt.scatter(xs, ys, alpha=0.4, s=12)

        # Trend line (OLS)
        if len(xs) >= 2:
            slope, intercept = np.polyfit(x_arr, y_arr, 1)
            x_line = np.linspace(float(np.min(x_arr)), float(np.max(x_arr)), 100)
            y_line = slope * x_line + intercept
            plt.plot(x_line, y_line, color="red", linewidth=1.5, label="Trend line")

            # R^2
            y_hat = slope * x_arr + intercept
            ss_res = float(np.sum((y_arr - y_hat) ** 2))
            ss_tot = float(np.sum((y_arr - np.mean(y_arr)) ** 2))
            r2 = 0.0 if ss_tot <= 1e-9 else max(0.0, 1.0 - ss_res / ss_tot)
            plt.text(
                0.03,
                0.97,
                f"R^2={r2:.3f}",
                transform=plt.gca().transAxes,
                verticalalignment="top",
                bbox={"boxstyle": "round", "facecolor": "white", "alpha": 0.8},
            )

        plt.xlabel("Brain complexity (neurons + connections)")
        plt.ylabel("Fitness proxy (lifespan or age)")
        plt.title("Brain Complexity vs Fitness")
        plt.grid(True, alpha=0.3)
        if len(xs) >= 2:
            plt.legend()
        out_path = os.path.join(out_dir, "brain_complexity_vs_fitness.png")
        plt.tight_layout()
        plt.savefig(out_path, dpi=150)
        plt.close()
        print(f"Saved brain complexity vs fitness: {out_path}")

        # Neural efficiency: E = F / C
        eps = 1e-9
        efficiency = y_arr / np.maximum(x_arr, eps)
        plt.figure(figsize=(8, 5))
        plt.hist(efficiency, bins=30, color="#59A14F", alpha=0.85)
        plt.xlabel("Neural efficiency E = lifespan / complexity")
        plt.ylabel("Count")
        plt.title("Neural Efficiency Distribution")
        plt.grid(True, alpha=0.25)
        out_path = os.path.join(out_dir, "neural_efficiency_histogram.png")
        plt.tight_layout()
        plt.savefig(out_path, dpi=150)
        plt.close()
        print(f"Saved neural efficiency histogram: {out_path}")

        # Bloat zone stats: high complexity, low fitness
        c_p75 = float(np.percentile(x_arr, 75))
        f_p25 = float(np.percentile(y_arr, 25))
        bloat_mask = (x_arr >= c_p75) & (y_arr <= f_p25)
        bloat_count = int(np.sum(bloat_mask))
        total_count = int(len(x_arr))
        bloat_ratio = 0.0 if total_count == 0 else float(bloat_count / total_count)
        bloat_df = pd.DataFrame(
            [
                {
                    "sample_count": total_count,
                    "complexity_p75": c_p75,
                    "fitness_p25": f_p25,
                    "bloat_count": bloat_count,
                    "bloat_ratio": bloat_ratio,
                }
            ]
        )
        out_path = os.path.join(out_dir, "bloat_zone_stats.csv")
        bloat_df.to_csv(out_path, index=False)
        print(f"Saved bloat zone stats: {out_path}")

        # Optional richer metrics when death records are available
        if death_rows:
            death_df = pd.DataFrame(death_rows)
            death_df["density"] = death_df.apply(
                lambda r: 0.0
                if r["neuron_count"] < 2
                else float(r["connection_count"] / (r["neuron_count"] * (r["neuron_count"] - 1))),
                axis=1,
            )
            death_df["energy_efficiency"] = death_df.apply(
                lambda r: 0.0 if r["lifespan"] <= 1e-9 else float(r["total_energy_gained"] / r["lifespan"]),
                axis=1,
            )
            death_df["biomass_contribution"] = death_df["offspring_count"]

            out_path = os.path.join(out_dir, "brain_fitness_enriched_metrics.csv")
            death_df.to_csv(out_path, index=False)
            print(f"Saved enriched brain-fitness metrics: {out_path}")

            # Moving centroid over time
            if len(death_df) >= 10:
                n_bins = min(12, max(4, len(death_df) // 20))
                death_df = death_df.sort_values("death_time")
                death_df["time_bin"] = pd.cut(death_df["death_time"], bins=n_bins, labels=False, include_lowest=True)
                centroid = death_df.groupby("time_bin", as_index=False).agg(
                    mean_complexity=("complexity", "mean"),
                    mean_lifespan=("lifespan", "mean"),
                    count=("complexity", "count"),
                )
                plt.figure(figsize=(8, 6))
                plt.scatter(
                    centroid["mean_complexity"],
                    centroid["mean_lifespan"],
                    s=np.maximum(20, centroid["count"] * 3),
                    alpha=0.8,
                    c=np.arange(len(centroid)),
                    cmap="viridis",
                )
                plt.plot(centroid["mean_complexity"], centroid["mean_lifespan"], alpha=0.6, linewidth=1.2)
                plt.xlabel("Mean brain complexity")
                plt.ylabel("Mean lifespan")
                plt.title("Complexity-Fitness Moving Centroid")
                plt.grid(True, alpha=0.25)
                out_path = os.path.join(out_dir, "complexity_fitness_centroid_over_time.png")
                plt.tight_layout()
                plt.savefig(out_path, dpi=150)
                plt.close()
                print(f"Saved complexity-fitness centroid plot: {out_path}")
    else:
        print("Skipped brain complexity vs fitness: no mature samples found.")

def metric_dt_guess(summaries: List[SaveSummary]) -> float:
    if len(summaries) < 2:
        return 1.0
    dts = []
    for i in range(1, len(summaries)):
        dt = max(0.0, summaries[i].simulation_time - summaries[i - 1].simulation_time)
        if dt > 0:
            dts.append(dt)
    return float(np.median(dts)) if dts else 1.0


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
        choices=["summary", "heatmap", "compare_neutral"],
        required=True,
        help="Chế độ phân tích: 'summary', 'heatmap', hoặc 'compare_neutral'.",
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
    parser.add_argument(
        "--normal-inputs",
        nargs="*",
        default=[],
        help="Danh sách file/dir/pattern cho run normal khi mode='compare_neutral'.",
    )
    parser.add_argument(
        "--neutral-inputs",
        nargs="*",
        default=[],
        help="Danh sách file/dir/pattern cho run neutral khi mode='compare_neutral'.",
    )
    parser.add_argument(
        "--neutral-threshold-policy",
        choices=["max_top_innovation", "p95_top_innovation"],
        default="max_top_innovation",
        help="Cách gợi ý ngưỡng L từ neutral run.",
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
    elif args.mode == "compare_neutral":
        normal_files = expand_input_patterns(args.normal_inputs)
        neutral_files = expand_input_patterns(args.neutral_inputs)
        compare_neutral_runs(normal_files, neutral_files, out_dir, args.neutral_threshold_policy)


if __name__ == "__main__":
    main()

