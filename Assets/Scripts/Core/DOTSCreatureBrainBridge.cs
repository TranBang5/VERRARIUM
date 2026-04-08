using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Verrarium.Creature;
using Verrarium.DOTS.Adapters;
using Verrarium.DOTS.Components;
using Verrarium.DOTS.Systems;

namespace Verrarium.Core
{
    /// <summary>
    /// Bridge hybrid: dùng DOTS (BrainComputeSystem) để tính output brain,
    /// nhưng vẫn giữ Sense/Act và physics/render trên classic MonoBehaviour.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class DOTSCreatureBrainBridge : MonoBehaviour
    {
        private static DOTSCreatureBrainBridge instance;
        public static DOTSCreatureBrainBridge Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("DOTSCreatureBrainBridge");
                    instance = obj.AddComponent<DOTSCreatureBrainBridge>();
                    DontDestroyOnLoad(obj);
                }

                return instance;
            }
        }

        [SerializeField] private bool enableDotsBrain = true;
        [SerializeField] private bool disableExtraDotsSystems = true;
        [SerializeField] private int maxBrainUpdatesPerFrame = 100000;

        private EntityManager entityManager;
        private bool initialized;

        private readonly Dictionary<CreatureController, Entity> entitiesByCreature = new Dictionary<CreatureController, Entity>();

        private int lastFrameCount = -1;
        private int brainUpdatesThisFrame = 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void EnsureInitialized()
        {
            if (initialized) return;

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            entityManager = world.EntityManager;

            if (disableExtraDotsSystems)
            {
                var metabolism = world.GetExistingSystemManaged<MetabolismSystem>();
                if (metabolism != null) metabolism.Enabled = false;

                var aging = world.GetExistingSystemManaged<AgingSystem>();
                if (aging != null) aging.Enabled = false;

                var movement = world.GetExistingSystemManaged<MovementSystem>();
                if (movement != null) movement.Enabled = false;

                var brainCompute = world.GetExistingSystemManaged<BrainComputeSystem>();
                if (brainCompute != null) brainCompute.Enabled = true;
            }

            initialized = true;
        }

        public void RegisterCreature(CreatureController creature)
        {
            if (creature == null) return;
            if (!enableDotsBrain) return;

            EnsureInitialized();
            if (!initialized) return;

            if (entitiesByCreature.ContainsKey(creature)) return;
            if (entityManager == null) return;

            Entity entity = CreatureDOTSAdapter.CreateEntityFromCreature(creature, entityManager);
            entitiesByCreature[creature] = entity;

            creature.SetUseDotsBrain(true);

            // Sync initial inputs để não DOTS có dữ liệu ngay khi job chạy.
            SyncNeuralInputsToEntity(creature);
        }

        public void UnregisterCreature(CreatureController creature)
        {
            if (creature == null) return;

            if (entitiesByCreature.TryGetValue(creature, out var entity))
            {
                if (entityManager != null && entityManager.Exists(entity))
                {
                    entityManager.DestroyEntity(entity);
                }

                entitiesByCreature.Remove(creature);
            }

            creature.SetUseDotsBrain(false);
        }

        public void SyncNeuralInputsToEntity(CreatureController creature)
        {
            if (!TryGetCreatureEntity(creature, out var entity)) return;
            float[] src = creature.GetNeuralInputs();
            if (src == null || src.Length < 14) return;

            NeuralInputComponent input = new NeuralInputComponent
            {
                energyRatio = src[0],
                maturity = src[1],
                healthRatio = src[2],
                age = src[3],
                distToClosestPlant = src[4],
                angleToClosestPlant = src[5],
                distToClosestMeat = src[6],
                angleToClosestMeat = src[7],
                distToClosestCreature = src[8],
                angleToClosestCreature = src[9],
                grayscaleClosestCreature = src[10],
                pheromoneR = src[11],
                pheromoneG = src[12],
                pheromoneB = src[13]
            };

            entityManager.SetComponentData(entity, input);
        }

        public void CopyNeuralInputsToEntityAndTagForUpdate(CreatureController creature)
        {
            if (!TryGetCreatureEntity(creature, out var entity)) return;
            SyncNeuralInputsToEntity(creature);

            // Backward-compat cho entity cũ chưa có tag enableable.
            if (!entityManager.HasComponent<NeedsBrainUpdateTag>(entity))
            {
                entityManager.AddComponentData(entity, new NeedsBrainUpdateTag());
                entityManager.SetComponentEnabled<NeedsBrainUpdateTag>(entity, false);
            }

            // Budget thêm tag trong frame hiện tại (giới hạn CPU spike khi dân số tăng).
            if (Time.frameCount != lastFrameCount)
            {
                lastFrameCount = Time.frameCount;
                brainUpdatesThisFrame = 0;
            }

            if (!entityManager.IsComponentEnabled<NeedsBrainUpdateTag>(entity) &&
                brainUpdatesThisFrame < maxBrainUpdatesPerFrame)
            {
                entityManager.SetComponentEnabled<NeedsBrainUpdateTag>(entity, true);
                brainUpdatesThisFrame++;
            }
        }

        public void ApplyNeuralOutputsToCreature(CreatureController creature)
        {
            if (!TryGetCreatureEntity(creature, out var entity)) return;

            NeuralOutputComponent output = entityManager.GetComponentData<NeuralOutputComponent>(entity);

            // rotate field = rotate01 trong [0,1], cần chuyển về [-1,1] để CreatureController.Act() dùng.
            creature.ApplyNeuralOutputsFromDots(
                output.accelerate,
                output.rotate,
                output.layEgg,
                output.growth,
                output.heal,
                output.attack,
                output.eat,
                output.pheromoneOutput
            );
        }

        public void UpdateCreatureBrainIO(CreatureController creature, bool requestBrainCompute)
        {
            if (!TryGetCreatureEntity(creature, out var entity)) return;

            // Apply output trước để Act() dùng ngay trong tick hiện tại.
            NeuralOutputComponent output = entityManager.GetComponentData<NeuralOutputComponent>(entity);
            creature.ApplyNeuralOutputsFromDots(
                output.accelerate,
                output.rotate,
                output.layEgg,
                output.growth,
                output.heal,
                output.attack,
                output.eat,
                output.pheromoneOutput
            );

            if (!requestBrainCompute) return;

            float[] src = creature.GetNeuralInputs();
            if (src == null || src.Length < 14) return;

            NeuralInputComponent input = new NeuralInputComponent
            {
                energyRatio = src[0],
                maturity = src[1],
                healthRatio = src[2],
                age = src[3],
                distToClosestPlant = src[4],
                angleToClosestPlant = src[5],
                distToClosestMeat = src[6],
                angleToClosestMeat = src[7],
                distToClosestCreature = src[8],
                angleToClosestCreature = src[9],
                grayscaleClosestCreature = src[10],
                pheromoneR = src[11],
                pheromoneG = src[12],
                pheromoneB = src[13]
            };
            entityManager.SetComponentData(entity, input);

            // Backward-compat cho entity cũ chưa có tag enableable.
            if (!entityManager.HasComponent<NeedsBrainUpdateTag>(entity))
            {
                entityManager.AddComponentData(entity, new NeedsBrainUpdateTag());
                entityManager.SetComponentEnabled<NeedsBrainUpdateTag>(entity, false);
            }

            if (Time.frameCount != lastFrameCount)
            {
                lastFrameCount = Time.frameCount;
                brainUpdatesThisFrame = 0;
            }

            if (!entityManager.IsComponentEnabled<NeedsBrainUpdateTag>(entity) &&
                brainUpdatesThisFrame < maxBrainUpdatesPerFrame)
            {
                entityManager.SetComponentEnabled<NeedsBrainUpdateTag>(entity, true);
                brainUpdatesThisFrame++;
            }
        }

        private bool TryGetCreatureEntity(CreatureController creature, out Entity entity)
        {
            entity = Entity.Null;
            if (creature == null) return false;
            if (!enableDotsBrain) return false;

            EnsureInitialized();
            if (!initialized || entityManager == null) return false;
            if (!entitiesByCreature.TryGetValue(creature, out entity)) return false;
            if (!entityManager.Exists(entity)) return false;
            return true;
        }
    }
}

