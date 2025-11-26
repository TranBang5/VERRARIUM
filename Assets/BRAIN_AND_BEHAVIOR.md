# Bộ Não và Xử Lý Hành Động của Sinh Vật

## Tổng quan

Mỗi sinh vật trong VERRARIUM được điều khiển bởi một **mạng nơ-ron** (Neural Network) đơn giản. Bộ não này nhận thông tin từ môi trường thông qua các **đầu vào cảm giác**, xử lý thông tin, và đưa ra các **quyết định hành động** thông qua các đầu ra.

---

## 1. Kiến trúc Bộ Não

### 1.1. SimpleNeuralNetwork

**File:** `Assets/Scripts/Evolution/SimpleNeuralNetwork.cs`

Bộ não hiện tại là một mạng nơ-ron **feedforward** (truyền thẳng) đơn giản với cấu trúc:

```
[10 Inputs] → [Weight Matrix] → [7 Outputs]
```

**Đặc điểm:**
- **Cấu trúc cố định**: Tất cả 10 đầu vào được kết nối trực tiếp với tất cả 7 đầu ra
- **Không có lớp ẩn**: Đây là mạng tối thiểu, sẽ được nâng cấp lên rtNEAT để có cấu trúc tiến hóa
- **Hàm kích hoạt**: Sigmoid (đưa giá trị về phạm vi [0, 1])
- **Trọng số**: Mỗi kết nối có một trọng số (weight) có thể đột biến

**Công thức tính toán:**
```
output[i] = Sigmoid(Σ(input[j] * weight[i,j]))
```

### 1.2. Vòng đời của Bộ Não

1. **Khởi tạo**: Khi sinh vật được tạo, bộ não được khởi tạo với trọng số ngẫu nhiên
2. **Sao chép**: Khi sinh sản, bộ não của cha mẹ được sao chép sang con
3. **Đột biến**: Con nhận các đột biến ngẫu nhiên trên trọng số
4. **Tiến hóa**: Các bộ não giúp sinh vật sống sót và sinh sản sẽ được truyền lại

---

## 2. Đầu Vào Cảm Giác (10 Inputs)

**File xử lý:** `CreatureController.Sense()` trong `Assets/Scripts/Creature/CreatureController.cs`

Các đầu vào được tính toán mỗi frame trong `FixedUpdate()`:

### Input 0: EnergyRatio [0.0, 1.0]
- **Mô tả**: Tỷ lệ năng lượng hiện tại / năng lượng tối đa
- **Tính toán**: `energy / maxEnergy`
- **Ý nghĩa**: Sinh vật biết mình còn bao nhiêu năng lượng

### Input 1: Maturity [0.0, 1.0]
- **Mô tả**: Giai đoạn tăng trưởng (0 = mới sinh, 1 = trưởng thành)
- **Tính toán**: Biến `maturity` được cập nhật khi tăng trưởng
- **Ý nghĩa**: Sinh vật biết mình đã lớn đến mức nào

### Input 2: HealthRatio [0.0, 1.0]
- **Mô tả**: Tỷ lệ máu hiện tại / máu tối đa
- **Tính toán**: `currentHealth / genome.health`
- **Ý nghĩa**: Sinh vật biết mình còn bao nhiêu máu

### Input 3: Age [0.0, 1.0]
- **Mô tả**: Tuổi của sinh vật (chuẩn hóa)
- **Tính toán**: `Mathf.Clamp01(age / 100f)`
- **Ý nghĩa**: Sinh vật biết mình đã sống bao lâu

### Input 4: DistToClosestPlant [0.0, 1.0]
- **Mô tả**: Khoảng cách chuẩn hóa đến thực vật gần nhất
- **Tính toán**: `distance / visionRange` (1 = ở rìa tầm nhìn, 0 = chạm vào)
- **Ý nghĩa**: Sinh vật biết thực vật ở đâu và xa bao nhiêu

### Input 5: AngleToClosestPlant [-1, 1]
- **Mô tả**: Góc đến thực vật gần nhất
- **Tính toán**: `Vector2.SignedAngle(forward, direction) / 180f`
- **Ý nghĩa**: Sinh vật biết phải quay hướng nào để đến thực vật
- **Giá trị**: -1 = bên trái, 0 = phía trước, 1 = bên phải

### Input 6: DistToClosestMeat [0.0, 1.0]
- **Mô tả**: Khoảng cách chuẩn hóa đến thịt gần nhất
- **Tính toán**: Tương tự Input 4
- **Ý nghĩa**: Sinh vật biết thịt ở đâu

### Input 7: AngleToClosestMeat [-1, 1]
- **Mô tả**: Góc đến thịt gần nhất
- **Tính toán**: Tương tự Input 5
- **Ý nghĩa**: Sinh vật biết phải quay hướng nào để đến thịt

### Input 8: DistToClosestCreature [0.0, 1.0]
- **Mô tả**: Khoảng cách chuẩn hóa đến sinh vật khác gần nhất
- **Tính toán**: Tương tự Input 4
- **Ý nghĩa**: Sinh vật biết có sinh vật khác ở gần không

### Input 9: AngleToClosestCreature [-1, 1]
- **Mô tả**: Góc đến sinh vật khác gần nhất
- **Tính toán**: Tương tự Input 5
- **Ý nghĩa**: Sinh vật biết hướng của sinh vật khác

**Lưu ý:** Pheromone inputs (Input 10, 11) đã được tắt tạm thời.

---

## 3. Đầu Ra Hành Động (7 Outputs)

**File xử lý:** `CreatureController.Think()` và `CreatureController.Act()` trong `Assets/Scripts/Creature/CreatureController.cs`

Các đầu ra được tính toán từ mạng nơ-ron và được chuyển đổi thành hành động:

### Output 0: Accelerate [0.0, 1.0]
- **Mô tả**: Lực đẩy về phía trước
- **Xử lý**: 
  ```csharp
  if (accelerateOutput > 0.1f) {
      Vector2 force = transform.up * accelerateOutput * genome.speed * 10f;
      rb.AddForce(force);
      energy -= movementEnergyCost * Time.fixedDeltaTime;
  }
  ```
- **Thành phần điều khiển**: `CreatureController.Act()` → `Rigidbody2D.AddForce()`
- **Chi phí**: Tiêu thụ năng lượng khi di chuyển

### Output 1: Rotate [-1, 1]
- **Mô tả**: Lực xoay (torque)
- **Chuyển đổi**: Từ [0,1] sang [-1,1]: `(outputs[1] - 0.5f) * 2f`
- **Xử lý**:
  ```csharp
  if (Mathf.Abs(rotateOutput) > 0.1f) {
      float torque = rotateOutput * 50f;
      rb.AddTorque(torque);
      energy -= movementEnergyCost * 0.5f * Time.fixedDeltaTime;
  }
  ```
- **Thành phần điều khiển**: `CreatureController.Act()` → `Rigidbody2D.AddTorque()`
- **Chi phí**: Tiêu thụ năng lượng (ít hơn di chuyển)

### Output 2: LayEgg [0.0, 1.0]
- **Mô tả**: Quyết định đẻ trứng
- **Xử lý**:
  ```csharp
  if (layEggOutput > 0.5f) {
      TryReproduce();
  }
  ```
- **Điều kiện kiểm tra trong `TryReproduce()`**:
  - `age >= genome.reproAgeThreshold`
  - `energy >= genome.reproEnergyThreshold`
  - `maturity >= 0.8f`
- **Thành phần điều khiển**: `CreatureController.TryReproduce()` → `SimulationSupervisor.OnCreatureReproduction()`
- **Kết quả**: Tạo sinh vật mới với bộ gen và bộ não đột biến

### Output 3: Growth [0.0, 1.0]
- **Mô tả**: Quyết định tăng trưởng
- **Xử lý**:
  ```csharp
  if (growthOutput > 0.5f && maturity < 1f) {
      Grow();
  }
  ```
- **Điều kiện**: `energy >= genome.growthEnergyThreshold`
- **Thành phần điều khiển**: `CreatureController.Grow()`
- **Hiệu ứng**: 
  - Tăng `maturity` theo thời gian
  - Tăng kích thước (`size`)
  - Tiêu thụ năng lượng

### Output 4: Heal [0.0, 1.0]
- **Mô tả**: Quyết định hồi máu
- **Xử lý**:
  ```csharp
  if (healOutput > 0.5f) {
      Heal();
  }
  ```
- **Thành phần điều khiển**: `CreatureController.Heal()`
- **Hiệu ứng**:
  - Phục hồi máu: `currentHealth += 5f * Time.fixedDeltaTime`
  - Tiêu thụ năng lượng: `energy -= 2f * Time.fixedDeltaTime`

### Output 5: Attack [0.0, 1.0]
- **Mô tả**: Quyết định tấn công (chưa được triển khai đầy đủ)
- **Xử lý**: Hiện tại chưa có logic tấn công
- **Dự kiến**: Sẽ gây sát thương cho sinh vật khác trong tầm

### Output 6: Eat [0.0, 1.0]
- **Mô tả**: Quyết định ăn tài nguyên
- **Xử lý**:
  ```csharp
  if (eatOutput > 0.5f && nearbyResource != null) {
      TryEat();
  }
  ```
- **Thành phần điều khiển**: `CreatureController.TryEat()`
- **Logic**:
  1. Chọn tài nguyên dựa trên `genome.diet`:
     - `diet < 0.3`: Chỉ ăn thực vật
     - `diet > 0.7`: Chỉ ăn thịt
     - `0.3 <= diet <= 0.7`: Ăn tạp (chọn cái gần nhất)
  2. Kiểm tra khoảng cách (< 1.5f)
  3. Tiêu thụ tài nguyên và nhận năng lượng

---

## 4. Quy trình Sense-Think-Act

**File:** `CreatureController.FixedUpdate()` trong `Assets/Scripts/Creature/CreatureController.cs`

Mỗi frame vật lý (FixedUpdate), sinh vật thực hiện 3 bước:

### Bước 1: Sense (Cảm nhận)
```csharp
Sense(); // Thu thập 10 đầu vào cảm giác
```
- Tìm các đối tượng gần nhất (thực vật, thịt, sinh vật khác)
- Tính toán khoảng cách và góc
- Đọc trạng thái nội bộ (năng lượng, máu, tuổi, v.v.)
- Lưu vào mảng `neuralInputs[10]`

**Thành phần tham gia:**
- `SimulationSupervisor.FindClosestResource()` - Tìm tài nguyên
- `SimulationSupervisor.FindClosestCreature()` - Tìm sinh vật
- `MathUtils.NormalizeDistance()` - Chuẩn hóa khoảng cách
- `MathUtils.AngleToTarget()` - Tính góc

### Bước 2: Think (Suy nghĩ)
```csharp
Think(); // Kích hoạt Neural Network
```
- Đưa `neuralInputs` vào mạng nơ-ron
- Mạng tính toán: `outputs = brain.Compute(neuralInputs)`
- Chuyển đổi đầu ra thành các biến hành động:
  - `accelerateOutput`, `rotateOutput`, `layEggOutput`, v.v.

**Thành phần tham gia:**
- `SimpleNeuralNetwork.Compute()` - Tính toán mạng nơ-ron

### Bước 3: Act (Hành động)
```csharp
Act(); // Thực thi hành động
```
- Kiểm tra từng đầu ra và thực thi hành động tương ứng
- Áp dụng lực vật lý (di chuyển, xoay)
- Thực hiện hành động rời rạc (ăn, sinh sản, tăng trưởng, hồi máu)
- Cập nhật trạng thái (năng lượng, máu, maturity)

**Thành phần tham gia:**
- `Rigidbody2D.AddForce()` - Di chuyển
- `Rigidbody2D.AddTorque()` - Xoay
- `CreatureController.TryEat()` - Ăn
- `CreatureController.TryReproduce()` - Sinh sản
- `CreatureController.Grow()` - Tăng trưởng
- `CreatureController.Heal()` - Hồi máu

---

## 5. Các Thành Phần Điều Khiển

### 5.1. CreatureController (Thành phần chính)
**File:** `Assets/Scripts/Creature/CreatureController.cs`

**Vai trò:**
- Quản lý toàn bộ vòng đời và hành vi của sinh vật
- Điều phối quy trình Sense-Think-Act
- Xử lý tất cả các hành động

**Các phương thức quan trọng:**
- `FixedUpdate()` - Vòng lặp chính
- `Sense()` - Thu thập cảm giác
- `Think()` - Tính toán Neural Network
- `Act()` - Thực thi hành động
- `TryEat()`, `TryReproduce()`, `Grow()`, `Heal()` - Các hành động cụ thể

### 5.2. SimpleNeuralNetwork (Bộ não)
**File:** `Assets/Scripts/Evolution/SimpleNeuralNetwork.cs`

**Vai trò:**
- Lưu trữ cấu trúc và trọng số của mạng nơ-ron
- Tính toán đầu ra từ đầu vào
- Hỗ trợ sao chép và đột biến

**Các phương thức quan trọng:**
- `Compute(float[] inputs)` - Tính toán đầu ra
- `ChangeRandomWeight()` - Đột biến trọng số
- `FlipRandomWeight()` - Đảo ngược trọng số

### 5.3. SimulationSupervisor (Quản lý môi trường)
**File:** `Assets/Scripts/Core/SimulationSupervisor.cs`

**Vai trò:**
- Cung cấp thông tin môi trường cho Sense()
- Quản lý tài nguyên và sinh vật
- Xử lý sinh sản và cái chết

**Các phương thức quan trọng:**
- `FindClosestResource()` - Tìm tài nguyên gần nhất
- `FindClosestCreature()` - Tìm sinh vật gần nhất
- `OnCreatureReproduction()` - Xử lý sinh sản

### 5.4. MathUtils (Tiện ích toán học)
**File:** `Assets/Scripts/Utils/MathUtils.cs`

**Vai trò:**
- Cung cấp các hàm toán học cho Sense()
- Chuẩn hóa giá trị về phạm vi phù hợp

**Các hàm quan trọng:**
- `NormalizeDistance()` - Chuẩn hóa khoảng cách
- `AngleToTarget()` - Tính góc đến mục tiêu

### 5.5. Rigidbody2D (Vật lý)
**Component Unity**

**Vai trò:**
- Thực hiện di chuyển và xoay thông qua lực vật lý
- Đảm bảo hành vi vật lý thực tế

**Các phương thức được sử dụng:**
- `AddForce()` - Áp dụng lực đẩy
- `AddTorque()` - Áp dụng mô-men xoắn

---

## 6. Luồng Dữ Liệu Tổng Quan

```
┌─────────────────────────────────────────────────────────┐
│                    FixedUpdate()                        │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │        1. Sense()                │
        │  - Tìm đối tượng gần nhất        │
        │  - Tính khoảng cách/góc          │
        │  - Đọc trạng thái nội bộ         │
        │  → neuralInputs[10]              │
        └─────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │        2. Think()                │
        │  - brain.Compute(inputs)         │
        │  - Tính toán trọng số            │
        │  - Sigmoid activation            │
        │  → outputs[7]                    │
        └─────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │        3. Act()                  │
        │  - Di chuyển (AddForce)          │
        │  - Xoay (AddTorque)              │
        │  - Ăn (TryEat)                   │
        │  - Sinh sản (TryReproduce)       │
        │  - Tăng trưởng (Grow)            │
        │  - Hồi máu (Heal)                │
        └─────────────────────────────────┘
```

---

## 7. Điểm Quan Trọng

### 7.1. Tại sao dùng FixedUpdate?
- Đảm bảo tính toán vật lý nhất quán
- Độc lập với tốc độ khung hình
- Phù hợp với `Rigidbody2D`

### 7.2. Tại sao có ngưỡng (threshold)?
- Các hành động rời rạc (Eat, LayEgg, Growth) cần ngưỡng (> 0.5) để tránh kích hoạt liên tục
- Các hành động liên tục (Accelerate, Rotate) có ngưỡng thấp (> 0.1) để tránh nhiễu

### 7.3. Tại sao chuyển đổi Rotate từ [0,1] sang [-1,1]?
- Mạng nơ-ron chỉ xuất [0,1] (do Sigmoid)
- Xoay cần cả hướng trái (-1) và phải (1)
- Công thức: `(output - 0.5) * 2` chuyển [0,1] → [-1,1]

### 7.4. Tại sao có cooldown cho Eat?
- Tránh ăn quá nhanh trong một frame
- Tạo cảm giác thực tế hơn

---

## 8. Hạn chế Hiện tại và Hướng Phát triển

### Hạn chế:
- **Cấu trúc cố định**: Không có lớp ẩn, không thể tiến hóa cấu trúc
- **Đột biến đơn giản**: Chỉ đột biến trọng số, không có đột biến cấu trúc
- **Không có speciation**: Tất cả sinh vật cạnh tranh trực tiếp

### Hướng phát triển (rtNEAT):
- **Tiến hóa cấu trúc**: Thêm/xóa nơ-ron và kết nối
- **Speciation**: Bảo vệ các đổi mới mới
- **Innovation numbers**: Theo dõi lịch sử tiến hóa
- **Cấu trúc phức tạp hơn**: Nhiều lớp ẩn, kết nối không đầy đủ

---

*Tài liệu này mô tả hệ thống hiện tại của MVP. Khi tích hợp rtNEAT đầy đủ, một số phần sẽ thay đổi.*

