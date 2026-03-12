using System.Collections.Generic;
using UnityEngine;
using Verrarium.Data;

namespace Verrarium.Utils
{
    /// <summary>
    /// Quản lý hiệu ứng đám mây pheromone xuất hiện tại đuôi sinh vật mỗi lần nhả pheromone.
    /// Sử dụng pool để tránh tạo/destroy GameObject liên tục.
    /// </summary>
    public class PheromoneEmitCloudManager : MonoBehaviour
    {
        public static PheromoneEmitCloudManager Instance { get; private set; }

        [Header("Pooling Settings")]
        [SerializeField] private int initialPoolSize = 64;

        private readonly Queue<PheromoneCloud> pool = new Queue<PheromoneCloud>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            for (int i = 0; i < initialPoolSize; i++)
            {
                var cloud = CreateCloudInstance();
                cloud.gameObject.SetActive(false);
                pool.Enqueue(cloud);
            }
        }

        public void SpawnCloud(Vector2 position, PheromoneType type, float strength, float lifetimeSeconds)
        {
            PheromoneCloud cloud = GetFromPool();
            Color color = GetColorForType(type);
            cloud.Initialize(position, color, strength, ReturnToPool, lifetimeSeconds);
        }

        private PheromoneCloud GetFromPool()
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return CreateCloudInstance();
        }

        private void ReturnToPool(PheromoneCloud cloud)
        {
            if (cloud == null) return;
            pool.Enqueue(cloud);
        }

        private PheromoneCloud CreateCloudInstance()
        {
            GameObject obj = new GameObject("PheromoneCloud");
            obj.transform.SetParent(transform, worldPositionStays: false);
            return obj.AddComponent<PheromoneCloud>();
        }

        private static Color GetColorForType(PheromoneType type)
        {
            switch (type)
            {
                case PheromoneType.Red:
                    return new Color(1f, 0.4f, 0.4f, 0.9f);
                case PheromoneType.Green:
                    return new Color(0.4f, 1f, 0.4f, 0.9f);
                case PheromoneType.Blue:
                    return new Color(0.4f, 0.7f, 1f, 0.9f);
                default:
                    return new Color(1f, 1f, 1f, 0.9f);
            }
        }
    }
}

