# Hướng dẫn Setup UI Inspector cho Sinh vật

## Tổng quan

UI Inspector cho phép click vào một sinh vật để xem thông tin chi tiết về bộ gen và cấu trúc não của nó.

## Cấu trúc UI

```
Canvas
├── InspectorToggleButton (nút mũi tên nằm giữa cạnh trái, luôn hiển thị)
└── InspectorPanel (Panel bên trái)
    ├── Header
    │   └── CloseButton (X để đóng)
    ├── Tabs
    │   ├── GenomeTabButton
    │   └── BrainTabButton
    └── Content
        ├── GenomeTabContent
        │   └── ScrollView
        │       └── Content (GenomeRows sẽ được tạo ở đây)
        └── BrainTabContent
            ├── BrainInfoText (thông tin tổng quan)
            └── ScrollView
                └── Content (Neuron và Connection displays)
```

## Bước 1: Tạo Canvas và Panel

1. **Tạo Canvas:**
   - `GameObject → UI → Canvas`
   - Đặt tên: "InspectorCanvas"
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920x1080

2. **Tạo Inspector Panel:**
   - `GameObject → UI → Panel` (trong Canvas)
   - Đặt tên: "InspectorPanel"
   - Anchor: Left-Stretch (bên trái màn hình)
   - Width: 400
   - Position X: 200 (để panel ở giữa bên trái)
   - Background: Màu đen với alpha 0.8

## Bước 2: Tạo Toggle Button & Header

1. **Toggle Button (bên ngoài Panel):**
   - `GameObject → UI → Button` (con của Canvas, *không* nằm trong InspectorPanel)
   - Đặt tên: "InspectorToggleButton"
   - Anchor: (0, 0.5) để nằm giữa cạnh trái
   - Size: 32x80, Background màu xám
   - Thêm TextMeshPro child hiển thị mũi tên "▶"

2. **Close Button (trong Header):**
   - `GameObject → UI → Button` (trong InspectorPanel)
   - Đặt tên: "CloseButton"
   - Position: Top-Right
   - Text: "X"

## Bước 3: Tạo Tabs

1. **Tab Container:**
   - `GameObject → UI → Panel` (trong InspectorPanel)
   - Đặt tên: "TabContainer"
   - Add Component: `Horizontal Layout Group`
   - Position: Dưới header

2. **Genome Tab Button:**
   - `GameObject → UI → Button` (trong TabContainer)
   - Đặt tên: "GenomeTabButton"
   - Text: "Genome"

3. **Brain Tab Button:**
   - `GameObject → UI → Button` (trong TabContainer)
   - Đặt tên: "BrainTabButton"
   - Text: "Brain"

## Bước 4: Tạo Content Areas

### Genome Tab Content

1. **Genome Tab Content:**
   - `GameObject → UI → Panel` (trong InspectorPanel)
   - Đặt tên: "GenomeTabContent"
   - Position: Dưới tabs

2. **ScrollView cho Genome:**
   - `GameObject → UI → Scroll View` (trong GenomeTabContent)
   - Đặt tên: "GenomeScrollView"
   - Content: Tạo GameObject "GenomeContent" với Vertical Layout Group

### Brain Tab Content

1. **Brain Tab Content:**
   - `GameObject → UI → Panel` (trong InspectorPanel)
   - Đặt tên: "BrainTabContent"
   - Position: Dưới tabs
   - Ban đầu: SetActive(false)

2. **Brain Info Text:**
   - `GameObject → UI → Text - TextMeshPro` (trong BrainTabContent)
   - Đặt tên: "BrainInfoText"
   - Hiển thị: "Neurons: X, Connections: Y"

3. **ScrollView cho Brain:**
   - `GameObject → UI → Scroll View` (trong BrainTabContent)
   - Đặt tên: "BrainScrollView"
   - Content: Tạo GameObject "BrainContent" với Vertical Layout Group

## Bước 5: Tạo Prefabs (Tùy chọn)

### Genome Row Prefab

1. Tạo GameObject "GenomeRowPrefab":
   - Add Component: `Horizontal Layout Group`
   - Child 1: TextMeshPro "Label" (màu trắng)
   - Child 2: TextMeshPro "Value" (màu vàng)
   - Lưu vào `Assets/Prefabs/UI/GenomeRowPrefab.prefab`

### Neuron Display Prefab

1. Tạo GameObject "NeuronDisplayPrefab":
   - Add Component: `Image` (background)
   - Child: TextMeshPro "Text"
   - Lưu vào `Assets/Prefabs/UI/NeuronDisplayPrefab.prefab`

### Connection Display Prefab

1. Tạo GameObject "ConnectionDisplayPrefab":
   - Add Component: `Image` (background)
   - Child: TextMeshPro "Text"
   - Lưu vào `Assets/Prefabs/UI/ConnectionDisplayPrefab.prefab`

## Bước 6: Setup Script

1. **Thêm CreatureInspector Script:**
   - Chọn InspectorPanel
   - Add Component: `CreatureInspector`

2. **Gán References:**
   - Inspector Panel: InspectorPanel (chính nó)
   - Toggle Button: ToggleButton
   - Toggle Arrow: Image con của ToggleButton
   - Close Button: CloseButton
   - Genome Tab Button: GenomeTabButton
   - Brain Tab Button: BrainTabButton
   - Genome Tab Content: GenomeTabContent
   - Brain Tab Content: BrainTabContent
   - Genome Content Parent: GenomeContent (trong ScrollView)
   - Brain Info Text: BrainInfoText
   - Brain Content Parent: BrainContent (trong ScrollView)
   - Genome Row Prefab: GenomeRowPrefab
   - Neuron Display Prefab: NeuronDisplayPrefab
   - Connection Display Prefab: ConnectionDisplayPrefab

## Bước 7: Setup Camera

Đảm bảo Camera có tag "MainCamera" để script có thể tìm thấy.

## Bước 8: Test

1. Chạy game
2. Click vào một sinh vật
3. Panel sẽ xuất hiện bên trái
4. Thử chuyển đổi giữa các tab
5. Thử toggle và close

## Tùy chỉnh

### Màu sắc

- **Panel Background**: Đen với alpha 0.8
- **Input Neurons**: Xanh nhạt (0.5, 0.8, 1, 0.3)
- **Hidden Neurons**: Vàng nhạt (1, 0.8, 0.5, 0.3)
- **Output Neurons**: Tím nhạt (0.8, 0.5, 1, 0.3)
- **Positive Connections**: Xanh lá (0.5, 1, 0.5, 0.2)
- **Negative Connections**: Đỏ (1, 0.5, 0.5, 0.2)
- **Disabled Connections**: Xám (0.5, 0.5, 0.5, 0.2)

### Kích thước

- Panel Width: 400px
- Font Size Labels: 14
- Font Size Values: 14
- Font Size Neuron/Connection: 12/11

## Troubleshooting

**Panel không xuất hiện khi click:**
- Kiểm tra Camera có tag "MainCamera"
- Kiểm tra Creature có Collider2D
- Kiểm tra Layer của UI và Creature

**Prefabs không hiển thị:**
- Đảm bảo prefabs được gán vào script
- Kiểm tra prefabs có đúng structure

**ScrollView không scroll:**
- Kiểm tra Content có ContentSizeFitter
- Kiểm tra ScrollView có đúng size

