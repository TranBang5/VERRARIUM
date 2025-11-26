using UnityEngine;
using Verrarium.Core;

namespace Verrarium.Utils
{
    /// <summary>
    /// Hệ thống lưới pheromone - quản lý pheromone như một lớp dữ liệu trên lưới
    /// </summary>
    public class PheromoneGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 100;
        [SerializeField] private int gridHeight = 100;
        [SerializeField] private float cellSize = 0.5f;
        [SerializeField] private float decayRate = 0.95f; // Tỷ lệ phân rã mỗi frame
        [SerializeField] private float diffusionRate = 0.1f; // Tỷ lệ khuếch tán

        private float[,,] pheromoneGrid; // [x, y, type] - type: 0=Red, 1=Green, 2=Blue
        private Vector2 gridOrigin;

        private void Awake()
        {
            pheromoneGrid = new float[gridWidth, gridHeight, 3];
            
            // Tính toán origin để grid ở giữa thế giới
            if (SimulationSupervisor.Instance != null)
            {
                Vector2 worldSize = SimulationSupervisor.Instance.WorldSize;
                gridOrigin = new Vector2(-worldSize.x / 2, -worldSize.y / 2);
            }
        }

        private void FixedUpdate()
        {
            UpdatePheromone();
        }

        /// <summary>
        /// Cập nhật pheromone: phân rã và khuếch tán
        /// </summary>
        private void UpdatePheromone()
        {
            // Tạo grid mới cho bước tiếp theo
            float[,,] newGrid = new float[gridWidth, gridHeight, 3];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int type = 0; type < 3; type++)
                    {
                        float currentValue = pheromoneGrid[x, y, type];
                        
                        // Phân rã
                        currentValue *= decayRate;

                        // Khuếch tán từ các ô lân cận
                        float neighborSum = 0f;
                        int neighborCount = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                                {
                                    neighborSum += pheromoneGrid[nx, ny, type];
                                    neighborCount++;
                                }
                            }
                        }

                        if (neighborCount > 0)
                        {
                            float averageNeighbor = neighborSum / neighborCount;
                            currentValue += (averageNeighbor - currentValue) * diffusionRate;
                        }

                        newGrid[x, y, type] = Mathf.Max(0f, currentValue);
                    }
                }
            }

            pheromoneGrid = newGrid;
        }

        /// <summary>
        /// Thêm pheromone tại vị trí thế giới
        /// </summary>
        public void AddPheromone(Vector2 worldPosition, int type, float amount)
        {
            Vector2Int gridPos = WorldToGrid(worldPosition);
            if (IsValidGridPosition(gridPos))
            {
                pheromoneGrid[gridPos.x, gridPos.y, type] += amount;
                pheromoneGrid[gridPos.x, gridPos.y, type] = Mathf.Min(1f, pheromoneGrid[gridPos.x, gridPos.y, type]);
            }
        }

        /// <summary>
        /// Lấy cường độ pheromone tại vị trí
        /// </summary>
        public float GetPheromoneStrength(Vector2 worldPosition, int type)
        {
            Vector2Int gridPos = WorldToGrid(worldPosition);
            if (IsValidGridPosition(gridPos))
            {
                return pheromoneGrid[gridPos.x, gridPos.y, type];
            }
            return 0f;
        }

        /// <summary>
        /// Tìm hướng của pheromone mạnh nhất trong vùng lân cận
        /// </summary>
        public Vector2 GetPheromoneGradient(Vector2 worldPosition, int preferredType)
        {
            Vector2Int center = WorldToGrid(worldPosition);
            Vector2 gradient = Vector2.zero;

            float maxStrength = 0f;
            Vector2 maxDirection = Vector2.zero;

            // Kiểm tra các ô lân cận
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int x = center.x + dx;
                    int y = center.y + dy;

                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        float strength = pheromoneGrid[x, y, preferredType];
                        if (strength > maxStrength)
                        {
                            maxStrength = strength;
                            maxDirection = new Vector2(dx, dy).normalized;
                        }
                    }
                }
            }

            return maxDirection;
        }

        /// <summary>
        /// Chuyển đổi vị trí thế giới sang tọa độ lưới
        /// </summary>
        private Vector2Int WorldToGrid(Vector2 worldPosition)
        {
            Vector2 localPos = worldPosition - gridOrigin;
            int x = Mathf.FloorToInt(localPos.x / cellSize);
            int y = Mathf.FloorToInt(localPos.y / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Kiểm tra tọa độ lưới có hợp lệ không
        /// </summary>
        private bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridWidth && 
                   gridPos.y >= 0 && gridPos.y < gridHeight;
        }
    }
}

