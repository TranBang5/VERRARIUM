using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa các đầu vào cảm giác cho Neural Network
    /// </summary>
    public struct NeuralInputComponent : IComponentData
    {
        // Inputs theo Bảng 2 / thứ tự CreatureController (tổng 14)
        public float energyRatio;
        public float maturity;
        public float healthRatio;
        public float age;
        public float distToClosestPlant;
        public float angleToClosestPlant;
        public float distToClosestMeat;
        public float angleToClosestMeat;
        public float distToClosestCreature;
        public float angleToClosestCreature;

        // CreatureController index 10: grayscale màu sinh vật gần nhất
        public float grayscaleClosestCreature;

        // CreatureController index 11-13: pheromone tại miệng (R,G,B)
        public float pheromoneR;
        public float pheromoneG;
        public float pheromoneB;
    }
}

