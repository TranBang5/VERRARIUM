using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa trạng thái động của sinh vật (Energy, Health, Maturity, Age)
    /// </summary>
    public struct CreatureStateComponent : IComponentData
    {
        public float energy;
        public float maxEnergy;
        public float health;
        public float maxHealth;
        public float maturity; // 0 = mới sinh, 1 = trưởng thành
        public float age;
        public float lastEatTime;
        public float lastReproduceTime;
    }
}

