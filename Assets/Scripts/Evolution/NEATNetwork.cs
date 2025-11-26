using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Mạng nơ-ron NEAT đầy đủ - hỗ trợ tiến hóa cấu trúc và trọng số
    /// Theo thiết kế rtNEAT từ tài liệu
    /// </summary>
    public class NEATNetwork
    {
        private List<Neuron> neurons;
        private List<Connection> connections;
        private int inputCount;
        private int outputCount;
        private InnovationTracker innovationTracker;

        // Cache để tối ưu tính toán
        private Dictionary<int, List<Connection>> connectionsByToNeuron; // Map toNeuronId -> connections

        public int InputCount => inputCount;
        public int OutputCount => outputCount;
        public int NeuronCount => neurons.Count;
        public int ConnectionCount => connections.Count;

        /// <summary>
        /// Tạo mạng NEAT tối thiểu - tất cả đầu vào kết nối trực tiếp với tất cả đầu ra
        /// </summary>
        public NEATNetwork(int inputCount, int outputCount)
        {
            this.inputCount = inputCount;
            this.outputCount = outputCount;
            this.innovationTracker = InnovationTracker.Instance;
            
            neurons = new List<Neuron>();
            connections = new List<Connection>();
            connectionsByToNeuron = new Dictionary<int, List<Connection>>();

            // Tạo nút đầu vào
            for (int i = 0; i < inputCount; i++)
            {
                neurons.Add(new Neuron(i, NeuronType.Input, ActivationFunction.Linear));
            }

            // Tạo nút đầu ra
            for (int i = 0; i < outputCount; i++)
            {
                int outputId = inputCount + i;
                neurons.Add(new Neuron(outputId, NeuronType.Output, ActivationFunction.Sigmoid));
            }

            // Kết nối tất cả đầu vào với tất cả đầu ra
            for (int i = 0; i < inputCount; i++)
            {
                for (int o = 0; o < outputCount; o++)
                {
                    int outputId = inputCount + o;
                    int innovation = innovationTracker.GetInnovationNumber(i, outputId);
                    float weight = UnityEngine.Random.Range(-1f, 1f);
                    AddConnection(i, outputId, weight, innovation);
                }
            }
        }

        /// <summary>
        /// Sao chép mạng NEAT
        /// </summary>
        public NEATNetwork(NEATNetwork parent)
        {
            this.inputCount = parent.inputCount;
            this.outputCount = parent.outputCount;
            this.innovationTracker = InnovationTracker.Instance;

            neurons = new List<Neuron>();
            connections = new List<Connection>();
            connectionsByToNeuron = new Dictionary<int, List<Connection>>();

            // Sao chép nơ-ron
            foreach (var neuron in parent.neurons)
            {
                neurons.Add(new Neuron(neuron));
            }

            // Sao chép kết nối
            foreach (var connection in parent.connections)
            {
                AddConnection(connection.fromNeuronId, connection.toNeuronId, connection.weight, connection.innovationNumber, connection.enabled);
            }
        }

        /// <summary>
        /// Tính toán đầu ra từ đầu vào
        /// </summary>
        public float[] Compute(float[] inputs)
        {
            if (inputs.Length != inputCount)
            {
                UnityEngine.Debug.LogError($"Input count mismatch: expected {inputCount}, got {inputs.Length}");
                return new float[outputCount];
            }

            // Reset giá trị nơ-ron
            foreach (var neuron in neurons)
            {
                neuron.value = 0f;
            }

            // Đặt giá trị đầu vào
            for (int i = 0; i < inputCount; i++)
            {
                neurons[i].value = inputs[i];
            }

            // Tính toán theo thứ tự topo (từ đầu vào đến đầu ra)
            // Sắp xếp nơ-ron theo thứ tự: Input -> Hidden -> Output
            var sortedNeurons = neurons.OrderBy(n => 
                n.type == NeuronType.Input ? 0 : 
                n.type == NeuronType.Hidden ? 1 : 2).ToList();

            foreach (var neuron in sortedNeurons)
            {
                if (neuron.type == NeuronType.Input)
                    continue; // Đã set giá trị

                // Tính tổng từ các kết nối đến nơ-ron này
                float sum = 0f;
                if (connectionsByToNeuron.ContainsKey(neuron.id))
                {
                    foreach (var connection in connectionsByToNeuron[neuron.id])
                    {
                        if (!connection.enabled) continue;

                        var fromNeuron = neurons.FirstOrDefault(n => n.id == connection.fromNeuronId);
                        if (fromNeuron != null)
                        {
                            sum += fromNeuron.value * connection.weight;
                        }
                    }
                }

                // Áp dụng hàm kích hoạt
                neuron.value = neuron.Activate(sum);
            }

            // Lấy giá trị đầu ra
            float[] outputs = new float[outputCount];
            for (int i = 0; i < outputCount; i++)
            {
                int outputId = inputCount + i;
                var outputNeuron = neurons.FirstOrDefault(n => n.id == outputId);
                if (outputNeuron != null)
                {
                    outputs[i] = outputNeuron.value;
                }
            }

            return outputs;
        }

        /// <summary>
        /// Thêm kết nối
        /// </summary>
        private void AddConnection(int fromId, int toId, float weight, int innovationNumber, bool enabled = true)
        {
            // Kiểm tra xem kết nối đã tồn tại chưa
            if (connections.Any(c => c.fromNeuronId == fromId && c.toNeuronId == toId))
                return;

            var connection = new Connection(innovationNumber, fromId, toId, weight, enabled);
            connections.Add(connection);

            // Cập nhật cache
            if (!connectionsByToNeuron.ContainsKey(toId))
                connectionsByToNeuron[toId] = new List<Connection>();
            connectionsByToNeuron[toId].Add(connection);
        }

        /// <summary>
        /// Thêm kết nối mới (tự động tạo innovation number)
        /// </summary>
        public void AddNewConnection(int fromId, int toId, float weight)
        {
            int innovation = innovationTracker.GetInnovationNumber(fromId, toId);
            AddConnection(fromId, toId, weight, innovation, true);
        }

        /// <summary>
        /// Xóa kết nối
        /// </summary>
        public void RemoveConnection(int fromId, int toId)
        {
            var connection = connections.FirstOrDefault(c => c.fromNeuronId == fromId && c.toNeuronId == toId);
            if (connection != null)
            {
                connections.Remove(connection);
                if (connectionsByToNeuron.ContainsKey(toId))
                    connectionsByToNeuron[toId].Remove(connection);
            }
        }

        /// <summary>
        /// Thêm nơ-ron ẩn mới bằng cách tách một kết nối hiện có
        /// </summary>
        public void AddNewNeuron(int connectionFromId, int connectionToId)
        {
            // Tìm kết nối
            var connection = connections.FirstOrDefault(c => 
                c.fromNeuronId == connectionFromId && c.toNeuronId == connectionToId);
            
            if (connection == null) return;

            // Tạo ID mới cho nơ-ron ẩn
            int newNeuronId = neurons.Count > 0 ? neurons.Max(n => n.id) + 1 : inputCount + outputCount;

            // Tạo nơ-ron ẩn mới
            var newNeuron = new Neuron(newNeuronId, NeuronType.Hidden, ActivationFunction.Sigmoid);
            neurons.Add(newNeuron);

            // Vô hiệu hóa kết nối cũ
            connection.enabled = false;

            // Tạo hai kết nối mới
            // from -> newNeuron (weight = 1.0)
            AddNewConnection(connectionFromId, newNeuronId, 1.0f);
            
            // newNeuron -> to (weight = connection.weight cũ)
            AddNewConnection(newNeuronId, connectionToId, connection.weight);
        }

        /// <summary>
        /// Xóa nơ-ron ẩn và tất cả kết nối của nó
        /// </summary>
        public void RemoveNeuron(int neuronId)
        {
            var neuron = neurons.FirstOrDefault(n => n.id == neuronId);
            if (neuron == null || neuron.type != NeuronType.Hidden) return;

            // Xóa tất cả kết nối liên quan
            var connectionsToRemove = connections.Where(c => 
                c.fromNeuronId == neuronId || c.toNeuronId == neuronId).ToList();
            
            foreach (var conn in connectionsToRemove)
            {
                connections.Remove(conn);
                if (connectionsByToNeuron.ContainsKey(conn.toNeuronId))
                    connectionsByToNeuron[conn.toNeuronId].Remove(conn);
            }

            // Xóa nơ-ron
            neurons.Remove(neuron);
        }

        /// <summary>
        /// Lấy kết nối ngẫu nhiên
        /// </summary>
        public Connection GetRandomConnection()
        {
            if (connections.Count == 0) return null;
            return connections[UnityEngine.Random.Range(0, connections.Count)];
        }

        /// <summary>
        /// Lấy nơ-ron ẩn ngẫu nhiên
        /// </summary>
        public Neuron GetRandomHiddenNeuron()
        {
            var hiddenNeurons = neurons.Where(n => n.type == NeuronType.Hidden).ToList();
            if (hiddenNeurons.Count == 0) return null;
            return hiddenNeurons[UnityEngine.Random.Range(0, hiddenNeurons.Count)];
        }

        /// <summary>
        /// Lấy hai nơ-ron ngẫu nhiên chưa được kết nối
        /// </summary>
        public (int fromId, int toId)? GetRandomUnconnectedPair()
        {
            // Lấy tất cả cặp nơ-ron có thể kết nối (không phải output -> input/hidden)
            var possiblePairs = new List<(int, int)>();
            
            foreach (var fromNeuron in neurons)
            {
                if (fromNeuron.type == NeuronType.Output) continue; // Output không thể là nguồn
                
                foreach (var toNeuron in neurons)
                {
                    if (toNeuron.type == NeuronType.Input) continue; // Input không thể là đích
                    if (fromNeuron.id == toNeuron.id) continue; // Không tự kết nối
                    
                    // Kiểm tra xem đã kết nối chưa
                    if (!connections.Any(c => c.fromNeuronId == fromNeuron.id && c.toNeuronId == toNeuron.id))
                    {
                        possiblePairs.Add((fromNeuron.id, toNeuron.id));
                    }
                }
            }

            if (possiblePairs.Count == 0) return null;
            return possiblePairs[UnityEngine.Random.Range(0, possiblePairs.Count)];
        }

        /// <summary>
        /// Lấy tất cả nơ-ron
        /// </summary>
        public List<Neuron> GetNeurons()
        {
            return new List<Neuron>(neurons);
        }

        /// <summary>
        /// Lấy tất cả kết nối
        /// </summary>
        public List<Connection> GetConnections()
        {
            return new List<Connection>(connections);
        }
    }
}

