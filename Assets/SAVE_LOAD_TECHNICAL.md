# KỸ THUẬT SAVE/LOAD SYSTEM - CHI TIẾT KỸ THUẬT

## TỔNG QUAN

Hệ thống Save/Load của Verrarium sử dụng **JSON serialization** để lưu trữ toàn bộ trạng thái simulation. Hệ thống được thiết kế để:
- Lưu trữ đầy đủ trạng thái simulation (creatures, resources, settings)
- Hỗ trợ tối đa 20 save slots
- Tự động lưu (autosave) mỗi 10 phút
- Dễ dàng debug và chỉnh sửa (JSON format)

---

## 1. KIẾN TRÚC DỮ LIỆU (DATA ARCHITECTURE)

### 1.1. Hierarchical Data Structure

Hệ thống sử dụng cấu trúc dữ liệu phân cấp để tổ chức thông tin:

```
SimulationSaveData (Root)
├── Metadata (version, saveName, saveTime, simulationTime)
├── Statistics (totalBorn, totalDied, currentPopulation)
├── World Settings (worldSize, enableWorldBorder, useHexGrid)
├── Simulation Settings (targetPopulation, maxPopulation, spawnInterval, ...)
├── Creatures[] (List of CreatureSaveData)
│   ├── Genome (physical, metabolic, growth, reproduction traits)
│   ├── Brain (NEATNetworkSaveData)
│   │   ├── Neurons[] (id, type, activationFunction, bias)
│   │   └── Connections[] (innovationNumber, fromId, toId, weight, enabled)
│   ├── State (position, rotation, energy, health, maturity, age)
│   └── Lineage (lineageId, generationIndex)
└── Resources[] (List of ResourceSaveData)
    ├── Position
    ├── EnergyValue
    ├── ResourceType
    └── SpawnTime (for decay calculation)
```

### 1.2. Serializable Classes

Tất cả các class cần serialize đều được đánh dấu `[Serializable]`:

```csharp
[Serializable]
public class SimulationSaveData { ... }

[Serializable]
public class CreatureSaveData { ... }

[Serializable]
public class NEATNetworkSaveData { ... }
```

**Lý do**: Unity's `JsonUtility` chỉ serialize các class có attribute `[Serializable]`.

---

## 2. KỸ THUẬT SERIALIZATION

### 2.1. Unity JsonUtility

**Công nghệ**: Unity's built-in `JsonUtility` class

**Ưu điểm**:
- Đơn giản, không cần thư viện bên ngoài
- Tích hợp sẵn với Unity
- Hỗ trợ nested objects và arrays
- Tự động xử lý các kiểu dữ liệu cơ bản (int, float, string, bool, Vector2, Color, etc.)

**Hạn chế**:
- Không hỗ trợ Dictionary (phải chuyển thành List)
- Không hỗ trợ polymorphism (không serialize derived classes)
- Không hỗ trợ custom serialization logic

**Cách sử dụng**:

```csharp
// Serialize (Object → JSON string)
string json = JsonUtility.ToJson(saveData, true); // true = pretty print

// Deserialize (JSON string → Object)
SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);
```

### 2.2. Custom Type Handling

#### Vector2 & Vector3
Unity tự động serialize `Vector2` và `Vector3` thành JSON:
```json
"position": {"x": 10.5, "y": 20.3}
```

#### Color
Unity serialize `Color` thành RGBA:
```json
"color": {"r": 0.8, "g": 0.5, "b": 0.2, "a": 1.0}
```

#### DateTime
`DateTime` được serialize thành string (ISO format):
```json
"saveTime": "2025-12-26T10:30:45.1234567"
```

#### Enum
Enum được serialize thành số (int):
```json
"resourceType": 0  // 0 = Plant, 1 = Meat
"pheromoneType": 1  // 0 = Red, 1 = Green, 2 = Blue
```

---

## 3. QUY TRÌNH SAVE

### 3.1. Data Collection (Thu thập dữ liệu)

**Bước 1**: Lấy metadata và settings từ `SimulationSupervisor`

```csharp
SimulationSaveData saveData = new SimulationSaveData
{
    saveName = saveName,
    saveTime = DateTime.Now,
    simulationTime = supervisor.SimulationTime,
    totalCreaturesBorn = supervisor.TotalBorn,
    totalCreaturesDied = supervisor.TotalDied,
    currentPopulation = supervisor.CurrentPopulation,
    worldSize = supervisor.WorldSize,
    enableWorldBorder = supervisor.EnableWorldBorder,
    // ... other settings
};
```

**Bước 2**: Thu thập dữ liệu từ tất cả creatures

```csharp
var creatures = supervisor.GetActiveCreatures();
foreach (var creature in creatures)
{
    var creatureData = new CreatureSaveData
    {
        genome = creature.GetGenome(),           // Struct - copy by value
        brain = CreateBrainSaveData(creature.GetBrain()),  // Custom conversion
        position = creature.transform.position,
        rotation = creature.transform.eulerAngles.z,
        energy = creature.Energy,
        maxEnergy = creature.MaxEnergy,
        health = creature.Health,
        maxHealth = creature.MaxHealth,
        maturity = creature.Maturity,
        age = creature.Age,
        lastEatTime = creature.LastEatTime,
        lastReproduceTime = creature.LastReproduceTime
    };
    
    // Lấy lineage information
    var lineage = creature.GetLineageRecord();
    if (lineage != null)
    {
        creatureData.lineageId = lineage.LineageId.ToString();
        creatureData.generationIndex = lineage.GenerationIndex;
    }
    
    saveData.creatures.Add(creatureData);
}
```

**Bước 3**: Thu thập dữ liệu từ tất cả resources

```csharp
var resources = supervisor.GetActiveResources();
foreach (var resource in resources)
{
    var resourceData = new ResourceSaveData
    {
        position = resource.transform.position,
        energyValue = resource.EnergyValue,
        resourceType = (int)resource.Type,
        spawnTime = resource.SpawnTime  // Quan trọng cho decay mechanism
    };
    
    saveData.resources.Add(resourceData);
}
```

### 3.2. Brain Network Conversion

**Vấn đề**: `NEATNetwork` chứa các internal structures không thể serialize trực tiếp.

**Giải pháp**: Tạo custom conversion method:

```csharp
private static NEATNetworkSaveData CreateBrainSaveData(NEATNetwork brain)
{
    var brainData = new NEATNetworkSaveData
    {
        inputCount = brain.InputCount,
        outputCount = brain.OutputCount
    };

    // Convert neurons
    var neurons = brain.GetNeurons();  // Public getter
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

    // Convert connections
    var connections = brain.GetConnections();  // Public getter
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

### 3.3. File I/O

**Bước 1**: Đảm bảo thư mục save tồn tại

```csharp
private static void EnsureSaveDirectory()
{
    string savePath = Path.Combine(Application.persistentDataPath, "Saves");
    if (!Directory.Exists(savePath))
    {
        Directory.CreateDirectory(savePath);
    }
}
```

**Bước 2**: Sanitize filename (loại bỏ ký tự không hợp lệ)

```csharp
private static string GetSaveFilePath(string saveName)
{
    // Loại bỏ các ký tự không hợp lệ trong tên file
    string safeName = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars()));
    return Path.Combine(SavePath, $"{safeName}.json");
}
```

**Bước 3**: Serialize và ghi file

```csharp
// Serialize to JSON (pretty print = true)
string json = JsonUtility.ToJson(saveData, true);

// Write to file
string filePath = GetSaveFilePath(saveName);
File.WriteAllText(filePath, json);
```

**Lưu ý**: 
- `Application.persistentDataPath` là thư mục lưu trữ persistent trên mỗi platform:
  - Windows: `%userprofile%\AppData\LocalLow\<companyname>\<productname>\Saves`
  - Mac: `~/Library/Application Support/<companyname>/<productname>/Saves`
  - Linux: `~/.config/unity3d/<companyname>/<productname>/Saves`

---

## 4. QUY TRÌNH LOAD

### 4.1. File Reading

```csharp
string filePath = GetSaveFilePath(saveName);

if (!File.Exists(filePath))
{
    Debug.LogWarning($"Save file not found: {saveName}");
    return null;
}

// Đọc file
string json = File.ReadAllText(filePath);

// Deserialize
SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);
```

### 4.2. Data Reconstruction (Tái tạo dữ liệu)

**Bước 1**: Restore world settings

```csharp
supervisor.SetWorldSize(saveData.worldSize);
supervisor.SetEnableWorldBorder(saveData.enableWorldBorder);
supervisor.SetTargetPopulationSize(saveData.targetPopulationSize);
supervisor.SetMaxPopulationSize(saveData.maxPopulationSize);
// ... other settings
```

**Bước 2**: Clear existing simulation

```csharp
supervisor.ClearSimulation();  // Xóa tất cả creatures và resources hiện tại
```

**Bước 3**: Recreate resources

```csharp
foreach (var resourceData in saveData.resources)
{
    if (resourceData.resourceType == 0) // Plant
    {
        supervisor.SpawnPlant(resourceData.position);
        var resource = supervisor.GetActiveResources().LastOrDefault();
        if (resource != null)
        {
            // Tính toán thời gian còn lại cho decay
            // spawnTime trong saveData là thời gian spawn gốc (relative to simulationTime)
            float timeSinceSpawn = Time.time - resourceData.spawnTime;
            float remainingDecayTime = resourceDecayTime - timeSinceSpawn;
            if (remainingDecayTime > 0)
            {
                resource.SetDecayTime(remainingDecayTime);
            }
        }
    }
    // TODO: Handle meat resources if needed
}
```

**Lưu ý**: 
- `Resource.spawnTime` là private field, không có public getter
- **BUG HIỆN TẠI**: Khi save, `spawnTime` KHÔNG được lưu vào `ResourceSaveData` (code hiện tại chỉ lưu position, energyValue, resourceType)
- Khi load, code cố gắng sử dụng `resourceData.spawnTime` nhưng giá trị này sẽ là 0 (default)
- **CẦN FIX**: Thêm logic để lưu spawnTime khi save (có thể cần thêm public getter hoặc reflection)

**Bước 4**: Recreate creatures

```csharp
foreach (var creatureData in saveData.creatures)
{
    // Reconstruct brain network
    NEATNetwork brain = SimulationSaveSystem.CreateBrainFromSaveData(creatureData.brain);
    
    // Spawn creature với genome và brain
    CreatureController creature = supervisor.SpawnCreature(
        creatureData.position,
        creatureData.genome,
        brain
    );
    
    // Restore state
    if (creature != null)
    {
        // Restore position and rotation first
        creature.transform.position = creatureData.position;
        creature.transform.rotation = Quaternion.Euler(0, 0, creatureData.rotation);
        
        // Restore creature state (energy, health, maturity, age)
        creature.SetStateFromSave(
            creatureData.energy,
            creatureData.maxEnergy,
            creatureData.health,
            creatureData.maturity,
            creatureData.age
        );
        
        // Note: lastEatTime và lastReproduceTime không được restore
        // vì SetStateFromSave() không có parameters cho chúng.
        // Điều này có nghĩa là creatures sẽ có cooldown reset khi load.
        
        // Restore lineage (tạo mới record nếu cần)
        if (!string.IsNullOrEmpty(creatureData.lineageId))
        {
            var lineageRecord = CreatureLineageRegistry.CreateRecord(
                creatureData.genome, 
                null
            );
            creature.SetLineageRecord(lineageRecord);
        }
    }
}
```

### 4.3. Brain Network Reconstruction

**Vấn đề**: Cần tái tạo `NEATNetwork` từ `NEATNetworkSaveData`.

**Giải pháp**: Custom factory method:

```csharp
public static NEATNetwork CreateBrainFromSaveData(NEATNetworkSaveData brainData)
{
    if (brainData == null) return null;

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

---

## 5. ERROR HANDLING (XỬ LÝ LỖI)

### 5.1. Try-Catch Blocks

Tất cả file I/O operations đều được bọc trong try-catch:

```csharp
try
{
    EnsureSaveDirectory();
    SimulationSaveData saveData = CreateSaveData(saveName, supervisor);
    string json = JsonUtility.ToJson(saveData, true);
    string filePath = GetSaveFilePath(saveName);
    File.WriteAllText(filePath, json);
    Debug.Log($"Game saved successfully: {saveName}");
    return true;
}
catch (Exception e)
{
    Debug.LogError($"Error saving game: {e.Message}\n{e.StackTrace}");
    return false;
}
```

### 5.2. Null Checks

Kiểm tra null trước khi sử dụng:

```csharp
if (supervisor == null)
{
    Debug.LogError("SimulationSupervisor is null!");
    return false;
}

if (brain == null)
    return null;
```

### 5.3. File Validation

Kiểm tra file tồn tại trước khi load:

```csharp
if (!File.Exists(filePath))
{
    Debug.LogWarning($"Save file not found: {saveName}");
    return null;
}
```

### 5.4. Corrupted File Handling

Khi đọc danh sách save files, bỏ qua các file bị corrupt:

```csharp
var files = Directory.GetFiles(SavePath, "*.json")
    .Select(filePath =>
    {
        try
        {
            string json = File.ReadAllText(filePath);
            SimulationSaveData data = JsonUtility.FromJson<SimulationSaveData>(json);
            // ... process data
        }
        catch
        {
            // Skip corrupted files
            return null;
        }
    })
    .Where(info => info != null)  // Filter out nulls
    .ToArray();
```

---

## 6. AUTOSAVE MECHANISM

### 6.1. Timer-Based Autosave

```csharp
[Header("Autosave Settings")]
[SerializeField] private bool enableAutosave = true;
[SerializeField] private float autosaveInterval = 600f; // 10 phút = 600 giây
private float lastAutosaveTime = 0f;

private void Update()
{
    if (isPaused) return;
    
    simulationTime += Time.deltaTime;
    UpdateAutosave();  // Check autosave
}

private void UpdateAutosave()
{
    if (!enableAutosave) return;

    if (simulationTime - lastAutosaveTime >= autosaveInterval)
    {
        PerformAutosave();
        lastAutosaveTime = simulationTime;
    }
}

private void PerformAutosave()
{
    Debug.Log("Performing autosave...");
    SimulationSaveSystem.Save(SimulationSaveSystem.AUTOSAVE_NAME, this);
}
```

### 6.2. Autosave Identification

Autosave files được nhận diện bằng tên đặc biệt:

```csharp
public const string AUTOSAVE_NAME = "autosave";

bool isAutosave = fileName == AUTOSAVE_NAME || fileName.StartsWith(AUTOSAVE_NAME + "_");
```

### 6.3. Autosave Display Priority

Autosave luôn hiển thị ở đầu danh sách:

```csharp
.OrderByDescending(info => info.isAutosave)  // Autosave first
.ThenByDescending(info => info.saveTime)     // Then by time
```

---

## 7. FILENAME SANITIZATION

### 7.1. Invalid Character Removal

```csharp
private static string GetSaveFilePath(string saveName)
{
    // Loại bỏ các ký tự không hợp lệ: < > : " / \ | ? *
    string safeName = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars()));
    return Path.Combine(SavePath, $"{safeName}.json");
}
```

### 7.2. Validation

```csharp
public static bool IsValidSaveName(string saveName)
{
    if (string.IsNullOrWhiteSpace(saveName))
        return false;

    char[] invalidChars = Path.GetInvalidFileNameChars();
    return !saveName.Any(c => invalidChars.Contains(c));
}
```

---

## 8. PERFORMANCE CONSIDERATIONS

### 8.1. File Size

**Ước tính kích thước file**:
- Metadata: ~500 bytes
- Mỗi creature: ~2-5 KB (tùy thuộc vào số lượng neurons/connections)
- Mỗi resource: ~100 bytes
- **Ví dụ**: 100 creatures + 50 resources ≈ 200-500 KB

### 8.2. Serialization Time

- **Save**: ~10-50ms cho 100 creatures (tùy thuộc vào network complexity)
- **Load**: ~50-200ms cho 100 creatures (bao gồm GameObject instantiation)

### 8.3. Memory Usage

- JSON string tạm thời: ~2x kích thước file
- Deserialized objects: ~1.5x kích thước file
- **Tổng**: ~3.5x kích thước file trong quá trình save/load

---

## 9. LIMITATIONS & FUTURE IMPROVEMENTS

### 9.1. Current Limitations

1. **Không hỗ trợ Dictionary**: Phải chuyển thành List
2. **Không hỗ trợ polymorphism**: Không thể serialize derived classes
3. **Không nén**: File JSON không được nén (có thể lớn)
4. **Không mã hóa**: File JSON có thể đọc được bằng text editor

### 9.2. Potential Improvements

1. **Compression**: Sử dụng GZip để nén JSON
2. **Binary Format**: Chuyển sang binary format (nhỏ hơn, nhanh hơn)
3. **Incremental Save**: Chỉ lưu thay đổi (delta save)
4. **Encryption**: Mã hóa file để bảo vệ dữ liệu
5. **Versioning**: Hỗ trợ migration giữa các phiên bản

---

## 10. FILE JSON MẪU

Xem file `SAVE_FILE_EXAMPLE.json` để xem cấu trúc JSON đầy đủ.

### 10.1. Cấu trúc JSON

**Root Object**: `SimulationSaveData`
- **Metadata**: Thông tin về save file (version, tên, thời gian)
- **Statistics**: Thống kê simulation (population, born, died)
- **World Settings**: Cấu hình world (size, border, hexgrid)
- **Simulation Settings**: Các tham số simulation (spawn rate, population limits)
- **Creatures Array**: Danh sách tất cả creatures với đầy đủ genome, brain, state
- **Resources Array**: Danh sách tất cả resources với position, energy, type

### 10.2. Điểm quan trọng trong JSON

1. **Genome**: Struct được serialize trực tiếp, bao gồm tất cả traits (physical, metabolic, growth, reproduction, sensory, behavioral, evolution)

2. **Brain Network**: 
   - `neurons`: Danh sách neurons với id, type (0=Input, 1=Hidden, 2=Output), activationFunction, bias
   - `connections`: Danh sách connections với innovationNumber, fromNeuronId, toNeuronId, weight, enabled

3. **State**: 
   - `position`: Vector2 (x, y)
   - `rotation`: Float (degrees)
   - `energy`, `maxEnergy`: Float
   - `health`, `maxHealth`: Float
   - `maturity`: Float (0-1)
   - `age`: Float (seconds)
   - `lastEatTime`, `lastReproduceTime`: Float (seconds, relative to simulationTime)

4. **Resources**:
   - `spawnTime`: Float (seconds, relative to simulationTime) - quan trọng cho decay calculation

### 10.3. Limitations trong JSON mẫu

- **lastEatTime và lastReproduceTime**: Được lưu nhưng không được restore khi load (do `SetStateFromSave()` không có parameters cho chúng)
- **LineageId**: Được lưu nhưng khi load sẽ tạo lineage record mới (không thể restore exact GUID)
- **Resource spawnTime**: Được lưu nhưng khi load chỉ tính toán `remainingDecayTime` (không restore exact spawnTime)

