using UnityEngine;

namespace Verrarium.Resources
{
    /// <summary>
    /// Script cơ bản cho tài nguyên (Thực vật và Thịt)
    /// </summary>
    public class Resource : MonoBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private float energyValue = 50f;
        [SerializeField] private ResourceType resourceType = ResourceType.Plant;

        private CircleCollider2D triggerCollider;

        public float EnergyValue => energyValue;
        public ResourceType Type => resourceType;

        private void Awake()
        {
            // Đảm bảo có CircleCollider2D với isTrigger = true
            triggerCollider = GetComponent<CircleCollider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            }
            triggerCollider.isTrigger = true;
        }

        /// <summary>
        /// Được gọi khi sinh vật ăn tài nguyên này
        /// </summary>
        public float Consume()
        {
            float energy = energyValue;
            Destroy(gameObject);
            return energy;
        }

        /// <summary>
        /// Thiết lập giá trị năng lượng (dùng cho thịt từ sinh vật chết)
        /// </summary>
        public void SetEnergyValue(float value)
        {
            energyValue = value;
        }
    }

    public enum ResourceType
    {
        Plant,
        Meat
    }
}

