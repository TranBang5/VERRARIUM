using System.Collections.Generic;
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
        [SerializeField] private float resourceSpawnInterval = 5f; // Global spawn interval - thời gian giữa mỗi lần spawn tài nguyên
        [SerializeField] private int resourcesPerSpawn = 3; // Số lượng tài nguyên spawn mỗi interval
        [SerializeField] private int initialResources = 30; // Số lượng thực vật ban đầu khi khởi động giả lập
        [SerializeField] private float resourceDecayTime = 60f; // Thời gian resource tồn tại trước khi decay (giây)
        [SerializeField] private float minResourceDistance = 3f; // Khoảng cách tối thiểu giữa các tài nguyên
        [SerializeField] private int maxResources = 200; // Giới hạn số lượng tài nguyên cây tối đa trên toàn map
        
        // Legacy fields (cho tương thích với save/load và legacy spawn)
        [SerializeField] private int plantsPerSpawn = 2; // Dùng cho legacy spawn (khi không có hex grid)
        [SerializeField] private float resourceSpawnPopulationThreshold = 0.8f; // Dừng spawn resource khi dân số >= 80% max population (legacy)
        
        [Header("Grid Fertility Settings")]
        [SerializeField] private float hotspotFertilityRate = 0.5f; // Tỷ lệ spawn của grid hotspot (0-1)
        [SerializeField] private float normalFertilityRate = 0.3f; // Tỷ lệ spawn của grid bình thường (0-1)
        [SerializeField] private int hotspotFertilityLevel = 10; // Fertility level của grid hotspot
        [SerializeField] private int normalFertilityLevel = 5; // Fertility level của grid bình thường
        [SerializeField, Range(0f, 2f)] private float globalFertilityRateMultiplier = 1f; // Nhân với fertilityRate của từng grid
        
        [Header("Fertility Recovery Settings")]
        [SerializeField] private float baseRecoveryInterval = 10f; // Thời gian cơ bản để hồi lại 1 fertility level (giây)
        [SerializeField] private float emptyGridRecoveryMultiplier = 15f; // Hệ số nhân recovery interval khi grid không có tài nguyên (chậm hơn rất nhiều)
        [SerializeField, Range(0.5f, 3f)] private float recoveryIntervalRandomness = 1.5f; // Độ ngẫu nhiên của recovery interval
        
        [Header("Grid Capacity Settings")]
        [SerializeField] private int hotspotMaxCapacity = 15; // Số lượng tài nguyên tối đa ở hotspot
        [SerializeField] private int normalGridMaxCapacity = 5; // Số lượng tài nguyên tối đa ở grid bình thường
        
        [Header("Hotspot Settings")]
        [SerializeField] private int numberOfHotspots = 3; // Số lượng hotspot
        [SerializeField] private int gridsPerHotspot = 1; // Số lượng grid trong mỗi hotspot group
        [SerializeField] private int minHotspotDistance = 5; // Khoảng cách tối thiểu giữa các hotspot (số hex cells)
        
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
        
        // Danh sách các hotspot (mỗi hotspot là 3 grid liền kề)
        private List<List<HexCoordinates>> hotspotGroups = new List<List<HexCoordinates>>();

        // Thống kê
        private int totalCreaturesBorn = 0;
        private int totalCreaturesDied = 0;
        private float simulationTime = 0f;
        private System.DateTime simulationStartTime;
        
        [Header("Autosave Settings")]
        [SerializeField] private bool enableAutosave = true;
        [SerializeField] private float autosaveInterval = 600f; // 10 phút = 600 giây
        private float lastAutosaveTime = 0f;

        // Pause state
        private bool isPaused = false;
        public bool IsPaused => isPaused;
        public bool EnablePheromones => enablePheromones;
        public HexGrid HexGrid => hexGrid;
        // Speciation System
        [Header("Speciation Settings")]
        [SerializeField] private bool enableSpeciation = true;
        private SpeciationSystem speciationSystem;

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
            }
            
            // Ghi nhận thời điểm bắt đầu chạy giả lập
            simulationStartTime = System.DateTime.Now;

            InitializeWorld();
            
            // Khởi tạo Spatial Hash Grids
            resourceGrid = new SpatialHashGrid<Resource>(spatialGridCellSize, worldSize);
            creatureGrid = new SpatialHashGrid<CreatureController>(spatialGridCellSize, worldSize);
            
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
            
            // Spawn initial resources trước khi spawn creatures
            // SpawnInitialResources(); // Đã bỏ logic initial resource spawn
            
            SpawnInitialCreatures();
            InvokeRepeating(nameof(SpawnResources), 1f, resourceSpawnInterval);

            // Rebuild spatial grids định kỳ
            InvokeRepeating(nameof(RebuildSpatialGrids), 2f, 2f);
            
            // Khởi tạo autosave
            lastAutosaveTime = 0f;
            
            // Khởi tạo drainDecayInterval = 4x resourceSpawnInterval
            if (drainDecayInterval <= 0f)
            {
                drainDecayInterval = resourceSpawnInterval * 4f;
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
        /// Sinh ra các thực vật ban đầu - chỉ spawn trong hotspot và 2 grid ngẫu nhiên bên cạnh
        /// </summary>
        private void SpawnInitialResources()
        {
            if (plantPrefab == null) return;
            
            // Tính số lượng tài nguyên cần spawn
            int resourcesToSpawn = Mathf.Min(initialResources, maxResources);
            
            if (!useHexGrid || hexGrid == null)
            {
                // Fallback: spawn ngẫu nhiên nếu không có hex grid
                Debug.LogWarning($"SpawnInitialResources: Fallback về spawn ngẫu nhiên vì useHexGrid={useHexGrid}, hexGrid={hexGrid}");
                for (int i = 0; i < resourcesToSpawn; i++)
                {
                    Vector2 spawnPos = GetRandomPosition();
                    bool tooClose = false;
                    foreach (Resource existingResource in activeResources)
                    {
                        if (existingResource == null) continue;
                        float distance = Vector2.Distance(spawnPos, existingResource.transform.position);
                        if (distance < minResourceDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose)
                    {
                        SpawnPlant(spawnPos);
                    }
                }
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

            // Tập hợp tất cả cells có thể spawn (hotspot + neighbors) từ tất cả hotspot groups
            HashSet<HexCoordinates> allHotspotCoords = new HashSet<HexCoordinates>();
            List<HexCell> allSpawnableCells = new List<HexCell>();
            
            foreach (var hotspotGroup in hotspotGroups)
            {
                // Thêm hotspot cells
                foreach (var hotspotCoord in hotspotGroup)
                {
                    allHotspotCoords.Add(hotspotCoord);
                    HexCell cell = hexGrid.GetCell(hotspotCoord);
                    if (cell != null && !allSpawnableCells.Any(c => c.Coordinates == hotspotCoord))
                    {
                        allSpawnableCells.Add(cell);
                    }
                }
                
                // Thêm neighbor cells
                foreach (var hotspotCoord in hotspotGroup)
                {
                    var neighbors = hexGrid.GetNeighbors(hotspotCoord);
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor != null && !allHotspotCoords.Contains(neighbor.Coordinates))
                        {
                            if (!allSpawnableCells.Any(c => c.Coordinates == neighbor.Coordinates))
                            {
                                allSpawnableCells.Add(neighbor);
                            }
                        }
                    }
                }
            }
            
            if (allSpawnableCells.Count == 0)
            {
                Debug.LogError("Không có cells để spawn initial resources!");
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

            // Nếu chưa đủ, spawn ở bất kỳ grid nào còn chỗ (đảm bảo spawn đủ initialResources)
            if (spawnedCount < resourcesToSpawn)
            {
                var allCells = hexGrid.GetAllCells();
                var availableCells = allCells.Where(cell =>
                {
                    int currentCapacity = GetCurrentMaxCapacity(cell);
                    int currentResources = GetResourceCountInGrid(cell.Coordinates);
                    return currentResources < currentCapacity;
                }).OrderBy(cell => GetResourceCountInGrid(cell.Coordinates)).ToList(); // Ưu tiên grid có ít tài nguyên hơn

                foreach (var cell in availableCells)
                {
                    if (spawnedCount >= resourcesToSpawn) break;
                    
                    int currentCapacity = GetCurrentMaxCapacity(cell);
                    int currentResources = GetResourceCountInGrid(cell.Coordinates);
                    
                    while (currentResources < currentCapacity && spawnedCount < resourcesToSpawn && attempts < maxAttempts)
                    {
                        attempts++;
                        if (TrySpawnInGrid(cell, ref spawnedCount, resourcesToSpawn, ref attempts, maxAttempts))
                        {
                            currentResources = GetResourceCountInGrid(cell.Coordinates);
                        }
                        else if (attempts >= maxAttempts)
                        {
                            break;
                        }
                    }
                }
            }

            // Log kết quả
            if (spawnedCount < resourcesToSpawn)
            {
                Debug.LogWarning($"Không thể spawn đủ tài nguyên ban đầu: {spawnedCount}/{resourcesToSpawn}. Có thể do không đủ không gian hoặc maxResources quá thấp.");
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
                
                // Classify to species nếu có brain và speciation enabled
                int speciesId = -1;
                if (enableSpeciation && speciationSystem != null && brain != null)
                {
                    speciesId = speciationSystem.ClassifyToSpecies(brain);
                }
                
                if (lineageRecord == null)
                {
                    // Tạo lineage record với species ID
                    lineageRecord = Data.CreatureLineageRegistry.CreateRecord(genome, null, speciesId);
                }
                controller.SetLineageRecord(lineageRecord);
                Data.CreatureLineageRegistry.Bind(controller, lineageRecord);
                activeCreatures.Add(controller);
                
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
            
            // Classify child to species
            int childSpeciesId = -1;
            if (enableSpeciation && speciationSystem != null && childBrain != null)
            {
                childSpeciesId = speciationSystem.ClassifyToSpecies(childBrain);
            }
            
            // Tạo lineage record với species ID
            Data.CreatureLineageRecord childRecord = Data.CreatureLineageRegistry.CreateRecord(childGenome, parentRecord, childSpeciesId);

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
        /// Khởi tạo fertility cho các grid cells
        /// </summary>
        private void InitializeGridFertility()
        {
            if (hexGrid == null) return;
            
            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                // Xác định xem grid có phải hotspot không
                bool isHotspot = cell.isFertile;
                
                // Tính khoảng cách đến hotspot gần nhất (khoảng cách đến grid gần nhất trong nhóm hotspot)
                int minDistanceToHotspot = int.MaxValue;
                if (hotspotGroups.Count > 0)
                {
                    foreach (var hotspotGroup in hotspotGroups)
                    {
                        // Tìm khoảng cách tối thiểu đến nhóm hotspot này
                        int minDistanceToGroup = int.MaxValue;
                        foreach (var hotspotCoord in hotspotGroup)
                        {
                            int distance = cell.Coordinates.DistanceTo(hotspotCoord);
                            if (distance < minDistanceToGroup)
                            {
                                minDistanceToGroup = distance;
                            }
                        }
                        
                        // Cập nhật khoảng cách tối thiểu tổng thể
                        if (minDistanceToGroup < minDistanceToHotspot)
                        {
                            minDistanceToHotspot = minDistanceToGroup;
                        }
                    }
                }
                
                // Thiết lập fertility rate và level dựa trên hotspot
                if (isHotspot)
                {
                    cell.fertilityRate = hotspotFertilityRate;
                    cell.fertilityLevel = hotspotFertilityLevel;
                }
                else
                {
                    // Bắt đầu với normal fertility rate
                    cell.fertilityRate = normalFertilityRate;
                    
                    // Giảm fertility rate dựa trên khoảng cách đến hotspot (mỗi grid xa hơn giảm 0.05)
                    // Cho phép xuống 0 nếu quá xa
                    if (minDistanceToHotspot != int.MaxValue && minDistanceToHotspot > 0)
                    {
                        float reduction = minDistanceToHotspot * 0.05f;
                        cell.fertilityRate = Mathf.Max(0.1f, cell.fertilityRate - reduction);
                    }
                    
                    cell.fertilityLevel = normalFertilityLevel;
                }
                
                // Áp dụng global multiplier
                cell.fertilityRate = Mathf.Clamp01(cell.fertilityRate * globalFertilityRateMultiplier);
                
                // Thiết lập base max capacity dựa trên hotspot
                if (isHotspot)
                {
                    cell.baseMaxCapacity = hotspotMaxCapacity;
                }
                else
                {
                    cell.baseMaxCapacity = normalGridMaxCapacity;
                }
                
                // Recovery rate luôn là 1 (hồi lại 1 fertility level mỗi lần)
                cell.recoveryRate = 1f;
                
                // Khởi tạo recovery interval dựa trên baseRecoveryInterval với độ ngẫu nhiên
                float minRecoveryInterval = baseRecoveryInterval;
                float maxRecoveryInterval = baseRecoveryInterval * recoveryIntervalRandomness;
                cell.recoveryInterval = Random.Range(minRecoveryInterval, maxRecoveryInterval);
                
                // Khởi tạo thời gian khôi phục đầu tiên
                cell.nextFertilityRestoreTime = simulationTime + cell.recoveryInterval;
                
                // Khởi tạo drain counter
                cell.drainCounter = 0;
                cell.lastDrainTime = 0f;
                
                // Khởi tạo tracking
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
        /// Cập nhật khôi phục fertility level cho các grid
        /// </summary>
        private void UpdateGridFertilityRestoration()
        {
            if (hexGrid == null) return;
            
            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                if (simulationTime >= cell.nextFertilityRestoreTime)
                {
                    // Khôi phục 1 fertility level (recoveryRate luôn = 1)
                    bool isHotspot = cell.isFertile;
                    int maxFertilityLevel = isHotspot ? hotspotFertilityLevel : normalFertilityLevel;
                    cell.fertilityLevel = Mathf.Min(maxFertilityLevel, cell.fertilityLevel + Mathf.RoundToInt(cell.recoveryRate));
                    
                    // Kiểm tra số lượng tài nguyên hiện tại trong grid
                    int currentResources = GetResourceCountInGrid(cell.Coordinates);
                    
                    // Tính toán recovery interval tiếp theo
                    float minRecoveryInterval = baseRecoveryInterval;
                    float maxRecoveryInterval = baseRecoveryInterval * recoveryIntervalRandomness;
                    
                    // Nếu grid không chứa tài nguyên, recovery interval tăng rất nhiều (chậm hơn rất nhiều)
                    if (currentResources == 0)
                    {
                        // Tăng recovery interval lên rất nhiều khi grid trống
                        float emptyGridInterval = baseRecoveryInterval * emptyGridRecoveryMultiplier;
                        cell.recoveryInterval = Random.Range(emptyGridInterval, emptyGridInterval * recoveryIntervalRandomness);
                    }
                    else
                    {
                        // Recovery interval bình thường
                        cell.recoveryInterval = Random.Range(minRecoveryInterval, maxRecoveryInterval);
                    }
                    
                    // Thiết lập thời gian khôi phục tiếp theo
                    cell.nextFertilityRestoreTime = simulationTime + cell.recoveryInterval;
                }
            }
        }
        
        /// <summary>
        /// Lấy max capacity hiện tại của grid (baseMaxCapacity - drainCounter)
        /// </summary>
        private int GetCurrentMaxCapacity(HexCell cell)
        {
            return Mathf.Max(0, cell.baseMaxCapacity - cell.drainCounter);
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
        /// Sinh tài nguyên định kỳ - Logic mới: mỗi grid có fertility riêng
        /// </summary>
        /// <summary>
        /// Đếm số lượng tài nguyên cây (không tính thịt)
        /// </summary>
        private int GetPlantCount()
        {
            int count = 0;
            foreach (var resource in activeResources)
            {
                if (resource != null && resource.Type == ResourceType.Plant)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Sinh tài nguyên định kỳ - Logic với fertility rate và level, kết hợp drain counter
        /// </summary>
        private void SpawnResources()
        {
            // Dọn dẹp null entries trước khi kiểm tra
            activeResources.RemoveAll(r => r == null);
            
            // Kiểm tra giới hạn số lượng tài nguyên cây toàn cục (không tính thịt)
            int plantCount = GetPlantCount();
            if (plantCount >= maxResources)
            {
                return; // Không spawn thêm nếu đã đạt max
            }

            if (!useHexGrid || hexGrid == null)
            {
                // Fallback: sử dụng logic cũ nếu không có hex grid
                SpawnResourcesLegacy();
                return;
            }

            // Duyệt qua tất cả các grid cells và spawn tài nguyên dựa trên fertility rate và level
            var allCells = hexGrid.GetAllCells();
            int totalSpawned = 0;
            
            foreach (var cell in allCells)
            {
                // Kiểm tra giới hạn toàn cục (chỉ đếm cây, không đếm thịt)
                int currentPlantCount = GetPlantCount();
                if (currentPlantCount >= maxResources)
                {
                    break;
                }
                
                // Tính hệ số scale dựa trên dân số hiện tại
                int currentPopulation = activeCreatures.Count;
                float populationRatio = 0f;
                if (targetPopulationSize > 0)
                {
                    populationRatio = Mathf.Clamp01((float)currentPopulation / targetPopulationSize);
                }
                
                // Khi dân số đạt targetPopulationSize, threshold scale xuống 0 (không spawn),
                // khi dân số thấp, threshold gần 1 (spawn bình thường).
                float populationThreshold = 1f - populationRatio; // [1..0]
                if (populationThreshold <= 0f)
                {
                    continue; // Không spawn khi dân số đã đạt/qua target
                }
                
                // Roll một giá trị random trong [0,1] và so sánh với cả fertilityRate và populationThreshold
                float roll = Random.value;
                
                // Cần roll <= fertilityRate VÀ roll <= populationThreshold để spawn
                if (roll > cell.fertilityRate || roll > populationThreshold)
                {
                    continue; // Grid này không spawn lần này
                }
                
                // Kiểm tra fertility level - số lượng tài nguyên có thể spawn
                if (cell.fertilityLevel <= 0)
                {
                    continue; // Grid này đã hết fertility level
                }
                
                // Kiểm tra max capacity (có tính drain counter)
                int currentCapacity = GetCurrentMaxCapacity(cell);
                int currentResources = GetResourceCountInGrid(cell.Coordinates);
                if (currentResources >= currentCapacity)
                {
                    continue; // Grid đã đầy (do drain counter hoặc đã đạt baseMaxCapacity)
                }
                
                // Spawn tài nguyên trong grid này
                int resourcesToSpawn = Mathf.Min(cell.fertilityLevel, maxResources - currentPlantCount, currentCapacity - currentResources);
                
                for (int i = 0; i < resourcesToSpawn; i++)
                {
                    // Tạo vị trí spawn ngẫu nhiên trong grid
                    Vector2 gridCenter = hexGrid.HexToWorld(cell.Coordinates);
                    Vector2 spawnPos = gridCenter + Random.insideUnitCircle * (hexGrid.HexSize * 0.4f);
                    // Với hex grid, không clamp theo world bounds để tránh bị dồn về góc; chỉ clamp khi không dùng hex grid
                    if (!useHexGrid)
                    {
                        spawnPos = ClampToWorldBounds(spawnPos);
                    }
                    
                    // Kiểm tra khoảng cách tối thiểu với tài nguyên khác
                    bool tooClose = false;
                    foreach (Resource existingResource in activeResources)
                    {
                        if (existingResource == null) continue;
                        float distance = Vector2.Distance(spawnPos, existingResource.transform.position);
                        if (distance < minResourceDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    
                    if (!tooClose)
                    {
                        SpawnPlant(spawnPos);
                        totalSpawned++;
                        // Giảm 1 fertility level sau mỗi lần spawn tài nguyên (1 fertility level = 1 tài nguyên)
                        cell.fertilityLevel = Mathf.Max(0, cell.fertilityLevel - 1);
                        
                        // Kiểm tra lại giới hạn
                        currentPlantCount = GetPlantCount();
                        if (currentPlantCount >= maxResources)
                        {
                            break;
                        }
                    }
                }
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
            
            // Kiểm tra khoảng cách tối thiểu với tài nguyên khác
            bool tooClose = false;
            foreach (Resource existingResource in activeResources)
            {
                if (existingResource == null) continue;
                float distance = Vector2.Distance(spawnPos, existingResource.transform.position);
                if (distance < minResourceDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
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
            // Kiểm tra giới hạn số lượng tài nguyên cây (không tính thịt)
            int plantCount = GetPlantCount();
            if (plantCount >= maxResources)
            {
                return;
            }

            // Tính toán hệ số giảm spawn dựa trên dân số
            int currentPopulation = activeCreatures.Count;
            float populationRatio = (float)currentPopulation / maxPopulationSize;
            
            float spawnMultiplier = 1f;
            if (populationRatio >= resourceSpawnPopulationThreshold)
            {
                float excessRatio = (populationRatio - resourceSpawnPopulationThreshold) / (1f - resourceSpawnPopulationThreshold);
                spawnMultiplier = Mathf.Max(0f, 1f - excessRatio);
            }

            int plantsToSpawn = Mathf.RoundToInt(plantsPerSpawn * (1f + (targetPopulationSize - currentPopulation) / (float)targetPopulationSize));
            plantsToSpawn = Mathf.Max(1, plantsToSpawn);
            plantsToSpawn = Mathf.RoundToInt(plantsToSpawn * spawnMultiplier);
            plantsToSpawn = Mathf.Max(0, plantsToSpawn);
            int currentPlantCount = GetPlantCount();
            int availableSlots = maxResources - currentPlantCount;
            plantsToSpawn = Mathf.Min(plantsToSpawn, availableSlots);

            int spawnedCount = 0;
            int maxAttempts = plantsToSpawn * 5;
            int attempts = 0;

            while (spawnedCount < plantsToSpawn && attempts < maxAttempts)
            {
                attempts++;
                Vector2 spawnPos = GetRandomPosition();

                bool tooClose = false;
                foreach (Resource existingResource in activeResources)
                {
                    if (existingResource == null) continue;
                    float distance = Vector2.Distance(spawnPos, existingResource.transform.position);
                    if (distance < minResourceDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    SpawnPlant(spawnPos);
                    spawnedCount++;
                }
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
                // Thiết lập decay time cho resource
                plant.SetDecayTime(resourceDecayTime);
                
                activeResources.Add(plant);
                
                // Thêm vào spatial grid
                if (resourceGrid != null)
                {
                    resourceGrid.Add(plant);
                }
            }
        }

        /// <summary>
        /// Cập nhật quần thể - loại bỏ các sinh vật đã chết
        /// </summary>
        private void UpdatePopulation()
        {
            // Dọn dẹp danh sách (creatures tự xóa khi chết)
            activeCreatures.RemoveAll(c => c == null);
            
            // Dọn dẹp danh sách tài nguyên (resources tự xóa khi decay hoặc bị ăn)
            activeResources.RemoveAll(r => r == null);
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

            // Nếu có fertile areas được gán, đánh dấu các hex tương ứng
            if (fertileAreas.Count > 0)
            {
                foreach (Transform fertileArea in fertileAreas)
                {
                    if (fertileArea != null)
                    {
                        HexCoordinates coords = hexGrid.WorldToHex(fertileArea.position);
                        List<HexCoordinates> hotspotGroup = new List<HexCoordinates> { coords };
                        hexGrid.SetFertile(coords, true);
                        
                        // Thêm các grid lân cận để tạo group
                        var neighbors = hexGrid.GetNeighbors(coords);
                        int gridsToAdd = Mathf.Min(gridsPerHotspot - 1, neighbors.Count);
                        var shuffledNeighbors = neighbors.OrderBy(x => Random.value).Take(gridsToAdd).ToList();
                        
                        foreach (var neighbor in shuffledNeighbors)
                        {
                            hotspotGroup.Add(neighbor.Coordinates);
                            hexGrid.SetFertile(neighbor.Coordinates, true);
                        }
                        
                        hotspotGroups.Add(hotspotGroup);
                    }
                }
            }
            else
            {
                // Tạo n hotspot, mỗi hotspot là gridsPerHotspot grid liền kề
                var allCells = hexGrid.GetAllCells();
                var availableCells = new List<HexCell>(allCells);
                int hotspotsCreated = 0;
                int maxAttempts = numberOfHotspots * 200; // Giới hạn số lần thử
                int attempts = 0;

                while (hotspotsCreated < numberOfHotspots && availableCells.Count > 0 && attempts < maxAttempts)
                {
                    attempts++;
                    
                    // Chọn ngẫu nhiên một cell làm center của hotspot
                    HexCell candidateCell = availableCells[Random.Range(0, availableCells.Count)];
                    HexCoordinates candidateCoord = candidateCell.Coordinates;
                    
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
                        List<HexCoordinates> newHotspotGroup = new List<HexCoordinates> { candidateCoord };
                        hexGrid.SetFertile(candidateCoord, true);
                        availableCells.Remove(candidateCell);
                        
                        // Thêm các grid lân cận để tạo group
                        var neighbors = hexGrid.GetNeighbors(candidateCoord);
                        int gridsToAdd = Mathf.Min(gridsPerHotspot - 1, neighbors.Count);
                        var shuffledNeighbors = neighbors.OrderBy(x => Random.value).Take(gridsToAdd).ToList();
                        
                        foreach (var neighbor in shuffledNeighbors)
                        {
                            newHotspotGroup.Add(neighbor.Coordinates);
                            hexGrid.SetFertile(neighbor.Coordinates, true);
                            var cellToRemove = availableCells.FirstOrDefault(c => c.Coordinates == neighbor.Coordinates);
                            if (cellToRemove != null)
                            {
                                availableCells.Remove(cellToRemove);
                            }
                        }
                        
                        hotspotGroups.Add(newHotspotGroup);
                        hotspotsCreated++;
                    }
                    else
                    {
                        // Xóa candidate cell khỏi danh sách để không thử lại
                        availableCells.Remove(candidateCell);
                    }
                }
                
                if (hotspotsCreated < numberOfHotspots)
                {
                    Debug.LogWarning($"Chỉ tạo được {hotspotsCreated}/{numberOfHotspots} hotspot do không đủ không gian với khoảng cách tối thiểu {minHotspotDistance}");
                }
            }
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
        public void RemoveResource(Resource resource, bool wasConsumed = false)
        {
            activeResources.Remove(resource);
            
            // Nếu tài nguyên bị ăn (consumed), tăng drain counter cho grid chứa nó
            if (wasConsumed && resource != null && useHexGrid && hexGrid != null)
            {
                HexCoordinates gridCoords = hexGrid.WorldToHex(resource.transform.position);
                HexCell cell = hexGrid.GetCell(gridCoords);
                if (cell != null)
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
                    // Nhưng có thể restore speciesId
                    int speciesId = creatureData.speciesId >= 0 ? creatureData.speciesId : -1;
                    lineageRecord = Data.CreatureLineageRegistry.CreateRecord(creatureData.genome, null, speciesId);
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
                            creatureData.age
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

            // Clear spatial grids
            if (resourceGrid != null)
                resourceGrid.Clear();
            if (creatureGrid != null)
                creatureGrid.Clear();
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

