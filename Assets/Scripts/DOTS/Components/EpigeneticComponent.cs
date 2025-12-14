using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component cho Epigenetics - lưu trữ các thay đổi ngoại sinh trong đời sống
    /// Cho phép Hebbian Learning và truyền một phần trạng thái cho thế hệ sau
    /// </summary>
    public struct EpigeneticComponent : IComponentData
    {
        // Hebbian Learning: Cường độ kết nối thay đổi dựa trên hoạt động
        // Lưu trữ các điều chỉnh trọng số (weight adjustments) trong đời sống
        public float learningRate;
        public float plasticity; // Độ dẻo thần kinh - khả năng thay đổi
        
        // Trạng thái tích lũy để truyền cho thế hệ sau
        public float accumulatedExperience;
    }
}

