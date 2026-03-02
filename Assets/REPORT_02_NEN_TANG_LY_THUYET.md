# PHẦN 2: NỀN TẢNG LÝ THUYẾT

## 2.1. Neuroevolution (Tiến hóa Mạng Nơ-ron)

### 2.1.1. Khái niệm cơ bản

**Neuroevolution** là một nhánh của tính toán tiến hóa tập trung vào việc sử dụng các thuật toán tiến hóa để thiết kế và tối ưu hóa mạng nơ-ron nhân tạo. Khác với các phương pháp học máy truyền thống (như backpropagation), neuroevolution không cần gradient và có thể tiến hóa cả cấu trúc (topology) và trọng số (weights) của mạng nơ-ron.

**Ưu điểm của Neuroevolution**:

1. **Không cần gradient**: Phù hợp cho các bài toán không có hàm mục tiêu khả vi
2. **Tiến hóa cấu trúc**: Có thể tìm ra cấu trúc mạng tối ưu, không chỉ tối ưu trọng số
3. **Reinforcement Learning**: Phù hợp cho các bài toán reinforcement learning và adaptive behavior
4. **Open-ended**: Có thể tạo ra các giải pháp không mong đợi, sáng tạo

**Ứng dụng**:
- Game AI: Tiến hóa hành vi của NPCs
- Robotics: Tiến hóa controllers cho robots
- Artificial Life: Tiến hóa hành vi của các sinh vật nhân tạo
- Optimization: Tối ưu hóa các hàm phức tạp

### 2.1.2. Các phương pháp Neuroevolution

**1. Fixed Topology Evolution**:
- Chỉ tiến hóa trọng số, cấu trúc mạng cố định
- Ví dụ: Evolution Strategies cho neural networks
- **Hạn chế**: Không thể tìm ra cấu trúc tối ưu

**2. Topology Evolution**:
- Tiến hóa cả cấu trúc và trọng số
- Ví dụ: NEAT, HyperNEAT, CPPN
- **Ưu điểm**: Linh hoạt hơn, có thể tìm ra cấu trúc phức tạp

**3. Real-time Evolution**:
- Tiến hóa trong thời gian thực, không có thế hệ rõ ràng
- Ví dụ: rtNEAT
- **Ưu điểm**: Phù hợp cho các ứng dụng interactive

### 2.1.3. Vai trò trong Verrarium

Trong Verrarium, neuroevolution đóng vai trò trung tâm:
- **Mỗi sinh vật có một mạng nơ-ron** điều khiển hành vi
- **Mạng nơ-ron tiến hóa** qua các thế hệ
- **Không có hàm fitness tường minh** - chỉ có chọn lọc tự nhiên
- **Hành vi phức tạp xuất hiện** từ quá trình tiến hóa

---

## 2.2. Thuật toán NEAT (NeuroEvolution of Augmenting Topologies)

### 2.2.1. Tổng quan

**NEAT** được phát triển bởi Kenneth O. Stanley và Risto Miikkulainen vào năm 2002 [1]. NEAT là một thuật toán tiến hóa mạng nơ-ron có khả năng tiến hóa cả cấu trúc (topology) và trọng số (weights) của mạng nơ-ron.

**Đặc điểm chính của NEAT**:

1. **Minimal Initial Topology**: Bắt đầu từ cấu trúc tối thiểu (tất cả đầu vào kết nối trực tiếp với tất cả đầu ra)
2. **Incremental Growth**: Dần dần thêm neurons và connections thông qua mutation
3. **Innovation Numbers**: Sử dụng innovation numbers để theo dõi các cấu trúc mới
4. **Historical Marking**: Sử dụng historical marking để so sánh các mạng khác nhau
5. **Speciation**: Phân loài để bảo vệ các đổi mới khỏi bị loại bỏ sớm

### 2.2.2. Cấu trúc Mạng Nơ-ron trong NEAT

**Các loại Neurons**:

1. **Input Neurons**: Nhận dữ liệu đầu vào (sensory data)
2. **Output Neurons**: Tạo ra hành động (actions)
3. **Hidden Neurons**: Các neurons trung gian, được thêm vào qua quá trình tiến hóa

**Connections**:
- Mỗi connection có: `fromNeuronId`, `toNeuronId`, `weight`, `enabled`
- Connections có thể được thêm, xóa, hoặc disable/enable
- Mỗi connection có một **innovation number** duy nhất

**Innovation Numbers**:
- Mỗi cấu trúc mới (connection hoặc neuron) được gán một innovation number
- Innovation numbers cho phép so sánh các mạng khác nhau
- Quan trọng cho crossover operation

### 2.2.3. Các Toán tử Tiến hóa trong NEAT

**1. Mutation - Đột biến**:

**a) Weight Mutation**:
- **Change Weight**: Thay đổi trọng số một cách ngẫu nhiên
- **Flip Weight**: Đảo ngược dấu của trọng số
- **Xác suất**: Cao (0.8 cho change weight)

**b) Structure Mutation**:
- **Add Connection**: Thêm một connection mới giữa hai neurons
- **Remove Connection**: Xóa một connection hiện có
- **Add Neuron**: Thêm một hidden neuron mới (chia một connection thành hai)
- **Remove Neuron**: Xóa một hidden neuron (nếu không còn connections)
- **Xác suất**: Thấp hơn (0.3 cho add connection, 0.05 cho add neuron)

**c) Toggle Connection**:
- Enable/disable một connection
- Cho phép "tắt tạm thời" một connection mà không xóa nó

**2. Crossover - Tái tổ hợp**:

- Kết hợp hai mạng nơ-ron (cha mẹ) để tạo ra con cái
- Sử dụng innovation numbers để align các connections
- **Matching genes**: Cả hai cha mẹ đều có → chọn ngẫu nhiên
- **Disjoint genes**: Chỉ một cha mẹ có → giữ lại từ cha mẹ có fitness cao hơn
- **Excess genes**: Genes vượt quá → giữ lại từ cha mẹ có fitness cao hơn

**3. Selection - Chọn lọc**:

- Chọn các cá thể tốt nhất để sinh sản
- Trong NEAT truyền thống: Tournament selection hoặc fitness-proportionate selection
- Trong rtNEAT: Thay thế các cá thể kém hiệu quả ngay lập tức

### 2.2.4. Speciation (Phân loài)

**Mục đích**: Bảo vệ các đổi mới khỏi bị loại bỏ sớm do cạnh tranh với các mạng đã được tối ưu hóa tốt.

**Cơ chế**:
- Chia quần thể thành các **species** (loài) dựa trên similarity
- Tính **compatibility distance** giữa hai mạng:
  ```
  δ = (c₁ × E / N) + (c₂ × D / N) + (c₃ × W̄)
  ```
  - E: Số excess genes
  - D: Số disjoint genes
  - W̄: Average weight difference của matching genes
  - N: Số genes trong mạng lớn hơn
  - c₁, c₂, c₃: Hệ số điều chỉnh

- Hai mạng thuộc cùng species nếu `δ < δₜₕᵣₑₛₕₒₗd`

**Fitness Sharing**:
- Fitness được chia sẻ trong cùng một species
- Cho phép các species nhỏ có cơ hội phát triển
- Tránh một species chiếm ưu thế hoàn toàn

### 2.2.5. Tính toán Mạng Nơ-ron

**Forward Propagation**:

1. **Reset**: Đặt tất cả neuron values về 0
2. **Set Inputs**: Đặt giá trị cho input neurons
3. **Topological Sort**: Sắp xếp neurons theo thứ tự (Input → Hidden → Output)
4. **Compute**: Với mỗi neuron (không phải input):
   - Tính tổng từ các connections đến neuron này
   - Áp dụng activation function
   - Cộng bias
5. **Get Outputs**: Lấy giá trị từ output neurons

**Activation Functions**:
- **Input neurons**: Linear (f(x) = x)
- **Hidden neurons**: Sigmoid, Tanh, ReLU, hoặc Linear
- **Output neurons**: Sigmoid (để normalize về [0, 1])

---

## 2.3. rtNEAT (real-time NEAT)

### 2.3.1. Khái niệm

**rtNEAT** là một biến thể của NEAT được thiết kế cho các ứng dụng thời gian thực [2]. Khác với NEAT truyền thống (có thế hệ rõ ràng), rtNEAT tiến hóa liên tục trong thời gian thực.

**Đặc điểm chính**:

1. **No Generations**: Không có khái niệm "thế hệ" rõ ràng
2. **Continuous Replacement**: Thay thế các cá thể kém hiệu quả ngay lập tức
3. **Real-time Fitness**: Fitness được đánh giá liên tục dựa trên performance
4. **Immediate Evolution**: Đột biến và sinh sản xảy ra ngay khi có cơ hội

### 2.3.2. So sánh NEAT và rtNEAT

| **Đặc điểm** | **NEAT** | **rtNEAT** |
|--------------|----------|------------|
| **Thế hệ** | Có thế hệ rõ ràng | Không có thế hệ |
| **Fitness Evaluation** | Đánh giá sau mỗi thế hệ | Đánh giá liên tục |
| **Replacement** | Thay thế toàn bộ quần thể | Thay thế từng cá thể |
| **Phù hợp** | Offline optimization | Real-time applications |
| **Tốc độ** | Chậm hơn (batch processing) | Nhanh hơn (incremental) |

### 2.3.3. Cơ chế hoạt động của rtNEAT

**1. Continuous Evaluation**:
- Mỗi cá thể được đánh giá liên tục dựa trên performance
- Fitness = khả năng sống sót và sinh sản (trong Verrarium)

**2. Replacement Strategy**:
- Khi một cá thể chết hoặc kém hiệu quả → được thay thế ngay lập tức
- Thay thế bằng con cái của các cá thể tốt nhất hiện tại

**3. Real-time Mutation**:
- Đột biến xảy ra khi sinh sản
- Không cần chờ đến cuối thế hệ

**4. Dynamic Population**:
- Quần thể có thể thay đổi kích thước
- Tự động điều chỉnh dựa trên môi trường

---

## 2.4. Biến thể rtNEAT trong Verrarium

### 2.4.1. Tổng quan

Verrarium sử dụng một **biến thể của rtNEAT** được tùy chỉnh cho môi trường Artificial Life. Biến thể này kết hợp các đặc điểm của rtNEAT với các cơ chế sinh học phức tạp.

### 2.4.2. Đặc điểm của Biến thể Verrarium

**1. Life-based Selection**:
- **Không có hàm fitness tường minh**
- Selection dựa trên khả năng sống sót và sinh sản
- Sinh vật chết → không thể sinh sản
- Sinh vật sống sót và sinh sản → truyền gen cho thế hệ sau

**2. Reproduction-based Evolution**:
- Tiến hóa chỉ xảy ra khi sinh sản
- Mỗi lần sinh sản → đột biến genome và neural network
- Không có "generation gap" - quần thể luôn thay đổi

**3. Dual Evolution**:
- **Genome Evolution**: Tiến hóa các traits vật lý và sinh lý
- **Neural Network Evolution**: Tiến hóa cấu trúc và trọng số của mạng nơ-ron
- Hai quá trình tiến hóa độc lập nhưng tương tác với nhau

### 2.4.3. Cấu trúc Neural Network trong Verrarium

**Input Neurons (10 inputs)**:

1. **EnergyRatio** [0.0, 1.0]: Tỷ lệ năng lượng hiện tại / năng lượng tối đa
2. **Maturity** [0.0, 1.0]: Mức độ trưởng thành (0 = mới sinh, 1 = trưởng thành)
3. **HealthRatio** [0.0, 1.0]: Tỷ lệ máu hiện tại / máu tối đa
4. **Age** [0.0, 1.0]: Tuổi (chuẩn hóa về [0, 1])
5. **DistToClosestPlant** [0.0, 1.0]: Khoảng cách đến thực vật gần nhất (chuẩn hóa)
6. **AngleToClosestPlant** [-1.0, 1.0]: Góc đến thực vật gần nhất (-1 = trái, 0 = phía trước, 1 = phải)
7. **DistToClosestMeat** [0.0, 1.0]: Khoảng cách đến thịt gần nhất
8. **AngleToClosestMeat** [-1.0, 1.0]: Góc đến thịt gần nhất
9. **DistToClosestCreature** [0.0, 1.0]: Khoảng cách đến sinh vật gần nhất
10. **AngleToClosestCreature** [-1.0, 1.0]: Góc đến sinh vật gần nhất

**Output Neurons (7 outputs)**:

1. **Accelerate** [0.0, 1.0]: Lực đẩy về phía trước
2. **Rotate** [-1.0, 1.0]: Mô-men xoay (-1 = quay trái, 0 = không quay, 1 = quay phải)
3. **LayEgg** [0.0, 1.0]: Quyết định đẻ trứng (nếu > 0.5 và đủ điều kiện)
4. **Growth** [0.0, 1.0]: Quyết định tăng trưởng (nếu > 0.5 và đủ điều kiện)
5. **Heal** [0.0, 1.0]: Quyết định hồi máu (nếu > 0.5 và đủ điều kiện)
6. **Attack** [0.0, 1.0]: Quyết định tấn công (chưa implement)
7. **Eat** [0.0, 1.0]: Quyết định ăn (nếu > 0.5 và thức ăn trong phạm vi miệng)

### 2.4.4. Cơ chế Đột biến trong Verrarium

**1. Genome Mutation**:

- **Physical Traits**: size, color, speed, mouth traits
- **Metabolic Traits**: diet, health
- **Growth Traits**: growthDuration, growthEnergyThreshold
- **Reproduction Traits**: reproAgeThreshold, reproEnergyThreshold, reproCooldown
- **Sensory Traits**: visionRange
- **Behavioral Traits**: pheromoneType
- **Evolution Traits**: mutationRate (meta-evolution)

**Xác suất đột biến**: Mỗi trait có xác suất đột biến riêng (thường 0.2-0.3)

**2. Neural Network Mutation**:

Sử dụng **NEATMutator** với các toán tử:

- **Change Weight** (0.8): Thay đổi trọng số ngẫu nhiên
- **Flip Weight** (0.1): Đảo ngược dấu trọng số
- **Toggle Connection** (0.1): Enable/disable connection
- **Add Connection** (0.3): Thêm connection mới
- **Remove Connection** (0.2): Xóa connection
- **Add Neuron** (0.05): Thêm hidden neuron
- **Remove Neuron** (0.02): Xóa hidden neuron
- **Change Activation** (0.1): Thay đổi activation function

**Số lượng đột biến**: Sử dụng Poisson distribution với `λ = mutationRate` (từ genome)

### 2.4.5. Innovation Tracking

**InnovationTracker** (Singleton):
- Theo dõi tất cả các innovation numbers
- Đảm bảo mỗi cấu trúc mới có innovation number duy nhất
- Cho phép so sánh và crossover giữa các mạng

**Cơ chế**:
- Mỗi connection được xác định bởi `(fromNeuronId, toNeuronId)`
- Innovation number được gán dựa trên cặp này
- Nếu cặp đã tồn tại → sử dụng innovation number cũ
- Nếu cặp mới → tạo innovation number mới

---

## 2.5. Cách Vườn thú Ảo Hoạt động

### 2.5.1. Kiến trúc Tổng thể

Verrarium hoạt động như một **hệ sinh thái nhân tạo** với các thành phần chính:

1. **Môi trường (Environment)**: Không gian 2D với tài nguyên và ranh giới
2. **Sinh vật (Creatures)**: Các entity có genome, neural network, và vòng đời
3. **Tài nguyên (Resources)**: Thực vật và thịt cung cấp năng lượng
4. **Supervisor**: Quản lý toàn bộ simulation

### 2.5.2. Vòng đời của Sinh vật

**1. Sinh (Birth)**:
- Sinh vật được spawn từ trứng (egg)
- Nhận genome và neural network từ cha mẹ (với đột biến)
- Khởi tạo với năng lượng và máu ban đầu

**2. Tăng trưởng (Growth)**:
- Sinh vật tăng trưởng từ `maturity = 0` đến `maturity = 1`
- Tốc độ tăng trưởng phụ thuộc vào `growthDuration` (từ genome)
- Cần đủ năng lượng để tăng trưởng (`growthEnergyThreshold`)
- Khi trưởng thành → có thể sinh sản

**3. Sinh sản (Reproduction)**:
- Điều kiện:
  - `age >= reproAgeThreshold`
  - `energy >= reproEnergyThreshold`
  - `maturity >= 0.85`
  - `Time.time - lastReproduceTime >= reproCooldown`
  - Population < maxPopulationSize
- Tạo trứng (egg) với genome và neural network đã đột biến
- Trứng nở sau `incubationDuration` (60 giây)

**4. Chết (Death)**:
- Nguyên nhân:
  - `health <= 0`: Chết do sát thương (đói, lão hóa, tấn công)
  - `energy <= 0`: Chết do hết năng lượng (hiếm, vì thường chết đói trước)

### 2.5.3. Hệ thống Trao đổi Chất

**Năng lượng (Energy)**:
- **Tiêu thụ**: 
  - `baseMetabolicRate`: Tiêu thụ cơ bản mỗi frame
  - `movementEnergyCost`: Tiêu thụ khi di chuyển
  - `growthCost`: Tiêu thụ khi tăng trưởng
  - `reproductionCost`: Tiêu thụ khi sinh sản
- **Thu nhận**: 
  - Ăn thực vật hoặc thịt
  - Năng lượng thu được = `resource.EnergyValue`

**Máu (Health)**:
- **Mất máu**:
  - `agingDamageRate`: Lão hóa (khi `maturity >= 0.99`)
  - `starvationDamageRate`: Đói (khi `energy < 25% maxEnergy`)
- **Hồi máu**:
  - Có thể hồi máu nếu có đủ năng lượng (output `heal`)

### 2.5.4. Hệ thống Tài nguyên

**Thực vật (Plants)**:
- Spawn định kỳ (mỗi `resourceSpawnInterval` giây)
- Cung cấp năng lượng khi được ăn
- Tự động decay sau `resourceDecayTime` (60 giây)

**Thịt (Meat)**:
- Spawn khi sinh vật chết
- Cung cấp năng lượng (thường ít hơn thực vật)
- Cũng tự động decay

**Phân phối Tài nguyên**:
- Sử dụng **hybrid stochastic-local model**
- Có thể spawn trên hex grid (nếu enabled)
- Có thể spawn ở các "fertile areas"
- Có thể spawn toàn map (với xác suất thấp)
- Kiểm tra `minResourceDistance` để tránh clustering

### 2.5.5. Hệ thống Cảm giác và Hành động

**Cảm giác (Sensing)**:
- Mỗi frame, sinh vật thu thập thông tin từ môi trường
- Sử dụng **Spatial Hash Grid** để tìm tài nguyên và sinh vật gần nhất
- Tính toán khoảng cách và góc đến các đối tượng
- Chuẩn hóa dữ liệu về [0, 1] hoặc [-1, 1]

**Hành động (Acting)**:
- Neural network tính toán outputs từ inputs
- Áp dụng outputs:
  - **Accelerate**: Áp dụng lực đẩy
  - **Rotate**: Áp dụng mô-men xoay
  - **Eat**: Kiểm tra và ăn thức ăn (nếu trong phạm vi miệng)
  - **LayEgg**: Tạo trứng (nếu đủ điều kiện)
  - **Growth**: Tăng trưởng (nếu đủ điều kiện)
  - **Heal**: Hồi máu (nếu đủ điều kiện)

### 2.5.6. Cơ chế Tiến hóa

**1. Selection Pressure**:
- **Starvation**: Sinh vật phải tìm thức ăn thường xuyên
- **Resource Decay**: Tài nguyên biến mất, buộc sinh vật di chuyển
- **Aging**: Sinh vật chết khi già
- **Reproduction Requirements**: Phải đủ tuổi, năng lượng, và maturity

**2. Mutation**:
- Xảy ra khi sinh sản
- Genome và neural network đều bị đột biến
- Số lượng đột biến theo Poisson distribution

**3. Inheritance**:
- Con cái nhận genome và neural network từ cha mẹ
- Với đột biến, con cái có thể khác biệt đáng kể

**4. Speciation** (Chưa tích hợp đầy đủ):
- Có code nhưng chưa được sử dụng trong main loop
- Sẽ được tích hợp trong tương lai

### 2.5.7. Các Cơ chế Sinh học Đặc biệt

**1. Mouth System**:
- Sinh vật chỉ có thể ăn khi thức ăn trong phạm vi miệng
- `mouthRange`: Tầm với (scale theo size)
- `mouthAngleRange`: Góc mở (60° mỗi bên)
- `mouthAngle`: Góc của miệng (luôn 0° = phía trước)
- Tạo selection pressure cho navigation skills

**2. Starvation Mechanism**:
- Khi `energy < 25% maxEnergy` → bắt đầu nhận sát thương
- Damage tăng tuyến tính từ 0 (ở 25%) đến `starvationDamageRate` (khi energy = 0)
- Tạo selection pressure cho foraging behavior

**3. Resource Decay**:
- Tài nguyên tự động biến mất sau 60 giây
- Buộc sinh vật phải di chuyển và explore
- Tạo dynamic ecosystem

**4. Lifespan Balancing**:
- Sinh vật sống lâu (450 HP, metabolic rate thấp)
- Nhưng đẻ ít (cooldown 40s, threshold cao)
- K-selection strategy: Chất lượng > Số lượng

---

## 2.6. Tóm tắt

Verrarium sử dụng một **biến thể của rtNEAT** được tùy chỉnh cho môi trường Artificial Life:

- **Tiến hóa thời gian thực**: Không có thế hệ rõ ràng, tiến hóa liên tục
- **Life-based selection**: Selection dựa trên khả năng sống sót và sinh sản
- **Dual evolution**: Tiến hóa cả genome và neural network
- **Complex biology**: Vòng đời đầy đủ, trao đổi chất, starvation, resource decay
- **Emergent behavior**: Hành vi phức tạp xuất hiện từ quá trình tiến hóa

Hệ thống này tạo ra một môi trường nơi các sinh vật nhân tạo có thể tiến hóa và phát triển các hành vi phức tạp thông qua chọn lọc tự nhiên, không cần hàm fitness tường minh.

---

## Tài liệu tham khảo

[1] Stanley, K. O., & Miikkulainen, R. (2002). Evolving neural networks through augmenting topologies. *Evolutionary computation*, 10(2), 99-127.

[2] Stanley, K. O., Bryant, B. D., & Miikkulainen, R. (2005). Real-time neuroevolution in the NERO video game. *IEEE transactions on evolutionary computation*, 9(6), 653-668.

[3] Stanley, K. O., et al. (2019). Designing neural networks through neuroevolution. *Nature Machine Intelligence*, 1(1), 24-35.



