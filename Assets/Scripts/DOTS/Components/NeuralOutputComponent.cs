using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa các đầu ra từ Neural Network
    /// </summary>
    public struct NeuralOutputComponent : IComponentData
    {
        // 7 outputs theo Bảng 2
        public float accelerate;
        public float rotate;
        public float layEgg;
        public float growth;
        public float heal;
        public float attack;
        public float eat;
    }
}

