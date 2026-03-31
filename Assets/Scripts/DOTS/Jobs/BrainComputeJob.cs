using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Verrarium.DOTS.Jobs
{
    /// <summary>
    /// Burst-compiled Job để tính toán Neural Network song song
    /// Chuyển đổi từ NEATNetwork.Compute() sang NativeArrays
    /// Note: NativeArrays trong IJobParallelFor là read-only, cần dùng NativeArray<float> riêng cho neuron values
    /// </summary>
    [BurstCompile]
    public struct BrainComputeJob : IJobParallelFor
    {
        // Input data (read-only)
        [ReadOnly] public NativeArray<float> neuralInputs; // Flattened: [entity0_input0, entity0_input1, ..., entity1_input0, ...]
        [ReadOnly] public NativeArray<int> inputCounts; // Số lượng input cho mỗi entity
        [ReadOnly] public NativeArray<int> outputCounts; // Số lượng output cho mỗi entity

        // Offsets prefix-sum cho input/output theo từng entity.
        // (Không được giả định layout cố định entityIndex * count vì input/outputCount có thể khác nhau giữa entity.)
        [ReadOnly] public NativeArray<int> inputOffsets;
        [ReadOnly] public NativeArray<int> outputOffsets;
        
        // Brain structure (read-only)
        [ReadOnly] public NativeArray<JobNeuronData> allNeurons; // Tất cả neurons của tất cả entities (structure only)
        [ReadOnly] public NativeArray<JobConnectionData> allConnections; // Tất cả connections của tất cả entities
        [ReadOnly] public NativeArray<int> neuronOffsets; // Offset trong allNeurons cho mỗi entity
        [ReadOnly] public NativeArray<int> connectionOffsets; // Offset trong allConnections cho mỗi entity
        [ReadOnly] public NativeArray<int> neuronCounts; // Số lượng neurons cho mỗi entity
        [ReadOnly] public NativeArray<int> connectionCounts; // Số lượng connections cho mỗi entity
        [ReadOnly] public NativeArray<int> outputNeuronOffsets; // Offset output-neuron local index cho mỗi entity
        [ReadOnly] public NativeArray<int> outputNeuronCounts; // Số output-neuron local index cho mỗi entity
        [ReadOnly] public NativeArray<int> allOutputNeuronLocalIndices; // Flattened local neuron indices for outputs
        
        // Neuron values (read-write, separate array for mutable values)
        [NativeDisableParallelForRestriction]
        public NativeArray<float> neuronValues; // Flattened neuron values: [entity0_neuron0, entity0_neuron1, ...]
        
        // Output data (write-only)
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float> neuralOutputs; // Flattened outputs

        public void Execute(int entityIndex)
        {
            int inputCount = inputCounts[entityIndex];
            int outputCount = outputCounts[entityIndex];
            int neuronOffset = neuronOffsets[entityIndex];
            int connectionOffset = connectionOffsets[entityIndex];
            int neuronCount = neuronCounts[entityIndex];
            int connectionCount = connectionCounts[entityIndex];
            int valueOffset = neuronOffset; // Values array has same structure as neurons array

            // Reset neuron values
            for (int i = 0; i < neuronCount; i++)
            {
                neuronValues[valueOffset + i] = 0f;
            }

            // Set input values
            int inputStartIndex = inputOffsets[entityIndex];
            for (int i = 0; i < inputCount; i++)
            {
                var neuron = allNeurons[neuronOffset + i];
                if (neuron.type == 0) // Input neuron
                {
                    neuronValues[valueOffset + i] = neuralInputs[inputStartIndex + i];
                }
            }

            // Topological pass: Input -> Hidden -> Output
            for (int i = 0; i < neuronCount; i++)
            {
                var neuron = allNeurons[neuronOffset + i];
                if (neuron.type == 0) continue; // Skip input neurons (already set)

                // Tính tổng từ các connections đi vào neuron local index i.
                float sum = 0f;
                for (int j = 0; j < connectionCount; j++)
                {
                    var connection = allConnections[connectionOffset + j];
                    if (!connection.enabled || connection.toNeuronLocalIndex != i) continue;
                    if (connection.fromNeuronLocalIndex < 0 || connection.fromNeuronLocalIndex >= neuronCount) continue;
                    sum += neuronValues[valueOffset + connection.fromNeuronLocalIndex] * connection.weight;
                }

                // Áp dụng activation function
                float activated = sum + neuron.bias;
                neuronValues[valueOffset + i] = Activate(activated, neuron.activationFunction);
            }

            // Lấy output values theo mapping local index đã precompute ở collect phase.
            int outputStartIndex = outputOffsets[entityIndex];
            int outputLocalStart = outputNeuronOffsets[entityIndex];
            int mappedOutputCount = outputNeuronCounts[entityIndex];
            int finalOutputCount = math.min(outputCount, mappedOutputCount);
            for (int i = 0; i < finalOutputCount; i++)
            {
                int localNeuronIndex = allOutputNeuronLocalIndices[outputLocalStart + i];
                if (localNeuronIndex >= 0 && localNeuronIndex < neuronCount)
                {
                    neuralOutputs[outputStartIndex + i] = neuronValues[valueOffset + localNeuronIndex];
                }
            }
        }

        /// <summary>
        /// Áp dụng hàm kích hoạt (Burst-compatible)
        /// </summary>
        private float Activate(float input, int activationFunction)
        {
            switch (activationFunction)
            {
                case 0: // Sigmoid
                    return 1f / (1f + math.exp(-input));
                case 1: // Tanh
                    return math.tanh(input);
                case 2: // ReLU
                    return math.max(0f, input);
                case 3: // Linear
                    return input;
                default:
                    return 1f / (1f + math.exp(-input));
            }
        }
    }

    /// <summary>
    /// Neuron data structure cho Burst Job
    /// </summary>
    public struct JobNeuronData
    {
        public int id;
        public int type; // 0=Input, 1=Hidden, 2=Output
        public int activationFunction;
        public float bias;
        public float value;
    }

    /// <summary>
    /// Connection data structure cho Burst Job
    /// </summary>
    public struct JobConnectionData
    {
        public int innovationNumber;
        public int fromNeuronId;
        public int toNeuronId;
        public int fromNeuronLocalIndex;
        public int toNeuronLocalIndex;
        public float weight;
        public bool enabled;
    }
}

