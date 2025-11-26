using UnityEngine;

namespace Verrarium.World
{
    /// <summary>
    /// Một ô hex với các thuộc tính môi trường có thể customize
    /// </summary>
    [System.Serializable]
    public class HexCell
    {
        public HexCoordinates Coordinates { get; private set; }
        
        [Header("Environmental Properties")]
        public bool isFertile = false;           // Có thể sinh thực vật
        public float fertility = 0.5f;          // Độ màu mỡ (0-1)
        public float temperature = 0.5f;         // Nhiệt độ (0-1, có thể mở rộng)
        public float resourceDensity = 1.0f;    // Mật độ tài nguyên (multiplier)
        public float movementCost = 1.0f;       // Chi phí di chuyển (multiplier)
        public bool isObstacle = false;          // Có phải vật cản không
        
        [Header("Visual")]
        public Color cellColor = Color.white;   // Màu sắc để hiển thị
        
        // Cache
        private Vector2 worldPosition;
        private float hexSize;

        public HexCell(HexCoordinates coordinates, float hexSize)
        {
            Coordinates = coordinates;
            this.hexSize = hexSize;
            worldPosition = coordinates.ToWorld(hexSize);
        }

        /// <summary>
        /// Vị trí world của hex cell
        /// </summary>
        public Vector2 WorldPosition => worldPosition;

        /// <summary>
        /// Cập nhật hex size (khi grid thay đổi)
        /// </summary>
        public void UpdateHexSize(float newHexSize)
        {
            hexSize = newHexSize;
            worldPosition = Coordinates.ToWorld(hexSize);
        }

        /// <summary>
        /// Thiết lập độ màu mỡ
        /// </summary>
        public void SetFertility(float value)
        {
            fertility = Mathf.Clamp01(value);
            isFertile = fertility > 0.3f; // Tự động đánh dấu fertile nếu > 0.3
        }

        /// <summary>
        /// Thiết lập nhiệt độ
        /// </summary>
        public void SetTemperature(float value)
        {
            temperature = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Thiết lập mật độ tài nguyên
        /// </summary>
        public void SetResourceDensity(float value)
        {
            resourceDensity = Mathf.Clamp(value, 0f, 2f);
        }

        /// <summary>
        /// Thiết lập chi phí di chuyển
        /// </summary>
        public void SetMovementCost(float value)
        {
            movementCost = Mathf.Clamp(value, 0.1f, 5f);
        }

        /// <summary>
        /// Lấy màu sắc dựa trên thuộc tính
        /// </summary>
        public Color GetDisplayColor()
        {
            if (isObstacle)
                return Color.gray;
            
            // Màu dựa trên fertility và temperature
            float green = fertility;
            float blue = temperature;
            float red = 1f - fertility * 0.5f;
            
            return new Color(red, green, blue, 0.5f);
        }
    }
}

