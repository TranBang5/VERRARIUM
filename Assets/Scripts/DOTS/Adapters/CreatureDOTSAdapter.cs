using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Verrarium.Creature;
using Verrarium.Data;
using Verrarium.Evolution;
using Verrarium.DOTS.Components;
using Verrarium.DOTS.Evolution;

namespace Verrarium.DOTS.Adapters
{
    /// <summary>
    /// Adapter để chuyển đổi giữa MonoBehaviour CreatureController và ECS Entity
    /// Cho phép hệ thống chạy song song hoặc chuyển đổi dần dần
    /// </summary>
    public static class CreatureDOTSAdapter
    {
        /// <summary>
        /// Tạo Entity từ CreatureController
        /// </summary>
        public static Entity CreateEntityFromCreature(CreatureController creature, EntityManager entityManager)
        {
            Entity entity = entityManager.CreateEntity();

            // Genome Component
            GenomeComponent genomeComp = GenomeComponent.FromGenome(creature.GetGenome());
            entityManager.AddComponentData(entity, genomeComp);

            // State Component
            CreatureStateComponent stateComp = new CreatureStateComponent
            {
                energy = creature.Energy,
                maxEnergy = creature.MaxEnergy,
                health = creature.Health,
                maxHealth = creature.MaxHealth,
                maturity = creature.Maturity,
                age = creature.Age,
                lastEatTime = 0f,
                lastReproduceTime = 0f
            };
            entityManager.AddComponentData(entity, stateComp);

            // Brain Component
            NEATNetwork brain = creature.GetBrain();
            if (brain != null)
            {
                BrainComponent brainComp = new BrainComponent
                {
                    inputCount = brain.InputCount,
                    outputCount = brain.OutputCount,
                    neuronCount = brain.NeuronCount,
                    connectionCount = brain.ConnectionCount
                };
                entityManager.AddComponentData(entity, brainComp);

                // Neuron Data Buffer
                var neuronBuffer = entityManager.AddBuffer<NeuronData>(entity);
                var neurons = brain.GetNeurons();
                foreach (var neuron in neurons)
                {
                    neuronBuffer.Add(new NeuronData
                    {
                        id = neuron.id,
                        type = (int)neuron.type,
                        activationFunction = (int)neuron.activationFunction,
                        bias = neuron.bias,
                        value = neuron.value
                    });
                }

                // Connection Data Buffer
                var connectionBuffer = entityManager.AddBuffer<ConnectionData>(entity);
                var connections = brain.GetConnections();
                foreach (var conn in connections)
                {
                    connectionBuffer.Add(new ConnectionData
                    {
                        innovationNumber = conn.innovationNumber,
                        fromNeuronId = conn.fromNeuronId,
                        toNeuronId = conn.toNeuronId,
                        weight = conn.weight,
                        enabled = conn.enabled
                    });
                }
            }

            // Neural Input/Output Components
            entityManager.AddComponentData(entity, new NeuralInputComponent());
            entityManager.AddComponentData(entity, new NeuralOutputComponent());

            // Species Component
            entityManager.AddComponentData(entity, new SpeciesComponent
            {
                speciesId = -1, // Chưa được phân loại
                adjustedFitness = 0f
            });

            // Epigenetic Component
            entityManager.AddComponentData(entity, new EpigeneticComponent
            {
                learningRate = 0.01f,
                plasticity = 0.1f,
                accumulatedExperience = 0f
            });

            // Transform Component (Unity.Transforms)
            entityManager.AddComponentData(entity, new LocalTransform
            {
                Position = creature.transform.position,
                Rotation = creature.transform.rotation,
                Scale = creature.transform.localScale.x
            });

            return entity;
        }

        /// <summary>
        /// Cập nhật CreatureController từ Entity (để đồng bộ dữ liệu)
        /// </summary>
        public static void UpdateCreatureFromEntity(Entity entity, CreatureController creature, EntityManager entityManager)
        {
            if (!entityManager.Exists(entity)) return;

            // Cập nhật State
            if (entityManager.HasComponent<CreatureStateComponent>(entity))
            {
                var state = entityManager.GetComponentData<CreatureStateComponent>(entity);
                // Note: CreatureController không có setters công khai cho state
                // Cần thêm hoặc sử dụng reflection
            }

            // Cập nhật Transform
            if (entityManager.HasComponent<LocalTransform>(entity))
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                creature.transform.position = transform.Position;
                creature.transform.rotation = transform.Rotation;
                creature.transform.localScale = Vector3.one * transform.Scale;
            }
        }

        /// <summary>
        /// Chuyển đổi NEATNetwork sang BrainComponent và Buffers
        /// </summary>
        public static void ConvertBrainToComponents(Entity entity, NEATNetwork brain, EntityManager entityManager)
        {
            BrainComponent brainComp = new BrainComponent
            {
                inputCount = brain.InputCount,
                outputCount = brain.OutputCount,
                neuronCount = brain.NeuronCount,
                connectionCount = brain.ConnectionCount
            };
            entityManager.SetComponentData(entity, brainComp);

            // Clear existing buffers
            if (entityManager.HasBuffer<NeuronData>(entity))
            {
                entityManager.GetBuffer<NeuronData>(entity).Clear();
            }
            else
            {
                entityManager.AddBuffer<NeuronData>(entity);
            }

            if (entityManager.HasBuffer<ConnectionData>(entity))
            {
                entityManager.GetBuffer<ConnectionData>(entity).Clear();
            }
            else
            {
                entityManager.AddBuffer<ConnectionData>(entity);
            }

            // Add neurons
            var neuronBuffer = entityManager.GetBuffer<NeuronData>(entity);
            var neurons = brain.GetNeurons();
            foreach (var neuron in neurons)
            {
                neuronBuffer.Add(new NeuronData
                {
                    id = neuron.id,
                    type = (int)neuron.type,
                    activationFunction = (int)neuron.activationFunction,
                    bias = neuron.bias,
                    value = neuron.value
                });
            }

            // Add connections
            var connectionBuffer = entityManager.GetBuffer<ConnectionData>(entity);
            var connections = brain.GetConnections();
            foreach (var conn in connections)
            {
                connectionBuffer.Add(new ConnectionData
                {
                    innovationNumber = conn.innovationNumber,
                    fromNeuronId = conn.fromNeuronId,
                    toNeuronId = conn.toNeuronId,
                    weight = conn.weight,
                    enabled = conn.enabled
                });
            }
        }
    }
}

