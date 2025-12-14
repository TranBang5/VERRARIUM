using Unity.Collections;
using Unity.Entities;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa mạng nơ-ron dưới dạng NativeArrays để tương thích với Burst
    /// Thay thế cho NEATNetwork class-based structure
    /// </summary>
    public struct BrainComponent : IComponentData
    {
        // Metadata
        public int inputCount;
        public int outputCount;
        public int neuronCount;
        public int connectionCount;

        // NativeArrays sẽ được lưu trong DynamicBuffer
        // Không thể lưu trực tiếp NativeArray trong IComponentData
    }

    /// <summary>
    /// DynamicBuffer chứa dữ liệu nơ-ron (Flat Array representation)
    /// Mỗi neuron: [id, type, activationFunction, bias]
    /// </summary>
    public struct NeuronData : IBufferElementData
    {
        public int id;
        public int type; // 0=Input, 1=Hidden, 2=Output
        public int activationFunction; // 0=Sigmoid, 1=Tanh, 2=ReLU, 3=Linear
        public float bias;
        public float value; // Giá trị hiện tại sau khi tính toán
    }

    /// <summary>
    /// DynamicBuffer chứa dữ liệu kết nối (Flat Array representation)
    /// Mỗi connection: [innovationNumber, fromNeuronId, toNeuronId, weight, enabled]
    /// </summary>
    public struct ConnectionData : IBufferElementData
    {
        public int innovationNumber;
        public int fromNeuronId;
        public int toNeuronId;
        public float weight;
        public bool enabled;
    }

    /// <summary>
    /// DynamicBuffer chứa adjacency list cho tối ưu tính toán
    /// Index = toNeuronId, Value = index trong ConnectionData buffer
    /// </summary>
    public struct ConnectionIndex : IBufferElementData
    {
        public int connectionIndex;
    }
}

