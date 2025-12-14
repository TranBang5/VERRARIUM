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
        
        // Brain structure (read-only)
        [ReadOnly] public NativeArray<JobNeuronData> allNeurons; // Tất cả neurons của tất cả entities (structure only)
        [ReadOnly] public NativeArray<JobConnectionData> allConnections; // Tất cả connections của tất cả entities
        [ReadOnly] public NativeArray<int> neuronOffsets; // Offset trong allNeurons cho mỗi entity
        [ReadOnly] public NativeArray<int> connectionOffsets; // Offset trong allConnections cho mỗi entity
        [ReadOnly] public NativeArray<int> neuronCounts; // Số lượng neurons cho mỗi entity
        [ReadOnly] public NativeArray<int> connectionCounts; // Số lượng connections cho mỗi entity
        
        // Neuron values (read-write, separate array for mutable values)
        public NativeArray<float> neuronValues; // Flattened neuron values: [entity0_neuron0, entity0_neuron1, ...]
        
        // Output data (write-only)
        [WriteOnly] public NativeArray<float> neuralOutputs; // Flattened outputs

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
            int inputStartIndex = entityIndex * inputCount;
            for (int i = 0; i < inputCount; i++)
            {
                var neuron = allNeurons[neuronOffset + i];
                if (neuron.type == 0) // Input neuron
                {
                    neuronValues[valueOffset + i] = neuralInputs[inputStartIndex + i];
                }
            }

            // Topological sort: Input -> Hidden -> Output
            // Tính toán theo thứ tự
            for (int i = 0; i < neuronCount; i++)
            {
                var neuron = allNeurons[neuronOffset + i];
                if (neuron.type == 0) continue; // Skip input neurons (already set)

                // Tính tổng từ các connections đến neuron này
                float sum = 0f;
                for (int j = 0; j < connectionCount; j++)
                {
                    var connection = allConnections[connectionOffset + j];
                    if (!connection.enabled || connection.toNeuronId != neuron.id) continue;

                    // Tìm from neuron
                    for (int k = 0; k < neuronCount; k++)
                    {
                        var fromNeuron = allNeurons[neuronOffset + k];
                        if (fromNeuron.id == connection.fromNeuronId)
                        {
                            sum += neuronValues[valueOffset + k] * connection.weight;
                            break;
                        }
                    }
                }

                // Áp dụng activation function
                float activated = sum + neuron.bias;
                neuronValues[valueOffset + i] = Activate(activated, neuron.activationFunction);
            }

            // Lấy output values
            int outputStartIndex = entityIndex * outputCount;
            int outputNeuronStartId = inputCount; // Output neurons start after input neurons
            for (int i = 0; i < outputCount; i++)
            {
                int outputNeuronId = outputNeuronStartId + i;
                for (int j = 0; j < neuronCount; j++)
                {
                    var neuron = allNeurons[neuronOffset + j];
                    if (neuron.id == outputNeuronId && neuron.type == 2) // Output neuron
                    {
                        neuralOutputs[outputStartIndex + i] = neuronValues[valueOffset + j];
                        break;
                    }
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
        public float weight;
        public bool enabled;
    }
}

