# TÍCH HỢP SPECIATION VÀO LINEAGE

## Tổng quan

Đã tích hợp **Speciation System** vào **Lineage System** để người dùng có thể nhìn vào lineage code và biết được sinh vật thuộc species nào.

## Thay đổi

### 1. CreatureLineageRecord

**Thêm 2 fields mới**:
- `SpeciesId` (int): ID của species (-1 = chưa phân loại)
- `SpeciesCode` (string): Code dễ đọc (ví dụ: "SP-001", "SP-002")

```csharp
public sealed class CreatureLineageRecord
{
    // ... existing fields ...
    public int SpeciesId { get; }
    public string SpeciesCode { get; } // "SP-001", "SP-002", ...
}
```

### 2. CreatureLineageRegistry

**Cập nhật `CreateRecord()`**:
- Thêm parameter `speciesId` (optional, default = -1)
- Encode `speciesId` vào `GenomeCode` hash
- Tạo `SpeciesCode` tự động: `"SP-{speciesId:000}"`

**Thêm methods mới**:
- `GetRecordsBySpecies(int speciesId)`: Lấy tất cả records thuộc một species
- `Reset()`: Reset registry khi bắt đầu simulation mới

### 3. SimulationSupervisor

**Thêm SpeciationSystem**:
- Field: `speciationSystem` (SpeciationSystem)
- Field: `enableSpeciation` (bool, có thể bật/tắt trong Inspector)

**Tích hợp vào SpawnCreature()**:
```csharp
// Classify to species nếu có brain và speciation enabled
int speciesId = -1;
if (enableSpeciation && speciationSystem != null && brain != null)
{
    speciesId = speciationSystem.ClassifyToSpecies(brain);
}

// Tạo lineage record với species ID
lineageRecord = CreatureLineageRegistry.CreateRecord(genome, null, speciesId);
```

**Tích hợp vào OnCreatureReproduce()**:
- Classify child brain to species
- Tạo lineage record với species ID của child

### 4. UI (CreaturePopupUI)

**Hiển thị Species Code**:
```csharp
if (lineage.SpeciesId >= 0)
{
    CreateGenomeRow(genomeListRoot, "Species", lineage.SpeciesCode);
}
```

### 5. Save/Load System

**Thêm `speciesId` vào `CreatureSaveData`**:
```csharp
public int speciesId = -1; // -1 = chưa phân loại
```

**Save**: Lưu `lineage.SpeciesId` vào `creatureData.speciesId`

**Load**: Restore `speciesId` khi tạo lineage record mới

## Cách sử dụng

### 1. Bật Speciation

Trong Unity Inspector, trên `SimulationSupervisor`:
- ✅ Check `Enable Speciation`

### 2. Xem Species trong UI

Khi click vào một creature:
- Hiển thị "Species: SP-001" (nếu đã được phân loại)
- Hiển thị "Species: UNKNOWN" (nếu chưa phân loại hoặc speciation tắt)

### 3. GenomeCode phản ánh Species

`GenomeCode` bây giờ bao gồm cả `speciesId` trong hash, nên:
- Cùng species → có thể có GenomeCode tương tự (nhưng không chắc chắn vì còn phụ thuộc vào genome)
- Khác species → GenomeCode khác nhau

**Lưu ý**: GenomeCode vẫn phụ thuộc vào nhiều yếu tố (genome traits, lineageId, speciesId), nên không thể chỉ dựa vào GenomeCode để xác định species. Nên dùng `SpeciesCode` hoặc `SpeciesId` trực tiếp.

## Ví dụ

```
Creature A (thế hệ đầu):
  LineageId: 1
  GenerationIndex: 0
  SpeciesId: 1
  SpeciesCode: "SP-001"
  GenomeCode: "A3F2B1C4" (bao gồm speciesId=1 trong hash)

Creature B (con của A, cùng species):
  LineageId: 2
  GenerationIndex: 1
  SpeciesId: 1 (kế thừa từ parent hoặc được classify lại)
  SpeciesCode: "SP-001"
  GenomeCode: "B7E4D2F8" (khác vì genome khác)

Creature C (con của A, khác species do mutation lớn):
  LineageId: 3
  GenerationIndex: 1
  SpeciesId: 2 (được classify vào species mới)
  SpeciesCode: "SP-002"
  GenomeCode: "C9A1B3E5" (khác vì speciesId khác)
```

## Lợi ích

1. **Dễ nhận biết**: Người dùng có thể nhìn vào SpeciesCode và biết sinh vật thuộc nhóm nào
2. **Phân tích Evolution**: Có thể theo dõi sự phát triển của các species
3. **Visualization**: Có thể visualize creatures theo species (màu sắc, grouping)
4. **Research**: Hỗ trợ nghiên cứu về speciation và diversity

## Hạn chế

1. **Speciation chưa hoàn chỉnh**: SpeciationSystem chưa được tích hợp đầy đủ (chưa có fitness sharing trong selection)
2. **Save/Load**: SpeciesId được restore nhưng SpeciationSystem state không được restore (phải classify lại)
3. **Performance**: Classify to species mỗi lần spawn có thể tốn performance (nhưng negligible)

## Kế hoạch Cải thiện

1. **Restore SpeciationSystem state** khi load game
2. **Visualization**: Hiển thị creatures với màu sắc theo species
3. **Statistics**: Thống kê số lượng creatures theo species
4. **Fitness Sharing**: Tích hợp fitness sharing vào selection (nếu có fitness function)





