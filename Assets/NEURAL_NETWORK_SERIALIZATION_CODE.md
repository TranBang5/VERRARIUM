# CODE MÔ PHỎNG GIẢI PHÁP SERIALIZATION CỦA NEURAL NETWORKS

## TỔNG QUAN

Giải pháp Serialization của Neural Networks được triển khai qua 3 thành phần chính:
1. **Data Structures** (Save Data Classes)
2. **Serialization Logic** (NEATNetwork → Save Data)
3. **Deserialization Logic** (Save Data → NEATNetwork)

---

## 1. DATA STRUCTURES (Cấu trúc dữ liệu lưu trữ)

**File**: `Assets/Scripts/Save/SimulationSaveData.cs`

### 1.1. NEATNetworkSaveData
```csharp
/// <summary>
/// Dữ liệu lưu trữ mạng NEAT
/// </summary>
[Serializable]
public class NEATNetworkSaveData
{
    public int inputCount;
    public int outputCount;
    public List<NeuronSaveData> neurons = new List<NeuronSaveData>();
    public List<ConnectionSaveData> connections = new List<ConnectionSaveData>();
}
```

### 1.2. NeuronSaveData
```csharp
/// <summary>
/// Dữ liệu lưu trữ một nơ-ron
/// </summary>
[Serializable]
public class NeuronSaveData
{
    public int id;
    public int type; // 0 = Input, 1 = Hidden, 2 = Output
    public int activationFunction; // 0 = Sigmoid, 1 = Tanh, 2 = ReLU, 3 = Linear
    public float bias;
}
```

### 1.3. ConnectionSaveData
```csharp
/// <summary>
/// Dữ liệu lưu trữ một kết nối
/// </summary>
[Serializable]
public class ConnectionSaveData
{
    public int innovationNumber;
    public int fromNeuronId;
    public int toNeuronId;
    public float weight;
    public bool enabled;
}
```

**Vị trí**: `Assets/Scripts/Save/SimulationSaveData.cs` (dòng 84-116)

---

## 2. SERIALIZATION (NEATNetwork → Save Data)

**File**: `Assets/Scripts/Save/SimulationSaveSystem.cs`

### 2.1. Method CreateBrainSaveData()

**Vị trí**: `Assets/Scripts/Save/SimulationSaveSystem.cs` (dòng 286-328)

```csharp
/// <summary>
/// Tạo BrainSaveData từ NEATNetwork
/// </summary>
private static NEATNetworkSaveData CreateBrainSaveData(NEATNetwork brain)
{
    if (brain == null)
        return null;

    var brainData = new NEATNetworkSaveData
    {
        inputCount = brain.InputCount,
        outputCount = brain.OutputCount
    };

    // Lưu neurons
    var neurons = brain.GetNeurons();
    foreach (var neuron in neurons)
    {
        brainData.neurons.Add(new NeuronSaveData
        {
            id = neuron.id,
            type = (int)neuron.type,
            activationFunction = (int)neuron.activationFunction,
            bias = neuron.bias
        });
    }

    // Lưu connections
    var connections = brain.GetConnections();
    foreach (var connection in connections)
    {
        brainData.connections.Add(new ConnectionSaveData
        {
            innovationNumber = connection.innovationNumber,
            fromNeuronId = connection.fromNeuronId,
            toNeuronId = connection.toNeuronId,
            weight = connection.weight,
            enabled = connection.enabled
        });
    }

    return brainData;
}
```

**Cách sử dụng**:
```csharp
// Trong CreateSaveData() method
var creatureData = new CreatureSaveData
{
    genome = creature.GetGenome(),
    brain = CreateBrainSaveData(creature.GetBrain()),  // ← Gọi ở đây
    // ... other fields
};
```

**Vị trí gọi**: `Assets/Scripts/Save/SimulationSaveSystem.cs` (dòng 246)

---

## 3. DESERIALIZATION (Save Data → NEATNetwork)

### 3.1. Method CreateBrainFromSaveData() (SimulationSaveSystem)

**File**: `Assets/Scripts/Save/SimulationSaveSystem.cs`

**Vị trí**: `Assets/Scripts/Save/SimulationSaveSystem.cs` (dòng 330-374)

```csharp
/// <summary>
/// Tạo NEATNetwork từ BrainSaveData
/// </summary>
public static NEATNetwork CreateBrainFromSaveData(NEATNetworkSaveData brainData)
{
    if (brainData == null)
        return null;

    // Convert save data thành Neuron và Connection objects
    List<Neuron> neurons = new List<Neuron>();
    List<Connection> connections = new List<Connection>();

    // Tạo neurons
    foreach (var neuronData in brainData.neurons)
    {
        Neuron neuron = new Neuron(
            neuronData.id,
            (NeuronType)neuronData.type,
            (ActivationFunction)neuronData.activationFunction
        );
        neuron.bias = neuronData.bias;
        neurons.Add(neuron);
    }

    // Tạo connections
    foreach (var connData in brainData.connections)
    {
        Connection connection = new Connection(
            connData.innovationNumber,
            connData.fromNeuronId,
            connData.toNeuronId,
            connData.weight,
            connData.enabled
        );
        connections.Add(connection);
    }

    // Tạo network từ save data
    return NEATNetwork.CreateFromSaveData(
        brainData.inputCount,
        brainData.outputCount,
        neurons,
        connections
    );
}
```

**Cách sử dụng**:
```csharp
// Trong LoadGame() method
foreach (var creatureData in saveData.creatures)
{
    // Create brain from save data
    NEATNetwork brain = SimulationSaveSystem.CreateBrainFromSaveData(creatureData.brain);
    
    // Spawn creature với brain đã restore
    GameObject creatureObj = SpawnCreature(creatureData.position, creatureData.genome, brain);
}
```

**Vị trí gọi**: `Assets/Scripts/Core/SimulationSupervisor.cs` (dòng 1494)

---

### 3.2. Method CreateFromSaveData() (NEATNetwork Factory)

**File**: `Assets/Scripts/Evolution/NEATNetwork.cs`

**Vị trí**: `Assets/Scripts/Evolution/NEATNetwork.cs` (dòng 333-355)

```csharp
/// <summary>
/// Tạo NEATNetwork từ save data (dùng cho load game)
/// </summary>
public static NEATNetwork CreateFromSaveData(int inputCount, int outputCount, 
    List<Neuron> savedNeurons, List<Connection> savedConnections)
{
    NEATNetwork network = new NEATNetwork(inputCount, outputCount, true);

    // Thêm neurons từ save data
    foreach (var neuron in savedNeurons)
    {
        network.neurons.Add(new Neuron(neuron));
    }

    // Thêm connections từ save data
    foreach (var connection in savedConnections)
    {
        network.AddConnection(connection.fromNeuronId, connection.toNeuronId, 
            connection.weight, connection.innovationNumber, connection.enabled);
    }

    return network;
}
```

**Private Constructor** (dòng 322-331):
```csharp
/// <summary>
/// Constructor riêng để tạo từ save data
/// </summary>
private NEATNetwork(int inputCount, int outputCount, bool fromSaveData)
{
    this.inputCount = inputCount;
    this.outputCount = outputCount;
    this.innovationTracker = InnovationTracker.Instance;
    
    neurons = new List<Neuron>();
    connections = new List<Connection>();
    connectionsByToNeuron = new Dictionary<int, List<Connection>>();
}
```

---

## 4. FLOW DIAGRAM (Sơ đồ luồng)

### 4.1. Save Flow
```
NEATNetwork (Runtime Object)
    ↓ GetNeurons(), GetConnections()
Extract Data
    ↓ CreateBrainSaveData()
NEATNetworkSaveData (Serializable)
    ↓ JsonUtility.ToJson()
JSON String
    ↓ File.WriteAllText()
Save File (.json)
```

### 4.2. Load Flow
```
Save File (.json)
    ↓ File.ReadAllText()
JSON String
    ↓ JsonUtility.FromJson<SimulationSaveData>()
SimulationSaveData
    ↓ CreateBrainFromSaveData()
NEATNetworkSaveData
    ↓ Convert to Neuron/Connection objects
    ↓ NEATNetwork.CreateFromSaveData()
NEATNetwork (Runtime Object)
```

---

## 5. VÍ DỤ SỬ DỤNG HOÀN CHỈNH

### 5.1. Save Example
```csharp
// Trong SimulationSaveSystem.CreateSaveData()
var creatures = supervisor.GetActiveCreatures();
foreach (var creature in creatures)
{
    var creatureData = new CreatureSaveData
    {
        genome = creature.GetGenome(),
        brain = CreateBrainSaveData(creature.GetBrain()),  // ← Serialize brain
        position = creature.transform.position,
        // ... other fields
    };
    saveData.creatures.Add(creatureData);
}

// Serialize to JSON
string json = JsonUtility.ToJson(saveData, true);
File.WriteAllText(filePath, json);
```

### 5.2. Load Example
```csharp
// Trong SimulationSupervisor.LoadGame()
string json = File.ReadAllText(filePath);
SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);

foreach (var creatureData in saveData.creatures)
{
    // Deserialize brain
    NEATNetwork brain = SimulationSaveSystem.CreateBrainFromSaveData(creatureData.brain);
    
    // Spawn creature với brain đã restore
    GameObject creatureObj = SpawnCreature(
        creatureData.position, 
        creatureData.genome, 
        brain
    );
    
    // Restore state
    CreatureController creature = creatureObj.GetComponent<CreatureController>();
    creature.SetStateFromSave(/* ... */);
}
```

---

## 6. CÁC METHOD HỖ TRỢ TRONG NEATNetwork

**File**: `Assets/Scripts/Evolution/NEATNetwork.cs`

### 6.1. GetNeurons()
**Vị trí**: Dòng 305-312
```csharp
public List<Neuron> GetNeurons()
{
    return new List<Neuron>(neurons);
}
```

### 6.2. GetConnections()
**Vị trí**: Dòng 313-316
```csharp
public List<Connection> GetConnections()
{
    return new List<Connection>(connections);
}
```

**Lưu ý**: Các method này trả về **copy** của internal lists để đảm bảo data integrity.

---

## 7. ĐIỂM QUAN TRỌNG

1. **Innovation Numbers**: Được lưu và restore để đảm bảo tính nhất quán của NEAT algorithm
2. **Network Topology**: Toàn bộ cấu trúc network (neurons + connections) được lưu đầy đủ
3. **Internal Caches**: Không được lưu (như `connectionsByToNeuron`) - sẽ được rebuild khi load
4. **Activation Functions**: Được lưu dưới dạng int enum, restore về enum type khi load

---

## 8. TÓM TẮT VỊ TRÍ CODE

| Component | File | Dòng |
|-----------|------|------|
| **Data Structures** | `SimulationSaveData.cs` | 84-116 |
| **Serialize Method** | `SimulationSaveSystem.cs` | 286-328 |
| **Deserialize Method** | `SimulationSaveSystem.cs` | 330-374 |
| **Factory Method** | `NEATNetwork.cs` | 333-355 |
| **Private Constructor** | `NEATNetwork.cs` | 322-331 |
| **GetNeurons()** | `NEATNetwork.cs` | 305-312 |
| **GetConnections()** | `NEATNetwork.cs` | 313-316 |
| **Usage (Save)** | `SimulationSaveSystem.cs` | 246 |
| **Usage (Load)** | `SimulationSupervisor.cs` | 1494 |

---

## 9. TESTING

Để test serialization, có thể:

1. **Save một creature** với brain phức tạp
2. **Load lại** và so sánh:
   - Số lượng neurons
   - Số lượng connections
   - Innovation numbers
   - Weights và biases
   - Network output với cùng input

**Ví dụ test code**:
```csharp
// Test serialization
NEATNetwork original = creature.GetBrain();
NEATNetworkSaveData saveData = CreateBrainSaveData(original);
NEATNetwork restored = CreateBrainFromSaveData(saveData);

// Verify
Debug.Assert(original.GetNeurons().Count == restored.GetNeurons().Count);
Debug.Assert(original.GetConnections().Count == restored.GetConnections().Count);
```

