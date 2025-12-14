using System.Collections.Generic;
using UnityEngine;
using Verrarium.Creature;

namespace Verrarium.Core
{
    /// <summary>
    /// Quản lý time-slicing cho brain updates - phân phối neural network computation qua nhiều frame
    /// Giảm lag khi có nhiều creatures
    /// </summary>
    public class BrainUpdateManager : MonoBehaviour
    {
        private static BrainUpdateManager instance;
        public static BrainUpdateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("BrainUpdateManager");
                    instance = obj.AddComponent<BrainUpdateManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private List<CreatureController> creaturesToUpdate = new List<CreatureController>();
        private int currentIndex = 0;
        private int updatesPerFrame = 5; // Số lượng creatures update brain mỗi frame

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (creaturesToUpdate.Count == 0) return;

            // Update một số creatures mỗi frame
            int updateCount = Mathf.Min(updatesPerFrame, creaturesToUpdate.Count);
            
            for (int i = 0; i < updateCount; i++)
            {
                if (currentIndex >= creaturesToUpdate.Count)
                {
                    currentIndex = 0;
                }

                CreatureController creature = creaturesToUpdate[currentIndex];
                if (creature != null)
                {
                    // Chỉ update brain, không update sense/act
                    creature.UpdateBrainOnly();
                }

                currentIndex++;
            }
        }

        /// <summary>
        /// Đăng ký creature để update brain
        /// </summary>
        public void RegisterCreature(CreatureController creature)
        {
            if (creature != null && !creaturesToUpdate.Contains(creature))
            {
                creaturesToUpdate.Add(creature);
            }
        }

        /// <summary>
        /// Hủy đăng ký creature
        /// </summary>
        public void UnregisterCreature(CreatureController creature)
        {
            creaturesToUpdate.Remove(creature);
            if (currentIndex >= creaturesToUpdate.Count)
            {
                currentIndex = 0;
            }
        }

        /// <summary>
        /// Đặt số lượng updates mỗi frame (điều chỉnh để balance performance)
        /// </summary>
        public void SetUpdatesPerFrame(int count)
        {
            updatesPerFrame = Mathf.Max(1, count);
        }
    }
}

