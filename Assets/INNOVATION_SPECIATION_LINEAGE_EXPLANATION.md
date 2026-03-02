# GIẢI THÍCH: INNOVATION NUMBER, SPECIATION VÀ LINEAGE

## 1. Innovation Number (Số Đổi mới)

### 1.1. Khái niệm

**Innovation Number** là một số duy nhất được gán cho mỗi cấu trúc mới (connection hoặc neuron) trong quá trình tiến hóa NEAT. Nó được sử dụng để theo dõi lịch sử tiến hóa và so sánh các mạng nơ-ron khác nhau.

### 1.2. Cách hoạt động trong Verrarium

**InnovationTracker** (Singleton pattern):
```csharp
public class InnovationTracker
{
    private int nextInnovationNumber = 1;
    private Dictionary<string, int> innovationMap; // Map (fromId,toId) -> innovationNumber
    
    public int GetInnovationNumber(int fromNeuronId, int toNeuronId)
    {
        string key = $"{fromNeuronId}_{toNeuronId}";
        
        if (innovationMap.ContainsKey(key))
        {
            // Connection này đã tồn tại → trả về innovation number cũ
            return innovationMap[key];
        }
        else
        {
            // Connection mới → tạo innovation number mới
            int innovation = nextInnovationNumber++;
            innovationMap[key] = innovation;
            return innovation;
        }
    }
}
```

**Cơ chế**:
1. Mỗi connection được xác định bởi cặp `(fromNeuronId, toNeuronId)`
2. Nếu cặp này đã tồn tại → sử dụng innovation number cũ
3. Nếu cặp này mới → tạo innovation number mới (tăng dần từ 1)

**Ví dụ**:
- Connection từ neuron 0 → neuron 10: Innovation #1
- Connection từ neuron 1 → neuron 10: Innovation #2
- Connection từ neuron 0 → neuron 10 (lần sau): Vẫn là Innovation #1 (đã tồn tại)

### 1.3. Sử dụng trong Code

**1. Khi tạo connection mới** (`NEATNetwork.cs`):
```csharp
public void AddNewConnection(int fromId, int toId, float weight)
{
    int innovation = innovationTracker.GetInnovationNumber(fromId, toId);
    AddConnection(fromId, toId, weight, innovation);
}
```

**2. Khi sao chép network** (`NEATNetwork.cs`):
```csharp
// Giữ nguyên innovation numbers từ parent
AddConnection(connection.fromNeuronId, connection.toNeuronId, 
              connection.weight, connection.innovationNumber, connection.enabled);
```

**3. Trong SpeciationSystem** (`SpeciationSystem.cs`):
```csharp
// Sử dụng innovation numbers để so sánh networks
var dict1 = new Dictionary<int, Connection>();
foreach (var conn in conns1)
{
    dict1[conn.innovationNumber] = conn; // Key = innovation number
}
```

**4. Trong Save/Load**:
- Innovation numbers được lưu trong JSON
- Khi load, giữ nguyên innovation numbers để đảm bảo tính nhất quán

### 1.4. Tầm quan trọng

- **Historical Marking**: Cho phép so sánh các mạng khác nhau dựa trên innovation numbers
- **Crossover**: Quan trọng cho việc kết hợp hai mạng (matching genes dựa trên innovation numbers)
- **Speciation**: Sử dụng để tính compatibility distance

---

## 2. Speciation (Phân loài)

### 2.1. Khái niệm

**Speciation** là cơ chế phân chia quần thể thành các "loài" (species) dựa trên sự tương đồng về cấu trúc mạng nơ-ron. Mục đích là bảo vệ các đổi mới khỏi bị loại bỏ sớm do cạnh tranh với các mạng đã được tối ưu hóa tốt.

### 2.2. Trạng thái trong Verrarium

**⚠️ CHƯA ĐƯỢC TÍCH HỢP VÀO MAIN LOOP**

- ✅ Code đã có đầy đủ: `SpeciationSystem.cs`
- ✅ Logic hoàn chỉnh: Compatibility distance, Fitness sharing
- ⚠️ **KHÔNG được gọi** trong `SimulationSupervisor` hoặc `CreatureController`
- ⚠️ Chỉ có trong DOTS folder nhưng không được sử dụng

### 2.3. Cách hoạt động (Nếu được tích hợp)

**1. Compatibility Distance**:
```csharp
public float ComputeCompatibilityDistance(NEATNetwork network1, NEATNetwork network2)
{
    // So sánh dựa trên innovation numbers
    // E = Excess genes (vượt quá)
    // D = Disjoint genes (không khớp)
    // W̄ = Average weight difference của matching genes
    
    float distance = (C1 * excess) / N + (C2 * disjoint) / N + C3 * avgWeightDiff;
    return distance;
}
```

**Công thức**:
```
δ = (c₁ × E / N) + (c₂ × D / N) + (c₃ × W̄)
```

Với:
- `E`: Số excess genes (genes có innovation number > max của network nhỏ hơn)
- `D`: Số disjoint genes (genes chỉ có trong một network)
- `W̄`: Average weight difference của matching genes
- `N`: Số genes trong network lớn hơn
- `c₁ = 1.0`, `c₂ = 1.0`, `c₃ = 0.4`: Hệ số điều chỉnh

**2. Classification**:
```csharp
public int ClassifyToSpecies(NEATNetwork network)
{
    // Tìm loài tương thích (distance < threshold = 3.0)
    foreach (var species in speciesMap.Values)
    {
        float distance = ComputeCompatibilityDistance(network, species.representative);
        if (distance < COMPATIBILITY_THRESHOLD) // 3.0
        {
            species.members.Add(network);
            return species.id;
        }
    }
    
    // Không tìm thấy → tạo loài mới
    int newSpeciesId = nextSpeciesId++;
    // ...
}
```

**3. Fitness Sharing**:
```csharp
public float GetAdjustedFitness(NEATNetwork network, int speciesId, float rawFitness)
{
    var species = speciesMap[speciesId];
    return rawFitness / species.members.Count; // Chia sẻ fitness trong loài
}
```

**Lợi ích**:
- Bảo vệ các đổi mới khỏi bị loại bỏ sớm
- Cho phép nhiều loài cùng phát triển
- Tăng diversity trong quần thể

### 2.4. Tại sao chưa được tích hợp?

**Lý do**:
1. **Life-based Selection**: Verrarium sử dụng chọn lọc tự nhiên thuần túy (không có fitness function), nên speciation không cần thiết ngay lập tức
2. **Real-time Evolution**: rtNEAT không có thế hệ rõ ràng, nên speciation phức tạp hơn
3. **Priority**: Các tính năng cốt lõi (vòng đời, metabolism, save/load) được ưu tiên

**Kế hoạch tích hợp**:
- Cần tính fitness cho mỗi creature (dựa trên tuổi thọ, số con, v.v.)
- Cần gọi `ClassifyToSpecies()` khi creature spawn
- Cần sử dụng adjusted fitness trong selection (nếu có)

---

## 3. Lineage (Phả hệ)

### 3.1. Khái niệm

**Lineage** là hệ thống theo dõi phả hệ của mỗi sinh vật, cho phép biết được sinh vật đó thuộc thế hệ nào và có cha mẹ là ai.

### 3.2. Cách hoạt động trong Verrarium

**CreatureLineageRegistry** (Static class):
```csharp
public static class CreatureLineageRegistry
{
    private static int nextLineageId = 1; // Tự động tăng
    private static Dictionary<int, CreatureLineageRecord> InstanceLookup; // GameObject → Record
    private static Dictionary<int, CreatureLineageRecord> LineageLookup; // LineageId → Record
}
```

**CreatureLineageRecord**:
```csharp
public sealed class CreatureLineageRecord
{
    public int LineageId { get; }              // ID duy nhất của lineage
    public int GenerationIndex { get; }        // Thế hệ (0 = thế hệ đầu)
    public string GenomeCode { get; }          // Hash của genome
    public string ParentGenomeCode { get; }    // Hash của parent genome
    public Genome GenomeSnapshot { get; }      // Snapshot của genome
    public CreatureLineageRecord Parent { get; } // Reference đến parent
}
```

### 3.3. Cách tính LineageId

**LineageId**: Tự động tăng, bắt đầu từ 1
```csharp
public static CreatureLineageRecord CreateRecord(Genome genome, CreatureLineageRecord parent)
{
    int id = nextLineageId++; // Tự động tăng: 1, 2, 3, ...
    string genomeCode = ComputeGenomeCode(genome, id);
    var record = new CreatureLineageRecord(id, genome, parent, genomeCode);
    return record;
}
```

**Đặc điểm**:
- Mỗi sinh vật có một LineageId duy nhất
- LineageId tăng dần theo thời gian spawn
- **KHÔNG** phải là ID của cha mẹ (mỗi sinh vật có ID riêng)

### 3.4. Cách tính GenerationIndex

**GenerationIndex**: Tính từ parent
```csharp
internal CreatureLineageRecord(int id, Genome genome, CreatureLineageRecord parent, string genomeCode)
{
    LineageId = id;
    GenomeSnapshot = genome;
    Parent = parent;
    GenerationIndex = parent != null ? parent.GenerationIndex + 1 : 0; // ← Đây
    ParentGenomeCode = parent != null ? parent.GenomeCode : "ROOT";
    GenomeCode = genomeCode;
}
```

**Công thức**:
- Nếu có parent: `GenerationIndex = parent.GenerationIndex + 1`
- Nếu không có parent (thế hệ đầu): `GenerationIndex = 0`

**Ví dụ**:
```
Thế hệ đầu (spawn ban đầu):
  Creature A: LineageId=1, GenerationIndex=0, Parent=null

Thế hệ 1 (con của A):
  Creature B: LineageId=2, GenerationIndex=1, Parent=A
  Creature C: LineageId=3, GenerationIndex=1, Parent=A

Thế hệ 2 (con của B):
  Creature D: LineageId=4, GenerationIndex=2, Parent=B
  Creature E: LineageId=5, GenerationIndex=2, Parent=B
```

### 3.5. GenomeCode

**GenomeCode**: Hash SHA1 của genome + lineageId
```csharp
private static string ComputeGenomeCode(Genome genome, int lineageId)
{
    var buffer = new List<byte>(64);
    
    // Serialize tất cả genome fields
    AddFloat(genome.size);
    AddFloat(genome.speed);
    AddFloat(genome.diet);
    // ... (tất cả traits)
    AddFloat(lineageId); // ← Bao gồm lineageId
    
    // Hash SHA1 → 8 ký tự hex
    using SHA1 sha = SHA1.Create();
    byte[] hash = sha.ComputeHash(buffer.ToArray());
    // Lấy 4 bytes đầu → 8 ký tự hex
    return sb.ToString(); // Ví dụ: "A3F2B1C4"
}
```

**Mục đích**:
- Tạo một "fingerprint" duy nhất cho mỗi genome
- Dùng để hiển thị trong UI
- Có thể dùng để so sánh genomes

### 3.6. Sử dụng trong Code

**1. Khi spawn creature mới** (`SimulationSupervisor.cs`):
```csharp
public GameObject SpawnCreature(Vector2 position, Genome genome, NEATNetwork brain = null, 
                                 CreatureLineageRecord lineageRecord = null)
{
    // Nếu không có lineage record → tạo mới (thế hệ đầu)
    if (lineageRecord == null)
    {
        lineageRecord = CreatureLineageRegistry.CreateRecord(genome, null); // Parent = null
    }
    
    controller.SetLineageRecord(lineageRecord);
    CreatureLineageRegistry.Bind(controller, lineageRecord);
}
```

**2. Khi sinh sản** (`SimulationSupervisor.cs`):
```csharp
private void OnCreatureReproduce(CreatureController parent, Genome childGenome, NEATNetwork childBrain)
{
    // Lấy lineage record của parent
    CreatureLineageRecord parentRecord = CreatureLineageRegistry.Get(parent);
    
    // Tạo lineage record cho con (với parent)
    CreatureLineageRecord childRecord = CreatureLineageRegistry.CreateRecord(childGenome, parentRecord);
    
    // Spawn egg với lineage record
    SpawnEgg(position, childGenome, childBrain, childRecord);
}
```

**3. Trong UI** (`CreaturePopupUI.cs`):
```csharp
CreatureLineageRecord lineage = creature.GetLineageRecord();
if (lineage != null)
{
    // Hiển thị thông tin lineage
    CreateGenomeRow(genomeListRoot, "Generation", lineage.GenerationIndex.ToString());
    CreateGenomeRow(genomeListRoot, "Lineage ID", $"#{lineage.LineageId:0000}");
    CreateGenomeRow(genomeListRoot, "Genome Code", lineage.GenomeCode);
    CreateGenomeRow(genomeListRoot, "Parent Code", lineage.ParentGenomeCode);
}
```

**4. Trong Save/Load**:
```csharp
// Save
creatureData.lineageId = lineage.LineageId.ToString();
creatureData.generationIndex = lineage.GenerationIndex;

// Load (Lưu ý: Không thể restore chính xác lineage IDs)
// Tạo mới lineage record với parent = null
lineageRecord = CreatureLineageRegistry.CreateRecord(creatureData.genome, null);
```

### 3.7. Hạn chế

**1. Save/Load**:
- Lineage IDs không thể restore chính xác (tạo mới khi load)
- GenerationIndex có thể không chính xác sau khi load
- Parent references bị mất

**2. Không có Lineage Tree**:
- Chỉ lưu parent trực tiếp, không có cây phả hệ đầy đủ
- Không thể truy ngược nhiều thế hệ

---

## 4. Tóm tắt

| **Khái niệm** | **Trạng thái** | **Cách tính** | **Sử dụng** |
|---------------|----------------|---------------|-------------|
| **Innovation Number** | ✅ Đang dùng | Tự động tăng từ 1, dựa trên (fromId, toId) | So sánh networks, Speciation (chưa tích hợp) |
| **Speciation** | ⚠️ Chưa tích hợp | Compatibility distance dựa trên innovation numbers | Chưa được gọi trong main loop |
| **LineageId** | ✅ Đang dùng | Tự động tăng từ 1 | Theo dõi phả hệ, hiển thị UI |
| **GenerationIndex** | ✅ Đang dùng | `parent.GenerationIndex + 1` (root = 0) | Hiển thị thế hệ, phân tích evolution |
| **GenomeCode** | ✅ Đang dùng | SHA1 hash của genome + lineageId | Hiển thị UI, so sánh genomes |

---

## 5. Kế hoạch Cải thiện

**1. Speciation**:
- Tích hợp vào main loop
- Tính fitness cho mỗi creature (tuổi thọ, số con, v.v.)
- Sử dụng adjusted fitness trong selection

**2. Lineage**:
- Cải thiện save/load để restore lineage IDs
- Xây dựng lineage tree đầy đủ
- Visualization tool cho lineage tree

**3. Innovation Tracking**:
- Reset khi bắt đầu simulation mới (đã có `Reset()` method)
- Có thể cải thiện để hỗ trợ multiple simulations





