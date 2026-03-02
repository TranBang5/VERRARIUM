## Phân tích dữ liệu giả lập Verrarium

Thư mục này chứa các script Python dùng để phân tích file JSON save của giả lập Verrarium.  
Các script **chạy độc lập với Unity** – bạn chỉ cần Python và các thư viện trong `requirements.txt`.

### 1. Cài đặt môi trường

```bash
cd analysis
pip install -r requirements.txt
```

### 2. Script chính: `analyze_simulation.py`

Script này hỗ trợ 3 nhóm phân tích:

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

### 3. Cách chạy

Từ thư mục `analysis`:

```bash
# Thống kê tuổi + số loài cho nhiều file save
python analyze_simulation.py \
  --mode summary \
  --inputs "D:/Games/Verrarium/Saves/autosave_*.json" \
  --out-dir results

# Vẽ ma trận dân số cho 1 file save
python analyze_simulation.py --mode heatmap --input "D:/Games/Verrarium/Assets/Data/autosave_20260212-220851-221854.json" --matrix-size 50 50 --out-dir results
```

Kết quả (file CSV, PNG) sẽ được lưu trong thư mục con `results` (tạo tự động nếu chưa có).

