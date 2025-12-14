# DOTS Implementation - Verrarium

## Tổng quan

Thư mục này chứa implementation của Data-Oriented Technology Stack (DOTS) cho hệ thống Verrarium, bao gồm:

1. **ECS Components**: Chuyển đổi dữ liệu từ MonoBehaviour sang Entity Component System
2. **Burst Jobs**: Song song hóa tính toán Neural Network với Burst Compiler
3. **ECS Systems**: Các hệ thống xử lý song song cho Metabolism, Movement, Aging, và Brain Computation
4. **Evolution Systems**: Speciation và Epigenetics

## Cấu trúc

### Components (`Components/`)
- `GenomeComponent.cs`: Bộ gen của sinh vật (IComponentData)
- `CreatureStateComponent.cs`: Trạng thái động (Energy, Health, Maturity, Age)
- `BrainComponent.cs`: Metadata của mạng nơ-ron
- `NeuralInputComponent.cs`: Đầu vào cảm giác cho Neural Network
- `NeuralOutputComponent.cs`: Đầu ra từ Neural Network
- `SpeciesComponent.cs`: Đánh dấu loài cho Speciation
- `EpigeneticComponent.cs`: Trạng thái ngoại sinh cho Epigenetics

### Systems (`Systems/`)
- `MetabolismSystem.cs`: Xử lý trao đổi chất song song
- `MovementSystem.cs`: Xử lý di chuyển sử dụng Unity Physics 2D (DOTS)
- `AgingSystem.cs`: Xử lý lão hóa
- `BrainComputeSystem.cs`: Tính toán Neural Network với Burst Jobs

### Jobs (`Jobs/`)
- `BrainComputeJob.cs`: Burst-compiled job để tính toán mạng nơ-ron song song

### Evolution (`Evolution/`)
- `NEATNetworkNative.cs`: Wrapper để chuyển đổi NEATNetwork sang NativeArrays
- `SpeciationSystem.cs`: Hệ thống phân loài với Fitness Sharing
- `EpigeneticsSystem.cs`: Hệ thống di truyền ngoại sinh với Hebbian Learning

## Cách sử dụng

### 1. Tích hợp với hệ thống hiện tại

Hệ thống DOTS được thiết kế để chạy song song với hệ thống MonoBehaviour hiện tại. Để chuyển đổi:

1. Tạo Entity từ GameObject:
```csharp
EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
Entity entity = entityManager.CreateEntity();

// Thêm components
entityManager.AddComponentData(entity, GenomeComponent.FromGenome(genome));
entityManager.AddComponentData(entity, new CreatureStateComponent { ... });
```

2. Chuyển đổi NEATNetwork sang NativeArrays:
```csharp
var nativeNetwork = NEATNetworkNative.FromNEATNetwork(neatNetwork);
// Sử dụng nativeNetwork.neurons và nativeNetwork.connections
```

### 2. Sử dụng Speciation

```csharp
var speciationSystem = new SpeciationSystem();
int speciesId = speciationSystem.ClassifyToSpecies(network);
float adjustedFitness = speciationSystem.GetAdjustedFitness(network, speciesId, rawFitness);
```

### 3. Sử dụng Epigenetics

```csharp
var epigeneticsSystem = new EpigeneticsSystem();
epigeneticsSystem.ApplyHebbianLearning(network, inputs, outputs, learningRate, plasticity);
var experience = epigeneticsSystem.AccumulateExperience(network, inputs, outputs);
epigeneticsSystem.InheritEpigeneticState(childNetwork, experience);
```

## Lưu ý

1. **Unity Physics 2D (DOTS)**: Cần cài đặt package `com.unity.physics` và `com.unity.physics2d` để sử dụng MovementSystem.

2. **Burst Compiler**: Tất cả Jobs và Systems đều được đánh dấu `[BurstCompile]` để tối ưu hóa hiệu năng.

3. **NativeArrays**: Cần dispose các NativeArrays sau khi sử dụng để tránh memory leak.

4. **Hybrid System**: Hệ thống hiện tại vẫn sử dụng MonoBehaviour. DOTS implementation là một bước chuyển đổi dần dần.

## Tương lai

- [ ] Hoàn thiện Unity Physics 2D integration
- [ ] Tạo converter tự động từ GameObject sang Entity
- [ ] Implement Spatial Partitioning với DOTS
- [ ] Tối ưu hóa BrainComputeJob với SIMD instructions
- [ ] Tích hợp đầy đủ với SimulationSupervisor

