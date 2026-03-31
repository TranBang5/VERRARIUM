using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa các đầu ra từ Neural Network
    /// </summary>
    public struct NeuralOutputComponent : IComponentData
    {
        // Outputs theo Bảng 2 / thứ tự CreatureController (tổng 8)
        public float accelerate;
        public float rotate;
        public float layEgg;
        public float growth;
        public float heal;
        public float attack;
        public float eat;

        // CreatureController output index 7: điều khiển thả pheromone
        public float pheromoneOutput;
    }
}

