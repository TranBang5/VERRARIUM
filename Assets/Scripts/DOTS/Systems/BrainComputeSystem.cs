using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
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
        // Persistent buffers để giảm cấp phát NativeList mỗi frame.
        private NativeList<float> neuralInputs;
        private NativeList<float> neuralOutputs;
        private NativeList<int> inputCounts;
        private NativeList<int> outputCounts;
        private NativeList<int> neuronOffsets;
        private NativeList<int> inputOffsets;
        private NativeList<int> connectionOffsets;
        private NativeList<int> neuronCounts;
        private NativeList<int> connectionCounts;
        private NativeList<JobNeuronData> allNeurons;
        private NativeList<JobConnectionData> allConnections;
        private NativeList<float> neuronValues;
        private NativeList<int> outputOffsets;
        private NativeList<int> outputNeuronOffsets;
        private NativeList<int> outputNeuronCounts;
        private NativeList<int> allOutputNeuronLocalIndices;
        private NativeList<Entity> pendingEntities;
        private NativeList<int> pendingEntityOutputStarts;
        private NativeList<int> pendingEntityOutputCounts;
        private JobHandle pendingBrainJobHandle;
        private bool hasPendingBrainResults;

        protected override void OnCreate()
        {
            neuralInputs = new NativeList<float>(Allocator.Persistent);
            neuralOutputs = new NativeList<float>(Allocator.Persistent);
            inputCounts = new NativeList<int>(Allocator.Persistent);
            outputCounts = new NativeList<int>(Allocator.Persistent);
            neuronOffsets = new NativeList<int>(Allocator.Persistent);
            inputOffsets = new NativeList<int>(Allocator.Persistent);
            connectionOffsets = new NativeList<int>(Allocator.Persistent);
            neuronCounts = new NativeList<int>(Allocator.Persistent);
            connectionCounts = new NativeList<int>(Allocator.Persistent);
            allNeurons = new NativeList<JobNeuronData>(Allocator.Persistent);
            allConnections = new NativeList<JobConnectionData>(Allocator.Persistent);
            neuronValues = new NativeList<float>(Allocator.Persistent);
            outputOffsets = new NativeList<int>(Allocator.Persistent);
            outputNeuronOffsets = new NativeList<int>(Allocator.Persistent);
            outputNeuronCounts = new NativeList<int>(Allocator.Persistent);
            allOutputNeuronLocalIndices = new NativeList<int>(Allocator.Persistent);
            pendingEntities = new NativeList<Entity>(Allocator.Persistent);
            pendingEntityOutputStarts = new NativeList<int>(Allocator.Persistent);
            pendingEntityOutputCounts = new NativeList<int>(Allocator.Persistent);
            pendingBrainJobHandle = default;
            hasPendingBrainResults = false;
        }

        protected override void OnDestroy()
        {
            if (hasPendingBrainResults)
            {
                pendingBrainJobHandle.Complete();
            }

            if (pendingEntities.IsCreated) pendingEntities.Dispose();
            if (pendingEntityOutputStarts.IsCreated) pendingEntityOutputStarts.Dispose();
            if (pendingEntityOutputCounts.IsCreated) pendingEntityOutputCounts.Dispose();
            if (neuralInputs.IsCreated) neuralInputs.Dispose();
            if (neuralOutputs.IsCreated) neuralOutputs.Dispose();
            if (inputCounts.IsCreated) inputCounts.Dispose();
            if (outputCounts.IsCreated) outputCounts.Dispose();
            if (neuronOffsets.IsCreated) neuronOffsets.Dispose();
            if (inputOffsets.IsCreated) inputOffsets.Dispose();
            if (connectionOffsets.IsCreated) connectionOffsets.Dispose();
            if (neuronCounts.IsCreated) neuronCounts.Dispose();
            if (connectionCounts.IsCreated) connectionCounts.Dispose();
            if (allNeurons.IsCreated) allNeurons.Dispose();
            if (allConnections.IsCreated) allConnections.Dispose();
            if (neuronValues.IsCreated) neuronValues.Dispose();
            if (outputOffsets.IsCreated) outputOffsets.Dispose();
            if (outputNeuronOffsets.IsCreated) outputNeuronOffsets.Dispose();
            if (outputNeuronCounts.IsCreated) outputNeuronCounts.Dispose();
            if (allOutputNeuronLocalIndices.IsCreated) allOutputNeuronLocalIndices.Dispose();
        }

        protected override void OnUpdate()
        {
            ApplyPendingBrainOutputs();

            // Collect all entities with brain components
            var query = GetEntityQuery(
                ComponentType.ReadOnly<BrainComponent>(),
                ComponentType.ReadOnly<NeuralInputComponent>(),
                ComponentType.ReadWrite<NeuralOutputComponent>(),
                ComponentType.ReadOnly<NeedsBrainUpdateTag>()
            );

            int entityCount = query.CalculateEntityCount();
            neuralInputs.Clear();
            neuralOutputs.Clear();
            inputCounts.Clear();
            outputCounts.Clear();
            neuronOffsets.Clear();
            inputOffsets.Clear();
            connectionOffsets.Clear();
            neuronCounts.Clear();
            connectionCounts.Clear();
            allNeurons.Clear();
            allConnections.Clear();
            neuronValues.Clear();
            outputOffsets.Clear();
            outputNeuronOffsets.Clear();
            outputNeuronCounts.Clear();
            allOutputNeuronLocalIndices.Clear();
            pendingEntities.Clear();
            pendingEntityOutputStarts.Clear();
            pendingEntityOutputCounts.Clear();

            if (entityCount == 0) return;

            // Collect data from entities
            int neuronOffset = 0;
            int connectionOffset = 0;
            int valueOffset = 0;
            int inputOffset = 0;
            int outputOffset = 0;

            Entities
                .ForEach((Entity entity,
                    in BrainComponent brain,
                    in NeuralInputComponent inputs,
                    in NeuralOutputComponent outputs,
                    in NeedsBrainUpdateTag _tag,
                    in DynamicBuffer<NeuronData> neurons,
                    in DynamicBuffer<ConnectionData> connections) =>
                {
                    // Inputs (mimic CreatureController.Sense() thứ tự 0..13)
                    // Lưu ý: phải thêm đúng brain.inputCount giá trị/entity, nếu không job sẽ đọc sai offset.
                    int inputCount = brain.inputCount;
                    inputOffsets.Add(inputOffset);
                    // outputOffsets theo thứ tự entity (prefix-sum)
                    outputOffsets.Add(outputOffset);
                    outputNeuronOffsets.Add(allOutputNeuronLocalIndices.Length);
                    pendingEntities.Add(entity);
                    pendingEntityOutputStarts.Add(outputOffset);
                    pendingEntityOutputCounts.Add(brain.outputCount);

                    // After we append to neuralInputs/neuralOutputs, update prefix sums.
                    for (int i = 0; i < inputCount; i++)
                    {
                        float v = 0f;
                        switch (i)
                        {
                            case 0: v = inputs.energyRatio; break;
                            case 1: v = inputs.maturity; break;
                            case 2: v = inputs.healthRatio; break;
                            case 3: v = inputs.age; break;
                            case 4: v = inputs.distToClosestPlant; break;
                            case 5: v = inputs.angleToClosestPlant; break;
                            case 6: v = inputs.distToClosestMeat; break;
                            case 7: v = inputs.angleToClosestMeat; break;
                            case 8: v = inputs.distToClosestCreature; break;
                            case 9: v = inputs.angleToClosestCreature; break;
                            case 10: v = inputs.grayscaleClosestCreature; break;
                            case 11: v = inputs.pheromoneR; break;
                            case 12: v = inputs.pheromoneG; break;
                            case 13: v = inputs.pheromoneB; break;
                        }
                        neuralInputs.Add(v);
                    }
                    inputOffset += inputCount;

                    inputCounts.Add(inputCount);
                    outputCounts.Add(brain.outputCount);
                    neuronOffsets.Add(neuronOffset);
                    connectionOffsets.Add(connectionOffset);
                    neuronCounts.Add(neurons.Length);
                    connectionCounts.Add(connections.Length);

                    // Prepare output buffer length để BrainComputeJob có thể write an toàn
                    // (NativeList.Length phải >= entityCount * outputCount).
                    for (int i = 0; i < brain.outputCount; i++)
                    {
                        neuralOutputs.Add(0f);
                    }
                    outputOffset += brain.outputCount;

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

                    // Precompute neuron id -> local index để job không phải tìm tuyến tính.
                    var neuronIndexById = new Dictionary<int, int>(neurons.Length);
                    for (int i = 0; i < neurons.Length; i++)
                    {
                        neuronIndexById[neurons[i].id] = i;
                    }

                    // Connections
                    for (int i = 0; i < connections.Length; i++)
                    {
                        var c = connections[i];
                        int fromLocal = neuronIndexById.TryGetValue(c.fromNeuronId, out var fromIndex) ? fromIndex : -1;
                        int toLocal = neuronIndexById.TryGetValue(c.toNeuronId, out var toIndex) ? toIndex : -1;
                        allConnections.Add(new JobConnectionData
                        {
                            innovationNumber = c.innovationNumber,
                            fromNeuronId = c.fromNeuronId,
                            toNeuronId = c.toNeuronId,
                            fromNeuronLocalIndex = fromLocal,
                            toNeuronLocalIndex = toLocal,
                            weight = c.weight,
                            enabled = c.enabled
                        });
                    }

                    // Precompute mapping output slot -> local neuron index.
                    // Giữ tương thích với giả định output neuron id = inputCount + i.
                    for (int i = 0; i < brain.outputCount; i++)
                    {
                        int outputNeuronId = brain.inputCount + i;
                        int localIndex = neuronIndexById.TryGetValue(outputNeuronId, out var mappedIndex) ? mappedIndex : -1;
                        allOutputNeuronLocalIndices.Add(localIndex);
                    }
                    outputNeuronCounts.Add(brain.outputCount);

                    neuronOffset += neurons.Length;
                    connectionOffset += connections.Length;
                    valueOffset += neurons.Length;
                })
                .WithoutBurst()
                .Run(); // Run synchronously to collect data

            // Create and schedule job
            var job = new BrainComputeJob
            {
                neuralInputs = neuralInputs.AsArray(),
                neuralOutputs = neuralOutputs.AsArray(),
                inputCounts = inputCounts.AsArray(),
                outputCounts = outputCounts.AsArray(),
                neuronOffsets = neuronOffsets.AsArray(),
                inputOffsets = inputOffsets.AsArray(),
                connectionOffsets = connectionOffsets.AsArray(),
                neuronCounts = neuronCounts.AsArray(),
                connectionCounts = connectionCounts.AsArray(),
                allNeurons = allNeurons.AsArray(),
                allConnections = allConnections.AsArray(),
                neuronValues = neuronValues.AsArray(),
                outputOffsets = outputOffsets.AsArray(),
                outputNeuronOffsets = outputNeuronOffsets.AsArray(),
                outputNeuronCounts = outputNeuronCounts.AsArray(),
                allOutputNeuronLocalIndices = allOutputNeuronLocalIndices.AsArray()
            };

            pendingBrainJobHandle = job.Schedule(entityCount, 32, Dependency);
            hasPendingBrainResults = true;
            Dependency = pendingBrainJobHandle;

        }

        private void ApplyPendingBrainOutputs()
        {
            if (!hasPendingBrainResults) return;

            pendingBrainJobHandle.Complete();
            hasPendingBrainResults = false;

            for (int entityIndex = 0; entityIndex < pendingEntities.Length; entityIndex++)
            {
                Entity entity = pendingEntities[entityIndex];
                if (!EntityManager.Exists(entity)) continue;
                if (!EntityManager.HasComponent<NeuralOutputComponent>(entity)) continue;

                var outputs = EntityManager.GetComponentData<NeuralOutputComponent>(entity);
                int outputStart = pendingEntityOutputStarts[entityIndex];
                int outputCount = pendingEntityOutputCounts[entityIndex];

                for (int i = 0; i < outputCount; i++)
                {
                    float v = neuralOutputs[outputStart + i];
                    switch (i)
                    {
                        case 0: outputs.accelerate = v; break;
                        case 1: outputs.rotate = v; break;
                        case 2: outputs.layEgg = v; break;
                        case 3: outputs.growth = v; break;
                        case 4: outputs.heal = v; break;
                        case 5: outputs.attack = v; break;
                        case 6: outputs.eat = v; break;
                        case 7: outputs.pheromoneOutput = v; break;
                    }
                }

                EntityManager.SetComponentData(entity, outputs);

                if (EntityManager.HasComponent<NeedsBrainUpdateTag>(entity))
                {
                    EntityManager.SetComponentEnabled<NeedsBrainUpdateTag>(entity, false);
                }
            }
        }
    }
}

