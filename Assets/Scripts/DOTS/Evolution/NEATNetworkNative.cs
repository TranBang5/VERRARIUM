using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Verrarium.Evolution;

namespace Verrarium.DOTS.Evolution
{
    /// <summary>
    /// Wrapper class để chuyển đổi NEATNetwork class-based sang NativeArrays
    /// Cho phép tương thích với Burst Compiler và Job System
    /// </summary>
    public class NEATNetworkNative
    {
        public NativeList<NeuronDataNative> neurons;
        public NativeList<ConnectionDataNative> connections;
        public int inputCount;
        public int outputCount;

        public NEATNetworkNative(int inputCount, int outputCount, Allocator allocator = Allocator.TempJob)
        {
            this.inputCount = inputCount;
            this.outputCount = outputCount;
            neurons = new NativeList<NeuronDataNative>(inputCount + outputCount, allocator);
            connections = new NativeList<ConnectionDataNative>(inputCount * outputCount, allocator);
        }

        /// <summary>
        /// Chuyển đổi từ NEATNetwork class sang NativeArrays
        /// </summary>
        public static NEATNetworkNative FromNEATNetwork(NEATNetwork network, Allocator allocator = Allocator.TempJob)
        {
            var native = new NEATNetworkNative(network.InputCount, network.OutputCount, allocator);
            
            var neurons = network.GetNeurons();
            var conns = network.GetConnections();

            foreach (var neuron in neurons)
            {
                native.neurons.Add(new NeuronDataNative
                {
                    id = neuron.id,
                    type = (int)neuron.type,
                    activationFunction = (int)neuron.activationFunction,
                    bias = neuron.bias,
                    value = neuron.value
                });
            }

            foreach (var conn in conns)
            {
                native.connections.Add(new ConnectionDataNative
                {
                    innovationNumber = conn.innovationNumber,
                    fromNeuronId = conn.fromNeuronId,
                    toNeuronId = conn.toNeuronId,
                    weight = conn.weight,
                    enabled = conn.enabled
                });
            }

            return native;
        }

        /// <summary>
        /// Chuyển đổi từ NativeArrays về NEATNetwork class
        /// </summary>
        public NEATNetwork ToNEATNetwork()
        {
            var network = new NEATNetwork(inputCount, outputCount);
            
            // Clear default connections
            var defaultConns = network.GetConnections();
            foreach (var conn in defaultConns)
            {
                network.RemoveConnection(conn.fromNeuronId, conn.toNeuronId);
            }

            // Add neurons
            for (int i = 0; i < neurons.Length; i++)
            {
                var n = neurons[i];
                if (n.type != 0 && n.type != 2) // Not input or output
                {
                    // Add hidden neuron by splitting a connection
                    // This is a simplified approach - in practice, you'd need to reconstruct the full topology
                }
            }

            // Add connections
            for (int i = 0; i < connections.Length; i++)
            {
                var c = connections[i];
                network.AddNewConnection(c.fromNeuronId, c.toNeuronId, c.weight);
                if (!c.enabled)
                {
                    network.RemoveConnection(c.fromNeuronId, c.toNeuronId);
                }
            }

            return network;
        }

        public void Dispose()
        {
            if (neurons.IsCreated) neurons.Dispose();
            if (connections.IsCreated) connections.Dispose();
        }
    }

    /// <summary>
    /// Neuron data structure cho NativeArrays
    /// </summary>
    public struct NeuronDataNative
    {
        public int id;
        public int type;
        public int activationFunction;
        public float bias;
        public float value;
    }

    /// <summary>
    /// Connection data structure cho NativeArrays
    /// </summary>
    public struct ConnectionDataNative
    {
        public int innovationNumber;
        public int fromNeuronId;
        public int toNeuronId;
        public float weight;
        public bool enabled;
    }
}

