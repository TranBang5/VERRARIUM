using System.Collections.Generic;
using Verrarium.Evolution;

namespace Verrarium.DOTS.Evolution
{
    /// <summary>
    /// Hệ thống Epigenetics - Hebbian Learning và truyền trạng thái cho thế hệ sau
    /// Cho phép sinh vật thay đổi nhẹ trạng thái mạng nơ-ron trong đời sống
    /// </summary>
    public class EpigeneticsSystem
    {
        private const float DEFAULT_LEARNING_RATE = 0.01f;
        private const float DEFAULT_PLASTICITY = 0.1f;
        private const float HERITABILITY = 0.3f; // 30% trạng thái được truyền cho con

        /// <summary>
        /// Áp dụng Hebbian Learning: "Neurons that fire together, wire together"
        /// Tăng cường trọng số của các kết nối được sử dụng thường xuyên
        /// </summary>
        public void ApplyHebbianLearning(NEATNetwork network, float[] inputs, float[] outputs, float learningRate, float plasticity)
        {
            var connections = network.GetConnections();
            var neurons = network.GetNeurons();

            // Tạo dictionary để tra cứu nhanh
            var neuronDict = new Dictionary<int, Neuron>();
            foreach (var neuron in neurons)
            {
                neuronDict[neuron.id] = neuron;
            }

            // Tính toán activation của mỗi neuron
            network.Compute(inputs); // Đảm bảo values đã được tính

            // Áp dụng Hebbian rule cho mỗi connection
            foreach (var connection in connections)
            {
                if (!connection.enabled) continue;

                var fromNeuron = neuronDict[connection.fromNeuronId];
                var toNeuron = neuronDict[connection.toNeuronId];

                // Hebbian rule: Δw = η * (pre * post)
                // η = learning rate, pre = from neuron activation, post = to neuron activation
                float preActivation = fromNeuron.value;
                float postActivation = toNeuron.value;

                // Chỉ tăng cường nếu cả hai đều kích hoạt
                if (preActivation > 0.1f && postActivation > 0.1f)
                {
                    float weightChange = learningRate * preActivation * postActivation * plasticity;
                    connection.weight = System.Math.Max(-5f, System.Math.Min(5f, connection.weight + weightChange));
                }
            }
        }

        /// <summary>
        /// Lưu trữ trạng thái tích lũy (accumulated experience) để truyền cho thế hệ sau
        /// </summary>
        public Dictionary<int, float> AccumulateExperience(NEATNetwork network, float[] inputs, float[] outputs)
        {
            var experience = new Dictionary<int, float>();
            var connections = network.GetConnections();

            network.Compute(inputs);

            foreach (var connection in connections)
            {
                if (!connection.enabled) continue;

                // Tích lũy "kinh nghiệm" dựa trên mức độ sử dụng
                float usage = System.Math.Abs(connection.weight * inputs.Length); // Simplified metric
                if (experience.ContainsKey(connection.innovationNumber))
                {
                    experience[connection.innovationNumber] += usage;
                }
                else
                {
                    experience[connection.innovationNumber] = usage;
                }
            }

            return experience;
        }

        /// <summary>
        /// Truyền một phần trạng thái tích lũy cho mạng con (Baldwin Effect)
        /// </summary>
        public void InheritEpigeneticState(NEATNetwork childNetwork, Dictionary<int, float> parentExperience)
        {
            var connections = childNetwork.GetConnections();

            foreach (var connection in connections)
            {
                if (parentExperience.ContainsKey(connection.innovationNumber))
                {
                    // Điều chỉnh trọng số dựa trên kinh nghiệm của cha mẹ
                    float parentInfluence = parentExperience[connection.innovationNumber] * HERITABILITY;
                    connection.weight = System.Math.Max(-5f, System.Math.Min(5f, connection.weight + parentInfluence * 0.1f));
                }
            }
        }

        /// <summary>
        /// Tính toán plasticity dựa trên tuổi và trải nghiệm
        /// Sinh vật trẻ có plasticity cao hơn, giảm dần theo tuổi
        /// </summary>
        public float ComputePlasticity(float age, float maxAge, float basePlasticity = DEFAULT_PLASTICITY)
        {
            float ageFactor = 1f - (age / maxAge); // Giảm dần từ 1.0 về 0.0
            return basePlasticity * ageFactor;
        }

        /// <summary>
        /// Tính toán learning rate dựa trên năng lượng và trạng thái
        /// Sinh vật có năng lượng cao học nhanh hơn
        /// </summary>
        public float ComputeLearningRate(float energyRatio, float baseLearningRate = DEFAULT_LEARNING_RATE)
        {
            return baseLearningRate * energyRatio; // Tỷ lệ với năng lượng
        }
    }
}

