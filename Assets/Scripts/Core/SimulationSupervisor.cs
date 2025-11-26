using System.Collections.Generic;
using UnityEngine;
using Verrarium.Resources;
using Verrarium.Creature;
using Verrarium.Data;
using Verrarium.Evolution;
using Verrarium.World;

namespace Verrarium.Core
{
    /// <summary>
    /// Supervisor quản lý toàn bộ giả lập - Singleton pattern
    /// Quản lý quần thể, tài nguyên, và các sự kiện tiến hóa
    /// </summary>
    public class SimulationSupervisor : MonoBehaviour
    {
        public static SimulationSupervisor Instance { get; private set; }

        [Header("Population Settings")]
        [SerializeField] private int targetPopulationSize = 50;
        [SerializeField] private int maxPopulationSize = 100;
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private GameObject eggPrefab; // Thêm reference tới prefab trứng

        [Header("Resource Settings")]
        [SerializeField] private GameObject plantPrefab;
        [SerializeField] private GameObject meatPrefab;
        [SerializeField] private float resourceSpawnInterval = 2f;
        [SerializeField] private int plantsPerSpawn = 5;
        [SerializeField] private float fertileAreaRadius = 3f;
        [SerializeField] private float globalSpawnChance = 0.3f; // 30% cơ hội sinh thực phẩm toàn bản đồ

        [Header("World Settings")]
        [SerializeField] private Vector2 worldSize = new Vector2(20f, 20f);
        [SerializeField] private Transform worldBounds;
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private float borderThickness = 2f; // Độ dày của đường viền vật lý

        [Header("Fertile Areas")]
        [SerializeField] private List<Transform> fertileAreas = new List<Transform>();

        // Quần thể hiện tại
        private List<CreatureController> activeCreatures = new List<CreatureController>();

        // Tài nguyên
        private List<Resource> activeResources = new List<Resource>();

        // Thống kê
        private int totalCreaturesBorn = 0;
        private int totalCreaturesDied = 0;
        private float simulationTime = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            InitializeWorld();
            
            // Tìm HexGrid nếu chưa được gán
            if (useHexGrid && hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
            }
            
            // Setup fertile areas trên hex grid
            if (useHexGrid && hexGrid != null)
            {
                SetupHexGridFertileAreas();
            }
            
            SpawnInitialCreatures();
            InvokeRepeating(nameof(SpawnResources), 1f, resourceSpawnInterval);
        }

        private void Update()
        {
            simulationTime += Time.deltaTime;
            UpdatePopulation();
        }

        /// <summary>
        /// Khởi tạo thế giới - tạo ranh giới nếu chưa có
        /// </summary>
        private void InitializeWorld()
        {
            if (worldBounds == null)
            {
                GameObject boundsObj = new GameObject("WorldBounds");
                boundsObj.transform.SetParent(transform);

                // Tạo EdgeCollider2D cho ranh giới
                EdgeCollider2D edgeCollider = boundsObj.AddComponent<EdgeCollider2D>();
                Vector2[] points = new Vector2[]
                {
                    new Vector2(-worldSize.x / 2, -worldSize.y / 2),
                    new Vector2(worldSize.x / 2, -worldSize.y / 2),
                    new Vector2(worldSize.x / 2, worldSize.y / 2),
                    new Vector2(-worldSize.x / 2, worldSize.y / 2),
                    new Vector2(-worldSize.x / 2, -worldSize.y / 2)
                };
                edgeCollider.points = points;

                worldBounds = boundsObj.transform;
            }
        }

        /// <summary>
        /// Sinh ra các sinh vật ban đầu
        /// </summary>
        private void SpawnInitialCreatures()
        {
            if (creaturePrefab == null)
            {
                Debug.LogError("Creature Prefab chưa được gán!");
                return;
            }

            for (int i = 0; i < targetPopulationSize / 2; i++)
            {
                SpawnCreature(GetRandomPosition(), Genome.CreateDefault());
            }
        }

        /// <summary>
        /// Sinh ra một sinh vật mới
        /// </summary>
        public GameObject SpawnCreature(Vector2 position, Genome genome, Evolution.NEATNetwork brain = null, Data.CreatureLineageRecord lineageRecord = null)
        {
            GameObject creatureObj = Instantiate(creaturePrefab, position, Quaternion.identity);
            CreatureController controller = creatureObj.GetComponent<CreatureController>();
            
            if (controller != null)
            {
                controller.Initialize(genome, brain);
                if (lineageRecord == null)
                {
                    lineageRecord = Data.CreatureLineageRegistry.CreateRecord(genome, null);
                }
                controller.SetLineageRecord(lineageRecord);
                Data.CreatureLineageRegistry.Bind(controller, lineageRecord);
                activeCreatures.Add(controller);
                totalCreaturesBorn++;
            }

            return creatureObj;
        }

        /// <summary>
        /// Đăng ký sinh vật chết
        /// </summary>
        public void OnCreatureDeath(CreatureController creature, Vector2 deathPosition, float size)
        {
            activeCreatures.Remove(creature);
            totalCreaturesDied++;

            // Tạo thịt từ xác chết
            if (meatPrefab != null)
            {
                GameObject meatObj = Instantiate(meatPrefab, deathPosition, Quaternion.identity);
                Resource meat = meatObj.GetComponent<Resource>();
                if (meat != null)
                {
                    // Năng lượng thịt tỷ lệ với kích thước
                    meat.SetEnergyValue(size * 30f);
                    activeResources.Add(meat);
                }
            }
        }

        /// <summary>
        /// Xử lý sinh sản - tạo trứng mới
        /// </summary>
        public void OnCreatureReproduction(CreatureController parent, Vector2 position, Genome parentGenome)
        {
            // Áp dụng đột biến dựa trên mutationRate của cha mẹ
            int numMutations = PoissonRandom(parentGenome.mutationRate);
            Genome childGenome = parentGenome;
            
            for (int i = 0; i < numMutations; i++)
            {
                childGenome = Genome.Mutate(childGenome, 0.1f);
            }

            // Sao chép và đột biến bộ não
            Evolution.NEATNetwork childBrain = null;
            if (parent != null)
            {
                Evolution.NEATNetwork parentBrain = parent.GetBrain();
                if (parentBrain != null)
                {
                    childBrain = new Evolution.NEATNetwork(parentBrain);
                    
                    // Áp dụng đột biến NEAT đầy đủ
                    Evolution.NEATMutator.Mutate(childBrain, numMutations);
                }
            }

            Data.CreatureLineageRecord parentRecord = Data.CreatureLineageRegistry.Get(parent);
            Data.CreatureLineageRecord childRecord = Data.CreatureLineageRegistry.CreateRecord(childGenome, parentRecord);

            // Sinh trứng thay vì sinh trực tiếp sinh vật
            SpawnEgg(position, childGenome, childBrain, childRecord);
        }

        /// <summary>
        /// Sinh ra một quả trứng
        /// </summary>
        private void SpawnEgg(Vector2 position, Genome genome, Evolution.NEATNetwork brain, Data.CreatureLineageRecord lineageRecord)
        {
            GameObject eggObj;
            
            if (eggPrefab != null)
            {
                eggObj = Instantiate(eggPrefab, position, Quaternion.identity);
            }
            else
            {
                // Fallback: Tạo trứng tạm thời nếu chưa có prefab
                eggObj = new GameObject("Egg_Temp");
                eggObj.transform.position = position;
                eggObj.AddComponent<SpriteRenderer>();
                eggObj.AddComponent<CircleCollider2D>();
                eggObj.AddComponent<CreatureEgg>();
            }

            CreatureEgg egg = eggObj.GetComponent<CreatureEgg>();
            if (egg != null)
            {
                egg.Initialize(genome, brain, lineageRecord);
            }
        }

        /// <summary>
        /// Sinh tài nguyên định kỳ
        /// </summary>
        private void SpawnResources()
        {
            // Điều chỉnh số lượng thực vật dựa trên mật độ dân số
            int currentPopulation = activeCreatures.Count;
            int plantsToSpawn = Mathf.RoundToInt(plantsPerSpawn * (1f + (targetPopulationSize - currentPopulation) / (float)targetPopulationSize));
            plantsToSpawn = Mathf.Max(1, plantsToSpawn);

            // Quyết định sinh ở đâu: Fertile Areas hay Global
            for (int i = 0; i < plantsToSpawn; i++)
            {
                Vector2 spawnPos;
                bool spawnGlobal = Random.value < globalSpawnChance;

                if (!spawnGlobal && useHexGrid && hexGrid != null)
            {
                // Sử dụng hex grid fertile cells
                var fertileCells = hexGrid.GetFertileCells();
                if (fertileCells.Count > 0)
                    {
                        HexCell fertileCell = fertileCells[Random.Range(0, fertileCells.Count)];
                        spawnPos = hexGrid.HexToWorld(fertileCell.Coordinates);
                        spawnPos += Random.insideUnitCircle * (hexGrid.HexSize * 0.5f); // Tăng phạm vi random trong cell
                }
                else
                {
                        spawnPos = GetRandomPosition();
                }
            }
                else if (!spawnGlobal && fertileAreas.Count > 0)
            {
                    // Sử dụng fertile areas cũ
                    Transform fertileArea = fertileAreas[Random.Range(0, fertileAreas.Count)];
                    spawnPos = (Vector2)fertileArea.position + Random.insideUnitCircle * (fertileAreaRadius * 1.5f); // Tăng bán kính spawn
            }
            else
                {
                    // Sinh ngẫu nhiên toàn bản đồ
                    spawnPos = GetRandomPosition();
                }

                SpawnPlant(spawnPos);
            }
        }

        /// <summary>
        /// Sinh một cây
        /// </summary>
        private void SpawnPlant(Vector2 position)
        {
            if (plantPrefab == null) return;

            GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity);
            Resource plant = plantObj.GetComponent<Resource>();
            if (plant != null)
            {
                activeResources.Add(plant);
            }
        }

        /// <summary>
        /// Cập nhật quần thể - loại bỏ các sinh vật đã chết
        /// </summary>
        private void UpdatePopulation()
        {
            // Dọn dẹp danh sách (creatures tự xóa khi chết)
            activeCreatures.RemoveAll(c => c == null);
        }

        /// <summary>
        /// Lấy vị trí ngẫu nhiên trong thế giới
        /// </summary>
        public Vector2 GetRandomPosition()
        {
            if (useHexGrid && hexGrid != null)
            {
                // Sử dụng hex grid
                HexCell randomCell = hexGrid.GetRandomCell();
                if (randomCell != null)
                {
                    return hexGrid.HexToWorld(randomCell.Coordinates);
                }
            }
            
            // Fallback: random trong world bounds
            return new Vector2(
                Random.Range(-worldSize.x / 2, worldSize.x / 2),
                Random.Range(-worldSize.y / 2, worldSize.y / 2)
            );
        }

        /// <summary>
        /// Setup fertile areas trên hex grid
        /// </summary>
        private void SetupHexGridFertileAreas()
        {
            if (hexGrid == null) return;

            // Nếu có fertile areas được gán, đánh dấu các hex tương ứng
            if (fertileAreas.Count > 0)
            {
                foreach (Transform fertileArea in fertileAreas)
                {
                    if (fertileArea != null)
                    {
                        HexCoordinates coords = hexGrid.WorldToHex(fertileArea.position);
                        hexGrid.SetFertile(coords, true);
                        
                        // Đánh dấu các hex lân cận cũng màu mỡ
                        var neighbors = hexGrid.GetNeighbors(coords);
                        foreach (var neighbor in neighbors)
                        {
                            hexGrid.SetFertile(neighbor.Coordinates, true);
                        }
                    }
                }
            }
            else
            {
                // Tạo một số fertile areas ngẫu nhiên
                int numFertileAreas = Mathf.Max(3, hexGrid.GridWidth / 5);
                for (int i = 0; i < numFertileAreas; i++)
                {
                    HexCell randomCell = hexGrid.GetRandomCell();
                    if (randomCell != null)
                    {
                        hexGrid.SetFertile(randomCell.Coordinates, true);
                        
                        // Đánh dấu lân cận
                        var neighbors = hexGrid.GetNeighbors(randomCell.Coordinates);
                        foreach (var neighbor in neighbors)
                        {
                            if (Random.value < 0.5f) // 50% chance
                            {
                                hexGrid.SetFertile(neighbor.Coordinates, true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tìm tài nguyên gần nhất của loại chỉ định
        /// </summary>
        public Resource FindClosestResource(Vector2 position, ResourceType type, float maxDistance)
        {
            Resource closest = null;
            float closestDistance = maxDistance;

            foreach (Resource resource in activeResources)
            {
                if (resource == null || resource.Type != type) continue;

                float distance = Vector2.Distance(position, resource.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = resource;
                }
            }

            return closest;
        }

        /// <summary>
        /// Tìm sinh vật gần nhất
        /// </summary>
        public CreatureController FindClosestCreature(Vector2 position, CreatureController exclude, float maxDistance)
        {
            CreatureController closest = null;
            float closestDistance = maxDistance;

            foreach (CreatureController creature in activeCreatures)
            {
                if (creature == null || creature == exclude) continue;

                float distance = Vector2.Distance(position, creature.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = creature;
                }
            }

            return closest;
        }

        /// <summary>
        /// Xóa tài nguyên khỏi danh sách
        /// </summary>
        public void RemoveResource(Resource resource)
        {
            activeResources.Remove(resource);
        }

        /// <summary>
        /// Tạo số ngẫu nhiên theo phân phối Poisson
        /// </summary>
        private int PoissonRandom(float lambda)
        {
            int k = 0;
            double p = 1.0;
            double L = Mathf.Exp(-lambda);

            do
            {
                k++;
                p *= Random.Range(0f, 1f);
            } while (p > L);

            return k - 1;
        }

        // Getters cho thống kê
        public int CurrentPopulation => activeCreatures.Count;
        public int TotalBorn => totalCreaturesBorn;
        public int TotalDied => totalCreaturesDied;
        public float SimulationTime => simulationTime;
        public Vector2 WorldSize => worldSize;

        // Getters cho settings
        public int GetTargetPopulationSize() => targetPopulationSize;
        public int GetMaxPopulationSize() => maxPopulationSize;
        public float GetResourceSpawnInterval() => resourceSpawnInterval;
        public int GetPlantsPerSpawn() => plantsPerSpawn;
        public Vector2 GetWorldSize() => worldSize;

        // Setters cho điều chỉnh từ UI
        public void SetTargetPopulationSize(int value)
        {
            targetPopulationSize = Mathf.Clamp(value, 10, 200);
        }

        public void SetMaxPopulationSize(int value)
        {
            maxPopulationSize = Mathf.Clamp(value, 20, 500);
            // Đảm bảo max >= target
            if (maxPopulationSize < targetPopulationSize)
                maxPopulationSize = targetPopulationSize;
        }

        public void SetResourceSpawnInterval(float value)
        {
            resourceSpawnInterval = Mathf.Clamp(value, 0.5f, 10f);
            // Cập nhật InvokeRepeating
            CancelInvoke(nameof(SpawnResources));
            InvokeRepeating(nameof(SpawnResources), resourceSpawnInterval, resourceSpawnInterval);
        }

        public void SetPlantsPerSpawn(int value)
        {
            plantsPerSpawn = Mathf.Clamp(value, 1, 20);
        }

        public void SetWorldSize(Vector2 newSize)
        {
            worldSize = new Vector2(
                Mathf.Clamp(newSize.x, 10f, 50f),
                Mathf.Clamp(newSize.y, 10f, 50f)
            );
            // Cập nhật ranh giới thế giới
            UpdateWorldBounds();
        }

        public void SetBaseMetabolicRate(float value)
        {
            // Áp dụng cho tất cả sinh vật hiện tại
            foreach (var creature in activeCreatures)
            {
                if (creature != null)
                {
                    creature.SetBaseMetabolicRate(value);
                }
            }
        }

        /// <summary>
        /// Cập nhật ranh giới thế giới
        /// </summary>
        private void UpdateWorldBounds()
        {
            if (worldBounds == null) return;

            // Nếu chưa có collider, thêm mới
            BoxCollider2D[] colliders = worldBounds.GetComponents<BoxCollider2D>();
            if (colliders.Length == 0)
            {
                // Xóa EdgeCollider cũ nếu có
                EdgeCollider2D edge = worldBounds.GetComponent<EdgeCollider2D>();
                if (edge != null) Destroy(edge);

                // Thêm 4 BoxCollider cho 4 cạnh
                for (int i = 0; i < 4; i++) worldBounds.gameObject.AddComponent<BoxCollider2D>();
                colliders = worldBounds.GetComponents<BoxCollider2D>();
            }

            if (colliders.Length >= 4)
            {
                float width = worldSize.x;
                float height = worldSize.y;
                float thick = borderThickness;

                // Top
                colliders[0].size = new Vector2(width + thick * 2, thick);
                colliders[0].offset = new Vector2(0, height / 2 + thick / 2);

                // Bottom
                colliders[1].size = new Vector2(width + thick * 2, thick);
                colliders[1].offset = new Vector2(0, -height / 2 - thick / 2);

                // Left
                colliders[2].size = new Vector2(thick, height + thick * 2);
                colliders[2].offset = new Vector2(-width / 2 - thick / 2, 0);

                // Right
                colliders[3].size = new Vector2(thick, height + thick * 2);
                colliders[3].offset = new Vector2(width / 2 + thick / 2, 0);
            }
        }
    }
}

