## Phân tích dữ liệu giả lập Verrarium

Thư mục này chứa các script Python dùng để phân tích file JSON save của giả lập Verrarium.  
Các script **chạy độc lập với Unity** – bạn chỉ cần Python và các thư viện trong `requirements.txt`.

### 1. Cài đặt môi trường

```bash
cd analysis
pip install -r requirements.txt
```

### 2. Script chính: `analyze_simulation.py`

Script này hỗ trợ các nhóm phân tích:

- **Thống kê tuổi** theo nhiều file save (theo thứ tự thời gian):
  - Trung bình, min, max tuổi.
  - Vẽ biểu đồ đường thể hiện sự thay đổi các chỉ số tuổi theo thời gian.
- **Đa dạng chủng loài (speciation)**:
  - Đếm số loài (distinct `speciesId`) trong mỗi file.
  - Vẽ biểu đồ đường thể hiện số loài theo thời gian.
- **Ma trận dân số (population heatmap)**:
  - Với một file save, chia map thành lưới \(m \times n\).
  - Mỗi sinh vật tăng 1 đơn vị vào ô gần nhất theo vị trí.
  - Vẽ heatmap grayscale và lưu thành hình ảnh.
- **Các metric tiến hóa nâng cao** (khi save có telemetry mới):
  - Carrying capacity stability (`population_over_time.png`)
  - Adaptive activity Bedau-lite (`adaptive_activity_bedau_lite.png`)
  - Evolutionary activity wave (`adaptive_activity_wave.png`)
  - Adaptive activity timeseries/data (`adaptive_activity_timeseries.csv`, `adaptive_activity_wave_matrix.csv`)
  - Morphological diversity (`morphology_hist_*.png`)
  - Cumulative evolutionary activity Bedau-lite (`cumulative_evolutionary_activity_bedau_lite.png`, gồm cả `A_cum(all)` và `A_cum(adaptive)` theo ngưỡng `L`)
  - So sánh normal vs neutral (`cumulative_activity_normal_vs_neutral.png`, `normal_vs_neutral_summary.csv`)
  - Brain complexity vs fitness (`brain_complexity_vs_fitness.png`)
  - Neural efficiency histogram (`neural_efficiency_histogram.png`)
  - Bloat zone stats (`bloat_zone_stats.csv`)
  - Moving centroid (`complexity_fitness_centroid_over_time.png`)

### 3. Cách chạy

Từ thư mục `analysis`:

```bash
# Thống kê tuổi + số loài cho nhiều file save
python analyze_simulation.py --mode summary --inputs "D:/Games/Verrarium/Assets/Data/autosave_*.json" --out-dir results

# Vẽ ma trận dân số cho 1 file save
python analyze_simulation.py --mode heatmap --input "D:/Games/Verrarium/Assets/Data/autosave_20260331-134805-182810.json" --matrix-size 20 20 --out-dir results

# So sánh Cumulative Activity giữa run normal và neutral
python analyze_simulation.py --mode compare_neutral --normal-inputs "D:/Games/Verrarium/Assets/Data/run_normal/autosave_*.json" --neutral-inputs "D:/Games/Verrarium/Assets/Data/run_neutral/autosave_*.json" --neutral-threshold-policy max_top_innovation --out-dir results_compare
```

Kết quả (file CSV, PNG) sẽ được lưu trong thư mục con `results` (tạo tự động nếu chưa có).

