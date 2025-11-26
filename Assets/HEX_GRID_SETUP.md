# Hướng dẫn Setup Hex Grid

## Tổng quan

Hex Grid là hệ thống chunk-based cho bản đồ, mỗi hex cell có thể được customize với các thuộc tính môi trường khác nhau như fertility, temperature, resource density, và movement cost.

## Cấu trúc

### HexCoordinates
- Hệ thống tọa độ axial (q, r) cho hex grid
- Chuyển đổi giữa hex coordinates và world position

### HexCell
Mỗi cell có các thuộc tính:
- **isFertile**: Có thể sinh thực vật
- **fertility**: Độ màu mỡ (0-1)
- **temperature**: Nhiệt độ (0-1)
- **resourceDensity**: Mật độ tài nguyên (multiplier, 0-2)
- **movementCost**: Chi phí di chuyển (multiplier, 0.1-5)
- **isObstacle**: Có phải vật cản không

### HexGrid
- Quản lý toàn bộ hex grid
- Dictionary lưu trữ các cells
- Hỗ trợ tìm kiếm, neighbors, và các operations

## Cách Setup

### Bước 1: Tạo HexGrid GameObject

1. Tạo GameObject mới: `GameObject → Create Empty`
2. Đặt tên: "HexGrid"
3. Add Component: `HexGrid` script

### Bước 2: Cấu hình HexGrid

Trong Inspector, điều chỉnh các thông số:

**Grid Settings:**
- **Grid Width**: 20 (số hex theo chiều ngang)
- **Grid Height**: 20 (số hex theo chiều dọc)
- **Hex Size**: 1.0 (kích thước mỗi hex)
- **Grid Offset**: (0, 0) (offset của grid)

**Visualization:**
- **Show Grid**: true (hiển thị grid trong Scene view)
- **Grid Color**: Màu của grid lines
- **Show Cell Colors**: true (hiển thị màu theo thuộc tính)

### Bước 3: Generate Grid

1. Click nút **"Regenerate Grid"** trong Inspector
2. Grid sẽ được tạo và hiển thị trong Scene view

### Bước 4: Customize Cells

#### Cách 1: Sử dụng Custom Editor (Khuyến nghị)

1. Chọn HexGrid GameObject
2. Trong Inspector, mở section **"Cell Editor"**
3. Nhập tọa độ hex (Q, R) của cell muốn chỉnh sửa
4. Điều chỉnh các thuộc tính:
   - Is Fertile
   - Fertility (0-1)
   - Temperature (0-1)
   - Resource Density (0-2)
   - Movement Cost (0.1-5)
   - Is Obstacle

#### Cách 2: Sử dụng Bulk Operations

Trong Inspector có các nút:
- **"Create Random Fertile Areas"**: Tạo 5 vùng màu mỡ ngẫu nhiên
- **"Clear All Fertile"**: Xóa tất cả fertile flags
- **"Reset All Cells"**: Reset tất cả cells về giá trị mặc định

#### Cách 3: Sử dụng Code

```csharp
HexGrid hexGrid = FindObjectOfType<HexGrid>();
HexCoordinates coords = new HexCoordinates(5, 3);

// Set fertility
hexGrid.SetFertility(coords, 0.8f);

// Set temperature
hexGrid.SetTemperature(coords, 0.6f);

// Set resource density
hexGrid.SetResourceDensity(coords, 1.5f);

// Set movement cost
hexGrid.SetMovementCost(coords, 2.0f);

// Set obstacle
hexGrid.SetObstacle(coords, true);
```

### Bước 5: Tích hợp với SimulationSupervisor

1. Chọn SimulationSupervisor GameObject
2. Trong Inspector:
   - **Use Hex Grid**: true
   - **Hex Grid**: Gán HexGrid GameObject

3. SimulationSupervisor sẽ tự động:
   - Tìm HexGrid nếu chưa gán
   - Setup fertile areas trên hex grid
   - Spawn resources trong các fertile cells

## Sử dụng Hex Grid như Chunks

### Lấy Cell tại World Position

```csharp
HexGrid hexGrid = FindObjectOfType<HexGrid>();
Vector2 worldPos = new Vector2(5, 3);
HexCell cell = hexGrid.GetCellAtWorldPosition(worldPos);
```

### Lấy Neighbors

```csharp
HexCoordinates coords = new HexCoordinates(5, 3);
List<HexCell> neighbors = hexGrid.GetNeighbors(coords);
```

### Tìm Fertile Cells

```csharp
List<HexCell> fertileCells = hexGrid.GetFertileCells();
HexCell randomFertile = hexGrid.GetRandomFertileCell();
```

### Chuyển đổi Coordinates

```csharp
// World → Hex
HexCoordinates coords = hexGrid.WorldToHex(worldPosition);

// Hex → World
Vector2 worldPos = hexGrid.HexToWorld(coords);
```

## Thuộc tính Môi trường

### Fertility (Độ màu mỡ)
- **0.0**: Không thể sinh thực vật
- **0.3-0.5**: Độ màu mỡ thấp
- **0.5-0.8**: Độ màu mỡ trung bình
- **0.8-1.0**: Độ màu mỡ cao
- **isFertile**: Tự động true nếu fertility > 0.3

### Temperature (Nhiệt độ)
- **0.0**: Lạnh
- **0.5**: Bình thường
- **1.0**: Nóng
- Có thể mở rộng để ảnh hưởng đến sinh vật

### Resource Density (Mật độ Tài nguyên)
- **0.0**: Không có tài nguyên
- **1.0**: Bình thường
- **2.0**: Dồi dào
- Ảnh hưởng đến số lượng tài nguyên spawn

### Movement Cost (Chi phí Di chuyển)
- **0.1**: Di chuyển rất nhanh
- **1.0**: Bình thường
- **5.0**: Di chuyển rất chậm
- Có thể tích hợp với CreatureController để ảnh hưởng tốc độ

### Obstacle (Vật cản)
- **true**: Sinh vật không thể đi qua
- Có thể tích hợp với pathfinding

## Visualization

### Scene View
- Grid được vẽ bằng Gizmos
- Màu sắc cell dựa trên thuộc tính:
  - **Xám**: Obstacle
  - **Xanh lá**: Fertility cao
  - **Xanh dương**: Temperature cao
  - **Đỏ**: Fertility thấp

### Runtime Visualization (Tùy chọn)
Có thể tạo UI hoặc sprites để hiển thị grid trong game:
- Tạo sprite cho mỗi hex cell
- Cập nhật màu sắc dựa trên `cell.GetDisplayColor()`
- Hiển thị fertility/temperature bằng gradient

## Tối ưu hóa

### Performance
- Dictionary lookup: O(1) cho GetCell
- Neighbors: O(1) - chỉ 6 neighbors
- GetAllCells: O(n) - n là số cells

### Memory
- Mỗi cell: ~100 bytes
- Grid 20x20: ~40KB
- Grid 100x100: ~1MB

## Mở rộng

### Thêm Thuộc tính Mới

1. Thêm vào `HexCell.cs`:
```csharp
public float humidity = 0.5f; // Độ ẩm
```

2. Thêm setter vào `HexGrid.cs`:
```csharp
public void SetHumidity(HexCoordinates coordinates, float humidity)
{
    HexCell cell = GetCell(coordinates);
    if (cell != null)
    {
        cell.humidity = Mathf.Clamp01(humidity);
    }
}
```

3. Thêm vào Custom Editor nếu cần

### Tích hợp với Pathfinding

Có thể sử dụng movement cost để tính toán pathfinding:
```csharp
float GetPathCost(HexCell from, HexCell to)
{
    return (from.movementCost + to.movementCost) / 2f;
}
```

### Tích hợp với CreatureController

Có thể ảnh hưởng đến tốc độ di chuyển:
```csharp
HexCell currentCell = hexGrid.GetCellAtWorldPosition(transform.position);
if (currentCell != null)
{
    float speedMultiplier = 1f / currentCell.movementCost;
    // Áp dụng speedMultiplier
}
```

## Troubleshooting

**Grid không hiển thị:**
- Kiểm tra "Show Grid" = true
- Kiểm tra Gizmos enabled trong Scene view
- Kiểm tra Grid Width/Height > 0

**Cells không tìm thấy:**
- Đảm bảo đã gọi GenerateGrid()
- Kiểm tra coordinates có trong range không

**Resources không spawn:**
- Kiểm tra có fertile cells không
- Kiểm tra Use Hex Grid = true trong SimulationSupervisor
- Kiểm tra Hex Grid được gán đúng

## Ví dụ Use Cases

### 1. Tạo Vùng Sinh thái Khác nhau

```csharp
// Vùng rừng (fertility cao, temperature trung bình)
for (int q = -5; q < 5; q++)
{
    for (int r = -5; r < 5; r++)
    {
        hexGrid.SetFertility(new HexCoordinates(q, r), 0.9f);
        hexGrid.SetTemperature(new HexCoordinates(q, r), 0.5f);
    }
}

// Vùng sa mạc (fertility thấp, temperature cao)
for (int q = 5; q < 10; q++)
{
    for (int r = -5; r < 5; r++)
    {
        hexGrid.SetFertility(new HexCoordinates(q, r), 0.1f);
        hexGrid.SetTemperature(new HexCoordinates(q, r), 0.9f);
    }
}
```

### 2. Tạo Đường đi (Movement Cost thấp)

```csharp
// Tạo đường đi từ (0,0) đến (10,10)
HexCoordinates start = new HexCoordinates(0, 0);
HexCoordinates end = new HexCoordinates(10, 10);
// ... pathfinding logic ...
foreach (var cell in path)
{
    hexGrid.SetMovementCost(cell.Coordinates, 0.5f);
}
```

### 3. Tạo Vật cản

```csharp
// Tạo một dãy vật cản
for (int q = 0; q < 10; q++)
{
    hexGrid.SetObstacle(new HexCoordinates(q, 5), true);
}
```

---

*Hex Grid system cung cấp một nền tảng mạnh mẽ để tạo các hệ sinh thái phức tạp với các vùng môi trường khác nhau.*

