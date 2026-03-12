using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Verrarium.Data
{
    /// <summary>
    /// Thông tin phả hệ cho từng sinh vật - phục vụ Inspector/Popup UI.
    /// </summary>
    public sealed class CreatureLineageRecord
    {
        public int LineageId { get; }
        public int GenerationIndex { get; }
        public string GenomeCode { get; }
        public string ParentGenomeCode { get; }
        public Genome GenomeSnapshot { get; }
        public CreatureLineageRecord Parent { get; }
        
        // Genus / Species integration
        public int GenusId { get; }
        public string GenusCode { get; }   // Dễ đọc: "GN-001", "GN-002", ...
        public int SpeciesId { get; }
        public string SpeciesCode { get; } // Dễ đọc: "SP-001", "SP-002", ... (trong Genus)

        internal CreatureLineageRecord(
            int id,
            Genome genome,
            CreatureLineageRecord parent,
            string genomeCode,
            int genusId = -1,
            int speciesId = -1)
        {
            LineageId = id;
            GenomeSnapshot = genome;
            Parent = parent;
            GenerationIndex = parent != null ? parent.GenerationIndex + 1 : 0;
            ParentGenomeCode = parent != null ? parent.GenomeCode : "ROOT";
            GenomeCode = genomeCode;
            GenusId = genusId;
            GenusCode = genusId >= 0 ? $"GN-{genusId:000}" : "UNKNOWN";
            SpeciesId = speciesId;
            SpeciesCode = speciesId >= 0 ? $"SP-{speciesId:000}" : "UNKNOWN";
        }
    }

    /// <summary>
    /// Registry đơn giản để ánh xạ CreatureController sang thông tin phả hệ.
    /// </summary>
    public static class CreatureLineageRegistry
    {
        private static readonly Dictionary<int, CreatureLineageRecord> InstanceLookup = new();
        private static readonly Dictionary<int, CreatureLineageRecord> LineageLookup = new();
        private static int nextLineageId = 1;

        /// <summary>
        /// Tạo lineage record với genus/species ID (nếu có)
        /// </summary>
        public static CreatureLineageRecord CreateRecord(
            Genome genome,
            CreatureLineageRecord parent,
            int genusId = -1,
            int speciesId = -1)
        {
            int id = nextLineageId++;
            string genomeCode = ComputeGenomeCode(genome, id, genusId, speciesId);
            var record = new CreatureLineageRecord(id, genome, parent, genomeCode, genusId, speciesId);
            LineageLookup[id] = record;
            return record;
        }
        
        /// <summary>
        /// Tạo lineage record không có genus/species (backward compatibility)
        /// </summary>
        public static CreatureLineageRecord CreateRecord(Genome genome, CreatureLineageRecord parent)
        {
            return CreateRecord(genome, parent, -1, -1);
        }

        public static void Bind(UnityEngine.Object owner, CreatureLineageRecord record)
        {
            if (owner == null || record == null)
                return;

            InstanceLookup[owner.GetInstanceID()] = record;
        }

        public static void Unbind(UnityEngine.Object owner)
        {
            if (owner == null)
                return;

            InstanceLookup.Remove(owner.GetInstanceID());
        }

        public static CreatureLineageRecord Get(UnityEngine.Object owner)
        {
            if (owner == null)
                return null;

            InstanceLookup.TryGetValue(owner.GetInstanceID(), out var record);
            return record;
        }

        private static string ComputeGenomeCode(Genome genome, int lineageId, int genusId = -1, int speciesId = -1)
        {
            // Serialize genome fields thành byte buffer rồi hash SHA1 -> 8 ký tự hex.
            // Bao gồm cả genusId và speciesId để genome code phản ánh phân loại
            var buffer = new List<byte>(64);

            void AddFloat(float value)
            {
                buffer.AddRange(System.BitConverter.GetBytes(value));
            }

            AddFloat(genome.size);
            AddFloat(genome.speed);
            AddFloat(genome.diet);
            AddFloat(genome.health);
            AddFloat(genome.growthDuration);
            AddFloat(genome.growthEnergyThreshold);
            AddFloat(genome.reproAgeThreshold);
            AddFloat(genome.reproEnergyThreshold);
            AddFloat(genome.reproCooldown);
            AddFloat(genome.visionRange);
            AddFloat(genome.mutationRate);
            AddFloat(genome.brainMutationRate);
            AddFloat(genome.color.r);
            AddFloat(genome.color.g);
            AddFloat(genome.color.b);
            AddFloat(genome.color.a);
            AddFloat((float)genome.pheromoneType);
            AddFloat(lineageId);
            AddFloat(genusId);
            AddFloat(speciesId); // Thêm speciesId vào hash

            using SHA1 sha = SHA1.Create();
            byte[] hash = sha.ComputeHash(buffer.ToArray());
            StringBuilder sb = new StringBuilder(8);
            for (int i = 0; i < 4 && i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Lấy tất cả lineage records thuộc một species
        /// </summary>
        public static List<CreatureLineageRecord> GetRecordsBySpecies(int speciesId)
        {
            var result = new List<CreatureLineageRecord>();
            foreach (var record in LineageLookup.Values)
            {
                if (record.SpeciesId == speciesId)
                {
                    result.Add(record);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Reset registry (dùng khi bắt đầu simulation mới)
        /// </summary>
        public static void Reset()
        {
            InstanceLookup.Clear();
            LineageLookup.Clear();
            nextLineageId = 1;
        }
    }
}











