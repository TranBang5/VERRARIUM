using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verrarium.Resources;
using Verrarium.Creature;
using Verrarium.Data;
using Verrarium.Evolution;
using Verrarium.World;
using Verrarium.Utils;
using Verrarium.Save;
using Verrarium.UI;
using Verrarium.DOTS.Evolution;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Verrarium.Core
{
    public enum ResourceRemovalReason
    {
        Unknown = 0,
        Consumed = 1,
        Decayed = 2
    }

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
        [SerializeField] private bool enablePlantPooling = true;
        [SerializeField, Min(0)] private int initialPlantPoolSize = 128;
        [SerializeField] private float resourceSpawnInterval = 5f; // Global spawn interval - thời gian giữa mỗi lần spawn tài nguyên
        [SerializeField] private int resourcesPerSpawn = 3; // Số lượng tài nguyên spawn mỗi interval
        [SerializeField] private int initialResources = 30; // Số lượng thực vật ban đầu khi khởi động giả lập
        [SerializeField] private float resourceDecayTime = 60f; // Thời gian resource tồn tại trước khi decay (giây)
        [SerializeField] private float minResourceDistance = 3f; // Khoảng cách tối thiểu giữa các tài nguyên
        [SerializeField] private int maxResources = 200; // Giới hạn số lượng tài nguyên cây tối đa trên toàn map
        
        // Legacy fields (cho tương thích với save/load và legacy spawn)
        [SerializeField] private int plantsPerSpawn = 2; // Dùng cho legacy spawn (khi không có hex grid)
        [SerializeField] private float resourceSpawnPopulationThreshold = 0.8f; // Dừng spawn resource khi dân số >= 80% max population (legacy)
        
        [Header("Hotspot Resource Logic")]
        [SerializeField] private int hotspotInitialFertilityPerGrid = 10; // Fertility ban đầu cho mỗi grid hotspot
        [SerializeField] private int hotspotMaxFertilityPerGrid = 10;     // Fertility tối đa cho mỗi grid hotspot
        [SerializeField, Min(0)] private int hotspotMaxResourcesPerGrid = 5; // Max plant per grid cho hotspot (dùng chung)
        [SerializeField, Min(0)] private int hotspotPlantsPerSpawnPerGrid = 1; // Mỗi interval, mỗi grid hotspot spawn tối đa N plant
        [SerializeField, Min(1)] private int maxPlantSpawnsPerFrame = 8; // Giới hạn số plant instantiate mỗi frame để tránh giật
        [SerializeField, Min(100)] private int maxQueuedPlantSpawns = 5000; // Tránh queue tăng vô hạn khi hệ thống bận
        [SerializeField, Min(0f)] private float decaySpawnBufferSeconds = 0.25f; // Đệm giữa decay và spawn để tránh spike cùng frame
        [SerializeField, Range(0f, 1f)] private float plantDecayTimeJitterPercent = 0.15f; // Jitter decay time để tránh decay đồng loạt
        [SerializeField] private bool enableHotspotSpawnDebugLogs = false;
        [SerializeField, Min(1)] private int hotspotSpawnDebugLogEveryCycles = 1;

        [Header("Fertility Time Recovery")]
        [SerializeField] private bool enableFertilityTimeRecovery = true;
        [SerializeField, Min(0.1f)] private float fertilityRecoveryIntervalSeconds = 10f; // Thời gian cơ bản để hồi fertility (giây)
        [SerializeField, Range(0.5f, 3f)] private float fertilityRecoveryIntervalRandomness = 1.5f; // Độ ngẫu nhiên cho interval
        [SerializeField, Min(1)] private int fertilityRecoveryAmount = 1; // Mỗi lần hồi, tăng fertility bao nhiêu

        [Header("Fertility Restore Triggers")]
        [SerializeField, Min(1)] private int plantDecayFertilityRestoreAmount = 1; // Plant decay -> hồi fertility cho chính grid đó
        [SerializeField, Min(0)] private int creatureDeathFertilityRestoreMin = 1; // Creature chết trong hotspot -> hồi fertility ngẫu nhiên 1 grid trong hotspot
        [SerializeField, Min(0)] private int creatureDeathFertilityRestoreMax = 3;
        
        [Header("Hotspot Settings")]
        [SerializeField] private int numberOfHotspots = 3; // Số lượng hotspot
        [SerializeField] private int gridsPerHotspot = 1; // Số lượng grid trong mỗi hotspot group
        [SerializeField] private int minHotspotDistance = 5; // Khoảng cách tối thiểu giữa các hotspot (số hex cells)
        
        [Header("Hotspot Test Case")]
        [SerializeField] private bool enableFixedHotspotTestCase = false; // Bật để dùng 3 hotspot cố định cho test
        [SerializeField, Min(0f)] private float fixedHotspotEdgeMargin = 1.5f; // Margin tránh đặt hotspot sát biên
        
        [Header("Drain Counter Settings")]
        [SerializeField] private float drainDecayInterval = 20f; // Thời gian decay drain counter (tự động = 4x spawn interval)

        [Header("World Settings")]
        [SerializeField] private Vector2 worldSize = new Vector2(20f, 20f);
        [SerializeField] private Transform worldBounds;
        [SerializeField] private bool enableWorldBorder = true; // Bật/tắt world border
        [SerializeField] private bool useHexGrid = true;
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private float borderThickness = 2f; // Độ dày của đường viền vật lý

        [Header("Pheromone Settings")]
        [SerializeField] private bool enablePheromones = false; // Toggle bật/tắt hệ thống pheromone

        [Header("Creature Spawn Settings")]
        [SerializeField] private bool spawnCreaturesNearHotspots = true; // Spawn sinh vật quanh hotspot (nếu có hex grid)
        [SerializeField, Min(0)] private int hotspotSpawnRadiusCells = 2; // Bán kính (tính theo số ô hex) quanh hotspot được phép spawn
        [SerializeField, Min(0f)] private float minCreatureSpawnDistance = 1.5f; // Khoảng cách tối thiểu giữa các sinh vật khi spawn để giảm va chạm
        [SerializeField, Min(1)] private int creatureSpawnAttemptsPerCreature = 25; // Số lần thử tìm vị trí hợp lệ cho mỗi sinh vật

        [Header("Fertile Areas")]
        [SerializeField] private List<Transform> fertileAreas = new List<Transform>();

        // Quần thể hiện tại
        private List<CreatureController> activeCreatures = new List<CreatureController>();

        // Tài nguyên
        private List<Resource> activeResources = new List<Resource>();

        // Spatial Hash Grids để tối ưu hóa queries
        private SpatialHashGrid<Resource> resourceGrid;
        private SpatialHashGrid<CreatureController> creatureGrid;
        private float spatialGridCellSize = 5f; // Kích thước cell cho spatial grid
        
        // Tracking tài nguyên theo hex grid
        private Dictionary<HexCoordinates, int> resourcesPerGridCell = new Dictionary<HexCoordinates, int>();
        private Dictionary<HexCoordinates, int> plantCountsByGridCell = new Dictionary<HexCoordinates, int>();
        private int currentPlantCount = 0;
        private Queue<HexCoordinates> pendingPlantSpawnQueue = new Queue<HexCoordinates>();
        private Queue<Resource> plantPool = new Queue<Resource>();
        private Transform plantPoolRoot;
        private float lastPlantDecayRealtime = -999f;
        private int spawnCycleCounter = 0;
        
        // Danh sách các hotspot (mỗi hotspot là 3 grid liền kề)
        private List<List<HexCoordinates>> hotspotGroups = new List<List<HexCoordinates>>();
        private Dictionary<HexCoordinates, int> hotspotGroupIndexByCoord = new Dictionary<HexCoordinates, int>();
        private int fixedCaseCenterHotspotGroupIndex = -1;
        private readonly List<HexCell> cachedHotspotResourceCells = new List<HexCell>();
        private bool hotspotResourceCellsDirty = true;

        // Thống kê
        private int totalCreaturesBorn = 0;
        private int totalCreaturesDied = 0;
        private float simulationTime = 0f;
        private System.DateTime simulationStartTime;
        
        [Header("Autosave Settings")]
        [SerializeField] private bool enableAutosave = true;
        [SerializeField] private float autosaveInterval = 600f; // 10 phút = 600 giây
        private float lastAutosaveTime = 0f;

        [Header("Warmup Settings")]
        [SerializeField] private bool enableWarmupPhase = true; // Bật warmup để spawn tài nguyên trước khi spawn sinh vật
        [SerializeField] private float warmupDuration = 30f;    // Thời gian warmup (giây)
        [SerializeField] private float warmupResourceInterval = 1f; // Interval spawn tài nguyên trong warmup (giây)

        [Header("Temp Save Settings")]
        [SerializeField] private bool loadTempSaveOnStart = false; // Nếu bật, sẽ tự động load file JSON duy nhất trong Assets/TempSave khi bắt đầu giả lập

        // Pause state
        private bool isPaused = false;
        public bool IsPaused => isPaused;
        public bool EnablePheromones => enablePheromones;
        public HexGrid HexGrid => hexGrid;
        // Speciation System
        [Header("Speciation Settings")]
        [SerializeField] private bool enableSpeciation = true;
        private SpeciationSystem speciationSystem;
        private GenusSystem genusSystem;

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
            // Initialize Speciation System
            if (enableSpeciation)
            {
                speciationSystem = new SpeciationSystem();
                genusSystem = new GenusSystem(speciationSystem);
            }
            
            // Ghi nhận thời điểm bắt đầu chạy giả lập
            simulationStartTime = System.DateTime.Now;

            InitializeWorld();
            
            // Khởi tạo Spatial Hash Grids
            resourceGrid = new SpatialHashGrid<Resource>(spatialGridCellSize, worldSize);
            creatureGrid = new SpatialHashGrid<CreatureController>(spatialGridCellSize, worldSize);
            InitializePlantPool();
            
            // Tìm HexGrid nếu chưa được gán
            if (useHexGrid && hexGrid == null)
            {
                hexGrid = FindFirstObjectByType<HexGrid>();
            }
            
            // Setup fertile areas trên hex grid
            if (useHexGrid && hexGrid != null)
            {
                SetupHexGridFertileAreas();
                InitializeGridFertility();
            }

            // Rebuild spatial grids định kỳ
            InvokeRepeating(nameof(RebuildSpatialGrids), 2f, 2f);
            
            // Khởi tạo autosave
            lastAutosaveTime = 0f;
            
            // Khởi tạo drainDecayInterval = 4x resourceSpawnInterval
            if (drainDecayInterval <= 0f)
            {
                drainDecayInterval = resourceSpawnInterval * 4f;
            }

            // Nếu được cấu hình, thử load save file duy nhất trong Assets/TempSave
            if (loadTempSaveOnStart)
            {
                TryLoadTempSaveFromAssets();
            }
            else
            {
                // Nếu bật warmup, spawn tài nguyên trước rồi mới spawn sinh vật
                if (enableWarmupPhase)
                {
                    StartCoroutine(WarmupRoutine());
                }
                else
                {
                    // Không dùng warmup: behavior cũ
                    SpawnInitialCreatures();
                    InvokeRepeating(nameof(SpawnResources), 1f, resourceSpawnInterval);
                }
            }
        }

        private void Update()
        {
            // Không cập nhật nếu đang pause
            // PauseMenu sẽ xử lý ESC key và gọi SetPaused()
            if (isPaused)
            {
                return;
            }

            simulationTime += Time.deltaTime;
            UpdatePopulation();
            UpdateAutosave();
            UpdateDrainCounterDecay();
            UpdateGridFertilityRestoration();
            UpdateGridCreaturePressure();
            ProcessPendingPlantSpawns();
        }

        /// <summary>
        /// Cập nhật áp lực sinh vật lên từng grid cell:
        /// - Nếu nhiều sinh vật đứng trên một cell trong thời gian dài, creaturePressure tăng dần.
        /// - Nếu ít hoặc không có sinh vật, creaturePressure giảm dần về 0.
        /// - creaturePressure sau đó được dùng để giảm capacity spawn tài nguyên (xem GetCurrentMaxCapacity).
        /// </summary>
        private void UpdateGridCreaturePressure()
        {
            if (!useHexGrid || hexGrid == null) return;
            if (activeCreatures == null || activeCreatures.Count == 0) return;

            // Xây dựng map số lượng sinh vật trên mỗi cell ở frame hiện tại
            Dictionary<HexCoordinates, int> creatureCounts = new Dictionary<HexCoordinates, int>();

            foreach (var creature in activeCreatures)
            {
                if (creature == null) continue;
                Vector2 pos = creature.transform.position;
                HexCoordinates coords = hexGrid.WorldToHex(pos);
                if (!creatureCounts.TryGetValue(coords, out int count))
                    count = 0;
                creatureCounts[coords] = count + 1;
            }

            float dt = Time.deltaTime;
            float increaseRatePerCreature = 0.5f;   // Mỗi sinh vật góp thêm áp lực mỗi giây
            float decayRate = 0.3f;                 // Tốc độ áp lực giảm khi vắng sinh vật
            float maxPressure = 5f;                 // Áp lực tối đa

            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                if (cell == null) continue;

                creatureCounts.TryGetValue(cell.Coordinates, out int countOnCell);

                if (countOnCell > 0)
                {
                    // Tăng áp lực theo số sinh vật
                    float delta = increaseRatePerCreature * countOnCell * dt;
                    cell.creaturePressure = Mathf.Clamp(cell.creaturePressure + delta, 0f, maxPressure);
                }
                else
                {
                    // Giảm áp lực khi không có sinh vật
                    float delta = decayRate * dt;
                    cell.creaturePressure = Mathf.Max(0f, cell.creaturePressure - delta);
                }

                cell.lastCreaturePressureUpdate = simulationTime;
            }
        }
        
        /// <summary>
        /// Cập nhật autosave - tự động lưu mỗi 10 phút
        /// </summary>
        private void UpdateAutosave()
        {
            if (!enableAutosave) return;
            
            if (simulationTime - lastAutosaveTime >= autosaveInterval)
            {
                PerformAutosave();
                lastAutosaveTime = simulationTime;
            }
        }
        
        /// <summary>
        /// Thực hiện autosave
        /// </summary>
        private void PerformAutosave()
        {
            if (Instance == null) return;
            
            // Đặt tên autosave theo format:
            // ngày chạy giả lập - thời điểm bắt đầu chạy giả lập - thời điểm lưu
            // và vẫn giữ prefix "autosave_" để tương thích với hệ thống hiện tại
            System.DateTime startTime = simulationStartTime;
            System.DateTime saveTime = System.DateTime.Now;
            string formattedTime = $"{startTime:yyyyMMdd}-{startTime:HHmmss}-{saveTime:HHmmss}";
            string autosaveName = $"{Save.SimulationSaveSystem.AUTOSAVE_NAME}_{formattedTime}";
            bool success = Save.SimulationSaveSystem.Save(autosaveName, this);
            
            if (success)
            {
                Debug.Log($"Autosave completed at {System.DateTime.Now:HH:mm:ss}");
            }
            else
            {
                Debug.LogWarning("Autosave failed!");
            }
        }

        /// <summary>
        /// Warmup: spawn tài nguyên nhanh trong một khoảng thời gian trước khi spawn sinh vật
        /// </summary>
        private IEnumerator WarmupRoutine()
        {
            // Bắt đầu spawn tài nguyên với interval warmup
            InvokeRepeating(nameof(SpawnResources), 1f, warmupResourceInterval);

            float elapsed = 0f;
            while (elapsed < warmupDuration)
            {
                // Không tăng thời gian nếu đang pause
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            // Chuyển sang interval spawn tài nguyên bình thường
            CancelInvoke(nameof(SpawnResources));
            InvokeRepeating(nameof(SpawnResources), resourceSpawnInterval, resourceSpawnInterval);

            // Sau khi tài nguyên đã được spawn trước, mới spawn sinh vật
            SpawnInitialCreatures();
        }

        /// <summary>
        /// Thử load một save file duy nhất từ thư mục Assets/TempSave.
        /// Giả định trong thư mục chỉ có một file .json hợp lệ.
        /// </summary>
        private void TryLoadTempSaveFromAssets()
        {
            try
            {
                string assetsPath = Application.dataPath;
                string tempSaveFolderPath = Path.Combine(assetsPath, "TempSave");

                if (!Directory.Exists(tempSaveFolderPath))
                {
                    Debug.LogWarning($"TempSave folder not found at path: {tempSaveFolderPath}");
                    return;
                }

                string[] jsonFiles = Directory.GetFiles(tempSaveFolderPath, "*.json");
                if (jsonFiles == null || jsonFiles.Length == 0)
                {
                    Debug.LogWarning($"No .json save file found in TempSave folder: {tempSaveFolderPath}");
                    return;
                }

                // Giả định chỉ có một file, nên lấy file đầu tiên
                string tempSaveFilePath = jsonFiles[0];

                string json = File.ReadAllText(tempSaveFilePath);
                Save.SimulationSaveData saveData = JsonUtility.FromJson<Save.SimulationSaveData>(json);
                if (saveData == null)
                {
                    Debug.LogError($"Failed to deserialize temp save file at path: {tempSaveFilePath}");
                    return;
                }

                LoadFromSaveData(saveData);
                Debug.Log($"Loaded simulation from temp save file: {tempSaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading temp save from Assets/TempSave: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Kiểm tra key press (hỗ trợ cả Input System và Legacy Input)
        /// </summary>
        private bool IsKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current == null)
                return false;

            if (!System.Enum.TryParse(keyCode.ToString(), true, out Key key))
                return false;

            KeyControl control = Keyboard.current[key];
            return control != null && control.wasPressedThisFrame;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!isPaused);
        }

        /// <summary>
        /// Set pause state
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f; // Dừng/chạy thời gian
            
            if (isPaused)
            {
                // Pause: Dừng tất cả InvokeRepeating
                CancelInvoke();
                
                // Dừng tất cả creatures
                foreach (var creature in activeCreatures)
                {
                    if (creature != null)
                    {
                        creature.SetPaused(true);
                    }
                }
                
                // PauseMenu sẽ tự xử lý việc hiển thị khi nhấn ESC
                // Không cần tìm và hiển thị ở đây để tránh conflict
            }
            else
            {
                // Unpause: Khởi động lại InvokeRepeating
                InvokeRepeating(nameof(SpawnResources), resourceSpawnInterval, resourceSpawnInterval);
                InvokeRepeating(nameof(RebuildSpatialGrids), 2f, 2f);
                
                // Tiếp tục tất cả creatures
                foreach (var creature in activeCreatures)
                {
                    if (creature != null)
                    {
                        creature.SetPaused(false);
                    }
                }
            }
        }

        /// <summary>
        /// Khởi tạo thế giới - tạo ranh giới nếu chưa có
        /// </summary>
        private void InitializeWorld()
        {
            if (!enableWorldBorder)
            {
                // Nếu border bị tắt, xóa worldBounds nếu có
                if (worldBounds != null)
                {
                    Destroy(worldBounds.gameObject);
                    worldBounds = null;
                }
                return;
            }

            if (worldBounds == null)
            {
                GameObject boundsObj = new GameObject("WorldBounds");
                boundsObj.transform.SetParent(transform);
                boundsObj.transform.position = Vector3.zero;

                // Tạo 4 BoxCollider2D cho 4 cạnh (solid walls)
                CreateWorldBoundaryColliders(boundsObj);

                worldBounds = boundsObj.transform;
            }
            else
            {
                // Đảm bảo colliders được cập nhật
                UpdateWorldBounds();
            }
        }

        /// <summary>
        /// Tạo các collider cho world boundary
        /// </summary>
        private void CreateWorldBoundaryColliders(GameObject boundsObj)
        {
            if (!enableWorldBorder) return;

            // Xóa tất cả components border cũ
            CleanupOldBorderComponents(boundsObj);

            // Xóa EdgeCollider cũ nếu có
            EdgeCollider2D edge = boundsObj.GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                if (Application.isPlaying)
                    Destroy(edge);
                else
                    DestroyImmediate(edge);
            }

            // Xóa BoxCollider cũ nếu có
            BoxCollider2D[] oldColliders = boundsObj.GetComponents<BoxCollider2D>();
            foreach (var col in oldColliders)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            // Xóa các wall objects cũ nếu có
            List<Transform> childrenToDestroy = new List<Transform>();
            foreach (Transform child in boundsObj.transform)
            {
                if (child.name.Contains("Wall"))
                {
                    childrenToDestroy.Add(child);
                }
            }
            foreach (var child in childrenToDestroy)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // Luôn tạo border hình chữ nhật
            CreateRectangularBorder(boundsObj);
        }

        /// <summary>
        /// Tạo border theo rìa hexgrid
        /// </summary>
        private void CreateHexGridBorder(GameObject boundsObj)
        {
            if (hexGrid == null || boundsObj == null) return;

            // Xóa các components cũ trước (không cần gọi lại vì đã được gọi trong CreateWorldBoundaryColliders)
            // CleanupOldBorderComponents(boundsObj);

            // Kiểm tra GameObject còn hợp lệ không
            if (boundsObj == null)
            {
                Debug.LogError("GameObject boundsObj đã bị destroy");
                return;
            }

            // Tìm tất cả các hex cells ở rìa
            List<Vector2> borderPoints = GetHexGridBorderPoints();
            
            if (borderPoints == null || borderPoints.Count == 0)
            {
                Debug.LogWarning("Không tìm thấy border points từ hexgrid, sử dụng border hình chữ nhật");
                CreateRectangularBorder(boundsObj);
                return;
            }

            // Tạo visual border với LineRenderer
            CreateHexGridBorderVisual(boundsObj, borderPoints);

            // Tạo colliders cho border
            CreateHexGridBorderColliders(boundsObj, borderPoints);
        }

        /// <summary>
        /// Xóa các components border cũ
        /// </summary>
        private void CleanupOldBorderComponents(GameObject boundsObj)
        {
            if (boundsObj == null) return;

            // Xóa LineRenderer cũ (bao gồm cả trong child objects)
            LineRenderer[] lineRenderers = boundsObj.GetComponentsInChildren<LineRenderer>(true);
            for (int i = lineRenderers.Length - 1; i >= 0; i--)
            {
                if (lineRenderers[i] != null)
                {
                    if (Application.isPlaying)
                        DestroyImmediate(lineRenderers[i]);
                    else
                        DestroyImmediate(lineRenderers[i]);
                }
            }

            // Xóa PolygonCollider2D cũ
            PolygonCollider2D[] polygonColliders = boundsObj.GetComponents<PolygonCollider2D>();
            for (int i = polygonColliders.Length - 1; i >= 0; i--)
            {
                if (polygonColliders[i] != null)
                {
                    if (Application.isPlaying)
                        DestroyImmediate(polygonColliders[i]);
                    else
                        DestroyImmediate(polygonColliders[i]);
                }
            }

            // Xóa child objects có tên "BorderLine"
            List<Transform> childrenToDestroy = new List<Transform>();
            foreach (Transform child in boundsObj.transform)
            {
                if (child.name == "BorderLine")
                {
                    childrenToDestroy.Add(child);
                }
            }
            foreach (var child in childrenToDestroy)
            {
                if (Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // Trong editor mode, force update để đảm bảo components đã được xóa
            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                EditorUtility.SetDirty(boundsObj);
                #endif
            }
        }

        /// <summary>
        /// Lấy các điểm border từ hexgrid
        /// </summary>
        private List<Vector2> GetHexGridBorderPoints()
        {
            List<Vector2> borderPoints = new List<Vector2>();
            
            if (hexGrid == null) return borderPoints;

            // Lấy tất cả cells
            var allCells = hexGrid.GetAllCells();
            if (allCells.Count == 0) return borderPoints;

            // Dictionary để lưu các cạnh border (edge -> count)
            Dictionary<string, List<Vector2>> borderEdges = new Dictionary<string, List<Vector2>>();

            // 6 hướng của hex (theo thứ tự: Right, TopRight, TopLeft, Left, BottomLeft, BottomRight)
            Vector2[] hexDirections = new Vector2[]
                {
                new Vector2(1, 0),           // Right
                new Vector2(0.5f, 0.866f),    // TopRight
                new Vector2(-0.5f, 0.866f),  // TopLeft
                new Vector2(-1, 0),          // Left
                new Vector2(-0.5f, -0.866f), // BottomLeft
                new Vector2(0.5f, -0.866f)   // BottomRight
            };

            float hexSize = hexGrid.HexSize;

            foreach (var cell in allCells)
            {
                Vector2 cellCenter = hexGrid.HexToWorld(cell.Coordinates);
                
                // Kiểm tra 6 neighbors
                var neighbors = hexGrid.GetNeighbors(cell.Coordinates);
                bool[] hasNeighbor = new bool[6];
                
                // 6 hướng lân cận trong hex grid
                HexCoordinates[] neighborDirs = new HexCoordinates[]
                {
                    new HexCoordinates(1, 0),   // Right
                    new HexCoordinates(1, -1),  // Top-right
                    new HexCoordinates(0, -1),  // Top-left
                    new HexCoordinates(-1, 0),  // Left
                    new HexCoordinates(-1, 1),  // Bottom-left
                    new HexCoordinates(0, 1)    // Bottom-right
                };

                for (int i = 0; i < 6; i++)
                {
                    HexCoordinates neighborCoords = new HexCoordinates(
                        cell.Coordinates.q + neighborDirs[i].q,
                        cell.Coordinates.r + neighborDirs[i].r
                    );
                    
                    HexCell neighbor = hexGrid.GetCell(neighborCoords);
                    hasNeighbor[i] = (neighbor != null);
                }

                // Với mỗi cạnh không có neighbor, thêm vào border
                for (int i = 0; i < 6; i++)
                {
                    if (!hasNeighbor[i])
                    {
                        // Tính toán 2 điểm của cạnh hex
                        int nextI = (i + 1) % 6;
                        float angle1 = (i * 60f - 30f) * Mathf.Deg2Rad;
                        float angle2 = (nextI * 60f - 30f) * Mathf.Deg2Rad;
                        
                        Vector2 point1 = cellCenter + new Vector2(
                            hexSize * Mathf.Cos(angle1),
                            hexSize * Mathf.Sin(angle1)
                        );
                        Vector2 point2 = cellCenter + new Vector2(
                            hexSize * Mathf.Cos(angle2),
                            hexSize * Mathf.Sin(angle2)
                        );

                        // Tạo key duy nhất cho cạnh (sắp xếp điểm để tránh trùng lặp)
                        string edgeKey = point1.x < point2.x || (point1.x == point2.x && point1.y < point2.y)
                            ? $"{point1.x:F3},{point1.y:F3}_{point2.x:F3},{point2.y:F3}"
                            : $"{point2.x:F3},{point2.y:F3}_{point1.x:F3},{point1.y:F3}";

                        if (!borderEdges.ContainsKey(edgeKey))
                        {
                            borderEdges[edgeKey] = new List<Vector2> { point1, point2 };
                        }
                    }
                }
            }

            // Chuyển đổi các cạnh thành danh sách điểm liên tục
            borderPoints = ConnectBorderEdges(borderEdges.Values.ToList());
            
            return borderPoints;
        }

        /// <summary>
        /// Kết nối các cạnh border thành một đường liên tục
        /// </summary>
        private List<Vector2> ConnectBorderEdges(List<List<Vector2>> edges)
        {
            if (edges.Count == 0) return new List<Vector2>();

            // Xây dựng graph của các điểm kết nối
            Dictionary<Vector2, List<Vector2>> pointConnections = new Dictionary<Vector2, List<Vector2>>();
            HashSet<Vector2> allPoints = new HashSet<Vector2>();

            foreach (var edge in edges)
            {
                if (edge.Count < 2) continue;
                
                Vector2 p1 = edge[0];
                Vector2 p2 = edge[1];
                
                // Làm tròn để tránh floating point errors
                p1 = new Vector2(Mathf.Round(p1.x * 100f) / 100f, Mathf.Round(p1.y * 100f) / 100f);
                p2 = new Vector2(Mathf.Round(p2.x * 100f) / 100f, Mathf.Round(p2.y * 100f) / 100f);
                
                allPoints.Add(p1);
                allPoints.Add(p2);

                if (!pointConnections.ContainsKey(p1))
                    pointConnections[p1] = new List<Vector2>();
                if (!pointConnections.ContainsKey(p2))
                    pointConnections[p2] = new List<Vector2>();

                // Chỉ thêm nếu chưa có
                if (!pointConnections[p1].Contains(p2))
                    pointConnections[p1].Add(p2);
                if (!pointConnections[p2].Contains(p1))
                    pointConnections[p2].Add(p1);
            }

            if (allPoints.Count == 0) return new List<Vector2>();

            // Tìm điểm bắt đầu (điểm có tọa độ y nhỏ nhất, nếu bằng nhau thì x nhỏ nhất)
            Vector2 startPoint = allPoints.OrderBy(p => p.y).ThenBy(p => p.x).First();

            // Xây dựng đường border bằng cách đi theo các điểm kết nối
            List<Vector2> borderPoints = new List<Vector2>();
            Vector2 currentPoint = startPoint;
            Vector2 previousPoint = startPoint + Vector2.left; // Hướng ban đầu là sang trái
            int maxIterations = allPoints.Count * 3; // Giới hạn để tránh vòng lặp vô hạn
            int iterations = 0;

            while (iterations < maxIterations)
            {
                borderPoints.Add(currentPoint);

                if (!pointConnections.ContainsKey(currentPoint) || pointConnections[currentPoint].Count == 0)
                    break;

                // Tìm điểm tiếp theo: chọn điểm có góc nhỏ nhất (theo chiều kim đồng hồ)
                Vector2 direction = (currentPoint - previousPoint).normalized;
                Vector2 nextPoint = currentPoint;
                float bestAngle = float.MaxValue;
                bool foundNext = false;

                foreach (var neighbor in pointConnections[currentPoint])
                {
                    if (Vector2.Distance(neighbor, previousPoint) < 0.01f) continue; // Không quay lại

                    Vector2 toNeighbor = (neighbor - currentPoint).normalized;
                    float angle = Vector2.SignedAngle(direction, toNeighbor);
                    
                    // Chọn góc dương nhỏ nhất (theo chiều kim đồng hồ)
                    if (angle < 0) angle += 360f;
                    if (angle < bestAngle && angle > 0.1f) // Bỏ qua góc quá nhỏ
                    {
                        bestAngle = angle;
                        nextPoint = neighbor;
                        foundNext = true;
                    }
                }

                // Nếu không tìm thấy điểm tốt, lấy điểm đầu tiên không phải previous
                if (!foundNext)
                {
                    foreach (var neighbor in pointConnections[currentPoint])
                    {
                        if (Vector2.Distance(neighbor, previousPoint) > 0.01f)
                        {
                            nextPoint = neighbor;
                            foundNext = true;
                            break;
                        }
                    }
                }

                // Nếu quay lại điểm đầu, đóng vòng
                if (Vector2.Distance(nextPoint, startPoint) < 0.1f && borderPoints.Count > 2)
                {
                    break;
                }

                if (!foundNext || nextPoint == currentPoint)
                    break;

                previousPoint = currentPoint;
                currentPoint = nextPoint;
                iterations++;
            }

            // Loại bỏ các điểm trùng lặp liên tiếp
            List<Vector2> cleanedPoints = new List<Vector2>();
            for (int i = 0; i < borderPoints.Count; i++)
            {
                if (i == 0 || Vector2.Distance(borderPoints[i], cleanedPoints[cleanedPoints.Count - 1]) > 0.01f)
                {
                    cleanedPoints.Add(borderPoints[i]);
                }
            }

            return cleanedPoints;
        }

        /// <summary>
        /// Tạo visual border cho hexgrid
        /// </summary>
        private void CreateHexGridBorderVisual(GameObject boundsObj, List<Vector2> borderPoints)
        {
            if (boundsObj == null || borderPoints == null || borderPoints.Count < 2)
            {
                if (boundsObj == null)
                    Debug.LogError("boundsObj is null trong CreateHexGridBorderVisual");
                if (borderPoints == null)
                    Debug.LogError("borderPoints is null trong CreateHexGridBorderVisual");
                if (borderPoints != null && borderPoints.Count < 2)
                    Debug.LogWarning($"borderPoints chỉ có {borderPoints.Count} điểm, cần ít nhất 2 điểm");
                return;
            }

            // Kiểm tra lại GameObject còn hợp lệ không
            if (boundsObj == null)
            {
                Debug.LogError("GameObject boundsObj đã bị destroy trước khi tạo LineRenderer");
                return;
            }

            // Đảm bảo không còn LineRenderer nào
            LineRenderer existingRenderer = boundsObj.GetComponent<LineRenderer>();
            if (existingRenderer != null)
            {
                if (Application.isPlaying)
                    Destroy(existingRenderer);
                else
                    DestroyImmediate(existingRenderer);
                
                // Đợi một frame trong play mode để Unity xử lý
                if (Application.isPlaying)
                {
                    // Trong play mode, có thể cần đợi một frame
                    // Nhưng vì đây là synchronous call, chúng ta sẽ thử lại ngay
                }
            }

            try
            {
                // Kiểm tra lại một lần nữa sau khi cleanup
                existingRenderer = boundsObj.GetComponent<LineRenderer>();
                if (existingRenderer != null)
                {
                    Debug.LogWarning("LineRenderer vẫn còn sau khi cleanup, force destroy");
                    if (Application.isPlaying)
                        DestroyImmediate(existingRenderer);
                    else
                        DestroyImmediate(existingRenderer);
                }

                // Tạo LineRenderer để vẽ border
                LineRenderer lineRenderer = boundsObj.AddComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    // Thử kiểm tra xem có vấn đề gì không
                    Component[] allComponents = boundsObj.GetComponents<Component>();
                    Debug.LogError($"Không thể tạo LineRenderer cho border. GameObject: {boundsObj.name}, Active: {boundsObj.activeSelf}, Components: {allComponents.Length}");
                    
                    // Thử tạo LineRenderer bằng cách khác
                    GameObject lineObj = new GameObject("BorderLine");
                    lineObj.transform.SetParent(boundsObj.transform);
                    lineObj.transform.localPosition = Vector3.zero;
                    lineRenderer = lineObj.AddComponent<LineRenderer>();
                    
                    if (lineRenderer == null)
                    {
                        Debug.LogError("Vẫn không thể tạo LineRenderer ngay cả với GameObject mới");
                        Destroy(lineObj);
                        return;
                    }
                }

                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.positionCount = borderPoints.Count + 1; // +1 để đóng vòng
                lineRenderer.useWorldSpace = true;
                lineRenderer.sortingOrder = 100;

                // Đặt các điểm
                for (int i = 0; i < borderPoints.Count; i++)
                {
                    lineRenderer.SetPosition(i, new Vector3(borderPoints[i].x, borderPoints[i].y, 0));
                }
                // Đóng vòng
                lineRenderer.SetPosition(borderPoints.Count, new Vector3(borderPoints[0].x, borderPoints[0].y, 0));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Lỗi khi tạo LineRenderer: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// Tạo colliders cho hexgrid border
        /// </summary>
        private void CreateHexGridBorderColliders(GameObject boundsObj, List<Vector2> borderPoints)
        {
            if (boundsObj == null || borderPoints == null || borderPoints.Count < 2) return;

            // Tạo một PolygonCollider2D để tạo border dày
            PolygonCollider2D polygonCollider = boundsObj.AddComponent<PolygonCollider2D>();
            if (polygonCollider == null)
            {
                Debug.LogError("Không thể tạo PolygonCollider2D cho border");
                return;
            }
            
            // Tạo một đường viền dày bằng cách offset các điểm
            List<Vector2> outerPoints = new List<Vector2>();
            List<Vector2> innerPoints = new List<Vector2>();
            
            float offset = borderThickness * 0.5f;
            
            for (int i = 0; i < borderPoints.Count; i++)
            {
                Vector2 current = borderPoints[i];
                Vector2 prev = borderPoints[(i - 1 + borderPoints.Count) % borderPoints.Count];
                Vector2 next = borderPoints[(i + 1) % borderPoints.Count];
                
                // Tính toán vector pháp tuyến
                Vector2 dir1 = (current - prev).normalized;
                Vector2 dir2 = (next - current).normalized;
                Vector2 normal1 = new Vector2(-dir1.y, dir1.x);
                Vector2 normal2 = new Vector2(-dir2.y, dir2.x);
                Vector2 avgNormal = (normal1 + normal2).normalized;
                
                outerPoints.Add(current + avgNormal * offset);
                innerPoints.Add(current - avgNormal * offset);
            }

            // Tạo path cho polygon (outer + inner reversed)
            List<Vector2> polygonPath = new List<Vector2>(outerPoints);
            innerPoints.Reverse();
            polygonPath.AddRange(innerPoints);
            
            polygonCollider.SetPath(0, polygonPath.ToArray());
            polygonCollider.isTrigger = false; // Solid wall
        }

        /// <summary>
        /// Tạo border hình chữ nhật (fallback)
        /// </summary>
        private void CreateRectangularBorder(GameObject boundsObj)
        {
            float width = worldSize.x;
            float height = worldSize.y;
            float thick = borderThickness;

            // Tạo visual border với LineRenderer
            CreateBorderVisual(boundsObj, width, height);

            // Top wall
            GameObject topWall = new GameObject("TopWall");
            topWall.transform.SetParent(boundsObj.transform);
            topWall.transform.localPosition = Vector3.zero;
            BoxCollider2D topCollider = topWall.AddComponent<BoxCollider2D>();
            topCollider.size = new Vector2(width + thick * 2, thick);
            topCollider.offset = new Vector2(0, height / 2 + thick / 2);
            topCollider.isTrigger = false; // Solid wall

            // Bottom wall
            GameObject bottomWall = new GameObject("BottomWall");
            bottomWall.transform.SetParent(boundsObj.transform);
            bottomWall.transform.localPosition = Vector3.zero;
            BoxCollider2D bottomCollider = bottomWall.AddComponent<BoxCollider2D>();
            bottomCollider.size = new Vector2(width + thick * 2, thick);
            bottomCollider.offset = new Vector2(0, -height / 2 - thick / 2);
            bottomCollider.isTrigger = false; // Solid wall

            // Left wall
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(boundsObj.transform);
            leftWall.transform.localPosition = Vector3.zero;
            BoxCollider2D leftCollider = leftWall.AddComponent<BoxCollider2D>();
            leftCollider.size = new Vector2(thick, height + thick * 2);
            leftCollider.offset = new Vector2(-width / 2 - thick / 2, 0);
            leftCollider.isTrigger = false; // Solid wall

            // Right wall
            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(boundsObj.transform);
            rightWall.transform.localPosition = Vector3.zero;
            BoxCollider2D rightCollider = rightWall.AddComponent<BoxCollider2D>();
            rightCollider.size = new Vector2(thick, height + thick * 2);
            rightCollider.offset = new Vector2(width / 2 + thick / 2, 0);
            rightCollider.isTrigger = false; // Solid wall
        }

        /// <summary>
        /// Tạo visual border với màu trắng
        /// </summary>
        private void CreateBorderVisual(GameObject boundsObj, float width, float height)
        {
            if (boundsObj == null) return;

            // Xóa LineRenderer cũ nếu có
            LineRenderer[] oldRenderers = boundsObj.GetComponents<LineRenderer>();
            foreach (var oldRenderer in oldRenderers)
            {
                if (Application.isPlaying)
                    Destroy(oldRenderer);
                else
                    DestroyImmediate(oldRenderer);
            }

            // Tạo LineRenderer để vẽ border
            LineRenderer lineRenderer = boundsObj.AddComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError("Không thể tạo LineRenderer cho border");
                return;
            }

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 5; // 4 góc + điểm đầu để đóng vòng
            lineRenderer.useWorldSpace = false;
            lineRenderer.sortingOrder = 100; // Đảm bảo hiển thị trên cùng

            // Tạo các điểm cho border (hình chữ nhật)
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            
            lineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0)); // Bottom-left
            lineRenderer.SetPosition(1, new Vector3(halfWidth, -halfHeight, 0));  // Bottom-right
            lineRenderer.SetPosition(2, new Vector3(halfWidth, halfHeight, 0));    // Top-right
            lineRenderer.SetPosition(3, new Vector3(-halfWidth, halfHeight, 0));   // Top-left
            lineRenderer.SetPosition(4, new Vector3(-halfWidth, -halfHeight, 0));  // Đóng vòng (về bottom-left)
        }

        /// <summary>
        /// Sinh ra các thực vật ban đầu - chỉ spawn trong hotspot
        /// </summary>
        private void SpawnInitialResources()
        {
            if (plantPrefab == null) return;
            
            // Tính số lượng tài nguyên cần spawn
            int resourcesToSpawn = Mathf.Min(initialResources, maxResources);
            
            // Rule: chỉ hotspot mới có thể spawn tài nguyên. Không có hex grid/hotspot => không spawn.
            if (!useHexGrid || hexGrid == null)
            {
                Debug.LogWarning($"SpawnInitialResources: Không spawn vì rule hotspot-only (useHexGrid={useHexGrid}, hexGrid={hexGrid}).");
                return;
            }

            // Kiểm tra hotspot groups
            if (hotspotGroups == null || hotspotGroups.Count == 0)
            {
                Debug.LogError("Không có hotspot groups! Có thể SetupHexGridFertileAreas() chưa được gọi hoặc không tạo được hotspot.");
                return;
            }
            
            Debug.Log($"SpawnInitialResources: Có {hotspotGroups.Count} hotspot groups, cần spawn {resourcesToSpawn} tài nguyên");
            
            // Debug: Log vị trí của các hotspot groups
            for (int i = 0; i < hotspotGroups.Count; i++)
            {
                var group = hotspotGroups[i];
                Debug.Log($"Hotspot group {i} có {group.Count} cells:");
                foreach (var coord in group)
                {
                    HexCell cell = hexGrid.GetCell(coord);
                    if (cell != null)
                    {
                        Vector2 worldPos = hexGrid.HexToWorld(coord);
                        Debug.Log($"  - Cell {coord}: worldPos = {worldPos}");
                    }
                }
            }
            
            // Spawn tài nguyên: spawn tuần tự từng hotspot group (hotspot 1 -> 2 -> 3 -> quay lại 1)
            // Đảm bảo spawn đủ số lượng initialResources (bỏ qua interval và spawn đủ)
            int spawnedCount = 0;
            int maxAttempts = resourcesToSpawn * 100; // Tăng số lần thử để đảm bảo spawn đủ
            int attempts = 0;

            // Tập hợp tất cả hotspot cells có thể spawn
            List<HexCell> allSpawnableCells = BuildHotspotResourceCells();
            
            if (allSpawnableCells.Count == 0)
            {
                Debug.LogError("Không có hotspot cells hợp lệ để spawn initial resources!");
                return;
            }
            
            // Spawn luân phiên giữa các cells để đảm bảo đều
            bool canSpawnMore = true;
            while (spawnedCount < resourcesToSpawn && canSpawnMore && attempts < maxAttempts)
            {
                canSpawnMore = false;
                
                // Spawn 1 tài nguyên ở mỗi cell (nếu còn chỗ)
                foreach (var cell in allSpawnableCells)
                {
                    if (spawnedCount >= resourcesToSpawn) break;
                    
                    int currentCapacity = GetCurrentMaxCapacity(cell);
                    int currentResources = GetResourceCountInGrid(cell.Coordinates);
                    
                    if (currentResources < currentCapacity)
                    {
                        attempts++;
                        if (TrySpawnInGrid(cell, ref spawnedCount, resourcesToSpawn, ref attempts, maxAttempts))
                        {
                            canSpawnMore = true; // Có thể spawn thêm ở lần lặp tiếp theo
                        }
                    }
                }
                
                // Nếu không spawn được thêm gì, thoát
                if (!canSpawnMore)
                {
                    break;
                }
            }

            // Không fallback ra ngoài hotspot: nếu không spawn đủ thì chấp nhận thiếu.

            // Log kết quả
            if (spawnedCount < resourcesToSpawn)
            {
                Debug.LogWarning($"Không thể spawn đủ tài nguyên ban đầu theo rule hotspot-only: {spawnedCount}/{resourcesToSpawn}. Có thể do hotspot quá nhỏ/đầy hoặc maxResources quá thấp.");
            }
            else
            {
                Debug.Log($"Đã spawn {spawnedCount} tài nguyên ban đầu thành công tại {hotspotGroups.Count} hotspot groups.");
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

            int countToSpawn = targetPopulationSize / 2;

            // Nếu có hex grid + hotspot, spawn quanh hotspot với khoảng cách tối thiểu
            if (spawnCreaturesNearHotspots && useHexGrid && hexGrid != null && hotspotGroups != null && hotspotGroups.Count > 0)
            {
                List<Vector2> spawnedPositions = new List<Vector2>(countToSpawn);
                List<HexCell> candidateCells;
                if (enableFixedHotspotTestCase && fixedCaseCenterHotspotGroupIndex >= 0)
                {
                    candidateCells = BuildSpawnCandidateCellsFromHotspotGroup(fixedCaseCenterHotspotGroupIndex, hotspotSpawnRadiusCells);
                    if (candidateCells.Count == 0)
                    {
                        candidateCells = BuildHotspotSpawnCandidateCells(hotspotSpawnRadiusCells);
                    }
                }
                else
                {
                    candidateCells = BuildHotspotSpawnCandidateCells(hotspotSpawnRadiusCells);
                }

                for (int i = 0; i < countToSpawn; i++)
                {
                    Vector2 pos;
                    bool found = TryGetSpawnPositionFromCells(candidateCells, spawnedPositions, out pos);
                    if (!found)
                    {
                        pos = GetRandomPosition();
                    }

                    SpawnCreature(pos, Genome.CreateDefault());
                    spawnedPositions.Add(pos);
                }

                return;
            }

            // Fallback: behavior cũ
            for (int i = 0; i < countToSpawn; i++)
            {
                SpawnCreature(GetRandomPosition(), Genome.CreateDefault());
            }
        }

        private List<HexCell> BuildHotspotSpawnCandidateCells(int radiusCells)
        {
            List<HexCell> candidates = new List<HexCell>();
            if (hexGrid == null || hotspotGroups == null || hotspotGroups.Count == 0) return candidates;

            var allCells = hexGrid.GetAllCells();
            if (allCells == null || allCells.Count == 0) return candidates;

            // Gom tất cả hotspot coords để tính khoảng cách nhanh
            List<HexCoordinates> hotspotCoords = new List<HexCoordinates>();
            foreach (var group in hotspotGroups)
            {
                if (group == null) continue;
                foreach (var coord in group)
                {
                    hotspotCoords.Add(coord);
                }
            }

            foreach (var cell in allCells)
            {
                int minDist = int.MaxValue;
                foreach (var coord in hotspotCoords)
                {
                    int d = cell.Coordinates.DistanceTo(coord);
                    if (d < minDist) minDist = d;
                    if (minDist == 0) break;
                }

                if (minDist != int.MaxValue && minDist <= radiusCells)
                {
                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        private bool TryGetSpawnPositionFromCells(List<HexCell> candidateCells, List<Vector2> alreadySpawned, out Vector2 position)
        {
            position = Vector2.zero;
            if (candidateCells == null || candidateCells.Count == 0) return false;

            int attempts = Mathf.Max(1, creatureSpawnAttemptsPerCreature);
            for (int i = 0; i < attempts; i++)
            {
                HexCell cell = candidateCells[Random.Range(0, candidateCells.Count)];
                Vector2 center = hexGrid.HexToWorld(cell.Coordinates);

                // Spawn trong phạm vi bên trong cell để tránh sát rìa
                Vector2 candidate = center + Random.insideUnitCircle * (hexGrid.HexSize * 0.4f);
                candidate = ClampToWorldBounds(candidate);

                bool tooClose = false;
                float minDist = Mathf.Max(0f, minCreatureSpawnDistance);
                for (int j = 0; j < alreadySpawned.Count; j++)
                {
                    if (Vector2.Distance(candidate, alreadySpawned[j]) < minDist)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        private List<HexCell> BuildSpawnCandidateCellsFromHotspotGroup(int groupIndex, int radiusCells)
        {
            List<HexCell> candidates = new List<HexCell>();
            if (hexGrid == null || hotspotGroups == null || hotspotGroups.Count == 0) return candidates;
            if (groupIndex < 0 || groupIndex >= hotspotGroups.Count) return candidates;

            var allCells = hexGrid.GetAllCells();
            if (allCells == null || allCells.Count == 0) return candidates;

            var group = hotspotGroups[groupIndex];
            if (group == null || group.Count == 0) return candidates;

            foreach (var cell in allCells)
            {
                int minDist = int.MaxValue;
                for (int i = 0; i < group.Count; i++)
                {
                    int d = cell.Coordinates.DistanceTo(group[i]);
                    if (d < minDist) minDist = d;
                    if (minDist == 0) break;
                }

                if (minDist != int.MaxValue && minDist <= radiusCells)
                {
                    candidates.Add(cell);
                }
            }

            return candidates;
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

                int genusId = -1;
                int speciesIdInGenus = -1;
                var brainForSpeciation = controller.GetBrain();

                // Phân loại Genus/Species nếu có brain và speciation enabled
                if (enableSpeciation && genusSystem != null && brainForSpeciation != null)
                {
                    // Sinh vật khởi tạo thế hệ 0
                    int generationIndex = lineageRecord != null ? lineageRecord.GenerationIndex : 0;
                    (genusId, speciesIdInGenus) = genusSystem.Classify(genome, brainForSpeciation, generationIndex);
                }

                if (lineageRecord == null)
                {
                    // Tạo lineage record với Genus/Species ID
                    lineageRecord = Data.CreatureLineageRegistry.CreateRecord(genome, null, genusId, speciesIdInGenus);
                }
                else if (enableSpeciation && genusSystem != null && brainForSpeciation != null)
                {
                    // Khi load từ save: đăng ký lại vào GenusSystem dựa trên Genus/Species và thế hệ trong lineage
                    genusSystem.RegisterExisting(lineageRecord.GenusId, lineageRecord.SpeciesId, genome, brainForSpeciation, lineageRecord.GenerationIndex);
                }

                controller.SetLineageRecord(lineageRecord);
                Data.CreatureLineageRegistry.Bind(controller, lineageRecord);
                activeCreatures.Add(controller);

                // Hybrid DOTS brain: tạo entity cho creature để DOTS tính output.
                DOTSCreatureBrainBridge.Instance.RegisterCreature(controller);
                
                // Thêm vào spatial grid
                if (creatureGrid != null)
                {
                    creatureGrid.Add(controller);
                }
                
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

            // Hybrid DOTS brain: hủy entity tương ứng
            DOTSCreatureBrainBridge.Instance.UnregisterCreature(creature);
            
            // Xóa khỏi spatial grid
            if (creatureGrid != null)
            {
                creatureGrid.Remove(creature);
            }
            
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
                    
                    // Thiết lập decay time cho thịt (tương tự cây)
                    meat.SetDecayTime(resourceDecayTime);
                    
                    activeResources.Add(meat);
                    
                    // Thêm vào spatial grid
                    if (resourceGrid != null)
                    {
                        resourceGrid.Add(meat);
                    }
                }
            }

            // Creature chết trong hotspot -> hồi fertility cho 1 grid ngẫu nhiên trong hotspot đó
            RestoreFertilityOnCreatureDeath(deathPosition);
        }

        /// <summary>
        /// Xử lý sinh sản - tạo trứng mới
        /// </summary>
        public void OnCreatureReproduction(CreatureController parent, Vector2 position, Genome parentGenome)
        {
            // Tách mutation rate: genome vs brain (NEAT). Save cũ: brainMutationRate = 0 -> fallback theo mutationRate.
            int numGenomeMutations = PoissonRandom(parentGenome.mutationRate);
            float brainLambda = parentGenome.brainMutationRate > 0f ? parentGenome.brainMutationRate : parentGenome.mutationRate;
            int numBrainMutations = PoissonRandom(brainLambda);

            // Mỗi thuộc tính genome chỉ đột biến tối đa 1 lần: truyền numGenomeMutations vào Mutate để chọn ngẫu nhiên từng ấy thuộc tính khác nhau
            Genome childGenome = Genome.Mutate(parentGenome, 0.1f, numGenomeMutations);

            // Sao chép và đột biến bộ não
            Evolution.NEATNetwork childBrain = null;
            if (parent != null)
            {
                Evolution.NEATNetwork parentBrain = parent.GetBrain();
                if (parentBrain != null)
                {
                    childBrain = new Evolution.NEATNetwork(parentBrain);
                    
                    // Áp dụng đột biến NEAT đầy đủ
                    Evolution.NEATMutator.Mutate(childBrain, numBrainMutations);
                }
            }

            Data.CreatureLineageRecord parentRecord = Data.CreatureLineageRegistry.Get(parent);
            
            // Classify child to Genus/Species (dựa trên GenerationIndex = parent.GenerationIndex + 1)
            int childGenusId = -1;
            int childSpeciesIdInGenus = -1;
            if (enableSpeciation && genusSystem != null && childBrain != null)
            {
                int childGenerationIndex = parentRecord != null ? parentRecord.GenerationIndex + 1 : 0;
                (childGenusId, childSpeciesIdInGenus) = genusSystem.Classify(childGenome, childBrain, childGenerationIndex);
            }
            
            // Tạo lineage record với Genus/Species ID
            Data.CreatureLineageRecord childRecord = Data.CreatureLineageRegistry.CreateRecord(childGenome, parentRecord, childGenusId, childSpeciesIdInGenus);

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
        /// Khởi tạo fertility/capacity cho hotspot grids.
        /// Rule hiện tại: chỉ hotspot grids spawn plant, các grid khác fertilityLevel=0 để tránh dùng nhầm.
        /// </summary>
        private void InitializeGridFertility()
        {
            if (hexGrid == null) return;

            // Rebuild index map cho hotspot coords -> group index
            RebuildHotspotIndex();

            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                if (cell == null) continue;

                bool isHotspot = hotspotGroupIndexByCoord.ContainsKey(cell.Coordinates);
                if (isHotspot)
                {
                    cell.fertilityLevel = Mathf.Clamp(hotspotInitialFertilityPerGrid, 0, hotspotMaxFertilityPerGrid);
                    cell.baseMaxCapacity = Mathf.Max(0, hotspotMaxResourcesPerGrid);
                }
                else
                {
                    cell.fertilityLevel = 0;
                    cell.baseMaxCapacity = 0;
                }

                // Drain counter vẫn dùng để giảm capacity khi bị ăn (nếu bạn muốn giữ cơ chế này)
                cell.drainCounter = 0;
                cell.lastDrainTime = 0f;

                // Time recovery schedule
                float minInterval = fertilityRecoveryIntervalSeconds;
                float maxInterval = fertilityRecoveryIntervalSeconds * fertilityRecoveryIntervalRandomness;
                cell.recoveryInterval = Random.Range(minInterval, maxInterval);
                cell.nextFertilityRestoreTime = simulationTime + cell.recoveryInterval;

                resourcesPerGridCell[cell.Coordinates] = 0;
            }
        }
        
        /// <summary>
        /// Cập nhật drain counter decay cho các grid
        /// </summary>
        private void UpdateDrainCounterDecay()
        {
            if (hexGrid == null) return;
            
            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                // Decay drain counter nếu đã qua drainDecayInterval
                if (cell.drainCounter > 0 && simulationTime - cell.lastDrainTime >= drainDecayInterval)
                {
                    cell.drainCounter = Mathf.Max(0, cell.drainCounter - 1);
                    cell.lastDrainTime = simulationTime; // Reset timer sau mỗi lần decay
                }
            }
        }
        
        /// <summary>
        /// Cơ chế hồi fertility theo thời gian đã bị tắt.
        /// Fertility chỉ hồi khi plant decay hoặc creature chết trong hotspot (xem RestoreFertilityOn*).
        /// </summary>
        private void UpdateGridFertilityRestoration()
        {
            if (!enableFertilityTimeRecovery) return;
            if (!useHexGrid || hexGrid == null) return;
            if (hotspotGroups == null || hotspotGroups.Count == 0) return;
            if (fertilityRecoveryAmount <= 0) return;

            var hotspotCells = BuildHotspotResourceCells();
            if (hotspotCells.Count == 0) return;

            float minInterval = fertilityRecoveryIntervalSeconds;
            float maxInterval = fertilityRecoveryIntervalSeconds * fertilityRecoveryIntervalRandomness;

            foreach (var cell in hotspotCells)
            {
                if (cell == null) continue;

                if (simulationTime >= cell.nextFertilityRestoreTime)
                {
                    cell.fertilityLevel = Mathf.Clamp(
                        cell.fertilityLevel + fertilityRecoveryAmount,
                        0,
                        hotspotMaxFertilityPerGrid
                    );

                    cell.recoveryInterval = Random.Range(minInterval, maxInterval);
                    cell.nextFertilityRestoreTime = simulationTime + cell.recoveryInterval;
                }
            }
        }
        
        /// <summary>
        /// Lấy max capacity hiện tại của grid:
        /// baseMaxCapacity - drainCounter - creaturePressureFactor
        /// </summary>
        private int GetCurrentMaxCapacity(HexCell cell)
        {
            if (cell == null) return 0;

            // Áp dụng penalty do tài nguyên bị ăn (drainCounter)
            int capacity = cell.baseMaxCapacity - cell.drainCounter;

            // Áp dụng penalty thêm do sinh vật đứng trên cell quá lâu (creaturePressure)
            // creaturePressure [0, maxPressure] được scale thành [0, maxPressureCapacityPenalty]
            float maxPressureCapacityPenalty = 3f;
            float pressure01 = Mathf.Clamp01(cell.creaturePressure / 5f); // 5f khớp với maxPressure trong UpdateGridCreaturePressure
            int pressurePenalty = Mathf.RoundToInt(maxPressureCapacityPenalty * pressure01);

            capacity -= pressurePenalty;

            return Mathf.Max(0, capacity);
        }
        
        /// <summary>
        /// Lấy số lượng tài nguyên trong một grid cell
        /// </summary>
        private int GetResourceCountInGrid(HexCoordinates coords)
        {
            if (hexGrid == null || resourceGrid == null) return 0;
            
            int count = 0;
            Vector2 gridCenter = hexGrid.HexToWorld(coords);
            float gridRadius = hexGrid.HexSize * 0.6f; // Khoảng cách để xem xét tài nguyên trong grid
            
            // Đếm tài nguyên trong grid
            foreach (var resource in activeResources)
            {
                if (resource == null) continue;
                float distance = Vector2.Distance(resource.transform.position, gridCenter);
                if (distance <= gridRadius)
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Lấy số lượng plant trong một grid cell (không tính meat) để áp dụng max per grid cho plant.
        /// </summary>
        private int GetPlantCountInGrid(HexCoordinates coords)
        {
            if (!useHexGrid || hexGrid == null) return 0;
            return plantCountsByGridCell.TryGetValue(coords, out int count) ? count : 0;
        }
        
        /// <summary>
        /// Sinh tài nguyên định kỳ - Logic mới: mỗi grid có fertility riêng
        /// </summary>
        /// <summary>
        /// Đếm số lượng tài nguyên cây (không tính thịt)
        /// </summary>
        private int GetPlantCount()
        {
            return currentPlantCount;
        }
        
        private void SpawnResources()
        {
            spawnCycleCounter++;

            // Dọn dẹp null entries trước khi kiểm tra
            CompactResourcesAndRefreshTracking();
            
            // Kiểm tra giới hạn số lượng tài nguyên cây toàn cục (không tính thịt)
            if (currentPlantCount >= maxResources)
            {
                return; // Không spawn thêm nếu đã đạt max
            }

            // Rule: chỉ hotspot mới có thể spawn tài nguyên. Không có hex grid/hotspot => không spawn.
            if (!useHexGrid || hexGrid == null)
            {
                Debug.LogWarning($"SpawnResources: Không spawn vì rule hotspot-only (useHexGrid={useHexGrid}, hexGrid={hexGrid}).");
                return;
            }

            // Chỉ duyệt qua hotspot cells và spawn tài nguyên dựa trên:
            // - Max plant per grid (dùng chung cho hotspot)
            // - Fertility level của từng grid (tiêu hao khi spawn)
            var allCells = BuildHotspotResourceCells();
            if (allCells.Count == 0)
            {
                return;
            }

            // Scale spawn theo dân số (giữ lại setting legacy để vẫn chỉnh được trong Inspector)
            int currentPopulation = activeCreatures.Count;
            float populationRatio = 0f;
            if (targetPopulationSize > 0)
            {
                populationRatio = Mathf.Clamp01((float)currentPopulation / targetPopulationSize);
            }
            float spawnMultiplier = 1f;
            if (populationRatio >= resourceSpawnPopulationThreshold)
            {
                float denom = Mathf.Max(0.0001f, 1f - resourceSpawnPopulationThreshold);
                float excessRatio = (populationRatio - resourceSpawnPopulationThreshold) / denom;
                spawnMultiplier = Mathf.Max(0f, 1f - Mathf.Clamp01(excessRatio));
            }
            if (spawnMultiplier <= 0f) return;

            // Bước 1: Tạo spawn tickets theo từng hotspot group.
            var groupTickets = new Dictionary<int, Queue<HexCoordinates>>();
            var plannedByGroup = new Dictionary<int, int>();
            var eligibleCellsByGroup = new Dictionary<int, int>();
            int plannedPendingCount = pendingPlantSpawnQueue.Count;

            int groupCount = hotspotGroups != null ? hotspotGroups.Count : 0;
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                var group = hotspotGroups[groupIndex];
                if (group == null || group.Count == 0) continue;

                // Xáo thứ tự cell trong mỗi group để tránh thiên vị cell đầu group
                var shuffledCoords = group.OrderBy(_ => Random.value).ToList();
                foreach (var coord in shuffledCoords)
                {
                    HexCell cell = hexGrid.GetCell(coord);
                    if (cell == null || cell.isObstacle) continue;
                    if (cell.fertilityLevel <= 0) continue;

                    int currentCapacity = GetCurrentMaxCapacity(cell);
                    int currentPlantsInCell = GetPlantCountInGrid(cell.Coordinates);
                    if (currentPlantsInCell >= currentCapacity) continue;

                    if (!eligibleCellsByGroup.ContainsKey(groupIndex)) eligibleCellsByGroup[groupIndex] = 0;
                    eligibleCellsByGroup[groupIndex]++;

                    int maxSpawnThisInterval = Mathf.Max(0, hotspotPlantsPerSpawnPerGrid);
                    maxSpawnThisInterval = Mathf.Max(0, Mathf.FloorToInt(maxSpawnThisInterval * spawnMultiplier));
                    if (maxSpawnThisInterval <= 0) continue;

                    int availableByFertility = cell.fertilityLevel;
                    int availableByCapacity = Mathf.Max(0, currentCapacity - currentPlantsInCell);
                    // Không áp quota global/queue ở bước planning để tránh group đầu "ăn hết" suất.
                    // Quota global/queue sẽ được áp ở bước enqueue round-robin bên dưới.
                    int spawnCount = Mathf.Min(maxSpawnThisInterval, availableByFertility, availableByCapacity);
                    if (spawnCount <= 0) continue;

                    if (!groupTickets.TryGetValue(groupIndex, out Queue<HexCoordinates> tickets))
                    {
                        tickets = new Queue<HexCoordinates>();
                        groupTickets[groupIndex] = tickets;
                    }

                    for (int i = 0; i < spawnCount; i++)
                    {
                        tickets.Enqueue(cell.Coordinates);
                    }
                    if (!plannedByGroup.ContainsKey(groupIndex)) plannedByGroup[groupIndex] = 0;
                    plannedByGroup[groupIndex] += spawnCount;
                    plannedPendingCount += spawnCount;
                }
            }

            if (groupTickets.Count == 0) return;

            // Bước 2: Enqueue round-robin giữa các hotspot group để tránh 1 group ăn hết queue cap.
            List<int> orderedGroups = new List<int>(groupTickets.Keys);
            if (orderedGroups.Count > 1)
            {
                // xáo nhẹ thứ tự để tránh group id nhỏ luôn đi trước
                orderedGroups = orderedGroups.OrderBy(_ => Random.value).ToList();
            }

            bool canContinue = true;
            var enqueuedByGroup = new Dictionary<int, int>();
            while (canContinue)
            {
                canContinue = false;
                foreach (int groupIndex in orderedGroups)
                {
                    if (pendingPlantSpawnQueue.Count >= maxQueuedPlantSpawns) break;
                    if ((currentPlantCount + pendingPlantSpawnQueue.Count) >= maxResources) break;

                    Queue<HexCoordinates> tickets = groupTickets[groupIndex];
                    if (tickets == null || tickets.Count == 0) continue;

                    HexCoordinates coord = tickets.Dequeue();
                    HexCell cell = hexGrid.GetCell(coord);
                    if (cell == null) continue;
                    if (cell.fertilityLevel <= 0) continue;

                    pendingPlantSpawnQueue.Enqueue(coord);
                    // Reserve fertility tại thời điểm enqueue.
                    cell.fertilityLevel = Mathf.Max(0, cell.fertilityLevel - 1);
                    if (!enqueuedByGroup.ContainsKey(groupIndex)) enqueuedByGroup[groupIndex] = 0;
                    enqueuedByGroup[groupIndex]++;
                    canContinue = true;
                }
            }

            if (enableHotspotSpawnDebugLogs &&
                hotspotSpawnDebugLogEveryCycles > 0 &&
                spawnCycleCounter % hotspotSpawnDebugLogEveryCycles == 0)
            {
                string plannedStr = FormatGroupCounts(plannedByGroup);
                string enqueuedStr = FormatGroupCounts(enqueuedByGroup);
                string eligibleStr = FormatGroupCounts(eligibleCellsByGroup);
                Debug.Log(
                    $"[HotspotSpawn][Cycle {spawnCycleCounter}] groups={hotspotGroups.Count}, cells={allCells.Count}, plants={currentPlantCount}, queue={pendingPlantSpawnQueue.Count}/{maxQueuedPlantSpawns}, eligible={eligibleStr}, planned={plannedStr}, enqueued={enqueuedStr}"
                );
            }
        }
        
        /// <summary>
        /// Thử spawn tài nguyên trong một grid
        /// </summary>
        private bool TrySpawnInGrid(HexCell cell, ref int totalSpawned, int maxToSpawn, ref int attempts, int maxAttempts)
        {
            int currentCapacity = GetCurrentMaxCapacity(cell);
            int currentResources = GetResourceCountInGrid(cell.Coordinates);
            
            if (currentResources >= currentCapacity)
            {
                return false; // Grid đã đầy
            }

            // Tạo vị trí spawn ngẫu nhiên trong grid
            Vector2 gridCenter = hexGrid.HexToWorld(cell.Coordinates);
            Vector2 spawnPos = gridCenter + Random.insideUnitCircle * (hexGrid.HexSize * 0.4f);
            // Với hex grid, không clamp theo world bounds để tránh bị dồn về góc; chỉ clamp khi không dùng hex grid
            if (!useHexGrid)
            {
                spawnPos = ClampToWorldBounds(spawnPos);
            }
            
            // Kiểm tra khoảng cách tối thiểu với tài nguyên khác bằng spatial grid query
            bool tooClose = IsTooCloseToExistingResource(spawnPos, minResourceDistance);
            
            if (!tooClose && totalSpawned < maxToSpawn)
            {
                SpawnPlant(spawnPos);
                totalSpawned++;
                return true;
            }
            
            attempts++;
            return false;
        }
        
        /// <summary>
        /// Logic spawn cũ (fallback khi không có hex grid)
        /// </summary>
        private void SpawnResourcesLegacy()
        {
            // Rule: hotspot-only. Legacy spawn bị vô hiệu hoá để tránh spawn ngoài hotspot.
            Debug.LogWarning("SpawnResourcesLegacy: Bị vô hiệu hoá vì rule hotspot-only.");
            return;
        }

        /// <summary>
        /// Sinh một cây
        /// </summary>
        private void SpawnPlant(Vector2 position)
        {
            if (plantPrefab == null) return;
            Resource plant = AcquirePlantResource(position);
            if (plant != null)
            {
                // Thiết lập decay time cho resource
                plant.SetDecayTime(GetPlantDecayTimeWithJitter());
                
                activeResources.Add(plant);
                IncrementPlantTracking(position);
                
                // Thêm vào spatial grid
                if (resourceGrid != null)
                {
                    resourceGrid.Add(plant);
                }
            }
        }

        private void InitializePlantPool()
        {
            if (!enablePlantPooling || plantPrefab == null) return;
            if (plantPoolRoot == null)
            {
                GameObject root = new GameObject("PlantPool");
                plantPoolRoot = root.transform;
                plantPoolRoot.SetParent(transform, false);
            }

            int warmCount = Mathf.Max(0, initialPlantPoolSize);
            for (int i = 0; i < warmCount; i++)
            {
                Resource plant = CreatePooledPlantInstance();
                if (plant != null)
                {
                    plantPool.Enqueue(plant);
                }
            }
        }

        private Resource CreatePooledPlantInstance()
        {
            if (plantPrefab == null) return null;

            GameObject plantObj = Instantiate(plantPrefab, Vector3.zero, Quaternion.identity, plantPoolRoot);
            Resource resource = plantObj.GetComponent<Resource>();
            if (resource == null)
            {
                Destroy(plantObj);
                return null;
            }

            resource.SetDespawnHandler(ReturnPlantToPool);
            plantObj.SetActive(false);
            return resource;
        }

        private Resource AcquirePlantResource(Vector2 position)
        {
            Resource plant = null;

            if (enablePlantPooling)
            {
                while (plantPool.Count > 0 && plant == null)
                {
                    plant = plantPool.Dequeue();
                }

                if (plant == null)
                {
                    plant = CreatePooledPlantInstance();
                }
            }
            else
            {
                GameObject plantObj = Instantiate(plantPrefab, position, Quaternion.identity);
                plant = plantObj.GetComponent<Resource>();
            }

            if (plant == null) return null;

            Transform t = plant.transform;
            t.SetParent(null, false);
            t.position = position;
            t.rotation = Quaternion.identity;

            if (!plant.gameObject.activeSelf)
            {
                plant.gameObject.SetActive(true);
            }

            return plant;
        }

        private void ReturnPlantToPool(Resource plant)
        {
            if (plant == null) return;

            // Nếu pooling tắt, fallback về destroy để tránh giữ object mồ côi.
            if (!enablePlantPooling)
            {
                Destroy(plant.gameObject);
                return;
            }

            if (plant.Type != ResourceType.Plant)
            {
                Destroy(plant.gameObject);
                return;
            }

            Transform t = plant.transform;
            if (plantPoolRoot != null)
            {
                t.SetParent(plantPoolRoot, false);
            }
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            plant.gameObject.SetActive(false);
            plantPool.Enqueue(plant);
        }

        private void ProcessPendingPlantSpawns()
        {
            if (pendingPlantSpawnQueue == null || pendingPlantSpawnQueue.Count == 0) return;
            if (!useHexGrid || hexGrid == null) return;
            if (Time.time - lastPlantDecayRealtime < decaySpawnBufferSeconds) return;

            int instantiateBudget = Mathf.Max(1, maxPlantSpawnsPerFrame);
            int spawnedThisFrame = 0;
            int attempts = 0;
            int maxAttempts = instantiateBudget * 5; // guard để tránh loop quá dài khi nhiều request fail
            var successByGroup = new Dictionary<int, int>();
            var failGlobalCapByGroup = new Dictionary<int, int>();
            var failCellCapByGroup = new Dictionary<int, int>();
            var failTooCloseByGroup = new Dictionary<int, int>();
            var failUnknownByGroup = new Dictionary<int, int>();

            while (pendingPlantSpawnQueue.Count > 0 && spawnedThisFrame < instantiateBudget && attempts < maxAttempts)
            {
                attempts++;
                HexCoordinates coords = pendingPlantSpawnQueue.Dequeue();
                HexCell cell = hexGrid.GetCell(coords);
                if (cell == null) continue;
                int groupIndex = GetHotspotGroupIndex(coords);

                // Nếu điều kiện không còn hợp lệ tại thời điểm xử lý, hoàn fertility đã reserve.
                if (currentPlantCount >= maxResources)
                {
                    RefundFertility(cell, 1);
                    IncrementGroupCounter(failGlobalCapByGroup, groupIndex);
                    continue;
                }

                int currentCapacity = GetCurrentMaxCapacity(cell);
                int currentPlantsInCell = GetPlantCountInGrid(coords);
                if (currentPlantsInCell >= currentCapacity)
                {
                    RefundFertility(cell, 1);
                    IncrementGroupCounter(failCellCapByGroup, groupIndex);
                    continue;
                }

                Vector2 gridCenter = hexGrid.HexToWorld(coords);
                Vector2 spawnPos = gridCenter + Random.insideUnitCircle * (hexGrid.HexSize * 0.4f);

                bool tooClose = IsTooCloseToExistingResource(spawnPos, minResourceDistance);

                if (tooClose)
                {
                    RefundFertility(cell, 1);
                    IncrementGroupCounter(failTooCloseByGroup, groupIndex);
                    continue;
                }

                SpawnPlant(spawnPos);
                if (groupIndex >= 0) IncrementGroupCounter(successByGroup, groupIndex);
                else IncrementGroupCounter(failUnknownByGroup, groupIndex);
                spawnedThisFrame++;
            }

            if (enableHotspotSpawnDebugLogs && spawnedThisFrame > 0)
            {
                Debug.Log(
                    $"[HotspotSpawn][Frame] budget={instantiateBudget}, spawned={spawnedThisFrame}, queueLeft={pendingPlantSpawnQueue.Count}, success={FormatGroupCounts(successByGroup)}, failGlobalCap={FormatGroupCounts(failGlobalCapByGroup)}, failCellCap={FormatGroupCounts(failCellCapByGroup)}, failTooClose={FormatGroupCounts(failTooCloseByGroup)}, unknownGroup={FormatGroupCounts(failUnknownByGroup)}"
                );
            }
        }

        private void RefundFertility(HexCell cell, int amount)
        {
            if (cell == null || amount <= 0) return;
            cell.fertilityLevel = Mathf.Clamp(cell.fertilityLevel + amount, 0, hotspotMaxFertilityPerGrid);
        }

        private static void IncrementGroupCounter(Dictionary<int, int> map, int groupIndex)
        {
            if (!map.ContainsKey(groupIndex)) map[groupIndex] = 0;
            map[groupIndex]++;
        }

        private static string FormatGroupCounts(Dictionary<int, int> map)
        {
            if (map == null || map.Count == 0) return "{}";
            return "{" + string.Join(", ", map.OrderBy(kv => kv.Key).Select(kv => $"g{kv.Key}:{kv.Value}")) + "}";
        }

        private int GetHotspotGroupIndex(HexCoordinates coord)
        {
            if (hotspotGroupIndexByCoord != null && hotspotGroupIndexByCoord.TryGetValue(coord, out int idx))
            {
                return idx;
            }

            // Fallback tuyến tính để tránh miss do index map chưa đồng bộ.
            if (hotspotGroups == null) return -1;
            for (int i = 0; i < hotspotGroups.Count; i++)
            {
                var group = hotspotGroups[i];
                if (group == null) continue;
                if (group.Contains(coord))
                {
                    if (hotspotGroupIndexByCoord != null)
                    {
                        hotspotGroupIndexByCoord[coord] = i;
                    }
                    return i;
                }
            }

            return -1;
        }

        private float GetPlantDecayTimeWithJitter()
        {
            float baseDecay = Mathf.Max(0.01f, resourceDecayTime);
            float jitter = Mathf.Clamp01(plantDecayTimeJitterPercent);
            if (jitter <= 0f) return baseDecay;

            float minMul = Mathf.Max(0.01f, 1f - jitter);
            float maxMul = 1f + jitter;
            return baseDecay * Random.Range(minMul, maxMul);
        }

        private bool IsTooCloseToExistingResource(Vector2 position, float minDistance)
        {
            if (minDistance <= 0f) return false;

            // Ưu tiên spatial query để tránh quét toàn bộ activeResources mỗi lần spawn.
            if (resourceGrid != null)
            {
                var nearby = resourceGrid.FindClosest(position, minDistance, r => r != null);
                return nearby != null;
            }

            // Fallback nếu grid chưa khởi tạo.
            foreach (Resource existingResource in activeResources)
            {
                if (existingResource == null) continue;
                if (Vector2.Distance(position, existingResource.transform.position) < minDistance)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy danh sách các cell thuộc hotspot (hợp lệ để spawn tài nguyên).
        /// </summary>
        private List<HexCell> BuildHotspotResourceCells()
        {
            if (!useHexGrid || hexGrid == null)
            {
                cachedHotspotResourceCells.Clear();
                hotspotResourceCellsDirty = false;
                return cachedHotspotResourceCells;
            }
            if (!hotspotResourceCellsDirty)
            {
                return cachedHotspotResourceCells;
            }

            cachedHotspotResourceCells.Clear();
            if (hotspotGroups == null || hotspotGroups.Count == 0)
            {
                hotspotResourceCellsDirty = false;
                return cachedHotspotResourceCells;
            }

            HashSet<HexCoordinates> unique = new HashSet<HexCoordinates>();
            foreach (var group in hotspotGroups)
            {
                if (group == null) continue;
                foreach (var coord in group)
                {
                    if (!unique.Add(coord)) continue;
                    HexCell cell = hexGrid.GetCell(coord);
                    if (cell == null) continue;
                    if (cell.isObstacle) continue;
                    cachedHotspotResourceCells.Add(cell);
                }
            }

            hotspotResourceCellsDirty = false;
            return cachedHotspotResourceCells;
        }

        private void RebuildHotspotIndex()
        {
            hotspotGroupIndexByCoord.Clear();
            if (hotspotGroups == null) return;

            for (int groupIndex = 0; groupIndex < hotspotGroups.Count; groupIndex++)
            {
                var group = hotspotGroups[groupIndex];
                if (group == null) continue;
                foreach (var coord in group)
                {
                    // Nếu trùng coord giữa các group (không nên), giữ group đầu tiên
                    if (!hotspotGroupIndexByCoord.ContainsKey(coord))
                    {
                        hotspotGroupIndexByCoord[coord] = groupIndex;
                    }
                }
            }
        }

        private void RestoreFertilityOnPlantDecay(HexCoordinates gridCoords)
        {
            if (!useHexGrid || hexGrid == null) return;
            if (hotspotGroupIndexByCoord == null || hotspotGroupIndexByCoord.Count == 0) RebuildHotspotIndex();

            if (!hotspotGroupIndexByCoord.ContainsKey(gridCoords))
            {
                return; // Không phải hotspot
            }

            HexCell cell = hexGrid.GetCell(gridCoords);
            if (cell == null) return;

            int amount = Mathf.Max(0, plantDecayFertilityRestoreAmount);
            if (amount <= 0) return;

            cell.fertilityLevel = Mathf.Clamp(cell.fertilityLevel + amount, 0, hotspotMaxFertilityPerGrid);
        }

        private void RestoreFertilityOnCreatureDeath(Vector2 deathPosition)
        {
            if (!useHexGrid || hexGrid == null) return;
            if (hotspotGroups == null || hotspotGroups.Count == 0) return;

            if (hotspotGroupIndexByCoord == null || hotspotGroupIndexByCoord.Count == 0) RebuildHotspotIndex();

            HexCoordinates coords = hexGrid.WorldToHex(deathPosition);
            if (!hotspotGroupIndexByCoord.TryGetValue(coords, out int groupIndex))
            {
                return; // Chết ngoài hotspot
            }

            if (groupIndex < 0 || groupIndex >= hotspotGroups.Count) return;
            var group = hotspotGroups[groupIndex];
            if (group == null || group.Count == 0) return;

            HexCoordinates chosen = group[Random.Range(0, group.Count)];
            HexCell chosenCell = hexGrid.GetCell(chosen);
            if (chosenCell == null) return;

            int min = Mathf.Max(0, creatureDeathFertilityRestoreMin);
            int max = Mathf.Max(min, creatureDeathFertilityRestoreMax);
            int amount = Random.Range(min, max + 1);
            if (amount <= 0) return;

            chosenCell.fertilityLevel = Mathf.Clamp(chosenCell.fertilityLevel + amount, 0, hotspotMaxFertilityPerGrid);
        }

        /// <summary>
        /// Cập nhật quần thể - loại bỏ các sinh vật đã chết
        /// </summary>
        private void UpdatePopulation()
        {
            // Dọn dẹp danh sách (creatures tự xóa khi chết)
            activeCreatures.RemoveAll(c => c == null);
            
            // Dọn dẹp danh sách tài nguyên (resources tự xóa khi decay hoặc bị ăn)
            CompactResourcesAndRefreshTracking();

            // Cập nhật đại diện loài dựa trên "fitness" gần đúng nếu đang bật speciation
            // Ở đây dùng Age làm proxy cho fitness: sinh vật sống lâu hơn được coi là phù hợp hơn.
            UpdateSpeciesRepresentativesIfNeeded();
        }

        /// <summary>
        /// Cập nhật đại diện của mỗi loài trong GenusSystem
        /// Sử dụng proxy fitness kết hợp: Age, energyAccumulated/age và offspringCount.
        /// </summary>
        private void UpdateSpeciesRepresentativesIfNeeded()
        {
            if (!enableSpeciation || genusSystem == null) return;
            if (activeCreatures == null || activeCreatures.Count == 0) return;

            var fitnessMap = new Dictionary<NEATNetwork, float>();

            foreach (var creature in activeCreatures)
            {
                if (creature == null) continue;

                var brain = creature.GetBrain();
                if (brain == null) continue;

                float fitness = ComputeProxyFitness(creature);

                // Nếu nhiều Creature chia sẻ cùng một instance brain, lấy fitness cao nhất
                if (fitnessMap.TryGetValue(brain, out float existing))
                {
                    if (fitness > existing)
                    {
                        fitnessMap[brain] = fitness;
                    }
                }
                else
                {
                    fitnessMap[brain] = fitness;
                }
            }

            if (fitnessMap.Count > 0)
            {
                genusSystem.UpdateRepresentatives(fitnessMap);
            }
        }

        /// <summary>
        /// Proxy fitness cho một sinh vật:
        /// - Ưu tiên Age (sống lâu)
        /// - EnergyAccumulated/Age (khả năng kiếm ăn hiệu quả)
        /// - OffspringCount (khả năng sinh sản thành công)
        /// Tất cả đều không chuẩn hoá tuyệt đối, nhưng được scale tương đối đơn giản.
        /// </summary>
        private float ComputeProxyFitness(CreatureController creature)
        {
            if (creature == null) return 0f;

            float age = creature.Age;
            float totalEnergy = creature.TotalEnergyGained;
            int offspringCount = creature.OffspringCount;

            float energyPerAge = 0f;
            if (age > 1f)
            {
                energyPerAge = totalEnergy / age;
            }

            // Hệ số scale đơn giản:
            // - Age: giữ nguyên
            // - EnergyPerAge: nhân 0.5f để không lấn át Age
            // - OffspringCount: mỗi con đóng góp 10 đơn vị
            float fitness = age
                           + 0.5f * energyPerAge
                           + 10f * offspringCount;

            return fitness;
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
                    Vector2 pos = hexGrid.HexToWorld(randomCell.Coordinates);
                    // Đảm bảo vị trí nằm trong world bounds
                    return ClampToWorldBounds(pos);
                }
            }
            
            // Fallback: random trong world bounds
            return new Vector2(
                Random.Range(-worldSize.x / 2, worldSize.x / 2),
                Random.Range(-worldSize.y / 2, worldSize.y / 2)
            );
        }

        /// <summary>
        /// Clamp vị trí về trong world bounds (chỉ khi world border được bật)
        /// </summary>
        private Vector2 ClampToWorldBounds(Vector2 position)
        {
            // Nếu world border tắt, không clamp
            if (!enableWorldBorder)
            {
                return position;
            }

            float halfWidth = worldSize.x / 2f;
            float halfHeight = worldSize.y / 2f;
            float margin = 0.5f; // Margin nhỏ để tránh spawn ngay sát border

            return new Vector2(
                Mathf.Clamp(position.x, -halfWidth + margin, halfWidth - margin),
                Mathf.Clamp(position.y, -halfHeight + margin, halfHeight - margin)
            );
        }

        /// <summary>
        /// Setup fertile areas trên hex grid - tạo n hotspot, mỗi hotspot là 1 grid
        /// </summary>
        private void SetupHexGridFertileAreas()
        {
            if (hexGrid == null) return;
            
            hotspotGroups.Clear();
            hotspotResourceCellsDirty = true;
            fixedCaseCenterHotspotGroupIndex = -1;
            var claimedCoords = new HashSet<HexCoordinates>();
            var allCells = hexGrid.GetAllCells();
            var availableCells = new List<HexCell>(allCells.Where(c => c != null && !c.isObstacle));
            var availableCoords = new HashSet<HexCoordinates>(availableCells.Select(c => c.Coordinates));

            if (enableFixedHotspotTestCase)
            {
                SetupFixedHotspotTestCase(claimedCoords, availableCells, availableCoords);
                RebuildHotspotIndex();
                if (enableHotspotSpawnDebugLogs)
                {
                    string groupSizes = hotspotGroups.Count == 0
                        ? "{}"
                        : "{" + string.Join(", ", hotspotGroups.Select((g, i) => $"g{i}:{(g == null ? 0 : g.Count)}")) + "}";
                    Debug.Log($"[HotspotSetup][FixedCase] groups={hotspotGroups.Count}/3, centerGroup={fixedCaseCenterHotspotGroupIndex}, groupSizes={groupSizes}");
                }
                return;
            }

            // Nếu có fertile areas được gán, đánh dấu các hex tương ứng
            if (fertileAreas.Count > 0)
            {
                foreach (Transform fertileArea in fertileAreas)
                {
                    if (fertileArea != null)
                    {
                        HexCoordinates coords = hexGrid.WorldToHex(fertileArea.position);
                        if (claimedCoords.Contains(coords)) continue;

                        var group = BuildContiguousHotspotGroup(coords, gridsPerHotspot, claimedCoords, null);
                        if (group.Count == 0) continue;

                        foreach (var c in group)
                        {
                            hexGrid.SetFertile(c, true);
                            availableCoords.Remove(c);
                        }

                        hotspotGroups.Add(group);
                    }
                }

                // Nếu fertileAreas ít hơn numberOfHotspots, tự sinh thêm hotspot ngẫu nhiên để đạt target.
                if (hotspotGroups.Count < numberOfHotspots)
                {
                    int hotspotsCreated = hotspotGroups.Count;
                    int maxAttempts = numberOfHotspots * 200;
                    int attempts = 0;

                    while (hotspotsCreated < numberOfHotspots && availableCells.Count > 0 && attempts < maxAttempts)
                    {
                        attempts++;
                        HexCell candidateCell = availableCells[Random.Range(0, availableCells.Count)];
                        HexCoordinates candidateCoord = candidateCell.Coordinates;
                        if (!availableCoords.Contains(candidateCoord) || claimedCoords.Contains(candidateCoord))
                        {
                            availableCells.Remove(candidateCell);
                            availableCoords.Remove(candidateCoord);
                            continue;
                        }

                        bool tooClose = false;
                        foreach (var existingGroup in hotspotGroups)
                        {
                            if (existingGroup.Count > 0)
                            {
                                int distance = candidateCoord.DistanceTo(existingGroup[0]);
                                if (distance < minHotspotDistance)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                        }

                        if (tooClose)
                        {
                            availableCells.Remove(candidateCell);
                            availableCoords.Remove(candidateCoord);
                            continue;
                        }

                        var group = BuildContiguousHotspotGroup(candidateCoord, gridsPerHotspot, claimedCoords, availableCoords);
                        if (group.Count == 0)
                        {
                            availableCells.Remove(candidateCell);
                            availableCoords.Remove(candidateCoord);
                            continue;
                        }

                        foreach (var c in group)
                        {
                            hexGrid.SetFertile(c, true);
                            availableCoords.Remove(c);
                        }
                        availableCells.RemoveAll(c => c != null && !availableCoords.Contains(c.Coordinates));
                        hotspotGroups.Add(group);
                        hotspotsCreated++;
                    }
                }
            }
            else
            {
                // Tạo n hotspot, mỗi hotspot là gridsPerHotspot grid liền kề
                int hotspotsCreated = 0;
                int maxAttempts = numberOfHotspots * 200; // Giới hạn số lần thử
                int attempts = 0;

                while (hotspotsCreated < numberOfHotspots && availableCells.Count > 0 && attempts < maxAttempts)
                {
                    attempts++;
                    
                    // Chọn ngẫu nhiên một cell làm center của hotspot
                    HexCell candidateCell = availableCells[Random.Range(0, availableCells.Count)];
                    HexCoordinates candidateCoord = candidateCell.Coordinates;
                    if (!availableCoords.Contains(candidateCoord) || claimedCoords.Contains(candidateCoord))
                    {
                        availableCells.Remove(candidateCell);
                        availableCoords.Remove(candidateCoord);
                        continue;
                    }
                    
                    // Kiểm tra khoảng cách với các hotspot đã tạo (tính từ center của group)
                    bool tooClose = false;
                    foreach (var existingGroup in hotspotGroups)
                    {
                        // Lấy center của group (grid đầu tiên)
                        if (existingGroup.Count > 0)
                        {
                            int distance = candidateCoord.DistanceTo(existingGroup[0]);
                            if (distance < minHotspotDistance)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                    }
                    
                    // Nếu đủ xa, tạo hotspot group với gridsPerHotspot grid
                    if (!tooClose)
                    {
                        var group = BuildContiguousHotspotGroup(candidateCoord, gridsPerHotspot, claimedCoords, availableCoords);
                        if (group.Count == 0)
                        {
                            availableCells.Remove(candidateCell);
                            availableCoords.Remove(candidateCoord);
                            continue;
                        }

                        // Nếu không build đủ size, vẫn tạo group nhưng log để dễ debug tuning
                        if (group.Count < gridsPerHotspot)
                        {
                            Debug.LogWarning($"Hotspot group tại {candidateCoord} chỉ tạo được {group.Count}/{gridsPerHotspot} grids (có thể do obstacle/đã bị claim/không đủ không gian).");
                        }

                        foreach (var c in group)
                        {
                            hexGrid.SetFertile(c, true);
                            availableCoords.Remove(c);
                        }
                        availableCells.RemoveAll(c => c != null && !availableCoords.Contains(c.Coordinates));

                        hotspotGroups.Add(group);
                        hotspotsCreated++;
                    }
                    else
                    {
                        // Xóa candidate cell khỏi danh sách để không thử lại
                        availableCells.Remove(candidateCell);
                        availableCoords.Remove(candidateCoord);
                    }
                }
                
                if (hotspotsCreated < numberOfHotspots)
                {
                    Debug.LogWarning($"Chỉ tạo được {hotspotsCreated}/{numberOfHotspots} hotspot do không đủ không gian với khoảng cách tối thiểu {minHotspotDistance}");
                }
            }

            RebuildHotspotIndex();
            if (enableHotspotSpawnDebugLogs)
            {
                string groupSizes = hotspotGroups.Count == 0
                    ? "{}"
                    : "{" + string.Join(", ", hotspotGroups.Select((g, i) => $"g{i}:{(g == null ? 0 : g.Count)}")) + "}";
                Debug.Log($"[HotspotSetup] groups={hotspotGroups.Count}/{numberOfHotspots}, fertileAreas={fertileAreas.Count}, groupSizes={groupSizes}");
            }
        }

        private void SetupFixedHotspotTestCase(
            HashSet<HexCoordinates> claimedCoords,
            List<HexCell> availableCells,
            HashSet<HexCoordinates> availableCoords
        )
        {
            if (hexGrid == null) return;

            float halfWidth = worldSize.x * 0.5f;
            float halfHeight = worldSize.y * 0.5f;
            float margin = Mathf.Max(0f, fixedHotspotEdgeMargin);

            Vector2 center = Vector2.zero;
            Vector2 bottomLeft = new Vector2(-halfWidth + margin, -halfHeight + margin);
            Vector2 topRight = new Vector2(halfWidth - margin, halfHeight - margin);

            // Theo yêu cầu: 3 hotspot cố định với center đứng đầu để dùng spawn creature ban đầu.
            Vector2[] anchors = new Vector2[] { center, bottomLeft, topRight };

            for (int i = 0; i < anchors.Length; i++)
            {
                Vector2 anchor = ClampToWorldBounds(anchors[i]);
                if (!TryFindClosestAvailableCell(anchor, availableCells, availableCoords, out HexCell seedCell))
                {
                    Debug.LogWarning($"[HotspotSetup][FixedCase] Không tìm thấy cell hợp lệ gần anchor {anchor}.");
                    continue;
                }
                HexCoordinates seed = seedCell.Coordinates;

                var group = BuildContiguousHotspotGroup(seed, gridsPerHotspot, claimedCoords, availableCoords);
                if (group.Count == 0)
                {
                    Debug.LogWarning($"[HotspotSetup][FixedCase] Không thể tạo hotspot tại anchor {anchor}.");
                    continue;
                }

                foreach (var c in group)
                {
                    hexGrid.SetFertile(c, true);
                    availableCoords.Remove(c);
                }
                if (availableCells != null)
                {
                    availableCells.RemoveAll(c => c != null && !availableCoords.Contains(c.Coordinates));
                }

                int newIndex = hotspotGroups.Count;
                hotspotGroups.Add(group);
                if (i == 0)
                {
                    fixedCaseCenterHotspotGroupIndex = newIndex;
                }
            }
        }

        private bool TryFindClosestAvailableCell(
            Vector2 anchor,
            List<HexCell> availableCells,
            HashSet<HexCoordinates> availableCoords,
            out HexCell closestCell
        )
        {
            closestCell = null;
            if (availableCells == null || availableCells.Count == 0) return false;

            float bestDistSqr = float.MaxValue;
            for (int i = 0; i < availableCells.Count; i++)
            {
                HexCell cell = availableCells[i];
                if (cell == null) continue;
                if (cell.isObstacle) continue;
                if (availableCoords != null && !availableCoords.Contains(cell.Coordinates)) continue;

                Vector2 worldPos = hexGrid.HexToWorld(cell.Coordinates);
                float distSqr = (worldPos - anchor).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    closestCell = cell;
                }
            }

            return closestCell != null;
        }

        /// <summary>
        /// Tạo một hotspot group gồm tối đa <paramref name="targetSize"/> hex liền kề, bắt đầu từ seed.
        /// - Tránh trùng lặp thông qua claimedCoords.
        /// - Nếu availableCoords != null, chỉ chọn trong tập availableCoords (dùng cho random hotspot generation).
        /// - Bỏ qua cell obstacle.
        /// </summary>
        private List<HexCoordinates> BuildContiguousHotspotGroup(
            HexCoordinates seed,
            int targetSize,
            HashSet<HexCoordinates> claimedCoords,
            HashSet<HexCoordinates> availableCoords
        )
        {
            List<HexCoordinates> group = new List<HexCoordinates>();
            if (hexGrid == null) return group;
            if (targetSize <= 0) return group;
            if (claimedCoords == null) return group;

            HexCell seedCell = hexGrid.GetCell(seed);
            if (seedCell == null || seedCell.isObstacle) return group;
            if (claimedCoords.Contains(seed)) return group;
            if (availableCoords != null && !availableCoords.Contains(seed)) return group;

            // BFS-like random expansion
            var frontier = new List<HexCoordinates> { seed };
            var inGroup = new HashSet<HexCoordinates>();

            while (frontier.Count > 0 && group.Count < targetSize)
            {
                int frontierIndex = Random.Range(0, frontier.Count);
                HexCoordinates current = frontier[frontierIndex];
                frontier.RemoveAt(frontierIndex);

                if (inGroup.Contains(current)) continue;
                if (claimedCoords.Contains(current)) continue;
                if (availableCoords != null && !availableCoords.Contains(current)) continue;

                HexCell cell = hexGrid.GetCell(current);
                if (cell == null || cell.isObstacle) continue;

                inGroup.Add(current);
                group.Add(current);
                claimedCoords.Add(current);

                if (group.Count >= targetSize) break;

                var neighbors = hexGrid.GetNeighbors(current);
                // Shuffle-ish: push in random order
                foreach (var n in neighbors.OrderBy(_ => Random.value))
                {
                    if (n == null) continue;
                    HexCoordinates nc = n.Coordinates;
                    if (inGroup.Contains(nc)) continue;
                    if (claimedCoords.Contains(nc)) continue;
                    if (availableCoords != null && !availableCoords.Contains(nc)) continue;
                    if (n.isObstacle) continue;
                    frontier.Add(nc);
                }
            }

            return group;
        }

        /// <summary>
        /// Tìm tài nguyên gần nhất của loại chỉ định (sử dụng Spatial Hash Grid)
        /// </summary>
        public Resource FindClosestResource(Vector2 position, ResourceType type, float maxDistance)
        {
            if (resourceGrid == null) return null;
            
            return resourceGrid.FindClosest(position, maxDistance, (resource) => 
                resource != null && resource.Type == type
            );
        }

        /// <summary>
        /// Tìm sinh vật gần nhất (sử dụng Spatial Hash Grid)
        /// </summary>
        public CreatureController FindClosestCreature(Vector2 position, CreatureController exclude, float maxDistance)
        {
            if (creatureGrid == null) return null;
            
            return creatureGrid.FindClosest(position, maxDistance, (creature) => 
                creature != null && creature != exclude
            );
        }

        /// <summary>
        /// Xóa tài nguyên khỏi danh sách
        /// </summary>
        public void RemoveResource(Resource resource, ResourceRemovalReason reason = ResourceRemovalReason.Unknown)
        {
            activeResources.Remove(resource);
            if (resource != null && resource.Type == ResourceType.Plant)
            {
                DecrementPlantTracking(resource.transform.position);
            }
            
            if (resource != null && useHexGrid && hexGrid != null)
            {
                HexCoordinates gridCoords = hexGrid.WorldToHex(resource.transform.position);
                HexCell cell = hexGrid.GetCell(gridCoords);

                // Nếu plant decay trong hotspot: hồi fertility cho chính grid đó
                if (reason == ResourceRemovalReason.Decayed && resource.Type == ResourceType.Plant)
                {
                    lastPlantDecayRealtime = Time.time;
                    RestoreFertilityOnPlantDecay(gridCoords);
                }

                // Nếu resource bị ăn: tăng drain counter cho grid chứa nó (giữ lại cơ chế cũ)
                if (reason == ResourceRemovalReason.Consumed && cell != null)
                {
                    cell.drainCounter++;
                    cell.lastDrainTime = simulationTime;
                }
            }
            
            // Xóa khỏi spatial grid
            if (resourceGrid != null)
            {
                resourceGrid.Remove(resource);
            }
        }

        /// <summary>
        /// Rebuild spatial grids (gọi định kỳ để đảm bảo đồng bộ)
        /// </summary>
        private void RebuildSpatialGrids()
        {
            if (resourceGrid != null)
            {
                resourceGrid.Rebuild(activeResources);
            }
            if (creatureGrid != null)
            {
                creatureGrid.Rebuild(activeCreatures);
            }
        }

        private void IncrementPlantTracking(Vector2 worldPosition)
        {
            currentPlantCount++;
            if (!useHexGrid || hexGrid == null) return;

            HexCoordinates coords = hexGrid.WorldToHex(worldPosition);
            int current = 0;
            plantCountsByGridCell.TryGetValue(coords, out current);
            plantCountsByGridCell[coords] = current + 1;
        }

        private void DecrementPlantTracking(Vector2 worldPosition)
        {
            currentPlantCount = Mathf.Max(0, currentPlantCount - 1);
            if (!useHexGrid || hexGrid == null) return;

            HexCoordinates coords = hexGrid.WorldToHex(worldPosition);
            if (!plantCountsByGridCell.TryGetValue(coords, out int current)) return;
            current--;
            if (current <= 0)
            {
                plantCountsByGridCell.Remove(coords);
            }
            else
            {
                plantCountsByGridCell[coords] = current;
            }
        }

        private void CompactResourcesAndRefreshTracking()
        {
            int before = activeResources.Count;
            activeResources.RemoveAll(r => r == null);
            if (activeResources.Count != before)
            {
                RebuildPlantTrackingCache();
            }
        }

        private void RebuildPlantTrackingCache()
        {
            currentPlantCount = 0;
            plantCountsByGridCell.Clear();

            if (!useHexGrid || hexGrid == null)
            {
                for (int i = 0; i < activeResources.Count; i++)
                {
                    var resource = activeResources[i];
                    if (resource != null && resource.Type == ResourceType.Plant)
                    {
                        currentPlantCount++;
                    }
                }
                return;
            }

            for (int i = 0; i < activeResources.Count; i++)
            {
                var resource = activeResources[i];
                if (resource == null || resource.Type != ResourceType.Plant) continue;
                currentPlantCount++;
                HexCoordinates coords = hexGrid.WorldToHex(resource.transform.position);
                int current = 0;
                plantCountsByGridCell.TryGetValue(coords, out current);
                plantCountsByGridCell[coords] = current + 1;
            }
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
        public System.DateTime SimulationStartTime => simulationStartTime;
        public Vector2 WorldSize => worldSize;
        public bool EnableWorldBorder => enableWorldBorder;

        // Getters cho settings
        public int GetTargetPopulationSize() => targetPopulationSize;
        public int GetMaxPopulationSize() => maxPopulationSize;
        public float GetResourceSpawnInterval() => resourceSpawnInterval;
        public int GetPlantsPerSpawn() => resourcesPerSpawn; // Trả về resourcesPerSpawn thay vì plantsPerSpawn
        public Vector2 GetWorldSize() => worldSize;

        // Getters cho save/load
        public List<CreatureController> GetActiveCreatures() => new List<CreatureController>(activeCreatures);
        public List<Resource> GetActiveResources() => new List<Resource>(activeResources);

        // Setters cho điều chỉnh từ UI
        public void SetTargetPopulationSize(int value)
        {
            targetPopulationSize = Mathf.Clamp(value, 10, 1000);
        }

        public void SetMaxPopulationSize(int value)
        {
            maxPopulationSize = Mathf.Clamp(value, 20, 2000);
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
            resourcesPerSpawn = Mathf.Clamp(value, 1, 20); // Cập nhật resourcesPerSpawn thay vì plantsPerSpawn
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
        /// Load simulation từ save data
        /// </summary>
        public void LoadFromSaveData(Save.SimulationSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("Save data is null!");
                return;
            }

            // Pause simulation
            SetPaused(true);

            // Clear current simulation
            ClearSimulation();

            // Restore settings
            targetPopulationSize = saveData.targetPopulationSize;
            maxPopulationSize = saveData.maxPopulationSize;
            resourceSpawnInterval = saveData.resourceSpawnInterval;
            plantsPerSpawn = saveData.plantsPerSpawn;
            worldSize = saveData.worldSize;
            enableWorldBorder = saveData.enableWorldBorder;
            simulationTime = saveData.simulationTime;
            totalCreaturesBorn = saveData.totalCreaturesBorn;
            totalCreaturesDied = saveData.totalCreaturesDied;
            // Khôi phục thời điểm bắt đầu giả lập (hoặc dùng thời điểm hiện tại nếu save cũ không có trường này)
            simulationStartTime = saveData.simulationStartTime == default
                ? System.DateTime.Now
                : saveData.simulationStartTime;

            // Update world bounds
            UpdateWorldBounds();

            // Restore resources
            foreach (var resourceData in saveData.resources)
            {
                if (resourceData.resourceType == 0) // Plant
                {
                    SpawnPlant(resourceData.position);
                    // Set decay time if needed
                    var resource = activeResources.LastOrDefault();
                    if (resource != null)
                    {
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

            // Restore creatures
            foreach (var creatureData in saveData.creatures)
            {
                // Create brain from save data
                NEATNetwork brain = Save.SimulationSaveSystem.CreateBrainFromSaveData(creatureData.brain);
                
                // Get or create lineage record
                Data.CreatureLineageRecord lineageRecord = null;
                if (!string.IsNullOrEmpty(creatureData.lineageId))
                {
                    // Try to find existing record by checking LineageLookup
                    // Since LineageId is read-only, we'll create a new record if not found
                    // Note: This is a limitation - we can't restore exact lineage IDs
                    // Nhưng có thể restore Genus/Species IDs
                    int genusId = creatureData.genusId >= 0 ? creatureData.genusId : -1;
                    int speciesIdInGenus = creatureData.speciesInGenusId >= 0
                        ? creatureData.speciesInGenusId
                        : (creatureData.speciesId >= 0 ? creatureData.speciesId : -1);
                    lineageRecord = Data.CreatureLineageRegistry.CreateRecord(creatureData.genome, null, genusId, speciesIdInGenus);
                }

                // Spawn creature
                GameObject creatureObj = SpawnCreature(creatureData.position, creatureData.genome, brain, lineageRecord);
                
                if (creatureObj != null)
                {
                    CreatureController creature = creatureObj.GetComponent<CreatureController>();
                    if (creature != null)
                    {
                        // Restore state
                        creature.transform.position = creatureData.position;
                        creature.transform.rotation = Quaternion.Euler(0, 0, creatureData.rotation);
                        creature.SetStateFromSave(
                            creatureData.energy,
                            creatureData.maxEnergy,
                            creatureData.health,
                            creatureData.maturity,
                            creatureData.age,
                            creatureData.offspringCount
                        );
                    }
                }
            }

            // Restart resource spawning
            CancelInvoke(nameof(SpawnResources));
            InvokeRepeating(nameof(SpawnResources), resourceSpawnInterval, resourceSpawnInterval);

            // Unpause
            SetPaused(false);

            Debug.Log($"Simulation loaded: {saveData.saveName}");
        }

        /// <summary>
        /// Xóa toàn bộ simulation hiện tại
        /// </summary>
        private void ClearSimulation()
        {
            // Destroy all creatures
            foreach (var creature in activeCreatures)
            {
                if (creature != null)
                    Destroy(creature.gameObject);
            }
            activeCreatures.Clear();

            // Destroy all resources
            foreach (var resource in activeResources)
            {
                if (resource != null)
                    Destroy(resource.gameObject);
            }
            activeResources.Clear();
            pendingPlantSpawnQueue.Clear();
            currentPlantCount = 0;
            plantCountsByGridCell.Clear();

            // Clear spatial grids
            if (resourceGrid != null)
                resourceGrid.Clear();
            if (creatureGrid != null)
                creatureGrid.Clear();

            // Reset Genus/Species system
            if (genusSystem != null)
            {
                genusSystem.Reset();
            }
        }

        /// <summary>
        /// Cập nhật ranh giới thế giới
        /// </summary>
        private void UpdateWorldBounds()
        {
            if (!enableWorldBorder)
            {
                // Nếu border bị tắt, xóa worldBounds nếu có
                if (worldBounds != null)
                {
                    Destroy(worldBounds.gameObject);
                    worldBounds = null;
                }
                return;
            }

            if (worldBounds == null) return;

            // Tìm các wall objects (cho rectangular border)
            Transform topWall = worldBounds.Find("TopWall");
            Transform bottomWall = worldBounds.Find("BottomWall");
            Transform leftWall = worldBounds.Find("LeftWall");
            Transform rightWall = worldBounds.Find("RightWall");

            // Nếu chưa có walls, tạo mới
            if (topWall == null || bottomWall == null || leftWall == null || rightWall == null)
            {
                CreateWorldBoundaryColliders(worldBounds.gameObject);
                return;
            }

            float width = worldSize.x;
            float height = worldSize.y;
            float thick = borderThickness;

            // Cập nhật Top wall
            BoxCollider2D topCollider = topWall.GetComponent<BoxCollider2D>();
            if (topCollider != null)
            {
                topCollider.size = new Vector2(width + thick * 2, thick);
                topCollider.offset = new Vector2(0, height / 2 + thick / 2);
                topCollider.isTrigger = false;
            }

            // Cập nhật Bottom wall
            BoxCollider2D bottomCollider = bottomWall.GetComponent<BoxCollider2D>();
            if (bottomCollider != null)
            {
                bottomCollider.size = new Vector2(width + thick * 2, thick);
                bottomCollider.offset = new Vector2(0, -height / 2 - thick / 2);
                bottomCollider.isTrigger = false;
            }

            // Cập nhật Left wall
            BoxCollider2D leftCollider = leftWall.GetComponent<BoxCollider2D>();
            if (leftCollider != null)
            {
                leftCollider.size = new Vector2(thick, height + thick * 2);
                leftCollider.offset = new Vector2(-width / 2 - thick / 2, 0);
                leftCollider.isTrigger = false;
            }

            // Cập nhật Right wall
            BoxCollider2D rightCollider = rightWall.GetComponent<BoxCollider2D>();
            if (rightCollider != null)
            {
                rightCollider.size = new Vector2(thick, height + thick * 2);
                rightCollider.offset = new Vector2(width / 2 + thick / 2, 0);
                rightCollider.isTrigger = false;
            }

            // Cập nhật visual border - đảm bảo xóa và tạo lại để tránh vấn đề
            UpdateBorderVisual(worldBounds.gameObject, width, height);
        }

        /// <summary>
        /// Cập nhật visual border
        /// </summary>
        private void UpdateBorderVisual(GameObject boundsObj, float width, float height)
        {
            if (boundsObj == null) return;

            // Xóa tất cả LineRenderer cũ để đảm bảo không có conflict
            LineRenderer[] oldRenderers = boundsObj.GetComponents<LineRenderer>();
            foreach (var oldRenderer in oldRenderers)
            {
                if (Application.isPlaying)
                    DestroyImmediate(oldRenderer);
                else
                    DestroyImmediate(oldRenderer);
            }

            // Tạo lại border với kích thước mới
            CreateBorderVisual(boundsObj, width, height);
        }
    }
}

