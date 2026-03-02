using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verrarium.Core;

namespace Verrarium.World
{
    /// <summary>
    /// Quản lý hex grid - hệ thống chunk-based cho bản đồ
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 20;
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private Vector2 gridOffset = Vector2.zero;

        [Header("Visualization")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private bool showCellColors = true;

        // Dictionary lưu trữ các hex cells
        private Dictionary<HexCoordinates, HexCell> cells = new Dictionary<HexCoordinates, HexCell>();

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float HexSize => hexSize;
        public int CellCount => cells.Count;

        private void Awake()
        {
            GenerateGrid();
        }

        /// <summary>
        /// Tạo hex grid
        /// </summary>
        public void GenerateGrid()
        {
            cells.Clear();

            for (int q = -gridWidth / 2; q < gridWidth / 2; q++)
            {
                for (int r = -gridHeight / 2; r < gridHeight / 2; r++)
                {
                    HexCoordinates coords = new HexCoordinates(q, r);
                    HexCell cell = new HexCell(coords, hexSize);
                    cells[coords] = cell;
                }
            }
        }

        /// <summary>
        /// Lấy hex cell tại tọa độ
        /// </summary>
        public HexCell GetCell(HexCoordinates coordinates)
        {
            cells.TryGetValue(coordinates, out HexCell cell);
            return cell;
        }

        /// <summary>
        /// Lấy hex cell tại vị trí world
        /// </summary>
        public HexCell GetCellAtWorldPosition(Vector2 worldPos)
        {
            HexCoordinates coords = HexCoordinates.FromWorld(worldPos - gridOffset, hexSize);
            return GetCell(coords);
        }

        /// <summary>
        /// Chuyển đổi hex coordinates sang world position
        /// </summary>
        public Vector2 HexToWorld(HexCoordinates coordinates)
        {
            return coordinates.ToWorld(hexSize) + gridOffset;
        }

        /// <summary>
        /// Chuyển đổi world position sang hex coordinates
        /// </summary>
        public HexCoordinates WorldToHex(Vector2 worldPos)
        {
            return HexCoordinates.FromWorld(worldPos - gridOffset, hexSize);
        }

        /// <summary>
        /// Lấy các hex lân cận
        /// </summary>
        public List<HexCell> GetNeighbors(HexCoordinates coordinates)
        {
            List<HexCell> neighbors = new List<HexCell>();
            
            // 6 hướng lân cận trong hex grid
            HexCoordinates[] directions = new HexCoordinates[]
            {
                new HexCoordinates(1, 0),   // Right
                new HexCoordinates(1, -1),  // Top-right
                new HexCoordinates(0, -1),  // Top-left
                new HexCoordinates(-1, 0),  // Left
                new HexCoordinates(-1, 1),  // Bottom-left
                new HexCoordinates(0, 1)    // Bottom-right
            };

            foreach (var dir in directions)
            {
                HexCoordinates neighborCoords = new HexCoordinates(
                    coordinates.q + dir.q,
                    coordinates.r + dir.r
                );
                
                HexCell neighbor = GetCell(neighborCoords);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Lấy tất cả các hex cells
        /// </summary>
        public List<HexCell> GetAllCells()
        {
            return new List<HexCell>(cells.Values);
        }

        /// <summary>
        /// Thêm pheromone tại vị trí world
        /// </summary>
        public void AddPheromoneAtWorld(Vector2 worldPos, int type, float amount)
        {
            if (amount <= 0f) return;

            HexCell cell = GetCellAtWorldPosition(worldPos);
            if (cell == null) return;

            cell.AddPheromone(type, amount);
        }

        /// <summary>
        /// Lấy cường độ pheromone tại vị trí world
        /// </summary>
        public float GetPheromoneStrengthAtWorld(Vector2 worldPos, int type)
        {
            HexCell cell = GetCellAtWorldPosition(worldPos);
            return cell != null ? cell.GetPheromone(type) : 0f;
        }

        /// <summary>
        /// Decay pheromone trên toàn bộ hex grid
        /// </summary>
        public void DecayPheromones(float decayRate)
        {
            if (decayRate <= 0f || decayRate >= 1f) return;

            foreach (var cell in cells.Values)
            {
                cell.DecayPheromones(decayRate);
            }
        }

        private void Update()
        {
            if (SimulationSupervisor.Instance == null || !SimulationSupervisor.Instance.EnablePheromones)
                return;

            // Dùng decayRate cố định cho hex pheromones; có thể expose ra serialized nếu cần
            const float decayRate = 0.98f;
            DecayPheromones(decayRate);
        }

        /// <summary>
        /// Lấy các hex cells fertile
        /// </summary>
        public List<HexCell> GetFertileCells()
        {
            return cells.Values.Where(cell => cell.isFertile).ToList();
        }

        /// <summary>
        /// Thiết lập hex cell là fertile
        /// </summary>
        public void SetFertile(HexCoordinates coordinates, bool fertile)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.isFertile = fertile;
                if (fertile)
                {
                    cell.SetFertility(0.8f); // Tự động set fertility cao
                }
            }
        }

        /// <summary>
        /// Thiết lập fertility cho hex cell
        /// </summary>
        public void SetFertility(HexCoordinates coordinates, float fertility)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.SetFertility(fertility);
            }
        }

        /// <summary>
        /// Thiết lập temperature cho hex cell
        /// </summary>
        public void SetTemperature(HexCoordinates coordinates, float temperature)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.SetTemperature(temperature);
            }
        }

        /// <summary>
        /// Thiết lập resource density cho hex cell
        /// </summary>
        public void SetResourceDensity(HexCoordinates coordinates, float density)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.SetResourceDensity(density);
            }
        }

        /// <summary>
        /// Thiết lập movement cost cho hex cell
        /// </summary>
        public void SetMovementCost(HexCoordinates coordinates, float cost)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.SetMovementCost(cost);
            }
        }

        /// <summary>
        /// Thiết lập obstacle cho hex cell
        /// </summary>
        public void SetObstacle(HexCoordinates coordinates, bool isObstacle)
        {
            HexCell cell = GetCell(coordinates);
            if (cell != null)
            {
                cell.isObstacle = isObstacle;
            }
        }

        /// <summary>
        /// Lấy hex cell ngẫu nhiên
        /// </summary>
        public HexCell GetRandomCell()
        {
            if (cells.Count == 0) return null;
            
            var cellList = cells.Values.ToList();
            return cellList[Random.Range(0, cellList.Count)];
        }

        /// <summary>
        /// Lấy hex cell ngẫu nhiên fertile
        /// </summary>
        public HexCell GetRandomFertileCell()
        {
            var fertileCells = GetFertileCells();
            if (fertileCells.Count == 0) return null;
            
            return fertileCells[Random.Range(0, fertileCells.Count)];
        }

        /// <summary>
        /// Vẽ grid trong Scene view (Gizmos)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridColor;

            foreach (var cell in cells.Values)
            {
                if (showCellColors)
                {
                    Gizmos.color = cell.GetDisplayColor();
                }
                else
                {
                    Gizmos.color = gridColor;
                }

                DrawHexGizmo(cell.WorldPosition, hexSize);
            }
        }

        /// <summary>
        /// Vẽ một hex bằng Gizmos
        /// </summary>
        private void DrawHexGizmo(Vector2 center, float size)
        {
            Vector3[] vertices = new Vector3[7];
            
            for (int i = 0; i < 7; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad;
                float x = center.x + size * Mathf.Cos(angle);
                float y = center.y + size * Mathf.Sin(angle);
                vertices[i] = new Vector3(x, y, 0);
            }

            // Vẽ các cạnh
            for (int i = 0; i < 6; i++)
            {
                Gizmos.DrawLine(vertices[i], vertices[i + 1]);
            }
        }

        /// <summary>
        /// Thiết lập kích thước grid
        /// </summary>
        public void SetGridSize(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            GenerateGrid();
        }

        /// <summary>
        /// Thiết lập hex size
        /// </summary>
        public void SetHexSize(float size)
        {
            hexSize = size;
            // Cập nhật tất cả cells
            foreach (var cell in cells.Values)
            {
                cell.UpdateHexSize(hexSize);
            }
        }

        /// <summary>
        /// Thiết lập grid offset
        /// </summary>
        public void SetGridOffset(Vector2 offset)
        {
            gridOffset = offset;
        }
    }
}

