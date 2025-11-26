using UnityEngine;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Hệ thống đột biến NEAT - triển khai tất cả các toán tử đột biến từ Bảng 3
    /// </summary>
    public static class NEATMutator
    {
        // Xác suất mặc định cho mỗi loại đột biến
        private const float PROB_CHANGE_WEIGHT = 0.8f;
        private const float PROB_FLIP_WEIGHT = 0.1f;
        private const float PROB_TOGGLE_SYNAPSE = 0.1f;
        private const float PROB_ADD_SYNAPSE = 0.3f;
        private const float PROB_REMOVE_SYNAPSE = 0.2f;
        private const float PROB_CHANGE_ACTIVATION = 0.1f;
        private const float PROB_ADD_NEURON = 0.05f; // Hiếm hơn vì quan trọng hơn
        private const float PROB_REMOVE_NEURON = 0.02f; // Rất hiếm

        /// <summary>
        /// Áp dụng đột biến cho mạng NEAT dựa trên số lượng đột biến
        /// </summary>
        public static void Mutate(NEATNetwork network, int numMutations)
        {
            for (int i = 0; i < numMutations; i++)
            {
                ApplyRandomMutation(network);
            }
        }

        /// <summary>
        /// Áp dụng một đột biến ngẫu nhiên
        /// </summary>
        private static void ApplyRandomMutation(NEATNetwork network)
        {
            float roll = Random.Range(0f, 1f);
            float cumulative = 0f;

            // Đột biến cấu trúc (ưu tiên thấp hơn nhưng quan trọng)
            cumulative += PROB_ADD_NEURON;
            if (roll < cumulative)
            {
                Mutate_AddNewNeuron(network);
                return;
            }

            cumulative += PROB_REMOVE_NEURON;
            if (roll < cumulative)
            {
                Mutate_RemoveExistingNeuron(network);
                return;
            }

            cumulative += PROB_ADD_SYNAPSE;
            if (roll < cumulative)
            {
                Mutate_AddNewSynapse(network);
                return;
            }

            cumulative += PROB_REMOVE_SYNAPSE;
            if (roll < cumulative)
            {
                Mutate_RemoveExistingSynapse(network);
                return;
            }

            // Đột biến trọng số và trạng thái (phổ biến hơn)
            cumulative += PROB_CHANGE_WEIGHT;
            if (roll < cumulative)
            {
                Mutate_ChangeSynapseStrength(network);
                return;
            }

            cumulative += PROB_FLIP_WEIGHT;
            if (roll < cumulative)
            {
                Mutate_FlipSynapseStrength(network);
                return;
            }

            cumulative += PROB_TOGGLE_SYNAPSE;
            if (roll < cumulative)
            {
                Mutate_ToggleSynapse(network);
                return;
            }

            cumulative += PROB_CHANGE_ACTIVATION;
            if (roll < cumulative)
            {
                Mutate_ChangeNeuronActivation(network);
                return;
            }

            // Fallback: đột biến trọng số
            Mutate_ChangeSynapseStrength(network);
        }

        /// <summary>
        /// Đột biến 1: Thay đổi trọng số của một kết nối hiện có
        /// </summary>
        public static void Mutate_ChangeSynapseStrength(NEATNetwork network)
        {
            var connection = network.GetRandomConnection();
            if (connection == null) return;

            float change = Random.Range(-0.5f, 0.5f);
            connection.weight = Mathf.Clamp(connection.weight + change, -5f, 5f);
        }

        /// <summary>
        /// Đột biến 2: Đảo ngược dấu của trọng số
        /// </summary>
        public static void Mutate_FlipSynapseStrength(NEATNetwork network)
        {
            var connection = network.GetRandomConnection();
            if (connection == null) return;

            connection.weight = -connection.weight;
        }

        /// <summary>
        /// Đột biến 3: Bật/tắt kết nối
        /// </summary>
        public static void Mutate_ToggleSynapse(NEATNetwork network)
        {
            var connection = network.GetRandomConnection();
            if (connection == null) return;

            connection.enabled = !connection.enabled;
        }

        /// <summary>
        /// Đột biến 4: Thêm kết nối mới
        /// </summary>
        public static void Mutate_AddNewSynapse(NEATNetwork network)
        {
            var pair = network.GetRandomUnconnectedPair();
            if (pair == null) return;

            float weight = Random.Range(-1f, 1f);
            network.AddNewConnection(pair.Value.fromId, pair.Value.toId, weight);
        }

        /// <summary>
        /// Đột biến 5: Xóa kết nối hiện có
        /// </summary>
        public static void Mutate_RemoveExistingSynapse(NEATNetwork network)
        {
            var connection = network.GetRandomConnection();
            if (connection == null) return;

            // Không xóa nếu chỉ còn ít kết nối (tránh phá vỡ mạng)
            if (network.ConnectionCount <= network.InputCount + network.OutputCount)
                return;

            network.RemoveConnection(connection.fromNeuronId, connection.toNeuronId);
        }

        /// <summary>
        /// Đột biến 6: Thay đổi hàm kích hoạt của nơ-ron ẩn
        /// </summary>
        public static void Mutate_ChangeNeuronActivation(NEATNetwork network)
        {
            var neuron = network.GetRandomHiddenNeuron();
            if (neuron == null) return;

            // Chọn hàm kích hoạt mới ngẫu nhiên
            var functions = new[] { 
                ActivationFunction.Sigmoid, 
                ActivationFunction.Tanh, 
                ActivationFunction.ReLU,
                ActivationFunction.Linear 
            };
            neuron.activationFunction = functions[Random.Range(0, functions.Length)];
        }

        /// <summary>
        /// Đột biến 7: Thêm nơ-ron ẩn mới bằng cách tách kết nối
        /// </summary>
        public static void Mutate_AddNewNeuron(NEATNetwork network)
        {
            var connection = network.GetRandomConnection();
            if (connection == null || !connection.enabled) return;

            // Tách kết nối này
            network.AddNewNeuron(connection.fromNeuronId, connection.toNeuronId);
        }

        /// <summary>
        /// Đột biến 8: Xóa nơ-ron ẩn và tất cả kết nối của nó
        /// </summary>
        public static void Mutate_RemoveExistingNeuron(NEATNetwork network)
        {
            var neuron = network.GetRandomHiddenNeuron();
            if (neuron == null) return;

            // Không xóa nếu chỉ còn ít nơ-ron
            if (network.NeuronCount <= network.InputCount + network.OutputCount + 1)
                return;

            network.RemoveNeuron(neuron.id);
        }
    }
}

