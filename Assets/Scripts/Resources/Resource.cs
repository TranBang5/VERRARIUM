using UnityEngine;
using Verrarium.Core;
using System;

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
        private float decayTime = -1f; // -1 = không decay
        private float spawnTime = 0f;
        private bool isDecaying = false;
        private bool isRemovedFromSupervisor = false;
        private Action<Resource> onDespawnRequested;

        public float EnergyValue => energyValue;
        public ResourceType Type => resourceType;

        public void SetDespawnHandler(Action<Resource> handler)
        {
            onDespawnRequested = handler;
        }

        private void Awake()
        {
            // Đảm bảo có CircleCollider2D với isTrigger = true
            triggerCollider = GetComponent<CircleCollider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            }
            triggerCollider.isTrigger = true;
            spawnTime = Time.time;
        }

        private void Update()
        {
            // Kiểm tra decay nếu đã được thiết lập
            if (decayTime > 0f && !isDecaying)
            {
                if (Time.time - spawnTime >= decayTime)
                {
                    Decay();
                }
            }
        }

        /// <summary>
        /// Được gọi khi sinh vật ăn tài nguyên này
        /// </summary>
        public float Consume()
        {
            float energy = energyValue;
            RemoveFromSupervisor(ResourceRemovalReason.Consumed);
            if (onDespawnRequested != null)
            {
                onDespawnRequested(this);
            }
            else
            {
                Destroy(gameObject);
            }
            return energy;
        }

        /// <summary>
        /// Thiết lập giá trị năng lượng (dùng cho thịt từ sinh vật chết)
        /// </summary>
        public void SetEnergyValue(float value)
        {
            energyValue = value;
        }

        /// <summary>
        /// Thiết lập thời gian decay (giây). -1 = không decay
        /// </summary>
        public void SetDecayTime(float time)
        {
            decayTime = time;
            spawnTime = Time.time;
        }

        /// <summary>
        /// Resource decay - tự hủy sau một thời gian
        /// </summary>
        private void Decay()
        {
            if (isDecaying) return;
            isDecaying = true;
            
            RemoveFromSupervisor(ResourceRemovalReason.Decayed);
            if (onDespawnRequested != null)
            {
                onDespawnRequested(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Xóa resource khỏi supervisor
        /// </summary>
        private void RemoveFromSupervisor(ResourceRemovalReason reason)
        {
            if (isRemovedFromSupervisor) return; // Tránh gọi nhiều lần
            
            if (SimulationSupervisor.Instance != null)
            {
                SimulationSupervisor.Instance.RemoveResource(this, reason);
                isRemovedFromSupervisor = true;
            }
        }

        private void OnDestroy()
        {
            // Đảm bảo xóa khỏi supervisor khi bị destroy
            if (!isRemovedFromSupervisor)
            {
                RemoveFromSupervisor(ResourceRemovalReason.Unknown);
            }
        }

        private void OnEnable()
        {
            // Reset runtime flags để object có thể được tái sử dụng từ pool.
            isDecaying = false;
            isRemovedFromSupervisor = false;
            spawnTime = Time.time;
        }
    }

    public enum ResourceType
    {
        Plant,
        Meat
    }
}

