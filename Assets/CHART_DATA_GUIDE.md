# HƯỚNG DẪN SỬ DỤNG DỮ LIỆU BIỂU ĐỒ

Tài liệu này hướng dẫn cách sử dụng các file dữ liệu đã được trích xuất từ autosave để tạo biểu đồ phân tích.

## 📊 CÁC FILE DỮ LIỆU ĐÃ TẠO

### 1. File JSON Tổng Hợp
- **File**: `Assets/chart_data.json`
- **Mô tả**: Chứa tất cả dữ liệu phân tích dưới dạng JSON, dễ dàng import vào các công cụ phân tích
- **Cấu trúc**:
  ```json
  {
    "metadata": {...},
    "population": {...},
    "lifespan": {...},
    "brainDevelopment": {
      "byGeneration": [...],
      "overall": {...}
    }
  }
  ```

### 2. File CSV cho Tăng Trưởng Dân Số
- **File**: `Assets/chart_data_population.csv`
- **Các cột**:
  - `SimulationTime`: Thời gian simulation (giây)
  - `CurrentPopulation`: Dân số hiện tại
  - `TotalBorn`: Tổng số sinh vật đã sinh ra
  - `TotalDied`: Tổng số sinh vật đã chết
  - `NetGrowth`: Tăng trưởng ròng (Born - Died)
  - `SurvivalRate`: Tỷ lệ sống sót (%)
  - `TargetPopulation`: Mục tiêu dân số
  - `PopulationPercentage`: % so với mục tiêu
  - `BirthRatePerMinute`: Tốc độ sinh (/phút)
  - `DeathRatePerMinute`: Tốc độ chết (/phút)

### 3. File CSV cho Tuổi Thọ
- **File**: `Assets/chart_data_lifespan.csv`
- **Các cột**:
  - `SimulationTime`: Thời gian simulation (giây)
  - `AverageLifespan`: Tuổi thọ trung bình (giây)
  - `MedianLifespan`: Tuổi thọ trung vị (giây)
  - `MinLifespan`: Tuổi thọ ngắn nhất (giây)
  - `MaxLifespan`: Tuổi thọ dài nhất (giây)
  - `StdDevLifespan`: Độ lệch chuẩn (giây)
  - `TotalCreatures`: Tổng số sinh vật

### 4. File CSV cho Phân Bố Tuổi Thọ
- **File**: `Assets/chart_data_lifespan_distribution.csv`
- **Các cột**:
  - `AgeGroup`: Nhóm tuổi (0-30s, 30-60s, 60-120s, 120-300s, 300s+)
  - `Count`: Số lượng sinh vật trong nhóm
  - `Percentage`: Tỷ lệ phần trăm

### 5. File CSV cho Phát Triển Bộ Não Theo Thế Hệ
- **File**: `Assets/chart_data_brain_by_generation.csv`
- **Các cột**:
  - `Generation`: Số thế hệ
  - `AverageNeurons`: Số neurons trung bình
  - `MedianNeurons`: Số neurons trung vị
  - `MinNeurons`: Số neurons tối thiểu
  - `MaxNeurons`: Số neurons tối đa
  - `AverageConnections`: Số connections trung bình
  - `MedianConnections`: Số connections trung vị
  - `MinConnections`: Số connections tối thiểu
  - `MaxConnections`: Số connections tối đa
  - `CreatureCount`: Số lượng sinh vật trong thế hệ

### 6. File CSV cho Tổng Thể Bộ Não
- **File**: `Assets/chart_data_brain_overall.csv`
- **Các cột**: Tương tự như file theo thế hệ nhưng là thống kê tổng thể

---

## 📈 CÁC CHỈ SỐ PHÂN TÍCH CHÍNH

### 1. Tăng Trưởng Dân Số

#### Các chỉ số quan trọng:
- **Current Population**: Dân số hiện tại (162)
- **Target Population**: Mục tiêu (200)
- **Population Percentage**: 81% mục tiêu
- **Survival Rate**: 2.75% (rất thấp)
- **Birth Rate**: 58.99 sinh vật/phút
- **Death Rate**: 57.37 sinh vật/phút

#### Cách sử dụng:
- So sánh `CurrentPopulation` vs `TargetPopulation` để đánh giá hiệu quả
- Theo dõi `SurvivalRate` để đánh giá độ khắc nghiệt của môi trường
- So sánh `BirthRatePerMinute` vs `DeathRatePerMinute` để xem xu hướng tăng/giảm

### 2. Tuổi Thọ Trung Bình

#### Các chỉ số quan trọng:
- **Average Lifespan**: 89.08 giây (≈1.5 phút)
- **Median Lifespan**: 67.74 giây (≈1.1 phút)
- **Max Lifespan**: 724.18 giây (≈12 phút)
- **Min Lifespan**: 0.28 giây
- **Std Dev**: 87.57 giây (độ biến thiên lớn)

#### Phân bố tuổi thọ:
- **0-30s**: 38 sinh vật (23.5%)
- **30-60s**: 36 sinh vật (22.2%)
- **60-120s**: 50 sinh vật (30.9%)
- **120-300s**: 35 sinh vật (21.6%)
- **300s+**: 3 sinh vật (1.9%)

#### Cách sử dụng:
- Vẽ biểu đồ phân bố tuổi thọ để thấy xu hướng
- So sánh Average vs Median để phát hiện outliers
- Phân tích nhóm tuổi để hiểu rõ hơn về vòng đời

### 3. Sự Phát Triển Bộ Não

#### Các chỉ số quan trọng:
- **Average Neurons**: 22.1 neurons
- **Range**: 18-28 neurons
- **Average Connections**: 88.3 connections
- **Range**: 67-106 connections
- **Generations**: 26-65 (39 thế hệ)

#### Phát triển theo thế hệ:
- Có thể thấy xu hướng tăng/giảm số lượng neurons và connections qua các thế hệ
- Thế hệ cao hơn thường có bộ não phức tạp hơn (do tiến hóa)

#### Cách sử dụng:
- Vẽ line chart: Generation (X) vs AverageNeurons (Y)
- Vẽ line chart: Generation (X) vs AverageConnections (Y)
- So sánh hai đường để thấy mối quan hệ giữa neurons và connections

---

## 🎨 CÁCH TẠO BIỂU ĐỒ

### Option 1: Sử dụng Script Python (Đã có sẵn)

```bash
python visualize_charts.py
```

Script này sẽ tạo 3 file PNG:
- `Assets/chart_population.png`: Biểu đồ tăng trưởng dân số
- `Assets/chart_lifespan.png`: Biểu đồ tuổi thọ
- `Assets/chart_brain.png`: Biểu đồ phát triển bộ não

### Option 2: Sử dụng Excel/Google Sheets

1. Mở file CSV trong Excel hoặc Google Sheets
2. Chọn dữ liệu cần vẽ
3. Insert → Chart
4. Chọn loại biểu đồ phù hợp:
   - **Bar Chart**: Cho so sánh (Population, Born vs Died)
   - **Line Chart**: Cho xu hướng theo thời gian (Brain Development)
   - **Pie Chart**: Cho phân bố (Age Distribution)
   - **Scatter Plot**: Cho mối quan hệ (Neurons vs Connections)

### Option 3: Sử dụng Python với Matplotlib/Seaborn

```python
import pandas as pd
import matplotlib.pyplot as plt

# Đọc CSV
df = pd.read_csv('Assets/chart_data_brain_by_generation.csv')

# Vẽ biểu đồ
plt.figure(figsize=(10, 6))
plt.plot(df['Generation'], df['AverageNeurons'], marker='o')
plt.xlabel('Generation')
plt.ylabel('Average Neurons')
plt.title('Brain Development Over Generations')
plt.grid(True)
plt.savefig('brain_chart.png')
```

### Option 4: Sử dụng JavaScript (Chart.js, D3.js)

```javascript
// Đọc CSV và vẽ biểu đồ
fetch('Assets/chart_data_brain_by_generation.csv')
  .then(response => response.text())
  .then(data => {
    // Parse CSV và vẽ biểu đồ
    // Sử dụng Chart.js hoặc D3.js
  });
```

### Option 5: Sử dụng Online Tools

- **Google Sheets**: Upload CSV và tạo biểu đồ
- **Plotly**: https://plotly.com/chart-studio/
- **Datawrapper**: https://www.datawrapper.de/
- **Observable**: https://observablehq.com/

---

## 📋 VÍ DỤ BIỂU ĐỒ ĐỀ XUẤT

### 1. Biểu Đồ Tăng Trưởng Dân Số
- **Bar Chart**: Current Population vs Target Population
- **Bar Chart**: Total Born vs Total Died
- **Pie Chart**: Survival Rate
- **Line Chart**: Birth Rate vs Death Rate (nếu có nhiều điểm thời gian)

### 2. Biểu Đồ Tuổi Thọ
- **Bar Chart**: Average, Median, Min, Max Lifespan
- **Bar Chart**: Age Distribution (5 nhóm)
- **Pie Chart**: Age Distribution
- **Histogram**: Phân bố tuổi thọ chi tiết (nếu có dữ liệu đầy đủ)

### 3. Biểu Đồ Phát Triển Bộ Não
- **Line Chart**: Average Neurons theo Generation
- **Line Chart**: Average Connections theo Generation
- **Dual Axis Chart**: Neurons và Connections trên cùng biểu đồ
- **Scatter Plot**: Neurons vs Connections (mối quan hệ)
- **Bar Chart**: Min/Max/Average cho mỗi thế hệ

---

## 🔄 CẬP NHẬT DỮ LIỆU

Để cập nhật dữ liệu từ autosave mới:

```bash
# 1. Trích xuất dữ liệu từ autosave mới
python extract_chart_data.py

# 2. Tạo biểu đồ mới
python visualize_charts.py
```

**Lưu ý**: File autosave hiện tại chỉ là một snapshot tại một thời điểm. Để có dữ liệu theo thời gian, bạn cần:
1. Lưu nhiều autosave tại các thời điểm khác nhau
2. Hoặc tích hợp hệ thống tracking metrics vào code Unity

---

## 📊 DỮ LIỆU HIỆN TẠI

Từ file autosave hiện tại (simulation time: 6000.05s ≈ 100 phút):

- **Dân số**: 162/200 (81% mục tiêu)
- **Tuổi thọ trung bình**: 89.08 giây
- **Bộ não**: 22.1 neurons, 88.3 connections trung bình
- **Thế hệ**: 26-65 (39 thế hệ đã trải qua)

---

## 💡 GỢI Ý PHÂN TÍCH SÂU HƠN

1. **So sánh giữa các thế hệ**: Xem thế hệ nào có bộ não phức tạp nhất
2. **Mối quan hệ tuổi thọ và gen**: Phân tích xem gen nào ảnh hưởng đến tuổi thọ
3. **Hiệu quả sinh sản**: Tính số con trung bình mỗi thế hệ
4. **Tỷ lệ sống sót theo thế hệ**: Xem thế hệ nào sống sót tốt nhất

---

*Tài liệu được tạo tự động từ dữ liệu autosave*

