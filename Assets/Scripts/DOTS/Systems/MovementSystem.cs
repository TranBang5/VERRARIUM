using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Verrarium.DOTS.Components;
using static Unity.Entities.SystemAPI;

namespace Verrarium.DOTS.Systems
{
    /// <summary>
    /// ECS System xử lý di chuyển cho tất cả sinh vật
    /// Sử dụng Unity Physics 2D (DOTS) thay vì Box2D
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MetabolismSystem))]
    public partial class MovementSystem : SystemBase
    {
        private const float MOVEMENT_ENERGY_COST = 0.5f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities
                .WithBurst()
                .ForEach((
                    ref PhysicsVelocity velocity,
                    ref CreatureStateComponent state,
                    in NeuralOutputComponent outputs,
                    in GenomeComponent genome,
                    in LocalTransform transform) =>
                {
                    // Di chuyển dựa trên neural output
                    if (outputs.accelerate > 0.1f)
                    {
                        float2 forward = math.mul(transform.Rotation, new float3(0, 1, 0)).xy;
                        float2 force = forward * outputs.accelerate * genome.speed * 5f;
                        
                        // Áp dụng lực (Unity Physics 2D) - chuyển float2 thành float3
                        velocity.Linear += new float3(force.x, force.y, 0f) * deltaTime;
                        
                        // Tiêu thụ năng lượng
                        state.energy = math.max(0f, state.energy - MOVEMENT_ENERGY_COST * deltaTime);
                    }

                    // Xoay dựa trên neural output
                    if (math.abs(outputs.rotate) > 0.1f)
                    {
                        float torque = outputs.rotate * 15f;
                        velocity.Angular += torque * deltaTime;
                        
                        // Tiêu thụ năng lượng
                        state.energy = math.max(0f, state.energy - MOVEMENT_ENERGY_COST * 0.5f * deltaTime);
                    }
                })
                .ScheduleParallel();
        }
    }
}

