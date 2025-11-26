# Hướng dẫn Setup Dự án VERRARIUM trong Unity

## Mục lục
1. [Yêu cầu Hệ thống](#yêu-cầu-hệ-thống)
2. [Tạo Project Unity](#tạo-project-unity)
3. [Import và Kiểm tra Scripts](#import-và-kiểm-tra-scripts)
4. [Setup Scene Chính](#setup-scene-chính)
5. [Tạo Prefabs](#tạo-prefabs)
6. [Setup Hex Grid](#setup-hex-grid)
7. [Setup UI](#setup-ui)
8. [Cấu hình và Test](#cấu-hình-và-test)
9. [Troubleshooting](#troubleshooting)

---

## Yêu cầu Hệ thống

- **Unity Version**: 2021.3 LTS hoặc mới hơn (khuyến nghị 2022.3 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP) hoặc Built-in
- **TextMeshPro**: Đã được import tự động với Unity mới
- **Platform**: Windows, Mac, hoặc Linux

---

## Tạo Project Unity

### Bước 1: Tạo Project Mới

1. Mở Unity Hub
2. Click **"New Project"**
3. Chọn template: **"2D Core"** hoặc **"2D (URP)"**
4. Đặt tên project: **"Verrarium"**
5. Chọn location và click **"Create"**

### Bước 2: Kiểm tra Packages

1. Mở **Window → Package Manager**
2. Đảm bảo các packages sau đã được cài đặt:
   - **TextMeshPro** (Essential)
   - **Universal RP** (nếu dùng URP template)
   - **2D Sprite** (nếu chưa có)

### Bước 3: Import Scripts

Scripts đã có sẵn trong project, chỉ cần đảm bảo:
- Tất cả scripts trong `Assets/Scripts/` đã được compile
- Không có lỗi trong Console

---

## Setup Scene Chính

### Bước 1: Tạo Scene Mới

1. **File → New Scene**
2. Chọn **"2D"** template
3. Lưu scene: `Assets/Scenes/MainScene.unity`

### Bước 2: Setup Camera

1. Chọn **Main Camera**
2. **Projection**: Orthographic
3. **Size**: 10 (để nhìn thấy toàn bộ thế giới 20x20)
4. **Position**: (0, 0, -10)
5. Đảm bảo tag là **"MainCamera"**
6. **Camera Drag** (mới): thêm component `CameraDragController`
   - **Add Component → CameraDragController**
   - Drag Speed: 1
   - Clamp To World Bounds: true
   - Boundary Padding: 2

### Bước 3: Tạo SimulationSupervisor

1. **GameObject → Create Empty**
2. Đặt tên: **"SimulationSupervisor"**
3. **Add Component**: `SimulationSupervisor` script
4. Cấu hình trong Inspector:
   - **Target Population Size**: 50
   - **Max Population Size**: 100
   - **Resource Spawn Interval**: 2
   - **Plants Per Spawn**: 5
   - **World Size**: (20, 20)
   - **Use Hex Grid**: true (sẽ setup sau)
   - **Hex Grid**: (để trống, sẽ gán sau)

---

## Tạo Prefabs

### Prefab 1: Creature

#### Bước 1: Tạo GameObject

1. **GameObject → Create Empty**
2. Đặt tên: **"Creature"**

#### Bước 2: Add Components

1. **Add Component → Rigidbody2D**:
   - Body Type: **Dynamic**
   - Gravity Scale: **0**
   - Drag: **2**
   - Angular Drag: **5**

2. **Add Component → CircleCollider2D**:
   - Radius: **0.5**
   - Is Trigger: **false**

3. **Add Component → SpriteRenderer**:
   - (Sprite sẽ được tạo tự động khi chạy)

4. **Add Component → CreatureController**:
   - (Script sẽ tự tạo sprite nếu chưa có)

#### Bước 3: Lưu Prefab

1. Tạo thư mục: `Assets/Prefabs/Creatures/` (nếu chưa có)
2. Kéo GameObject vào thư mục
3. Xóa GameObject khỏi scene (prefab đã được tạo)

### Prefab 2: Plant

#### Bước 1: Tạo GameObject

1. **GameObject → Create Empty**
2. Đặt tên: **"Plant"**

#### Bước 2: Add Components

1. **Add Component → SpriteRenderer**:
   - Color: **Green** (0, 1, 0, 1)

2. **Add Component → CircleCollider2D**:
   - Radius: **0.3**
   - Is Trigger: **true**

3. **Add Component → Resource**:
   - Resource Type: **Plant**
   - Energy Value: **50**

#### Bước 3: Tạo Sprite Đơn giản

1. Tạo sprite bằng code hoặc:
   - **GameObject → 2D Object → Sprite → Circle**
   - Scale: (0.6, 0.6, 1)
   - Color: Green

#### Bước 4: Lưu Prefab

1. Tạo thư mục: `Assets/Prefabs/Resources/` (nếu chưa có)
2. Kéo GameObject vào thư mục
3. Xóa GameObject khỏi scene

### Prefab 3: Meat

Tương tự Plant nhưng:
- Color: **Red** (1, 0, 0, 1)
- Resource Type: **Meat**
- Energy Value: **30**

---

## Setup Hex Grid

### Bước 1: Tạo HexGrid

#### Cách 1: Tự động (Khuyến nghị)

1. **Menu**: `Verrarium → Create Hex Grid`
2. HexGrid sẽ được tạo tự động

#### Cách 2: Thủ công

1. **GameObject → Create Empty**
2. Đặt tên: **"HexGrid"**
3. **Add Component**: `HexGrid` script
4. Cấu hình:
   - **Grid Width**: 20
   - **Grid Height**: 20
   - **Hex Size**: 1.0
   - **Grid Offset**: (0, 0)
   - **Show Grid**: true
   - **Show Cell Colors**: true

5. Click **"Regenerate Grid"** trong Inspector

### Bước 2: Customize Hex Cells (Tùy chọn)

1. Chọn **HexGrid** GameObject
2. Trong Inspector, mở **"Cell Editor"**
3. Nhập coordinates (Q, R) của cell muốn chỉnh
4. Điều chỉnh:
   - Is Fertile
   - Fertility
   - Temperature
   - Resource Density
   - Movement Cost
   - Is Obstacle

Hoặc sử dụng **Bulk Operations**:
- **"Create Random Fertile Areas"**: Tạo 5 vùng màu mỡ ngẫu nhiên
- **"Clear All Fertile"**: Xóa tất cả fertile
- **"Reset All Cells"**: Reset về mặc định

### Bước 3: Gán HexGrid vào SimulationSupervisor

1. Chọn **SimulationSupervisor**
2. Kéo **HexGrid** GameObject vào field **"Hex Grid"**
3. Đảm bảo **"Use Hex Grid"** = true

---

## Setup UI

### Bước 1: Tạo Canvas

1. **GameObject → UI → Canvas**
2. Đặt tên: **"MainCanvas"**
3. Cấu hình:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler**: Scale With Screen Size
   - **Reference Resolution**: 1920x1080

### Bước 2: Tạo Environment Control Panel

#### Cách 1: Tự động (Khuyến nghị)

1. **Menu**: `Verrarium → Create Environment Control Panel`
2. Panel sẽ được tạo tự động ở top

#### Cách 2: Thủ công

Làm theo hướng dẫn trong `ENVIRONMENT_CONTROL_SETUP.md`

#### Gán References

1. Chọn **EnvironmentControlPanel** GameObject
2. Trong Inspector, gán tất cả references:
   - Control Panel
   - Toggle Button
   - Toggle Arrow (RectTransform)
   - Tất cả Sliders
   - Tất cả Value Texts
   - Advanced Section
   - Advanced Toggle Button

### Bước 3: Tạo Creature Inspector

#### Cách 1: Tự động (Khuyến nghị)

1. **Menu**: `Verrarium → Create Inspector UI`
2. Inspector panel và nút mũi tên ở giữa cạnh trái sẽ được tạo tự động
3. Nếu chưa có các prefab UI (Genome Row / Neuron / Connection), chạy menu `Verrarium → Create Creature Inspector Prefabs`

#### Cách 2: Thủ công

Làm theo hướng dẫn trong `INSPECTOR_UI_SETUP.md`

#### Gán References

1. Chọn **InspectorPanel** GameObject
2. Gán tất cả references vào `CreatureInspector` script

### Bước 4: Tạo Simulation UI (Tùy chọn)

1. **GameObject → UI → Canvas** (hoặc dùng MainCanvas)
2. **GameObject → UI → Text - TextMeshPro**
3. Đặt tên: **"StatsText"**
4. **Add Component**: `SimulationUI` script
5. Gán các TextMeshPro components

---

## Cấu hình và Test

### Bước 1: Gán Prefabs vào SimulationSupervisor

1. Chọn **SimulationSupervisor**
2. Kéo các prefabs vào:
   - **Creature Prefab**: `Assets/Prefabs/Creatures/Creature.prefab`
   - **Plant Prefab**: `Assets/Prefabs/Resources/Plant.prefab`
   - **Meat Prefab**: `Assets/Prefabs/Resources/Meat.prefab`

### Bước 2: Kiểm tra Cấu hình

Đảm bảo:
- ✅ SimulationSupervisor có tất cả prefabs được gán
- ✅ HexGrid đã được generate và gán vào SimulationSupervisor
- ✅ Camera có tag "MainCamera"
- ✅ Canvas và UI panels đã được setup
- ✅ Không có lỗi trong Console

### Bước 3: Chạy Test

1. **Nhấn Play**
2. Quan sát:
   - Sinh vật được spawn
   - Tài nguyên được spawn trên hex grid
   - Sinh vật di chuyển và tìm kiếm thức ăn
   - UI hoạt động (top bar, inspector)

### Bước 4: Test Các Tính năng

#### Test Environment Control Panel
1. Click mũi tên để mở/đóng panel
2. Điều chỉnh các sliders
3. Quan sát thay đổi trong giả lập

#### Test Creature Inspector
1. Click vào một sinh vật
2. Panel inspector xuất hiện bên trái
3. Chuyển đổi giữa tabs "Genome" và "Brain"
4. Xem thông tin chi tiết

#### Test Hex Grid
1. Trong Scene view, xem grid được vẽ
2. Sử dụng Cell Editor để customize cells
3. Quan sát resources spawn trong fertile cells

---

## Checklist Setup

### Scene Setup
- [ ] Scene mới được tạo và lưu
- [ ] Camera được cấu hình (Orthographic, Size = 10)
- [ ] Camera có tag "MainCamera"

### Prefabs
- [ ] Creature prefab được tạo với đầy đủ components
- [ ] Plant prefab được tạo
- [ ] Meat prefab được tạo
- [ ] Tất cả prefabs được gán vào SimulationSupervisor

### Hex Grid
- [ ] HexGrid GameObject được tạo
- [ ] Grid đã được generate
- [ ] HexGrid được gán vào SimulationSupervisor
- [ ] Use Hex Grid = true

### UI
- [ ] Canvas được tạo
- [ ] Environment Control Panel được setup
- [ ] Creature Inspector được setup
- [ ] Tất cả references được gán đúng

### SimulationSupervisor
- [ ] Tất cả prefabs được gán
- [ ] HexGrid được gán
- [ ] Settings được cấu hình đúng
- [ ] Không có lỗi trong Console

---

## Troubleshooting

### Lỗi: "Creature Prefab chưa được gán!"
**Giải pháp**: 
- Kiểm tra prefab đã được tạo chưa
- Gán prefab vào SimulationSupervisor

### Lỗi: "SimulationSupervisor not found!"
**Giải pháp**:
- Đảm bảo SimulationSupervisor GameObject có trong scene
- Đảm bảo script SimulationSupervisor được attach

### Lỗi: "HexGrid not found!"
**Giải pháp**:
- Tạo HexGrid GameObject
- Gán vào SimulationSupervisor
- Hoặc tắt "Use Hex Grid" nếu không dùng

### Sinh vật không xuất hiện
**Giải pháp**:
- Kiểm tra Creature Prefab đã được gán
- Kiểm tra Target Population Size > 0
- Kiểm tra Console có lỗi không

### UI không hiển thị
**Giải pháp**:
- Kiểm tra Canvas có trong scene
- Kiểm tra UI panels có được kích hoạt không
- Kiểm tra Canvas Scaler settings

### Hex Grid không hiển thị trong Scene view
**Giải pháp**:
- Kiểm tra "Show Grid" = true trong HexGrid
- Kiểm tra Gizmos enabled trong Scene view
- Kiểm tra Grid Width/Height > 0

### Resources không spawn
**Giải pháp**:
- Kiểm tra Plant Prefab đã được gán
- Kiểm tra có fertile cells trong hex grid không
- Kiểm tra Resource Spawn Interval > 0

### Click vào sinh vật không mở Inspector
**Giải pháp**:
- Kiểm tra Creature có Collider2D không
- Kiểm tra Camera có tag "MainCamera" không
- Kiểm tra CreatureInspector script đã được attach và references đã được gán

---

## Cấu hình Nâng cao

### Tối ưu Performance

1. **Giới hạn Quần thể**:
   - Max Population Size: 100-200 (tùy máy)
   - Target Population Size: 50-100

2. **Tối ưu Hex Grid**:
   - Grid Size: 20x20 đến 50x50 (tùy nhu cầu)
   - Chỉ hiển thị grid trong Editor, tắt trong Build

3. **Tối ưu UI**:
   - Chỉ update UI mỗi 0.1-0.5 giây thay vì mỗi frame
   - Sử dụng object pooling cho UI elements

### Customize Môi trường

1. **Tạo Vùng Sinh thái**:
   - Sử dụng HexGridEditor để tạo các vùng khác nhau
   - Set fertility, temperature cho từng vùng

2. **Tạo Đường đi**:
   - Set movement cost thấp cho các hex tạo đường
   - Sinh vật sẽ ưu tiên di chuyển qua đường

3. **Tạo Vật cản**:
   - Set isObstacle = true cho các hex
   - Sinh vật không thể đi qua

---

## Bước Tiếp theo

Sau khi setup xong:

1. **Chạy giả lập** và quan sát
2. **Điều chỉnh thông số** trong Environment Control Panel
3. **Customize hex grid** để tạo môi trường thú vị
4. **Quan sát tiến hóa** qua Creature Inspector
5. **Tối ưu hóa** dựa trên performance

---

## Tài liệu Tham khảo

- `README.md` - Tổng quan dự án
- `QUICK_START.md` - Hướng dẫn nhanh
- `HEX_GRID_SETUP.md` - Chi tiết về Hex Grid
- `ENVIRONMENT_CONTROL_SETUP.md` - Chi tiết về Control Panel
- `INSPECTOR_UI_SETUP.md` - Chi tiết về Inspector UI
- `BRAIN_AND_BEHAVIOR.md` - Giải thích về bộ não và hành vi
- `NEAT_SYSTEM.md` - Giải thích về hệ thống NEAT

---

*Chúc bạn setup thành công! Nếu gặp vấn đề, hãy kiểm tra phần Troubleshooting hoặc các tài liệu tham khảo.*

