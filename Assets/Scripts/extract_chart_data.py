"""
Script de trich xuat du lieu tu autosave.json de tao bieu do
Chay: python Assets/Scripts/extract_chart_data.py
"""

import json
import csv
import statistics
from collections import defaultdict
from datetime import datetime
import sys
import os

# Fix encoding for Windows
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Change to project root
script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(os.path.dirname(script_dir))
os.chdir(project_root)

# Đọc file autosave
print("Dang doc file autosave.json...")
with open('Assets/autosave.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

sim_time = data["simulationTime"]
creatures = data['creatures']

print(f"Da doc {len(creatures)} creatures tu simulation time {sim_time:.2f}s")

# ============================================
# 1. PHÂN TÍCH TĂNG TRƯỞNG DÂN SỐ
# ============================================
print("\n1. Tinh toan tang truong dan so...")

population_data = {
    "simulationTime": sim_time,
    "currentPopulation": data["currentPopulation"],
    "totalBorn": data["totalCreaturesBorn"],
    "totalDied": data["totalCreaturesDied"],
    "netGrowth": data["totalCreaturesBorn"] - data["totalCreaturesDied"],
    "survivalRate": (data["totalCreaturesBorn"] - data["totalCreaturesDied"]) / data["totalCreaturesBorn"] * 100 if data["totalCreaturesBorn"] > 0 else 0,
    "targetPopulation": data["targetPopulationSize"],
    "populationPercentage": (data["currentPopulation"] / data["targetPopulationSize"] * 100) if data["targetPopulationSize"] > 0 else 0
}

# Tính tốc độ sinh và chết
birth_rate = data["totalCreaturesBorn"] / (sim_time / 60) if sim_time > 0 else 0  # per minute
death_rate = data["totalCreaturesDied"] / (sim_time / 60) if sim_time > 0 else 0  # per minute

population_data["birthRatePerMinute"] = birth_rate
population_data["deathRatePerMinute"] = death_rate

# ============================================
# 2. PHÂN TÍCH TUỔI THỌ
# ============================================
print("2. Tinh toan tuoi tho trung binh...")

ages = [c.get('age', 0) for c in creatures if 'age' in c]
if ages:
    lifespan_data = {
        "simulationTime": sim_time,
        "averageLifespan": statistics.mean(ages),
        "medianLifespan": statistics.median(ages),
        "minLifespan": min(ages),
        "maxLifespan": max(ages),
        "stdDevLifespan": statistics.stdev(ages) if len(ages) > 1 else 0,
        "totalCreatures": len(ages)
    }
    
    # Phân bố tuổi thọ theo nhóm
    age_groups = {
        "0-30s": sum(1 for a in ages if 0 <= a < 30),
        "30-60s": sum(1 for a in ages if 30 <= a < 60),
        "60-120s": sum(1 for a in ages if 60 <= a < 120),
        "120-300s": sum(1 for a in ages if 120 <= a < 300),
        "300s+": sum(1 for a in ages if a >= 300)
    }
    lifespan_data["ageDistribution"] = age_groups
else:
    lifespan_data = {}

# ============================================
# 3. PHÂN TÍCH PHÁT TRIỂN BỘ NÃO
# ============================================
print("3. Tinh toan phat trien bo nao...")

brain_stats_by_generation = defaultdict(lambda: {"neurons": [], "connections": [], "count": 0})

for creature in creatures:
    if 'brain' in creature and creature['brain']:
        brain = creature['brain']
        gen = creature.get('generationIndex', 0)
        
        neuron_count = len(brain.get('neurons', []))
        connection_count = len(brain.get('connections', []))
        
        brain_stats_by_generation[gen]["neurons"].append(neuron_count)
        brain_stats_by_generation[gen]["connections"].append(connection_count)
        brain_stats_by_generation[gen]["count"] += 1

# Tính toán trung bình theo thế hệ
brain_development = []
for gen in sorted(brain_stats_by_generation.keys()):
    stats = brain_stats_by_generation[gen]
    if stats["neurons"]:
        brain_development.append({
            "generation": gen,
            "averageNeurons": statistics.mean(stats["neurons"]),
            "medianNeurons": statistics.median(stats["neurons"]),
            "minNeurons": min(stats["neurons"]),
            "maxNeurons": max(stats["neurons"]),
            "averageConnections": statistics.mean(stats["connections"]),
            "medianConnections": statistics.median(stats["connections"]),
            "minConnections": min(stats["connections"]),
            "maxConnections": max(stats["connections"]),
            "creatureCount": stats["count"]
        })

# Tính toán tổng thể
all_neurons = []
all_connections = []
for creature in creatures:
    if 'brain' in creature and creature['brain']:
        brain = creature['brain']
        all_neurons.append(len(brain.get('neurons', [])))
        all_connections.append(len(brain.get('connections', [])))

brain_overall = {
    "simulationTime": sim_time,
    "averageNeurons": statistics.mean(all_neurons) if all_neurons else 0,
    "medianNeurons": statistics.median(all_neurons) if all_neurons else 0,
    "minNeurons": min(all_neurons) if all_neurons else 0,
    "maxNeurons": max(all_neurons) if all_neurons else 0,
    "averageConnections": statistics.mean(all_connections) if all_connections else 0,
    "medianConnections": statistics.median(all_connections) if all_connections else 0,
    "minConnections": min(all_connections) if all_connections else 0,
    "maxConnections": max(all_connections) if all_connections else 0,
    "totalCreatures": len(all_neurons)
}

# ============================================
# 4. XUẤT DỮ LIỆU RA CSV
# ============================================
print("\n4. Xuat du lieu ra CSV...")

# CSV cho tăng trưởng dân số
with open('Assets/chart_data_population.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['SimulationTime', 'CurrentPopulation', 'TotalBorn', 'TotalDied', 
                     'NetGrowth', 'SurvivalRate', 'TargetPopulation', 'PopulationPercentage',
                     'BirthRatePerMinute', 'DeathRatePerMinute'])
    writer.writerow([
        population_data["simulationTime"],
        population_data["currentPopulation"],
        population_data["totalBorn"],
        population_data["totalDied"],
        population_data["netGrowth"],
        f"{population_data['survivalRate']:.2f}",
        population_data["targetPopulation"],
        f"{population_data['populationPercentage']:.2f}",
        f"{population_data['birthRatePerMinute']:.2f}",
        f"{population_data['deathRatePerMinute']:.2f}"
    ])

# CSV cho tuổi thọ
if lifespan_data:
    with open('Assets/chart_data_lifespan.csv', 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(['SimulationTime', 'AverageLifespan', 'MedianLifespan', 
                         'MinLifespan', 'MaxLifespan', 'StdDevLifespan', 'TotalCreatures'])
        writer.writerow([
            lifespan_data["simulationTime"],
            f"{lifespan_data['averageLifespan']:.2f}",
            f"{lifespan_data['medianLifespan']:.2f}",
            f"{lifespan_data['minLifespan']:.2f}",
            f"{lifespan_data['maxLifespan']:.2f}",
            f"{lifespan_data['stdDevLifespan']:.2f}",
            lifespan_data["totalCreatures"]
        ])
    
    # CSV cho phân bố tuổi thọ
    with open('Assets/chart_data_lifespan_distribution.csv', 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(['AgeGroup', 'Count', 'Percentage'])
        total = sum(lifespan_data["ageDistribution"].values())
        for group, count in lifespan_data["ageDistribution"].items():
            writer.writerow([group, count, f"{(count/total*100):.2f}" if total > 0 else "0.00"])

# CSV cho phát triển bộ não theo thế hệ
if brain_development:
    with open('Assets/chart_data_brain_by_generation.csv', 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(['Generation', 'AverageNeurons', 'MedianNeurons', 'MinNeurons', 'MaxNeurons',
                         'AverageConnections', 'MedianConnections', 'MinConnections', 'MaxConnections',
                         'CreatureCount'])
        for bd in brain_development:
            writer.writerow([
                bd["generation"],
                f"{bd['averageNeurons']:.1f}",
                f"{bd['medianNeurons']:.1f}",
                bd["minNeurons"],
                bd["maxNeurons"],
                f"{bd['averageConnections']:.1f}",
                f"{bd['medianConnections']:.1f}",
                bd["minConnections"],
                bd["maxConnections"],
                bd["creatureCount"]
            ])

# CSV cho tổng thể bộ não
with open('Assets/chart_data_brain_overall.csv', 'w', newline='', encoding='utf-8') as f:
    writer = csv.writer(f)
    writer.writerow(['SimulationTime', 'AverageNeurons', 'MedianNeurons', 'MinNeurons', 'MaxNeurons',
                     'AverageConnections', 'MedianConnections', 'MinConnections', 'MaxConnections',
                     'TotalCreatures'])
    writer.writerow([
        brain_overall["simulationTime"],
        f"{brain_overall['averageNeurons']:.1f}",
        f"{brain_overall['medianNeurons']:.1f}",
        brain_overall["minNeurons"],
        brain_overall["maxNeurons"],
        f"{brain_overall['averageConnections']:.1f}",
        f"{brain_overall['medianConnections']:.1f}",
        brain_overall["minConnections"],
        brain_overall["maxConnections"],
        brain_overall["totalCreatures"]
    ])

# ============================================
# 5. XUẤT DỮ LIỆU RA JSON
# ============================================
print("5. Xuat du lieu ra JSON...")

chart_data = {
    "metadata": {
        "simulationTime": sim_time,
        "simulationTimeMinutes": sim_time / 60,
        "extractionDate": datetime.now().isoformat(),
        "totalCreatures": len(creatures)
    },
    "population": population_data,
    "lifespan": lifespan_data,
    "brainDevelopment": {
        "byGeneration": brain_development,
        "overall": brain_overall
    }
}

with open('Assets/chart_data.json', 'w', encoding='utf-8') as f:
    json.dump(chart_data, f, indent=2, ensure_ascii=False)

print("\nHoan thanh!")
print("\nCac file da duoc tao:")
print("  - Assets/chart_data.json (du lieu day du)")
print("  - Assets/chart_data_population.csv")
print("  - Assets/chart_data_lifespan.csv")
print("  - Assets/chart_data_lifespan_distribution.csv")
print("  - Assets/chart_data_brain_by_generation.csv")
print("  - Assets/chart_data_brain_overall.csv")

