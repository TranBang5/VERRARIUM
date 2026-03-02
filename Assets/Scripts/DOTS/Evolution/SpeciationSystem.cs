using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Collections;
using Verrarium.Evolution;

namespace Verrarium.DOTS.Evolution
{
    /// <summary>
    /// Hệ thống Speciation (Phân loài) cho NEAT
    /// Triển khai Fitness Sharing để bảo vệ các đột biến mới lạ
    /// </summary>
    public class SpeciationSystem
    {
        private Dictionary<int, Species> speciesMap;
        private int nextSpeciesId = 1;
        // Ngưỡng tương thích để phân loài (giảm để dễ tách loài hơn cho mục đích nghiên cứu)
        private const float COMPATIBILITY_THRESHOLD = 1.5f;
        private const float C1 = 1.0f; // Hệ số cho excess genes
        private const float C2 = 1.0f; // Hệ số cho disjoint genes
        private const float C3 = 0.4f; // Hệ số cho weight differences

        public SpeciationSystem()
        {
            speciesMap = new Dictionary<int, Species>();
        }

        /// <summary>
        /// Tính toán khoảng cách tương thích giữa hai mạng NEAT
        /// Dựa trên công thức trong bài báo NEAT gốc
        /// </summary>
        public float ComputeCompatibilityDistance(NEATNetwork network1, NEATNetwork network2)
        {
            var conns1 = network1.GetConnections();
            var conns2 = network2.GetConnections();

            // Tạo dictionaries để tra cứu nhanh
            var dict1 = new Dictionary<int, Connection>();
            var dict2 = new Dictionary<int, Connection>();

            foreach (var conn in conns1)
            {
                dict1[conn.innovationNumber] = conn;
            }

            foreach (var conn in conns2)
            {
                dict2[conn.innovationNumber] = conn;
            }

            // Tìm excess, disjoint, và matching genes
            int excess = 0;
            int disjoint = 0;
            float weightDiffSum = 0f;
            int matchingCount = 0;

            int maxInnovation1 = conns1.Count > 0 ? conns1.Max(c => c.innovationNumber) : 0;
            int maxInnovation2 = conns2.Count > 0 ? conns2.Max(c => c.innovationNumber) : 0;
            int maxInnovation = System.Math.Max(maxInnovation1, maxInnovation2);

            for (int i = 1; i <= maxInnovation; i++)
            {
                bool has1 = dict1.ContainsKey(i);
                bool has2 = dict2.ContainsKey(i);

                if (has1 && has2)
                {
                    // Matching gene
                    matchingCount++;
                    weightDiffSum += System.Math.Abs(dict1[i].weight - dict2[i].weight);
                }
                else if (has1 || has2)
                {
                    // Disjoint or excess
                    if (i > System.Math.Min(maxInnovation1, maxInnovation2))
                    {
                        excess++;
                    }
                    else
                    {
                        disjoint++;
                    }
                }
            }

            // Normalize
            int N = System.Math.Max(conns1.Count, conns2.Count);
            if (N < 20) N = 1; // Avoid division by small numbers

            float avgWeightDiff = matchingCount > 0 ? weightDiffSum / matchingCount : 0f;

            // Compatibility distance formula
            float distance = (C1 * excess) / N + (C2 * disjoint) / N + C3 * avgWeightDiff;

            return distance;
        }

        /// <summary>
        /// Phân loại một mạng vào một loài (tìm loài tương thích hoặc tạo mới)
        /// </summary>
        public int ClassifyToSpecies(NEATNetwork network)
        {
            // Tìm loài tương thích nhất
            foreach (var species in speciesMap.Values)
            {
                if (species.representative == null) continue;

                float distance = ComputeCompatibilityDistance(network, species.representative);
                if (distance < COMPATIBILITY_THRESHOLD)
                {
                    species.members.Add(network);
                    return species.id;
                }
            }

            // Không tìm thấy loài tương thích - tạo loài mới
            int newSpeciesId = nextSpeciesId++;
            var newSpecies = new Species
            {
                id = newSpeciesId,
                representative = network,
                members = new List<NEATNetwork> { network }
            };
            speciesMap[newSpeciesId] = newSpecies;

            return newSpeciesId;
        }

        /// <summary>
        /// Tính toán adjusted fitness cho mỗi loài (Fitness Sharing)
        /// </summary>
        public void ComputeAdjustedFitness(Dictionary<NEATNetwork, float> fitnessMap)
        {
            foreach (var species in speciesMap.Values)
            {
                float speciesFitnessSum = 0f;
                int memberCount = 0;

                foreach (var member in species.members)
                {
                    if (fitnessMap.ContainsKey(member))
                    {
                        float rawFitness = fitnessMap[member];
                        float adjustedFitness = rawFitness / species.members.Count; // Fitness sharing
                        speciesFitnessSum += adjustedFitness;
                        memberCount++;
                    }
                }

                species.averageFitness = memberCount > 0 ? speciesFitnessSum / memberCount : 0f;
            }
        }

        /// <summary>
        /// Lấy adjusted fitness cho một mạng
        /// </summary>
        public float GetAdjustedFitness(NEATNetwork network, int speciesId, float rawFitness)
        {
            if (!speciesMap.ContainsKey(speciesId)) return rawFitness;

            var species = speciesMap[speciesId];
            return rawFitness / species.members.Count; // Fitness sharing
        }

        /// <summary>
        /// Cập nhật đại diện của loài (chọn mạng tốt nhất)
        /// </summary>
        public void UpdateSpeciesRepresentatives(Dictionary<NEATNetwork, float> fitnessMap)
        {
            foreach (var species in speciesMap.Values)
            {
                NEATNetwork bestMember = null;
                float bestFitness = float.MinValue;

                foreach (var member in species.members)
                {
                    if (fitnessMap.ContainsKey(member))
                    {
                        float fitness = fitnessMap[member];
                        if (fitness > bestFitness)
                        {
                            bestFitness = fitness;
                            bestMember = member;
                        }
                    }
                }

                if (bestMember != null)
                {
                    species.representative = bestMember;
                }
            }
        }

        /// <summary>
        /// Xóa các loài không có thành viên
        /// </summary>
        public void RemoveEmptySpecies()
        {
            var toRemove = new List<int>();
            foreach (var kvp in speciesMap)
            {
                if (kvp.Value.members.Count == 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                speciesMap.Remove(id);
            }
        }

        public void Reset()
        {
            speciesMap.Clear();
            nextSpeciesId = 1;
        }

        private class Species
        {
            public int id;
            public NEATNetwork representative;
            public List<NEATNetwork> members;
            public float averageFitness;
        }
    }
}

