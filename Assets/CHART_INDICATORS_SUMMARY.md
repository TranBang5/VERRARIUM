# TÓM TẮT CÁC CHỈ SỐ PHÂN TÍCH CHO BIỂU ĐỒ

## 📊 CÁC CHỈ SỐ CHÍNH ĐÃ TRÍCH XUẤT

### 1. TĂNG TRƯỞNG DÂN SỐ

| Chỉ Số | Giá Trị | Đơn Vị | Mô Tả |
|--------|---------|--------|-------|
| **Current Population** | 162 | sinh vật | Dân số hiện tại |
| **Target Population** | 200 | sinh vật | Mục tiêu dân số |
| **Population %** | 81.00 | % | % so với mục tiêu |
| **Total Born** | 5,899 | sinh vật | Tổng số đã sinh ra |
| **Total Died** | 5,737 | sinh vật | Tổng số đã chết |
| **Net Growth** | 162 | sinh vật | Tăng trưởng ròng |
| **Survival Rate** | 2.75 | % | Tỷ lệ sống sót |
| **Birth Rate** | 58.99 | /phút | Tốc độ sinh |
| **Death Rate** | 57.37 | /phút | Tốc độ chết |

**File CSV**: `chart_data_population.csv`

---

### 2. TUỔI THỌ TRUNG BÌNH

| Chỉ Số | Giá Trị | Đơn Vị | Mô Tả |
|--------|---------|--------|-------|
| **Average Lifespan** | 89.08 | giây | Tuổi thọ trung bình |
| **Median Lifespan** | 67.74 | giây | Tuổi thọ trung vị |
| **Min Lifespan** | 0.28 | giây | Tuổi thọ ngắn nhất |
| **Max Lifespan** | 724.18 | giây | Tuổi thọ dài nhất |
| **Std Dev** | 87.57 | giây | Độ lệch chuẩn |

#### Phân Bố Tuổi Thọ

| Nhóm Tuổi | Số Lượng | Tỷ Lệ |
|-----------|----------|-------|
| 0-30s | 38 | 23.5% |
| 30-60s | 36 | 22.2% |
| 60-120s | 50 | 30.9% |
| 120-300s | 35 | 21.6% |
| 300s+ | 3 | 1.9% |

**File CSV**: 
- `chart_data_lifespan.csv` (thống kê)
- `chart_data_lifespan_distribution.csv` (phân bố)

---

### 3. PHÁT TRIỂN BỘ NÃO

#### Tổng Thể

| Chỉ Số | Neurons | Connections |
|--------|---------|-------------|
| **Average** | 22.1 | 88.3 |
| **Median** | 22.0 | 88.0 |
| **Min** | 18 | 67 |
| **Max** | 28 | 106 |

#### Theo Thế Hệ

| Thế Hệ | Avg Neurons | Avg Connections | Số Lượng SV |
|--------|-------------|-----------------|-------------|
| 26 | 18.8 | 74.8 | 4 |
| 27 | 18.8 | 75.2 | 8 |
| 28 | 18.1 | 75.6 | 10 |
| ... | ... | ... | ... |
| 65 | 24.0 | 95.0 | 18 |

**Phạm vi thế hệ**: 26 - 65 (39 thế hệ)

**File CSV**: 
- `chart_data_brain_by_generation.csv` (theo thế hệ)
- `chart_data_brain_overall.csv` (tổng thể)

---

## 📈 CÁC BIỂU ĐỒ ĐỀ XUẤT

### 1. Biểu Đồ Tăng Trưởng Dân Số

#### A. Bar Chart: Current vs Target
- **X-axis**: Categories (Current, Target)
- **Y-axis**: Population
- **Data**: `CurrentPopulation`, `TargetPopulation`

#### B. Bar Chart: Born vs Died
- **X-axis**: Categories (Born, Died)
- **Y-axis**: Count
- **Data**: `TotalBorn`, `TotalDied`

#### C. Pie Chart: Survival Rate
- **Labels**: Survival, Death
- **Values**: `SurvivalRate`, `100 - SurvivalRate`

#### D. Bar Chart: Birth Rate vs Death Rate
- **X-axis**: Categories (Birth Rate, Death Rate)
- **Y-axis**: Rate per minute
- **Data**: `BirthRatePerMinute`, `DeathRatePerMinute`

---

### 2. Biểu Đồ Tuổi Thọ

#### A. Bar Chart: Thống Kê Tuổi Thọ
- **X-axis**: Statistics (Average, Median, Min, Max)
- **Y-axis**: Lifespan (seconds)
- **Data**: `AverageLifespan`, `MedianLifespan`, `MinLifespan`, `MaxLifespan`

#### B. Bar Chart: Phân Bố Tuổi Thọ
- **X-axis**: Age Groups (0-30s, 30-60s, 60-120s, 120-300s, 300s+)
- **Y-axis**: Count
- **Data**: `chart_data_lifespan_distribution.csv`

#### C. Pie Chart: Phân Bố Tuổi Thọ
- **Labels**: Age Groups
- **Values**: Count hoặc Percentage
- **Data**: `chart_data_lifespan_distribution.csv`

---

### 3. Biểu Đồ Phát Triển Bộ Não

#### A. Line Chart: Neurons Theo Thế Hệ
- **X-axis**: Generation
- **Y-axis**: Average Neurons
- **Data**: `Generation`, `AverageNeurons` từ `chart_data_brain_by_generation.csv`

#### B. Line Chart: Connections Theo Thế Hệ
- **X-axis**: Generation
- **Y-axis**: Average Connections
- **Data**: `Generation`, `AverageConnections` từ `chart_data_brain_by_generation.csv`

#### C. Dual Axis Chart: Neurons & Connections
- **X-axis**: Generation
- **Y-axis (Left)**: Average Neurons
- **Y-axis (Right)**: Average Connections
- **Data**: Cả hai từ `chart_data_brain_by_generation.csv`

#### D. Scatter Plot: Neurons vs Connections
- **X-axis**: Average Neurons
- **Y-axis**: Average Connections
- **Data**: `AverageNeurons`, `AverageConnections` từ `chart_data_brain_by_generation.csv`

#### E. Bar Chart: Min/Max/Average per Generation
- **X-axis**: Generation
- **Y-axis**: Neurons hoặc Connections
- **Data**: `MinNeurons`, `MaxNeurons`, `AverageNeurons` (hoặc Connections)

---

## 🎯 CÁCH SỬ DỤNG NHANH

### Excel/Google Sheets

1. Mở file CSV
2. Chọn cột cần vẽ
3. Insert → Chart
4. Chọn loại biểu đồ phù hợp

### Python

```python
import pandas as pd
import matplotlib.pyplot as plt

# Đọc dữ liệu
df = pd.read_csv('Assets/chart_data_brain_by_generation.csv')

# Vẽ biểu đồ
plt.plot(df['Generation'], df['AverageNeurons'])
plt.xlabel('Generation')
plt.ylabel('Average Neurons')
plt.title('Brain Development')
plt.show()
```

### JavaScript (Chart.js)

```javascript
// Đọc CSV và parse
const data = {
  labels: generations,
  datasets: [{
    label: 'Average Neurons',
    data: averageNeurons,
    borderColor: 'rgb(75, 192, 192)',
    tension: 0.1
  }]
};
```

---

## 📁 DANH SÁCH FILE

| File | Mô Tả | Format |
|------|-------|--------|
| `chart_data.json` | Dữ liệu đầy đủ | JSON |
| `chart_data_population.csv` | Tăng trưởng dân số | CSV |
| `chart_data_lifespan.csv` | Thống kê tuổi thọ | CSV |
| `chart_data_lifespan_distribution.csv` | Phân bố tuổi thọ | CSV |
| `chart_data_brain_by_generation.csv` | Bộ não theo thế hệ | CSV |
| `chart_data_brain_overall.csv` | Bộ não tổng thể | CSV |
| `chart_population.png` | Biểu đồ dân số (đã tạo) | PNG |
| `chart_lifespan.png` | Biểu đồ tuổi thọ (đã tạo) | PNG |
| `chart_brain.png` | Biểu đồ bộ não (đã tạo) | PNG |

---

## 🔄 CẬP NHẬT

Để cập nhật dữ liệu:

```bash
# Trích xuất dữ liệu mới
python extract_chart_data.py

# Tạo biểu đồ mới
python visualize_charts.py
```

---

*Tóm tắt được tạo từ dữ liệu autosave tại simulation time: 6000.05s (100 phút)*

