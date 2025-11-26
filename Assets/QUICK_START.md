# Hướng dẫn Nhanh - VERRARIUM MVP

## Bước 1: Tạo Prefabs (5 phút)

### Creature Prefab
1. Tạo GameObject mới: `GameObject → Create Empty`
2. Đặt tên: `Creature`
3. Thêm các component:
   - `Rigidbody2D`: 
     - Body Type: Dynamic
     - Gravity Scale: 0
     - Drag: 2
   - `CircleCollider2D`: Radius = 0.5
   - `SpriteRenderer` (tự động tạo sprite khi chạy)
   - `CreatureController` script
4. Kéo vào `Assets/Prefabs/Creatures/`

### Plant Prefab
1. Tạo GameObject mới
2. Đặt tên: `Plant`
3. Thêm:
   - `CircleCollider2D` (Is Trigger = true, Radius = 0.3)
   - `SpriteRenderer` (màu xanh)
   - `Resource` script (Type = Plant, Energy = 50)
4. Kéo vào `Assets/Prefabs/Resources/`

### Meat Prefab
1. Tương tự Plant nhưng:
   - Màu đỏ
   - Type = Meat
   - Energy = 30

## Bước 2: Setup Scene (3 phút)

1. **Tạo SimulationSupervisor:**
   - `GameObject → Create Empty` → Đặt tên `SimulationSupervisor`
   - Add Component: `SimulationSupervisor`
   - Gán prefabs vào các slot tương ứng
   - Điều chỉnh:
     - Target Population: 50
     - World Size: (20, 20)

2. **Tạo PheromoneGrid:**
   - `GameObject → Create Empty` → Đặt tên `PheromoneGrid`
   - Add Component: `PheromoneGrid`

3. **Setup Camera:**
   - Camera → Projection: Orthographic
   - Size: 10

## Bước 3: Chạy! (1 phút)

1. Nhấn Play
2. Quan sát các sinh vật di chuyển và tiến hóa

## Troubleshooting

**Sinh vật không xuất hiện:**
- Kiểm tra Creature Prefab đã được gán vào SimulationSupervisor
- Kiểm tra console có lỗi không

**Sinh vật không di chuyển:**
- Kiểm tra Rigidbody2D có Gravity Scale = 0
- Kiểm tra có tài nguyên trong scene không

**Lỗi NullReference:**
- Đảm bảo SimulationSupervisor là Singleton (chỉ có 1 instance)
- Kiểm tra tất cả prefabs đã được gán

## Tips

- Bắt đầu với quần thể nhỏ (20-30) để test
- Tăng Resource Spawn Interval nếu có quá nhiều tài nguyên
- Điều chỉnh World Size phù hợp với camera

