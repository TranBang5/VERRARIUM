using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Verrarium.Utils
{
    /// <summary>
    /// Spatial Hash Grid để tối ưu hóa spatial queries (O(1) thay vì O(n))
    /// Chia không gian thành các cell, mỗi object được lưu trong cell tương ứng
    /// </summary>
    public class SpatialHashGrid<T> where T : MonoBehaviour
    {
        private Dictionary<int, List<T>> grid;
        private float cellSize;
        private int gridWidth;
        private int gridHeight;

        public SpatialHashGrid(float cellSize, Vector2 worldSize)
        {
            this.cellSize = cellSize;
            this.gridWidth = Mathf.CeilToInt(worldSize.x / cellSize);
            this.gridHeight = Mathf.CeilToInt(worldSize.y / cellSize);
            this.grid = new Dictionary<int, List<T>>();
        }

        /// <summary>
        /// Lấy cell index từ world position
        /// </summary>
        private int GetCellIndex(Vector2 position)
        {
            int x = Mathf.FloorToInt((position.x + gridWidth * cellSize / 2f) / cellSize);
            int y = Mathf.FloorToInt((position.y + gridHeight * cellSize / 2f) / cellSize);
            return y * gridWidth + x;
        }

        /// <summary>
        /// Thêm object vào grid
        /// </summary>
        public void Add(T obj)
        {
            if (obj == null) return;
            int cellIndex = GetCellIndex(obj.transform.position);
            
            if (!grid.ContainsKey(cellIndex))
            {
                grid[cellIndex] = new List<T>();
            }
            
            if (!grid[cellIndex].Contains(obj))
            {
                grid[cellIndex].Add(obj);
            }
        }

        /// <summary>
        /// Xóa object khỏi grid
        /// </summary>
        public void Remove(T obj)
        {
            if (obj == null) return;
            int cellIndex = GetCellIndex(obj.transform.position);
            
            if (grid.ContainsKey(cellIndex))
            {
                grid[cellIndex].Remove(obj);
                if (grid[cellIndex].Count == 0)
                {
                    grid.Remove(cellIndex);
                }
            }
        }

        /// <summary>
        /// Cập nhật vị trí object (xóa khỏi cell cũ, thêm vào cell mới)
        /// </summary>
        public void UpdatePosition(T obj, Vector2 oldPosition, Vector2 newPosition)
        {
            int oldCell = GetCellIndex(oldPosition);
            int newCell = GetCellIndex(newPosition);
            
            if (oldCell != newCell)
            {
                RemoveFromCell(obj, oldCell);
                AddToCell(obj, newCell);
            }
        }

        private void RemoveFromCell(T obj, int cellIndex)
        {
            if (grid.ContainsKey(cellIndex))
            {
                grid[cellIndex].Remove(obj);
                if (grid[cellIndex].Count == 0)
                {
                    grid.Remove(cellIndex);
                }
            }
        }

        private void AddToCell(T obj, int cellIndex)
        {
            if (!grid.ContainsKey(cellIndex))
            {
                grid[cellIndex] = new List<T>();
            }
            if (!grid[cellIndex].Contains(obj))
            {
                grid[cellIndex].Add(obj);
            }
        }

        /// <summary>
        /// Tìm object gần nhất trong phạm vi
        /// </summary>
        public T FindClosest(Vector2 position, float maxDistance, System.Func<T, bool> filter = null)
        {
            T closest = null;
            float closestDistance = maxDistance;
            
            // Tính số cell cần kiểm tra dựa trên maxDistance
            int cellRadius = Mathf.CeilToInt(maxDistance / cellSize);
            int centerCell = GetCellIndex(position);
            int centerX = centerCell % gridWidth;
            int centerY = centerCell / gridWidth;
            
            // Kiểm tra các cell trong phạm vi
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    int cellX = centerX + dx;
                    int cellY = centerY + dy;
                    
                    if (cellX < 0 || cellX >= gridWidth || cellY < 0 || cellY >= gridHeight)
                        continue;
                    
                    int cellIndex = cellY * gridWidth + cellX;
                    
                    if (grid.ContainsKey(cellIndex))
                    {
                        foreach (T obj in grid[cellIndex])
                        {
                            if (obj == null) continue;
                            if (filter != null && !filter(obj)) continue;
                            
                            float distance = Vector2.Distance(position, obj.transform.position);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closest = obj;
                            }
                        }
                    }
                }
            }
            
            return closest;
        }

        /// <summary>
        /// Clear toàn bộ grid
        /// </summary>
        public void Clear()
        {
            grid.Clear();
        }

        /// <summary>
        /// Rebuild grid từ danh sách objects
        /// </summary>
        public void Rebuild(IEnumerable<T> objects)
        {
            Clear();
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Add(obj);
                }
            }
        }
    }
}

