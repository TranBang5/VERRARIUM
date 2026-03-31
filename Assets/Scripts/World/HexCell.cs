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
        
        [Header("Resource Spawning")]
        public float fertilityRate = 0.3f;       // Tỷ lệ khả năng spawn tại mỗi global spawn interval (0-1)
        public int fertilityLevel = 2;           // Số lượng tài nguyên grid có thể spawn
        public float nextFertilityRestoreTime = 0f; // Thời điểm khôi phục fertility level tiếp theo
        public float recoveryInterval = 10f;     // Khoảng thời gian giữa các lần khôi phục fertility level
        public float recoveryRate = 1f;           // Số lượng fertility level được khôi phục mỗi lần
        
        [Header("Drain Counter")]
        public int baseMaxCapacity = 5;          // Số lượng tài nguyên tối đa cơ bản của grid (trước khi áp dụng drain)
        public int drainCounter = 0;              // Số lần tài nguyên bị ăn (giảm maxCapacity)
        public float lastDrainTime = 0f;          // Thời điểm drain counter được tăng lần cuối

        [Header("Creature Pressure")]
        public float creaturePressure = 0f;       // Áp lực tích lũy từ sinh vật đứng trên ô này
        public float lastCreaturePressureUpdate = 0f; // Thời điểm cuối cùng cập nhật áp lực
        
        [Header("Visual")]
        public Color cellColor = Color.white;   // Màu sắc để hiển thị
        
        [Header("Pheromones")]
        public float pheromoneR = 0f;
        public float pheromoneG = 0f;
        public float pheromoneB = 0f;
        
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
        /// Cộng thêm pheromone vào cell
        /// </summary>
        public void AddPheromone(int type, float amount)
        {
            if (amount <= 0f) return;

            switch (type)
            {
                case 0:
                    pheromoneR = Mathf.Clamp01(pheromoneR + amount);
                    break;
                case 1:
                    pheromoneG = Mathf.Clamp01(pheromoneG + amount);
                    break;
                case 2:
                    pheromoneB = Mathf.Clamp01(pheromoneB + amount);
                    break;
            }
        }

        /// <summary>
        /// Lấy cường độ pheromone theo loại
        /// </summary>
        public float GetPheromone(int type)
        {
            return type switch
            {
                0 => pheromoneR,
                1 => pheromoneG,
                2 => pheromoneB,
                _ => 0f
            };
        }

        /// <summary>
        /// Decay pheromone theo thời gian
        /// </summary>
        public void DecayPheromones(float decayRate)
        {
            pheromoneR *= decayRate;
            pheromoneG *= decayRate;
            pheromoneB *= decayRate;

            if (pheromoneR < 0.001f) pheromoneR = 0f;
            if (pheromoneG < 0.001f) pheromoneG = 0f;
            if (pheromoneB < 0.001f) pheromoneB = 0f;
        }

        /// <summary>
        /// Cập nhật hex size (khi grid thay đổi)
        /// </summary>
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

