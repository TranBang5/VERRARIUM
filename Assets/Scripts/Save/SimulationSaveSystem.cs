using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verrarium.Core;
using Verrarium.Creature;
using Verrarium.Resources;
using Verrarium.Data;
using Verrarium.Evolution;

namespace Verrarium.Save
{
    /// <summary>
    /// Hệ thống quản lý save/load cho simulation
    /// </summary>
    public static class SimulationSaveSystem
    {
        private const string SAVE_DIRECTORY = "Saves";
        private const int MAX_SAVE_SLOTS = 20;
        public const string AUTOSAVE_NAME = "autosave"; // Tên file autosave đặc biệt
        
        /// <summary>
        /// Đường dẫn thư mục save
        /// </summary>
        public static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY);

        /// <summary>
        /// Đảm bảo thư mục save tồn tại
        /// </summary>
        private static void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }

        /// <summary>
        /// Lưu simulation hiện tại
        /// </summary>
        public static bool Save(string saveName, SimulationSupervisor supervisor)
        {
            if (supervisor == null)
            {
                Debug.LogError("SimulationSupervisor is null!");
                return false;
            }

            try
            {
                EnsureSaveDirectory();

                // Tạo save data
                SimulationSaveData saveData = CreateSaveData(saveName, supervisor);

                // Serialize to JSON
                string json = JsonUtility.ToJson(saveData, true);

                // Lưu file
                string filePath = GetSaveFilePath(saveName);
                File.WriteAllText(filePath, json);

                Debug.Log($"Game saved successfully: {saveName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving game: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Load simulation từ file
        /// </summary>
        public static SimulationSaveData Load(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file not found: {saveName}");
                    return null;
                }

                // Đọc file
                string json = File.ReadAllText(filePath);

                // Deserialize
                SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);

                Debug.Log($"Game loaded successfully: {saveName}");
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading game: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Xóa save file
        /// </summary>
        public static bool Delete(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Save file deleted: {saveName}");
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting save file: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả save files
        /// </summary>
        public static SaveFileInfo[] GetSaveFiles()
        {
            EnsureSaveDirectory();

            if (!Directory.Exists(SavePath))
                return new SaveFileInfo[0];

            try
            {
                var files = Directory.GetFiles(SavePath, "*.json")
                    .Select(filePath =>
                    {
                        try
                        {
                            string json = File.ReadAllText(filePath);
                            SimulationSaveData data = JsonUtility.FromJson<SimulationSaveData>(json);
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            
                            bool isAutosave = fileName == AUTOSAVE_NAME || fileName.StartsWith(AUTOSAVE_NAME + "_");
                            
                            return new SaveFileInfo
                            {
                                saveName = fileName,
                                displayName = isAutosave ? "[AUTOSAVE] " + data.saveName : data.saveName,
                                saveTime = data.saveTime,
                                simulationTime = data.simulationTime,
                                currentPopulation = data.currentPopulation,
                                filePath = filePath,
                                isAutosave = isAutosave
                            };
                        }
                        catch
                        {
                            // Skip corrupted files
                            return null;
                        }
                    })
                    .Where(info => info != null)
                    .OrderByDescending(info => info.isAutosave) // Autosave luôn ở đầu
                    .ThenByDescending(info => info.saveTime) // Sau đó sắp xếp theo thời gian
                    .ToArray();

                return files;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting save files: {e.Message}");
                return new SaveFileInfo[0];
            }
        }

        /// <summary>
        /// Kiểm tra xem save name có hợp lệ không
        /// </summary>
        public static bool IsValidSaveName(string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName))
                return false;

            // Kiểm tra ký tự không hợp lệ
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !saveName.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Kiểm tra xem save slot có tồn tại không
        /// </summary>
        public static bool SaveExists(string saveName)
        {
            string filePath = GetSaveFilePath(saveName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Lấy đường dẫn file save
        /// </summary>
        private static string GetSaveFilePath(string saveName)
        {
            // Sanitize filename
            string safeName = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(SavePath, $"{safeName}.json");
        }

        /// <summary>
        /// Tạo SaveData từ SimulationSupervisor
        /// </summary>
        private static SimulationSaveData CreateSaveData(string saveName, SimulationSupervisor supervisor)
        {
            SimulationSaveData saveData = new SimulationSaveData
            {
                saveName = saveName,
                saveTime = DateTime.Now,
                simulationTime = supervisor.SimulationTime,
                totalCreaturesBorn = supervisor.TotalBorn,
                totalCreaturesDied = supervisor.TotalDied,
                currentPopulation = supervisor.CurrentPopulation,
                worldSize = supervisor.WorldSize,
                enableWorldBorder = supervisor.EnableWorldBorder,
                targetPopulationSize = supervisor.GetTargetPopulationSize(),
                maxPopulationSize = supervisor.GetMaxPopulationSize(),
                resourceSpawnInterval = supervisor.GetResourceSpawnInterval(),
                plantsPerSpawn = supervisor.GetPlantsPerSpawn()
            };

            // Lưu creatures
            var creatures = supervisor.GetActiveCreatures();
            foreach (var creature in creatures)
            {
                if (creature == null) continue;

                var creatureData = new CreatureSaveData
                {
                    genome = creature.GetGenome(),
                    brain = CreateBrainSaveData(creature.GetBrain()),
                    position = creature.transform.position,
                    rotation = creature.transform.eulerAngles.z,
                    energy = creature.Energy,
                    maxEnergy = creature.MaxEnergy,
                    health = creature.Health,
                    maxHealth = creature.MaxHealth,
                    maturity = creature.Maturity,
                    age = creature.Age
                };

                var lineage = creature.GetLineageRecord();
                if (lineage != null)
                {
                    creatureData.lineageId = lineage.LineageId.ToString();
                    creatureData.generationIndex = lineage.GenerationIndex;
                }

                saveData.creatures.Add(creatureData);
            }

            // Lưu resources
            var resources = supervisor.GetActiveResources();
            foreach (var resource in resources)
            {
                if (resource == null) continue;

                var resourceData = new ResourceSaveData
                {
                    position = resource.transform.position,
                    energyValue = resource.EnergyValue,
                    resourceType = (int)resource.Type
                };

                saveData.resources.Add(resourceData);
            }

            return saveData;
        }

        /// <summary>
        /// Tạo BrainSaveData từ NEATNetwork
        /// </summary>
        private static NEATNetworkSaveData CreateBrainSaveData(NEATNetwork brain)
        {
            if (brain == null)
                return null;

            var brainData = new NEATNetworkSaveData
            {
                inputCount = brain.InputCount,
                outputCount = brain.OutputCount
            };

            // Lưu neurons
            var neurons = brain.GetNeurons();
            foreach (var neuron in neurons)
            {
                brainData.neurons.Add(new NeuronSaveData
                {
                    id = neuron.id,
                    type = (int)neuron.type,
                    activationFunction = (int)neuron.activationFunction,
                    bias = neuron.bias
                });
            }

            // Lưu connections
            var connections = brain.GetConnections();
            foreach (var connection in connections)
            {
                brainData.connections.Add(new ConnectionSaveData
                {
                    innovationNumber = connection.innovationNumber,
                    fromNeuronId = connection.fromNeuronId,
                    toNeuronId = connection.toNeuronId,
                    weight = connection.weight,
                    enabled = connection.enabled
                });
            }

            return brainData;
        }

        /// <summary>
        /// Tạo NEATNetwork từ BrainSaveData
        /// </summary>
        public static NEATNetwork CreateBrainFromSaveData(NEATNetworkSaveData brainData)
        {
            if (brainData == null)
                return null;

            // Convert save data thành Neuron và Connection objects
            List<Neuron> neurons = new List<Neuron>();
            List<Connection> connections = new List<Connection>();

            // Tạo neurons
            foreach (var neuronData in brainData.neurons)
            {
                Neuron neuron = new Neuron(
                    neuronData.id,
                    (NeuronType)neuronData.type,
                    (ActivationFunction)neuronData.activationFunction
                );
                neuron.bias = neuronData.bias;
                neurons.Add(neuron);
            }

            // Tạo connections
            foreach (var connData in brainData.connections)
            {
                Connection connection = new Connection(
                    connData.innovationNumber,
                    connData.fromNeuronId,
                    connData.toNeuronId,
                    connData.weight,
                    connData.enabled
                );
                connections.Add(connection);
            }

            // Tạo network từ save data
            return NEATNetwork.CreateFromSaveData(
                brainData.inputCount,
                brainData.outputCount,
                neurons,
                connections
            );
        }
    }

    /// <summary>
    /// Thông tin về một save file
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string saveName;
        public string displayName;
        public DateTime saveTime;
        public float simulationTime;
        public int currentPopulation;
        public string filePath;
        public bool isAutosave = false;

        public string GetDisplayText()
        {
            return $"{displayName}\n{saveTime:yyyy-MM-dd HH:mm:ss}\nPopulation: {currentPopulation}";
        }
    }
}

