using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Verrarium.DOTS.Components;
using static Unity.Entities.SystemAPI;

namespace Verrarium.DOTS.Systems
{
    /// <summary>
    /// ECS System xử lý lão hóa cho tất cả sinh vật
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MetabolismSystem))]
    public partial class AgingSystem : SystemBase
    {
        private const float AGING_START_MATURITY = 0.7f;
        private const float AGING_DAMAGE_RATE = 0.5f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities
                .WithBurst()
                .ForEach((ref CreatureStateComponent state) =>
                {
                    // Tăng tuổi
                    state.age += deltaTime;

                    // Lão hóa khi đạt ngưỡng trưởng thành
                    if (state.maturity >= AGING_START_MATURITY)
                    {
                        float agingFactor = (state.maturity - AGING_START_MATURITY) / (1f - AGING_START_MATURITY);
                        if (state.maturity > 1f)
                        {
                            agingFactor = 1f + (state.maturity - 1f) * 5f;
                        }

                        float damage = AGING_DAMAGE_RATE * agingFactor * deltaTime;
                        state.health = math.max(0f, state.health - damage);
                    }
                })
                .ScheduleParallel();
        }
    }
}

