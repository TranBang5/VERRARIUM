# VERRARIUM - Giả lập Sự sống Nhân tạo Thời gian thực

## Tổng quan

VERRARIUM là một hệ thống giả lập sự sống nhân tạo thời gian thực sử dụng Unity 2D và thuật toán tiến hóa mạng nơ-ron. Các sinh vật trong giả lập tiến hóa thông qua chọn lọc tự nhiên thuần túy - không có hàm fitness tường minh, chỉ có sự sống và cái chết.

## Tính năng MVP

- ✅ Hệ thống Genome với các đặc điểm có thể tiến hóa
- ✅ Neural Network đơn giản (sẽ được nâng cấp lên rtNEAT)
- ✅ Vòng đời đầy đủ: sinh, tăng trưởng, sinh sản, chết
- ✅ Hệ thống tài nguyên: thực vật và thịt
- ✅ Trao đổi chất và năng lượng
- ✅ Hệ thống pheromone (cơ bản)
- ✅ 12 đầu vào cảm giác và 7 đầu ra hành động

## Cấu trúc Dự án

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── SimulationSupervisor.cs    # Quản lý toàn bộ giả lập
│   │   └── SimulationUI.cs             # UI hiển thị thống kê
│   ├── Creature/
│   │   └── CreatureController.cs        # Controller cho sinh vật
│   ├── Data/
│   │   └── Genome.cs                    # Cấu trúc bộ gen
│   ├── Resources/
│   │   └── Resource.cs                  # Tài nguyên (thực vật/thịt)
│   ├── Evolution/
│   │   └── SimpleNeuralNetwork.cs       # Mạng nơ-ron đơn giản
│   └── Utils/
│       ├── MathUtils.cs                 # Các hàm tiện ích toán học
│       └── PheromoneGrid.cs             # Hệ thống pheromone
├── Prefabs/
│   ├── Creatures/                       # Prefab sinh vật
│   └── Resources/                       # Prefab tài nguyên
└── Scenes/
    └── MainScene.unity                # Scene chính
```

## Hướng dẫn Setup

### 1. Tạo Prefabs

#### Prefab Creature:
1. Tạo GameObject mới, đặt tên "Creature"
2. Thêm component:
   - `SpriteRenderer` (sẽ tự tạo sprite nếu chưa có)
   - `Rigidbody2D`:
     - Body Type: Dynamic
     - Gravity Scale: 0 (không có trọng lực)
     - Drag: 2 (ma sát)
     - Angular Drag: 5
   - `CircleCollider2D`:
     - Radius: 0.5
     - Is Trigger: false
   - `CreatureController` script
3. Lưu vào `Assets/Prefabs/Creatures/Creature.prefab`

#### Prefab Plant:
1. Tạo GameObject mới, đặt tên "Plant"
2. Thêm component:
   - `SpriteRenderer` (màu xanh lá)
   - `CircleCollider2D`:
     - Radius: 0.3
     - Is Trigger: true
   - `Resource` script:
     - Resource Type: Plant
     - Energy Value: 50
3. Lưu vào `Assets/Prefabs/Resources/Plant.prefab`

#### Prefab Meat:
1. Tạo GameObject mới, đặt tên "Meat"
2. Thêm component:
   - `SpriteRenderer` (màu đỏ)
   - `CircleCollider2D`:
     - Radius: 0.3
     - Is Trigger: true
   - `Resource` script:
     - Resource Type: Meat
     - Energy Value: 30
3. Lưu vào `Assets/Prefabs/Resources/Meat.prefab`

### 2. Setup Scene

1. Mở scene chính (hoặc tạo mới)
2. Tạo GameObject "SimulationSupervisor":
   - Thêm `SimulationSupervisor` script
   - Gán các prefab:
     - Creature Prefab
     - Plant Prefab
     - Meat Prefab
   - Điều chỉnh các tham số:
     - Target Population Size: 50
     - Max Population Size: 100
     - Resource Spawn Interval: 2
     - Plants Per Spawn: 5
     - World Size: (20, 20)

3. Tạo GameObject "PheromoneGrid":
   - Thêm `PheromoneGrid` script
   - Điều chỉnh tham số nếu cần

4. (Tùy chọn) Tạo UI Canvas:
   - Tạo Canvas với TextMeshPro
   - Thêm `SimulationUI` script
   - Gán các Text components

5. Thiết lập Camera:
   - Orthographic Camera
   - Size: 10 (để nhìn thấy toàn bộ thế giới 20x20)

### 3. Cài đặt Physics2D

1. Edit → Project Settings → Physics 2D
2. Đảm bảo:
   - Gravity: (0, 0) - không có trọng lực
   - Default Material: có thể tạo mới với ma sát phù hợp

## Cách Chạy

1. Mở scene chính
2. Nhấn Play
3. Quan sát các sinh vật:
   - Di chuyển ngẫu nhiên ban đầu (do Neural Network chưa được huấn luyện)
   - Tìm kiếm và ăn tài nguyên
   - Tăng trưởng và sinh sản
   - Tiến hóa dần dần qua các thế hệ

## Lưu ý

- **MVP hiện tại sử dụng SimpleNeuralNetwork** - một mạng nơ-ron đơn giản với cấu trúc cố định (tất cả đầu vào → tất cả đầu ra)
- **rtNEAT đầy đủ** sẽ được tích hợp trong tương lai để cho phép tiến hóa cấu trúc mạng
- Hệ thống pheromone đã được triển khai nhưng chưa được sử dụng đầy đủ trong hành vi

## Phát triển Tiếp theo

- [ ] Tích hợp rtNEAT đầy đủ (SharpNEAT hoặc tự triển khai)
- [ ] Cải thiện hệ thống pheromone - sử dụng trong hành vi
- [ ] Thêm hệ thống tấn công và chiến đấu
- [ ] Visualization tools (hiển thị mạng nơ-ron, cây phả hệ)
- [ ] Tối ưu hóa hiệu suất (Object Pooling, DOTS)
- [ ] Thêm các yếu tố môi trường thay đổi theo thời gian

## Tài liệu Tham khảo

Xem `Assets/DESIGN_ANALYSIS.md` để biết phân tích chi tiết về thiết kế.

