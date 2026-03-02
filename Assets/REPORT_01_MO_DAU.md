# PHẦN 1: MỞ ĐẦU

## 1.1. Giới thiệu chung

### 1.1.1. Bối cảnh nghiên cứu

Trong những thập kỷ gần đây, sự phát triển của trí tuệ nhân tạo và tính toán tiến hóa đã mở ra những khả năng mới trong việc mô phỏng và nghiên cứu các hệ thống sinh học phức tạp. Đặc biệt, lĩnh vực **Artificial Life (Alife)** - Sự sống Nhân tạo - đã trở thành một ngành nghiên cứu liên ngành quan trọng, kết hợp giữa khoa học máy tính, sinh học, và toán học để tạo ra các hệ thống sống nhân tạo có khả năng tiến hóa, thích ứng và phát triển.

**Verrarium** là một dự án nghiên cứu và phát triển hệ thống giả lập sự sống nhân tạo thời gian thực, sử dụng thuật toán tiến hóa mạng nơ-ron **NEAT (NeuroEvolution of Augmenting Topologies)** và biến thể **rtNEAT (real-time NEAT)** để tạo ra một "vườn thú ảo" nơi các sinh vật sống, tiến hóa và tương tác với nhau trong một môi trường mô phỏng.

### 1.1.2. Mục tiêu dự án

Dự án Verrarium hướng tới ba mục tiêu chính:

1. **Nghiên cứu Artificial Life**: Tạo ra một môi trường mô phỏng nơi các sinh vật nhân tạo có thể tiến hóa thông qua chọn lọc tự nhiên, không có hàm fitness tường minh, chỉ dựa trên khả năng sống sót và sinh sản.

2. **Áp dụng Tính toán Tiến hóa**: Triển khai và tối ưu hóa thuật toán NEAT/rtNEAT để tiến hóa cả cấu trúc và trọng số của mạng nơ-ron, cho phép các sinh vật phát triển các hành vi phức tạp và thích ứng với môi trường.

3. **Tạo ra Sản phẩm Giải trí và Giáo dục**: Phát triển một ứng dụng tương tác cho phép người dùng quan sát, tương tác và học hỏi về quá trình tiến hóa, tạo ra một công cụ giáo dục và giải trí độc đáo.

### 1.1.3. Phạm vi và giới hạn

**Phạm vi nghiên cứu**:
- Triển khai hệ thống tiến hóa mạng nơ-ron NEAT/rtNEAT
- Mô phỏng vòng đời đầy đủ của sinh vật (sinh, tăng trưởng, sinh sản, chết)
- Hệ thống tài nguyên và trao đổi chất
- Cơ chế tiến hóa dựa trên chọn lọc tự nhiên

**Giới hạn**:
- Môi trường 2D (không phải 3D)
- Tập trung vào hành vi cơ bản (tìm thức ăn, sinh sản, tránh nguy hiểm)
- Chưa bao gồm các cơ chế phức tạp như săn mồi, chiến đấu, hoặc hệ thống xã hội

---

## 1.2. Nghiên cứu liên quan

### 1.2.1. Artificial Life (Sự sống Nhân tạo)

**Artificial Life** là một lĩnh vực nghiên cứu liên ngành được định nghĩa bởi Christopher Langton vào năm 1987 như là "nghiên cứu về các hệ thống nhân tạo thể hiện các đặc tính hành vi của các hệ thống sống tự nhiên" [1]. Alife tập trung vào việc tạo ra các mô hình và mô phỏng của các quá trình sinh học, từ cấp độ phân tử đến cấp độ quần thể.

**Các hướng nghiên cứu chính trong Alife**:

1. **Soft Alife**: Mô phỏng trên máy tính (simulation-based)
   - Ví dụ: Conway's Game of Life, Tierra, Avida
   - Ưu điểm: Dễ kiểm soát, có thể quan sát toàn bộ quá trình
   - Ứng dụng: Nghiên cứu quá trình tiến hóa, sinh thái học tính toán

2. **Hard Alife**: Robot và hệ thống vật lý
   - Ví dụ: Evolutionary robotics
   - Ưu điểm: Tương tác với môi trường thực
   - Ứng dụng: Robotics, autonomous systems

3. **Wet Alife**: Hệ thống sinh học tổng hợp
   - Ví dụ: Synthetic biology
   - Ưu điểm: Sử dụng vật liệu sinh học thực
   - Ứng dụng: Y học, công nghệ sinh học

**Verrarium thuộc về Soft Alife**, sử dụng mô phỏng máy tính để tạo ra một hệ sinh thái nhân tạo nơi các sinh vật có thể tiến hóa và phát triển.

**Các nghiên cứu tiêu biểu**:

- **Tierra (1991)**: Một trong những hệ thống Alife đầu tiên, nơi các chương trình máy tính "sống" và "tiến hóa" trong một môi trường ảo [2].
- **Avida (1993)**: Hệ thống tiến hóa kỹ thuật số cho phép nghiên cứu quá trình tiến hóa trong điều kiện có kiểm soát [3].
- **Polyworld (1994)**: Một thế giới ảo nơi các sinh vật với mạng nơ-ron đơn giản tiến hóa để tìm thức ăn và sinh sản [4].

### 1.2.2. Tính toán Tiến hóa (Evolutionary Computation)

**Tính toán Tiến hóa** là một nhánh của trí tuệ nhân tạo sử dụng các cơ chế tiến hóa sinh học (chọn lọc tự nhiên, đột biến, tái tổ hợp) để giải quyết các bài toán tối ưu hóa và học máy.

**Các thuật toán chính**:

1. **Genetic Algorithms (GA)**: Sử dụng chuỗi nhị phân hoặc các biểu diễn khác, áp dụng crossover và mutation [5].
2. **Genetic Programming (GP)**: Tiến hóa các chương trình máy tính (thường là cây biểu thức) [6].
3. **Evolution Strategies (ES)**: Tối ưu hóa các tham số số thực, sử dụng mutation và selection [7].
4. **NeuroEvolution**: Tiến hóa mạng nơ-ron, bao gồm cả cấu trúc và trọng số [8].

**NeuroEvolution** là lĩnh vực quan trọng nhất đối với dự án Verrarium, vì nó cho phép tiến hóa cả cấu trúc và hành vi của các sinh vật nhân tạo.

**Các phương pháp NeuroEvolution**:

- **Fixed Topology Evolution**: Chỉ tiến hóa trọng số, cấu trúc mạng cố định (ví dụ: NEAT với cấu trúc ban đầu đơn giản)
- **Topology Evolution**: Tiến hóa cả cấu trúc và trọng số (ví dụ: NEAT, HyperNEAT)
- **Real-time Evolution**: Tiến hóa trong thời gian thực, không có thế hệ rõ ràng (ví dụ: rtNEAT)

### 1.2.3. NEAT và rtNEAT

**NEAT (NeuroEvolution of Augmenting Topologies)** được phát triển bởi Kenneth O. Stanley và Risto Miikkulainen vào năm 2002 [9]. NEAT là một thuật toán tiến hóa mạng nơ-ron có khả năng:

- Tiến hóa cả cấu trúc (topology) và trọng số (weights) của mạng nơ-ron
- Bắt đầu từ cấu trúc tối thiểu (tất cả đầu vào kết nối trực tiếp với tất cả đầu ra)
- Dần dần thêm neurons và connections thông qua mutation
- Sử dụng innovation numbers để theo dõi các cấu trúc mới
- Hỗ trợ speciation để bảo vệ các đổi mới

**rtNEAT (real-time NEAT)** là một biến thể của NEAT được thiết kế cho các ứng dụng thời gian thực [10]. Khác với NEAT truyền thống (có thế hệ rõ ràng), rtNEAT:

- Tiến hóa liên tục trong thời gian thực
- Thay thế các cá thể kém hiệu quả ngay lập tức
- Không có khái niệm "thế hệ" rõ ràng
- Phù hợp cho các ứng dụng game và simulation

**Verrarium sử dụng một biến thể của rtNEAT**, kết hợp giữa tiến hóa thời gian thực và các cơ chế sinh học phức tạp (vòng đời, trao đổi chất, sinh sản).

---

## 1.3. Sản phẩm liên quan

### 1.3.1. Các sản phẩm Artificial Life

**1. Creatures (1996-2001)**:
- Một trong những game Alife đầu tiên và nổi tiếng nhất
- Sử dụng mạng nơ-ron và hệ thống gen để tạo ra các sinh vật có thể học và tiến hóa
- Người chơi chăm sóc và huấn luyện các sinh vật
- **Ảnh hưởng**: Chứng minh rằng Alife có thể tạo ra trải nghiệm giải trí thú vị

**2. Spore (2008)**:
- Game tiến hóa từ đơn bào đến vũ trụ
- Sử dụng procedural generation và evolution
- Người chơi thiết kế và tiến hóa các sinh vật
- **Ảnh hưởng**: Cho thấy tiềm năng của evolution trong game design

**3. Species: Artificial Life, Real Evolution (2014)**:
- Simulation game tập trung vào tiến hóa
- Sử dụng genetic algorithms và neural networks
- Người chơi quan sát quá trình tiến hóa tự nhiên
- **Ảnh hưởng**: Chứng minh giá trị giáo dục của Alife simulation

**4. The Bibites (2020)**:
- Một simulation Alife tương tự Verrarium
- Sử dụng neural networks và genetic algorithms
- Tập trung vào tiến hóa tự nhiên, không có can thiệp từ người chơi
- **Ảnh hưởng**: Chứng minh rằng Alife simulation có thể thu hút cộng đồng nghiên cứu và người dùng

### 1.3.2. Các công cụ nghiên cứu

**1. Avida**:
- Platform nghiên cứu tiến hóa kỹ thuật số
- Sử dụng digital organisms với genetic code
- **Khác biệt với Verrarium**: Tập trung vào nghiên cứu, không phải giải trí

**2. Polyworld**:
- Mô phỏng Alife với neural networks
- Sinh vật tiến hóa để tìm thức ăn và sinh sản
- **Khác biệt với Verrarium**: Giao diện cũ, không có tính năng save/load

**3. SharpNEAT**:
- Implementation của NEAT trong C#
- Thư viện cho phép tích hợp NEAT vào các ứng dụng
- **Khác biệt với Verrarium**: Chỉ là thư viện, không phải sản phẩm hoàn chỉnh

### 1.3.3. Điểm khác biệt của Verrarium

**So với các sản phẩm hiện có**:

1. **Tích hợp rtNEAT đầy đủ**: Sử dụng thuật toán tiến hóa mạng nơ-ron hiện đại, không chỉ genetic algorithms đơn giản
2. **Hệ thống sinh học phức tạp**: Vòng đời đầy đủ, trao đổi chất, lão hóa, starvation mechanism
3. **Tính năng nghiên cứu**: Save/load system, pause menu, autosave - hỗ trợ các thí nghiệm dài hạn
4. **Tối ưu hóa performance**: Time-slicing, spatial partitioning, DOTS integration - hỗ trợ quy mô lớn
5. **Giao diện hiện đại**: Unity 2D với UI/UX tốt, dễ sử dụng

---

## 1.4. Thông tin về dự án Verrarium

### 1.4.1. Tổng quan dự án

**Verrarium** là một hệ thống giả lập sự sống nhân tạo thời gian thực được phát triển bằng Unity 2D và C#. Dự án kết hợp ba yếu tố chính:

1. **Artificial Life**: Tạo ra một hệ sinh thái nhân tạo nơi các sinh vật sống, tương tác và tiến hóa
2. **Tính toán Tiến hóa**: Sử dụng thuật toán rtNEAT để tiến hóa mạng nơ-ron điều khiển hành vi
3. **Giải trí và Giáo dục**: Tạo ra một sản phẩm tương tác cho phép người dùng quan sát và học hỏi về tiến hóa

### 1.4.2. Yếu tố 1: Artificial Life

**Định nghĩa trong Verrarium**:
- **Sinh vật nhân tạo**: Các entity có genome (bộ gen), neural network (bộ não), và vòng đời đầy đủ
- **Môi trường**: Không gian 2D với tài nguyên (thực vật, thịt), ranh giới, và các quy luật vật lý
- **Tương tác**: Sinh vật tương tác với môi trường (tìm thức ăn), với nhau (sinh sản), và với chính bản thân (trao đổi chất, lão hóa)

**Đặc điểm của Artificial Life trong Verrarium**:

1. **Emergent Behavior**: Hành vi phức tạp xuất hiện từ các quy tắc đơn giản
   - Không có lập trình tường minh cho "tìm thức ăn"
   - Hành vi xuất hiện từ neural network và selection pressure

2. **Open-ended Evolution**: Tiến hóa không có mục tiêu cuối cùng
   - Không có hàm fitness tường minh
   - Chỉ có chọn lọc tự nhiên: sống sót và sinh sản

3. **Complexity Growth**: Độ phức tạp tăng dần theo thời gian
   - Neural networks bắt đầu đơn giản (input → output)
   - Dần dần thêm hidden neurons và connections
   - Hành vi trở nên phức tạp hơn

### 1.4.3. Yếu tố 2: Tính toán Tiến hóa

**Thuật toán được sử dụng**: rtNEAT (real-time NeuroEvolution of Augmenting Topologies)

**Đặc điểm**:

1. **Real-time Evolution**: Tiến hóa liên tục, không có thế hệ rõ ràng
   - Sinh vật chết → được thay thế ngay lập tức bởi con cái của các sinh vật sống sót
   - Không có "generation gap" - quần thể luôn thay đổi

2. **Topology Evolution**: Tiến hóa cả cấu trúc và trọng số
   - Bắt đầu từ cấu trúc tối thiểu
   - Mutation có thể thêm neurons và connections
   - Innovation numbers theo dõi các cấu trúc mới

3. **No Explicit Fitness**: Không có hàm fitness tường minh
   - Fitness = khả năng sống sót và sinh sản
   - Chọn lọc tự nhiên thuần túy

**Cơ chế tiến hóa trong Verrarium**:

- **Reproduction**: Sinh vật sinh sản khi đủ điều kiện (tuổi, năng lượng, maturity)
- **Mutation**: Genome và neural network đều bị đột biến khi sinh sản
- **Selection**: Chỉ những sinh vật sống sót và sinh sản được mới có thể truyền gen
- **Speciation**: (Chưa tích hợp đầy đủ) Phân loài để bảo vệ các đổi mới

### 1.4.4. Yếu tố 3: Giải trí và Giáo dục

**Giải trí**:
- **Quan sát**: Người dùng có thể quan sát các sinh vật tiến hóa và phát triển
- **Tương tác**: Click vào sinh vật để xem thông tin chi tiết (genome, neural network, lineage)
- **Điều khiển**: Điều chỉnh các tham số môi trường (population size, resource spawn rate, world size)
- **Lưu trữ**: Save/load simulation để tiếp tục quan sát sau này

**Giáo dục**:
- **Học về Tiến hóa**: Quan sát quá trình tiến hóa trong thời gian thực
- **Hiểu Neural Networks**: Xem cấu trúc và hoạt động của mạng nơ-ron
- **Nghiên cứu Artificial Life**: Công cụ cho các thí nghiệm và nghiên cứu
- **Visualization**: Hiển thị lineage tree, neural network structure, population statistics

### 1.4.5. Công nghệ sử dụng

**Unity 2D**:
- Game engine phổ biến, dễ sử dụng
- Hỗ trợ physics, rendering, và UI tốt
- Cross-platform (Windows, Mac, Linux, Web)

**C#**:
- Ngôn ngữ lập trình chính
- Object-oriented, dễ maintain
- Tích hợp tốt với Unity

**DOTS (Data-Oriented Technology Stack)**:
- (Partial implementation) Cho performance scaling lớn
- Burst compiler cho parallel computation
- ECS architecture cho data-oriented design

### 1.4.6. Trạng thái hiện tại

**Đã hoàn thành**:
- ✅ Hệ thống Genome với các traits có thể tiến hóa
- ✅ Neural Network NEAT đầy đủ với topology evolution
- ✅ Vòng đời đầy đủ (sinh, tăng trưởng, sinh sản, chết)
- ✅ Hệ thống tài nguyên (thực vật, thịt) với decay mechanism
- ✅ Trao đổi chất và năng lượng
- ✅ Starvation mechanism
- ✅ Mouth system (sinh vật chỉ ăn được khi thức ăn trong phạm vi miệng)
- ✅ Save/Load system với JSON serialization
- ✅ Pause menu và autosave
- ✅ Performance optimizations (time-slicing, spatial partitioning)

**Đang phát triển**:
- ⚠️ DOTS integration (partial - chưa tích hợp đầy đủ)
- ⚠️ Speciation system (có code nhưng chưa tích hợp)
- ⚠️ Epigenetics system (có code nhưng chưa tích hợp)

**Kế hoạch tương lai**:
- 🔲 Creature Library System (lưu trữ và quản lý sinh vật)
- 🔲 Advanced analytics và visualization
- 🔲 Hoàn thiện DOTS integration

---

## 1.5. Cấu trúc báo cáo

Báo cáo này được chia thành 4 phần chính:

1. **Mở đầu** (Phần này): Giới thiệu dự án, nghiên cứu liên quan, và bối cảnh
2. **Nền tảng Lý thuyết**: Trình bày về neuroevolution, NEAT, rtNEAT, và cách vườn thú ảo hoạt động
3. **Kỹ thuật và Thuật toán Tiến hóa**: Chi tiết về công nghệ, kiến trúc, và implementation
4. **Thành quả và Kết luận**: Tình hình hiện tại, thành quả đạt được, và hướng phát triển

---

## Tài liệu tham khảo

[1] Langton, C. G. (1987). Artificial life. In *Artificial life* (Vol. 1, pp. 1-47).

[2] Ray, T. S. (1991). An approach to the synthesis of life. In *Artificial life II* (Vol. 11, pp. 371-408).

[3] Ofria, C., & Wilke, C. O. (2004). Avida: A software platform for research in computational evolutionary biology. *Artificial life*, 10(2), 191-229.

[4] Yaeger, L. (1994). Computational genetics, physiology, metabolism, neural systems, learning, vision, and behavior or polyworld: Life in a new context. In *Artificial life III* (Vol. 17, pp. 263-298).

[5] Holland, J. H. (1992). *Genetic algorithms*. Scientific american, 267(1), 66-73.

[6] Koza, J. R. (1992). *Genetic programming: on the programming of computers by means of natural selection* (Vol. 1). MIT press.

[7] Bäck, T., & Schwefel, H. P. (1993). An overview of evolutionary algorithms for parameter optimization. *Evolutionary computation*, 1(1), 1-23.

[8] Stanley, K. O., et al. (2019). Designing neural networks through neuroevolution. *Nature Machine Intelligence*, 1(1), 24-35.

[9] Stanley, K. O., & Miikkulainen, R. (2002). Evolving neural networks through augmenting topologies. *Evolutionary computation*, 10(2), 99-127.

[10] Stanley, K. O., Bryant, B. D., & Miikkulainen, R. (2005). Real-time neuroevolution in the NERO video game. *IEEE transactions on evolutionary computation*, 9(6), 653-668.



