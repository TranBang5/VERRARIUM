# Hệ thống NEAT Đầy đủ - VERRARIUM

## Tổng quan

Hệ thống NEAT (Neuroevolution of Augmenting Topologies) đã được triển khai đầy đủ theo thiết kế ban đầu. Khác với `SimpleNeuralNetwork` trước đây, hệ thống này cho phép **tiến hóa cả cấu trúc và trọng số** của mạng nơ-ron.

---

## 1. Kiến trúc

### 1.1. Các Thành phần Chính

#### Neuron (Nơ-ron)
**File:** `Assets/Scripts/Evolution/Neuron.cs`

Mỗi nơ-ron có:
- **ID duy nhất**: Để định danh trong mạng
- **Loại**: Input, Hidden, hoặc Output
- **Hàm kích hoạt**: Sigmoid, Tanh, ReLU, hoặc Linear (có thể đột biến)
- **Bias**: Giá trị bias (hiện tại = 0, có thể mở rộng)
- **Value**: Giá trị hiện tại sau khi tính toán

#### Connection (Kết nối)
**File:** `Assets/Scripts/Evolution/Connection.cs`

Mỗi kết nối có:
- **Innovation Number**: Số đổi mới toàn cục (quan trọng cho speciation)
- **From/To Neuron IDs**: Nơ-ron nguồn và đích
- **Weight**: Trọng số (có thể đột biến)
- **Enabled**: Kết nối có được kích hoạt không (có thể toggle)

#### InnovationTracker
**File:** `Assets/Scripts/Evolution/InnovationTracker.cs`

Singleton theo dõi tất cả các đổi mới:
- Đảm bảo mỗi loại đột biến cấu trúc có một innovation number duy nhất
- Cho phép so sánh và speciation trong tương lai
- Map: `(fromId, toId) -> innovationNumber`

#### NEATNetwork
**File:** `Assets/Scripts/Evolution/NEATNetwork.cs`

Mạng nơ-ron chính:
- Quản lý tất cả nơ-ron và kết nối
- Tính toán đầu ra từ đầu vào (topological sort)
- Hỗ trợ thêm/xóa nơ-ron và kết nối
- Cache để tối ưu tính toán

---

## 2. Cấu trúc Mạng

### 2.1. Khởi tạo Tối thiểu

Khi tạo mạng mới, cấu trúc ban đầu là:
```
[10 Input Neurons] → [7 Output Neurons]
```

Tất cả đầu vào được kết nối trực tiếp với tất cả đầu ra (70 kết nối).

### 2.2. Tiến hóa Cấu trúc

Mạng có thể phức tạp hóa theo thời gian:
- **Thêm nơ-ron ẩn**: Bằng cách tách một kết nối hiện có
- **Thêm kết nối**: Giữa hai nơ-ron chưa được kết nối
- **Xóa nơ-ron ẩn**: Loại bỏ nơ-ron không cần thiết
- **Xóa kết nối**: Loại bỏ kết nối không hiệu quả

### 2.3. Ví dụ Cấu trúc Tiến hóa

**Thế hệ 1:**
```
Inputs → Outputs
```

**Thế hệ 100 (sau nhiều đột biến):**
```
Inputs → [Hidden1] → [Hidden2] → Outputs
    ↓         ↓           ↓
    └─────────┴───────────┘
```

---

## 3. Các Toán tử Đột biến (Bảng 3)

**File:** `Assets/Scripts/Evolution/NEATMutator.cs`

### 3.1. Đột biến Trọng số

#### ChangeSynapseStrength
- **Mô tả**: Thay đổi trọng số của một kết nối hiện có
- **Xác suất**: 80% (phổ biến nhất)
- **Thực hiện**: `weight += Random.Range(-0.5, 0.5)`
- **Giới hạn**: Clamp về [-5, 5]

#### FlipSynapseStrength
- **Mô tả**: Đảo ngược dấu của trọng số
- **Xác suất**: 10%
- **Thực hiện**: `weight = -weight`

### 3.2. Đột biến Trạng thái Kết nối

#### ToggleSynapse
- **Mô tả**: Bật/tắt một kết nối (không xóa, chỉ vô hiệu hóa)
- **Xác suất**: 10%
- **Thực hiện**: `enabled = !enabled`
- **Lợi ích**: Kết nối vẫn được di truyền nhưng không hoạt động

### 3.3. Đột biến Cấu trúc Kết nối

#### AddNewSynapse
- **Mô tả**: Thêm kết nối mới giữa hai nơ-ron chưa được kết nối
- **Xác suất**: 30%
- **Thực hiện**: 
  - Tìm cặp nơ-ron chưa kết nối
  - Tạo innovation number mới
  - Thêm kết nối với trọng số ngẫu nhiên

#### RemoveExistingSynapse
- **Mô tả**: Xóa một kết nối hiện có
- **Xác suất**: 20%
- **Bảo vệ**: Không xóa nếu chỉ còn kết nối tối thiểu

### 3.4. Đột biến Nơ-ron

#### AddNewNeuron
- **Mô tả**: Thêm nơ-ron ẩn mới bằng cách tách một kết nối
- **Xác suất**: 5% (hiếm nhưng quan trọng)
- **Thực hiện**:
  1. Chọn kết nối ngẫu nhiên: `A → B`
  2. Vô hiệu hóa kết nối cũ
  3. Tạo nơ-ron ẩn mới: `H`
  4. Tạo hai kết nối mới:
     - `A → H` (weight = 1.0)
     - `H → B` (weight = weight cũ)

#### RemoveExistingNeuron
- **Mô tả**: Xóa nơ-ron ẩn và tất cả kết nối của nó
- **Xác suất**: 2% (rất hiếm)
- **Bảo vệ**: Không xóa nếu chỉ còn nơ-ron tối thiểu

### 3.5. Đột biến Hàm Kích hoạt

#### ChangeNeuronActivation
- **Mô tả**: Thay đổi hàm kích hoạt của nơ-ron ẩn
- **Xác suất**: 10%
- **Thực hiện**: Chọn ngẫu nhiên từ {Sigmoid, Tanh, ReLU, Linear}

---

## 4. Tính toán Mạng

### 4.1. Quy trình Tính toán

1. **Reset**: Đặt tất cả giá trị nơ-ron về 0
2. **Input**: Đặt giá trị đầu vào
3. **Topological Sort**: Sắp xếp nơ-ron theo thứ tự: Input → Hidden → Output
4. **Forward Pass**: 
   - Với mỗi nơ-ron (theo thứ tự):
     - Tính tổng từ các kết nối đến nó
     - Áp dụng hàm kích hoạt
     - Lưu giá trị
5. **Output**: Lấy giá trị từ các nơ-ron đầu ra

### 4.2. Tối ưu hóa

- **Cache kết nối**: `connectionsByToNeuron` map nhanh các kết nối đến mỗi nơ-ron
- **Topological sort**: Chỉ tính một lần, không cần DFS mỗi frame
- **Skip disabled**: Bỏ qua kết nối bị vô hiệu hóa

---

## 5. Tích hợp với Hệ thống

### 5.1. CreatureController

**Thay đổi:**
- `SimpleNeuralNetwork` → `NEATNetwork`
- `Initialize()` nhận `NEATNetwork` thay vì `SimpleNeuralNetwork`
- `Think()` sử dụng `brain.Compute(neuralInputs)` (không thay đổi)

### 5.2. SimulationSupervisor

**Thay đổi:**
- `OnCreatureReproduction()` sử dụng `NEATMutator.Mutate()` thay vì đột biến thủ công
- Số lượng đột biến dựa trên `genome.mutationRate` (Poisson distribution)

### 5.3. Tương thích Ngược

- `SimpleNeuralNetwork` vẫn tồn tại nhưng không được sử dụng
- Có thể xóa sau khi xác nhận hệ thống mới hoạt động tốt

---

## 6. Innovation Numbers

### 6.1. Mục đích

Innovation numbers đảm bảo:
- **Tính nhất quán**: Cùng một đột biến cấu trúc có cùng innovation number
- **So sánh**: Có thể so sánh hai mạng để tính độ tương đồng
- **Speciation**: Hỗ trợ phân loài trong tương lai

### 6.2. Cách hoạt động

1. Khi tạo kết nối mới: `(fromId, toId)`
2. Kiểm tra xem đã có innovation number cho cặp này chưa
3. Nếu có: Dùng lại (cùng đột biến)
4. Nếu không: Tạo innovation number mới

### 6.3. Ví dụ

```
Thế hệ 1: Input[0] → Output[0] → Innovation #1
Thế hệ 5: Input[0] → Output[0] → Innovation #1 (dùng lại)
Thế hệ 10: Input[1] → Output[0] → Innovation #2 (mới)
```

---

## 7. So sánh với SimpleNeuralNetwork

| Tính năng | SimpleNeuralNetwork | NEATNetwork |
|-----------|---------------------|-------------|
| Cấu trúc | Cố định (Input→Output) | Tiến hóa (có Hidden) |
| Đột biến | Chỉ trọng số | Cấu trúc + Trọng số |
| Innovation | Không | Có |
| Hàm kích hoạt | Cố định (Sigmoid) | Có thể đột biến |
| Toggle connection | Không | Có |
| Phức tạp hóa | Không | Có (AddNeuron) |

---

## 8. Hướng Phát triển

### 8.1. Speciation (Phân loài)

Hiện tại chưa có, nhưng có thể thêm:
- Tính độ tương đồng dựa trên innovation numbers
- Nhóm các mạng tương tự vào cùng loài
- Chia sẻ fitness trong loài

### 8.2. Crossover (Lai ghép)

Hiện tại chỉ có sinh sản vô tính, có thể thêm:
- Lai ghép hai cha mẹ
- Kết hợp innovation numbers
- Xử lý xung đột cấu trúc

### 8.3. Tối ưu hóa

- **Batch computation**: Tính nhiều mạng cùng lúc
- **GPU acceleration**: Sử dụng Compute Shaders
- **Caching**: Cache kết quả tính toán

---

## 9. Sử dụng

### 9.1. Tạo Mạng Mới

```csharp
var network = new NEATNetwork(inputCount: 10, outputCount: 7);
```

### 9.2. Sao chép và Đột biến

```csharp
var childNetwork = new NEATNetwork(parentNetwork);
NEATMutator.Mutate(childNetwork, numMutations: 5);
```

### 9.3. Tính toán

```csharp
float[] inputs = new float[10]; // ... điền giá trị
float[] outputs = network.Compute(inputs);
```

---

## 10. Lưu ý

1. **InnovationTracker là Singleton**: Tất cả mạng chia sẻ cùng tracker
2. **Topological order**: Mạng phải không có cycle (chỉ feedforward)
3. **Bảo vệ cấu trúc tối thiểu**: Không xóa quá nhiều để tránh phá vỡ mạng
4. **Hiệu suất**: Mạng phức tạp hơn sẽ chậm hơn, cần cân bằng

---

*Hệ thống NEAT này triển khai đầy đủ các yêu cầu từ tài liệu thiết kế và sẵn sàng cho việc tiến hóa phức tạp.*

