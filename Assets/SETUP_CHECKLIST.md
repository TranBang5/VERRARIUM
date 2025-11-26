# Setup Checklist - VERRARIUM

Checklist nhanh để đảm bảo setup đầy đủ.

## ✅ Pre-Setup

- [ ] Unity 2021.3 LTS hoặc mới hơn đã được cài đặt
- [ ] Project mới đã được tạo (2D template)
- [ ] Tất cả scripts đã được compile (không có lỗi)

## ✅ Scene Setup

- [ ] Scene mới: `MainScene.unity`
- [ ] Camera: Orthographic, Size = 10, tag = "MainCamera"
- [ ] SimulationSupervisor GameObject đã được tạo

## ✅ Prefabs

- [ ] Creature prefab:
  - [ ] Rigidbody2D (Dynamic, Gravity = 0)
  - [ ] CircleCollider2D (Radius = 0.5)
  - [ ] SpriteRenderer
  - [ ] CreatureController script
  - [ ] Đã lưu vào `Assets/Prefabs/Creatures/`

- [ ] Plant prefab:
  - [ ] SpriteRenderer (màu xanh)
  - [ ] CircleCollider2D (Trigger, Radius = 0.3)
  - [ ] Resource script (Type = Plant, Energy = 50)
  - [ ] Đã lưu vào `Assets/Prefabs/Resources/`

- [ ] Meat prefab:
  - [ ] SpriteRenderer (màu đỏ)
  - [ ] CircleCollider2D (Trigger, Radius = 0.3)
  - [ ] Resource script (Type = Meat, Energy = 30)
  - [ ] Đã lưu vào `Assets/Prefabs/Resources/`

## ✅ Hex Grid

- [ ] HexGrid GameObject đã được tạo
- [ ] HexGrid script đã được attach
- [ ] Grid đã được generate (click "Regenerate Grid")
- [ ] Grid hiển thị trong Scene view
- [ ] HexGrid được gán vào SimulationSupervisor
- [ ] Use Hex Grid = true trong SimulationSupervisor

## ✅ SimulationSupervisor

- [ ] Creature Prefab đã được gán
- [ ] Plant Prefab đã được gán
- [ ] Meat Prefab đã được gán
- [ ] HexGrid đã được gán
- [ ] Use Hex Grid = true
- [ ] Target Population Size = 50
- [ ] Max Population Size = 100
- [ ] Resource Spawn Interval = 2
- [ ] Plants Per Spawn = 5
- [ ] World Size = (20, 20)

## ✅ UI - Environment Control Panel

- [ ] Canvas đã được tạo
- [ ] EnvironmentControlPanel GameObject đã được tạo
- [ ] EnvironmentControlPanel script đã được attach
- [ ] Tất cả references đã được gán:
  - [ ] Control Panel
  - [ ] Toggle Button
  - [ ] Toggle Arrow
  - [ ] Tất cả Sliders
  - [ ] Tất cả Value Texts
  - [ ] Advanced Section
  - [ ] Advanced Toggle Button

## ✅ UI - Creature Inspector

- [ ] InspectorPanel GameObject đã được tạo
- [ ] CreatureInspector script đã được attach
- [ ] Tất cả references đã được gán:
  - [ ] Inspector Panel
  - [ ] Toggle Button
  - [ ] Close Button
  - [ ] Tab Buttons
  - [ ] Tab Contents
  - [ ] Content Parents
  - [ ] Prefabs (nếu có)

## ✅ Test

- [ ] Nhấn Play - không có lỗi trong Console
- [ ] Sinh vật được spawn
- [ ] Tài nguyên được spawn
- [ ] Sinh vật di chuyển
- [ ] Environment Control Panel hoạt động
- [ ] Creature Inspector hoạt động (click vào sinh vật)
- [ ] Hex Grid hiển thị trong Scene view

## ✅ Optional

- [ ] SimulationUI đã được setup (hiển thị thống kê)
- [ ] Hex cells đã được customize (fertile areas, etc.)
- [ ] Prefabs đã được tối ưu (sprites, materials)

---

**Khi tất cả đã được check, dự án đã sẵn sàng để chạy!**

