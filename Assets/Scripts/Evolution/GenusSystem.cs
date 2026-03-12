using System.Collections.Generic;
using Verrarium.Data;
using Verrarium.DOTS.Evolution;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Hệ thống phân loại Genus/Species hai tầng dựa trên:
    /// - Genus: kết hợp khoảng cách não NEAT + genome (GenusDistance).
    /// - Species: khoảng cách genome bên trong cùng Genus.
    /// </summary>
    public class GenusSystem
    {
        private class Member
        {
            public NEATNetwork brain;
            public Genome genome;
        }

        private class SpeciesInGenus
        {
            public int id;
            public Genome representativeGenome;
            public NEATNetwork representativeBrain;
            public readonly List<Member> members = new();

            // Epoch theo thế hệ trong loài
            public int currentMaxGeneration;
            public int lastEpochGeneration;
        }

        private class Genus
        {
            public int id;
            public Genome representativeGenome;
            public NEATNetwork representativeBrain;
            public readonly List<SpeciesInGenus> species = new();
        }

        private readonly SpeciationSystem speciationSystem;
        private readonly Dictionary<int, Genus> genera = new();
        private int nextGenusId = 1;
        private int nextSpeciesId = 1;

        // Ngưỡng tách Genus/Species - đã hạ thấp để chia loài/chi dễ hơn
        private const float GENUS_DISTANCE_THRESHOLD = 2.0f;
        private const float SPECIES_DISTANCE_THRESHOLD = 1.0f;
        // Số thế hệ trong một loài trước khi cập nhật representative (epoch)
        private const int EPOCH_GENERATION_STEP = 10;

        public GenusSystem(SpeciationSystem speciationSystem)
        {
            this.speciationSystem = speciationSystem;
        }

        /// <summary>
        /// Phân loại một cá thể vào Genus và Species (tạo mới nếu cần).
        /// Trả về (genusId, speciesId).
        /// generationIndex: GenerationIndex của cá thể (từ CreatureLineageRecord).
        /// </summary>
        public (int genusId, int speciesId) Classify(Genome genome, NEATNetwork brain, int generationIndex)
        {
            if (brain == null)
            {
                // Không có brain -> không phân loại
                return (-1, -1);
            }

            // 1. Tìm Genus phù hợp nhất (hoặc tạo mới)
            Genus targetGenus = null;
            float bestGenusDistance = float.MaxValue;

            foreach (var g in genera.Values)
            {
                float dGenus = GenomeDistance.ComputeGenusDistance(
                    brain, genome,
                    g.representativeBrain, g.representativeGenome,
                    speciationSystem);

                if (dGenus < bestGenusDistance)
                {
                    bestGenusDistance = dGenus;
                    targetGenus = g;
                }
            }

            if (targetGenus == null || bestGenusDistance > GENUS_DISTANCE_THRESHOLD)
            {
                // Tạo Genus mới
                int newGenusId = nextGenusId++;
                targetGenus = new Genus
                {
                    id = newGenusId,
                    representativeGenome = genome,
                    representativeBrain = brain
                };
                genera[newGenusId] = targetGenus;
            }

            // 2. Bên trong Genus, tìm Species phù hợp nhất (hoặc tạo mới)
            SpeciesInGenus targetSpecies = null;
            float bestSpeciesDistance = float.MaxValue;

            foreach (var s in targetGenus.species)
            {
                float dGenome = GenomeDistance.ComputeGenomeDistance(
                    genome,
                    s.representativeGenome);

                if (dGenome < bestSpeciesDistance)
                {
                    bestSpeciesDistance = dGenome;
                    targetSpecies = s;
                }
            }

            if (targetSpecies == null || bestSpeciesDistance > SPECIES_DISTANCE_THRESHOLD)
            {
                int newSpeciesId = nextSpeciesId++;
                targetSpecies = new SpeciesInGenus
                {
                    id = newSpeciesId,
                    representativeGenome = genome,
                    representativeBrain = brain,
                    currentMaxGeneration = generationIndex,
                    lastEpochGeneration = generationIndex
                };
                targetGenus.species.Add(targetSpecies);
            }
            else
            {
                // Cập nhật thế hệ tối đa trong loài
                if (generationIndex > targetSpecies.currentMaxGeneration)
                {
                    targetSpecies.currentMaxGeneration = generationIndex;
                }
            }

            // 3. Ghi nhận membership
            targetSpecies.members.Add(new Member
            {
                brain = brain,
                genome = genome
            });

            return (targetGenus.id, targetSpecies.id);
        }

        /// <summary>
        /// Đăng ký một cá thể đã có sẵn Genus/Species ID (từ save).
        /// Nếu Genus/Species chưa tồn tại, sẽ tạo mới và dùng cá thể này làm representative ban đầu.
        /// </summary>
        public void RegisterExisting(int genusId, int speciesId, Genome genome, NEATNetwork brain, int generationIndex)
        {
            if (brain == null || genusId < 0 || speciesId < 0)
            {
                return;
            }

            if (!genera.TryGetValue(genusId, out var genus))
            {
                genus = new Genus
                {
                    id = genusId,
                    representativeGenome = genome,
                    representativeBrain = brain
                };
                genera[genusId] = genus;
                if (genusId >= nextGenusId)
                {
                    nextGenusId = genusId + 1;
                }
            }

            SpeciesInGenus species = null;
            foreach (var s in genus.species)
            {
                if (s.id == speciesId)
                {
                    species = s;
                    break;
                }
            }

            if (species == null)
            {
                species = new SpeciesInGenus
                {
                    id = speciesId,
                    representativeGenome = genome,
                    representativeBrain = brain,
                    currentMaxGeneration = generationIndex,
                    lastEpochGeneration = generationIndex
                };
                genus.species.Add(species);
                if (speciesId >= nextSpeciesId)
                {
                    nextSpeciesId = speciesId + 1;
                }
            }

            if (generationIndex > species.currentMaxGeneration)
            {
                species.currentMaxGeneration = generationIndex;
            }

            species.members.Add(new Member
            {
                brain = brain,
                genome = genome
            });
        }

        /// <summary>
        /// Cập nhật representative cho mỗi Genus/Species dựa trên fitnessMap (ví dụ Age),
        /// nhưng chỉ khi loài đã vượt qua một epoch thế hệ (EPOCH_GENERATION_STEP).
        /// </summary>
        public void UpdateRepresentatives(Dictionary<NEATNetwork, float> fitnessMap)
        {
            if (fitnessMap == null || fitnessMap.Count == 0)
            {
                return;
            }

            foreach (var genus in genera.Values)
            {
                NEATNetwork bestGenusBrain = genus.representativeBrain;
                Genome bestGenusGenome = genus.representativeGenome;
                float bestGenusFitness = float.MinValue;
                bool genusUpdatedThisEpoch = false;

                foreach (var species in genus.species)
                {
                    // Kiểm tra xem loài này đã qua đủ số thế hệ để cập nhật representative chưa
                    if (species.currentMaxGeneration < species.lastEpochGeneration + EPOCH_GENERATION_STEP)
                    {
                        continue;
                    }

                    Member bestSpeciesMember = null;
                    float bestSpeciesFitness = float.MinValue;

                    foreach (var member in species.members)
                    {
                        if (member == null || member.brain == null)
                            continue;

                        if (!fitnessMap.TryGetValue(member.brain, out float fitness))
                            continue;

                        if (fitness > bestSpeciesFitness)
                        {
                            bestSpeciesFitness = fitness;
                            bestSpeciesMember = member;
                        }

                        if (fitness > bestGenusFitness)
                        {
                            bestGenusFitness = fitness;
                            bestGenusBrain = member.brain;
                            bestGenusGenome = member.genome;
                        }
                    }

                    if (bestSpeciesMember != null)
                    {
                        species.representativeBrain = bestSpeciesMember.brain;
                        species.representativeGenome = bestSpeciesMember.genome;
                        species.lastEpochGeneration = species.currentMaxGeneration;
                        genusUpdatedThisEpoch = true;
                    }
                }

                if (genusUpdatedThisEpoch && bestGenusBrain != null)
                {
                    genus.representativeBrain = bestGenusBrain;
                    genus.representativeGenome = bestGenusGenome;
                }
            }
        }

        /// <summary>
        /// Xóa toàn bộ thông tin Genus/Species (dùng khi reset simulation).
        /// </summary>
        public void Reset()
        {
            genera.Clear();
            nextGenusId = 1;
            nextSpeciesId = 1;
        }
    }
}

