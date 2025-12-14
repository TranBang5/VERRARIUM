using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Verrarium.DOTS.Components;
using static Unity.Entities.SystemAPI;

namespace Verrarium.DOTS.Systems
{
    /// <summary>
    /// ECS System xử lý trao đổi chất (Metabolism) cho tất cả sinh vật
    /// Chạy song song với Burst Compiler
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MetabolismSystem : SystemBase
    {
        private const float BASE_METABOLIC_RATE = 0.8f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities
                .WithBurst()
                .ForEach((ref CreatureStateComponent state, in GenomeComponent genome) =>
                {
                    // Cập nhật max energy dựa trên size và maturity
                    state.maxEnergy = genome.size * 100f * (1f + state.maturity * 0.5f);

                    // Tiêu thụ năng lượng cơ bản
                    float metabolicCost = BASE_METABOLIC_RATE * deltaTime;
                    state.energy = math.max(0f, state.energy - metabolicCost);

                    // Mất máu nếu hết năng lượng
                    if (state.energy <= 0f)
                    {
                        state.health -= 5f * deltaTime;
                        state.energy = 0f;
                    }
                })
                .ScheduleParallel();
        }
    }
}

