using System;
using System.Collections.Generic;
using UnityEngine;
using Verrarium.Data;
using Verrarium.Evolution;

namespace Verrarium.Save
{
    /// <summary>
    /// Dữ liệu lưu trữ toàn bộ trạng thái giả lập
    /// </summary>
    [Serializable]
    public class SimulationSaveData
    {
        // Metadata
        public string version = "1.0";
        public string saveName;
        public DateTime saveTime;
        public DateTime simulationStartTime;
        public float simulationTime;
        
        // Statistics
        public int totalCreaturesBorn;
        public int totalCreaturesDied;
        public int currentPopulation;
        
        // World Settings
        public Vector2 worldSize;
        public bool enableWorldBorder;
        public bool useHexGrid;
        
        // Simulation Settings
        public int targetPopulationSize;
        public int maxPopulationSize;
        public float resourceSpawnInterval;
        public int plantsPerSpawn;
        public int maxResources;
        public float resourceDecayTime;
        public float resourceSpawnPopulationThreshold;
        
        // Creatures
        public List<CreatureSaveData> creatures = new List<CreatureSaveData>();
        
        // Resources
        public List<ResourceSaveData> resources = new List<ResourceSaveData>();
    }

    /// <summary>
    /// Dữ liệu lưu trữ một sinh vật
    /// </summary>
    [Serializable]
    public class CreatureSaveData
    {
        public Genome genome;
        public NEATNetworkSaveData brain;
        public Vector2 position;
        public float rotation;
        public float energy;
        public float maxEnergy;
        public float health;
        public float maxHealth;
        public float maturity;
        public float age;
        public float lastEatTime;
        public float lastReproduceTime;
        // Evolution stats
        public int offspringCount;
        public string lineageId;
        public int generationIndex;
        // Genus / Species phân cấp
        public int genusId = -1;           // Genus ID (-1 = chưa phân loại)
        public int speciesInGenusId = -1;  // Species ID trong Genus (-1 = chưa phân loại)
        // Trường cũ giữ lại để tương thích (được gán cùng giá trị speciesInGenusId)
        public int speciesId = -1;
    }

    /// <summary>
    /// Dữ liệu lưu trữ một tài nguyên
    /// </summary>
    [Serializable]
    public class ResourceSaveData
    {
        public Vector2 position;
        public float energyValue;
        public int resourceType; // 0 = Plant, 1 = Meat
        public float spawnTime; // Thời gian spawn để tính decay
    }

    /// <summary>
    /// Dữ liệu lưu trữ mạng NEAT
    /// </summary>
    [Serializable]
    public class NEATNetworkSaveData
    {
        public int inputCount;
        public int outputCount;
        public List<NeuronSaveData> neurons = new List<NeuronSaveData>();
        public List<ConnectionSaveData> connections = new List<ConnectionSaveData>();
    }

    /// <summary>
    /// Dữ liệu lưu trữ một nơ-ron
    /// </summary>
    [Serializable]
    public class NeuronSaveData
    {
        public int id;
        public int type; // 0 = Input, 1 = Hidden, 2 = Output
        public int activationFunction; // 0 = Sigmoid, 1 = Tanh, 2 = ReLU, 3 = Linear
        public float bias;
    }

    /// <summary>
    /// Dữ liệu lưu trữ một kết nối
    /// </summary>
    [Serializable]
    public class ConnectionSaveData
    {
        public int innovationNumber;
        public int fromNeuronId;
        public int toNeuronId;
        public float weight;
        public bool enabled;
    }
}

