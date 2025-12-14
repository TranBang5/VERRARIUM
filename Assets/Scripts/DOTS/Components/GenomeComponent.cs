using Unity.Entities;
using Unity.Mathematics;
using Verrarium.Data;

namespace Verrarium.DOTS.Components
{
    /// <summary>
    /// ECS Component chứa bộ gen của sinh vật
    /// Data-Oriented: Dữ liệu được lưu trữ liền kề trong memory chunks
    /// </summary>
    public struct GenomeComponent : IComponentData
    {
        // Physical Traits
        public float size;
        public float speed;
        public float4 color; // Unity.Mathematics.float4 thay vì Color để tương thích Burst

        // Metabolic Traits
        public float diet;
        public float health;

        // Growth Traits
        public float growthDuration;
        public float growthEnergyThreshold;

        // Reproduction Traits
        public float reproAgeThreshold;
        public float reproEnergyThreshold;
        public float reproCooldown;

        // Sensory Traits
        public float visionRange;

        // Behavioral Traits
        public int pheromoneType; // Enum as int for Burst compatibility

        // Evolution Traits
        public float mutationRate;

        /// <summary>
        /// Chuyển đổi từ Genome struct sang GenomeComponent
        /// </summary>
        public static GenomeComponent FromGenome(Genome genome)
        {
            return new GenomeComponent
            {
                size = genome.size,
                speed = genome.speed,
                color = new Unity.Mathematics.float4(genome.color.r, genome.color.g, genome.color.b, genome.color.a),
                diet = genome.diet,
                health = genome.health,
                growthDuration = genome.growthDuration,
                growthEnergyThreshold = genome.growthEnergyThreshold,
                reproAgeThreshold = genome.reproAgeThreshold,
                reproEnergyThreshold = genome.reproEnergyThreshold,
                reproCooldown = genome.reproCooldown,
                visionRange = genome.visionRange,
                pheromoneType = (int)genome.pheromoneType,
                mutationRate = genome.mutationRate
            };
        }

        /// <summary>
        /// Chuyển đổi từ GenomeComponent về Genome struct
        /// </summary>
        public Genome ToGenome()
        {
            return new Genome
            {
                size = this.size,
                speed = this.speed,
                color = new UnityEngine.Color(this.color.x, this.color.y, this.color.z, this.color.w),
                diet = this.diet,
                health = this.health,
                growthDuration = this.growthDuration,
                growthEnergyThreshold = this.growthEnergyThreshold,
                reproAgeThreshold = this.reproAgeThreshold,
                reproEnergyThreshold = this.reproEnergyThreshold,
                reproCooldown = this.reproCooldown,
                visionRange = this.visionRange,
                pheromoneType = (PheromoneType)this.pheromoneType,
                mutationRate = this.mutationRate
            };
        }
    }
}

