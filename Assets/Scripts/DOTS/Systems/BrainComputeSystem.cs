using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Verrarium.DOTS.Components;
using Verrarium.DOTS.Jobs;
using JobNeuronData = Verrarium.DOTS.Jobs.JobNeuronData;
using JobConnectionData = Verrarium.DOTS.Jobs.JobConnectionData;

namespace Verrarium.DOTS.Systems
{
    /// <summary>
    /// ECS System tính toán Neural Network cho tất cả sinh vật
    /// Sử dụng Burst Job để song song hóa
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BrainComputeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Collect all entities with brain components
            var query = GetEntityQuery(
                ComponentType.ReadOnly<BrainComponent>(),
                ComponentType.ReadOnly<NeuralInputComponent>(),
                ComponentType.ReadWrite<NeuralOutputComponent>()
            );

            int entityCount = query.CalculateEntityCount();
            if (entityCount == 0) return;

            // Prepare NativeArrays for job
            var neuralInputs = new NativeList<float>(Allocator.TempJob);
            var neuralOutputs = new NativeList<float>(Allocator.TempJob);
            var inputCounts = new NativeList<int>(Allocator.TempJob);
            var outputCounts = new NativeList<int>(Allocator.TempJob);
            var neuronOffsets = new NativeList<int>(Allocator.TempJob);
            var connectionOffsets = new NativeList<int>(Allocator.TempJob);
            var neuronCounts = new NativeList<int>(Allocator.TempJob);
            var connectionCounts = new NativeList<int>(Allocator.TempJob);
            var allNeurons = new NativeList<JobNeuronData>(Allocator.TempJob);
            var allConnections = new NativeList<JobConnectionData>(Allocator.TempJob);
            var neuronValues = new NativeList<float>(Allocator.TempJob);

            // Collect data from entities
            int neuronOffset = 0;
            int connectionOffset = 0;
            int valueOffset = 0;

            Entities
                .WithAll<BrainComponent, NeuralInputComponent, NeuralOutputComponent>()
                .ForEach((Entity entity, in BrainComponent brain, in NeuralInputComponent inputs, 
                    in DynamicBuffer<NeuronData> neurons, in DynamicBuffer<ConnectionData> connections) =>
                {
                    // Inputs
                    neuralInputs.Add(inputs.energyRatio);
                    neuralInputs.Add(inputs.maturity);
                    neuralInputs.Add(inputs.healthRatio);
                    neuralInputs.Add(inputs.age);
                    neuralInputs.Add(inputs.distToClosestPlant);
                    neuralInputs.Add(inputs.angleToClosestPlant);
                    neuralInputs.Add(inputs.distToClosestMeat);
                    neuralInputs.Add(inputs.angleToClosestMeat);
                    neuralInputs.Add(inputs.distToClosestCreature);
                    neuralInputs.Add(inputs.angleToClosestCreature);

                    inputCounts.Add(brain.inputCount);
                    outputCounts.Add(brain.outputCount);
                    neuronOffsets.Add(neuronOffset);
                    connectionOffsets.Add(connectionOffset);
                    neuronCounts.Add(neurons.Length);
                    connectionCounts.Add(connections.Length);

                    // Neurons
                    for (int i = 0; i < neurons.Length; i++)
                    {
                        var n = neurons[i];
                        allNeurons.Add(new JobNeuronData
                        {
                            id = n.id,
                            type = n.type,
                            activationFunction = n.activationFunction,
                            bias = n.bias,
                            value = n.value
                        });
                        neuronValues.Add(0f); // Initialize values
                    }

                    // Connections
                    for (int i = 0; i < connections.Length; i++)
                    {
                        var c = connections[i];
                        allConnections.Add(new JobConnectionData
                        {
                            innovationNumber = c.innovationNumber,
                            fromNeuronId = c.fromNeuronId,
                            toNeuronId = c.toNeuronId,
                            weight = c.weight,
                            enabled = c.enabled
                        });
                    }

                    neuronOffset += neurons.Length;
                    connectionOffset += connections.Length;
                    valueOffset += neurons.Length;
                })
                .Run(); // Run synchronously to collect data

            // Create and schedule job
            var job = new BrainComputeJob
            {
                neuralInputs = neuralInputs.AsArray(),
                neuralOutputs = neuralOutputs.AsArray(),
                inputCounts = inputCounts.AsArray(),
                outputCounts = outputCounts.AsArray(),
                neuronOffsets = neuronOffsets.AsArray(),
                connectionOffsets = connectionOffsets.AsArray(),
                neuronCounts = neuronCounts.AsArray(),
                connectionCounts = connectionCounts.AsArray(),
                allNeurons = allNeurons.AsArray(),
                allConnections = allConnections.AsArray(),
                neuronValues = neuronValues.AsArray()
            };

            JobHandle jobHandle = job.Schedule(entityCount, 32, Dependency);
            jobHandle.Complete();

            // Write outputs back to entities
            int outputIndex = 0;
            Entities
                .WithAll<NeuralOutputComponent>()
                .ForEach((ref NeuralOutputComponent outputs, in BrainComponent brain) =>
                {
                    int outputCount = brain.outputCount;
                    outputs.accelerate = neuralOutputs[outputIndex++];
                    outputs.rotate = neuralOutputs[outputIndex++];
                    outputs.layEgg = neuralOutputs[outputIndex++];
                    outputs.growth = neuralOutputs[outputIndex++];
                    outputs.heal = neuralOutputs[outputIndex++];
                    outputs.attack = neuralOutputs[outputIndex++];
                    outputs.eat = neuralOutputs[outputIndex++];
                })
                .Run();

            // Cleanup
            neuralInputs.Dispose();
            neuralOutputs.Dispose();
            inputCounts.Dispose();
            outputCounts.Dispose();
            neuronOffsets.Dispose();
            connectionOffsets.Dispose();
            neuronCounts.Dispose();
            connectionCounts.Dispose();
            allNeurons.Dispose();
            allConnections.Dispose();
            neuronValues.Dispose();
        }
    }
}

