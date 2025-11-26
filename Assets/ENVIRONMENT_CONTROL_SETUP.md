# Hướng dẫn Setup Environment Control Panel

## Tổng quan

Environment Control Panel là một top bar cho phép người chơi điều chỉnh các thông số môi trường trong thời gian thực.

## Các Thông số Có thể Điều chỉnh

### Population (Quần thể)
- **Target Population Size**: Số lượng sinh vật mục tiêu (10-200)
- **Max Population Size**: Số lượng sinh vật tối đa (20-500)

### Resources (Tài nguyên)
- **Resource Spawn Interval**: Khoảng thời gian sinh tài nguyên (0.5-10 giây)
- **Plants Per Spawn**: Số cây sinh mỗi lần (1-20)

### World (Thế giới)
- **World Size X**: Kích thước thế giới theo trục X (10-50)
- **World Size Y**: Kích thước thế giới theo trục Y (10-50)

### Advanced (Nâng cao)
- **Base Metabolic Rate**: Tốc độ trao đổi chất cơ bản (0.1-5.0)

## Cách Setup

### Cách 1: Tự động tạo (Khuyến nghị)

1. Mở Unity Editor
2. Menu: `Verrarium → Create Environment Control Panel`
3. UI sẽ được tạo tự động ở top của màn hình
4. Gán các references vào `EnvironmentControlPanel` script:
   - Control Panel: GameObject "ControlPanel"
   - Toggle Button: GameObject "ToggleButton"
   - Toggle Arrow: Image con của ToggleButton
   - Các Sliders và Value Texts tương ứng

### Cách 2: Tạo thủ công

#### Bước 1: Tạo Top Bar Panel

1. Tạo Canvas (nếu chưa có):
   - `GameObject → UI → Canvas`
   - Đặt tên: "UICanvas"

2. Tạo Top Bar:
   - `GameObject → UI → Panel` (trong Canvas)
   - Đặt tên: "EnvironmentControlPanel"
   - Anchor: Top-Stretch
   - Height: 200
   - Position Y: 0
   - Background: Màu đen với alpha 0.9

#### Bước 2: Tạo Toggle Button

1. Tạo Button:
   - `GameObject → UI → Button` (trong EnvironmentControlPanel)
   - Đặt tên: "ToggleButton"
   - Position: Center-Top
   - Size: 40x30
   - Thêm Image con làm mũi tên (text "▼")

#### Bước 3: Tạo Control Panel

1. Tạo Panel con:
   - `GameObject → UI → Panel` (trong EnvironmentControlPanel)
   - Đặt tên: "ControlPanel"
   - Anchor: Stretch-Stretch
   - Top: 30 (để tránh toggle button)
   - Add Component: `Vertical Layout Group`

#### Bước 4: Tạo Sections

Cho mỗi section (Population, Resources, World, Advanced):

1. Tạo Section Container:
   - `GameObject` (trong ControlPanel)
   - Đặt tên: "[SectionName]Section"
   - Add Component: `Vertical Layout Group`

2. Tạo Title:
   - `GameObject → UI → Text - TextMeshPro` (trong Section)
   - Đặt tên: "Title"
   - Text: Tên section (ví dụ: "Population")
   - Font Size: 16, Bold

3. Tạo Controls:
   - Với mỗi control (ví dụ: "Target Population"):
     - Tạo GameObject với `Horizontal Layout Group`
     - Label: TextMeshPro "Label" (màu trắng)
     - Slider: `GameObject → UI → Slider`
     - Value Text: TextMeshPro "ValueText" (màu vàng)

#### Bước 5: Setup Script

1. Thêm `EnvironmentControlPanel` script vào EnvironmentControlPanel GameObject
2. Gán tất cả references:
   - Control Panel
   - Toggle Button
   - Toggle Arrow (RectTransform)
   - Tất cả Sliders
   - Tất cả Value Texts
   - Advanced Section
   - Advanced Toggle Button

## Cấu trúc UI

```
EnvironmentControlPanel (Top Bar)
├── ToggleButton (Center-Top)
│   └── Arrow (Image/Text "▼")
└── ControlPanel (Content)
    ├── PopulationSection
    │   ├── Title
    │   ├── TargetPopulationControl
    │   │   ├── Label
    │   │   ├── Slider
    │   │   └── ValueText
    │   └── MaxPopulationControl
    ├── ResourcesSection
    │   ├── Title
    │   ├── ResourceSpawnIntervalControl
    │   └── PlantsPerSpawnControl
    ├── WorldSection
    │   ├── Title
    │   ├── WorldSizeXControl
    │   └── WorldSizeYControl
    ├── AdvancedToggleButton
    └── AdvancedSection (initially hidden)
        ├── Title
        └── BaseMetabolicRateControl
```

## Tùy chỉnh

### Màu sắc
- **Panel Background**: Đen với alpha 0.9
- **Toggle Button**: Xám đậm (0.3, 0.3, 0.4)
- **Section Titles**: Trắng, Bold
- **Labels**: Trắng
- **Value Texts**: Vàng
- **Slider Fill**: Xanh dương (0.2, 0.6, 1)

### Kích thước
- Top Bar Height: 200px (khi mở)
- Toggle Button: 40x30px
- Section Title: 25px height
- Control Row: Auto height
- Label Width: 150px
- Value Text Width: 80px

## Tính năng

### Toggle Panel
- Click vào mũi tên để thu gọn/mở rộng panel
- Mũi tên xoay 180 độ khi đóng
- Panel thu gọn chỉ hiển thị toggle button

### Real-time Adjustment
- Tất cả thay đổi được áp dụng ngay lập tức
- World size thay đổi sẽ cập nhật ranh giới
- Resource spawn interval cập nhật InvokeRepeating
- Base metabolic rate áp dụng cho tất cả sinh vật hiện tại

### Advanced Section
- Click "Advanced" button để hiển thị/ẩn section nâng cao
- Chứa các thông số ít dùng hơn

## Lưu ý

1. **World Size**: Thay đổi sẽ cập nhật EdgeCollider2D của ranh giới
2. **Max Population**: Tự động điều chỉnh nếu nhỏ hơn Target Population
3. **Resource Spawn**: Cancel và restart InvokeRepeating khi thay đổi interval
4. **Base Metabolic Rate**: Áp dụng cho tất cả sinh vật hiện tại, không ảnh hưởng sinh vật mới sinh

## Troubleshooting

**Panel không hiển thị:**
- Kiểm tra Canvas có đúng render mode
- Kiểm tra Panel có đúng anchor và position

**Sliders không hoạt động:**
- Kiểm tra references đã được gán đúng
- Kiểm tra SimulationSupervisor.Instance không null

**Giá trị không cập nhật:**
- Kiểm tra các setter methods trong SimulationSupervisor
- Kiểm tra Event System có trong scene

