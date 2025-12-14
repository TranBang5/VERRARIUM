using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa các đầu vào cảm giác cho Neural Network
    /// </summary>
    public struct NeuralInputComponent : IComponentData
    {
        // 10 inputs theo Bảng 2
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
    }
}

