using UnityEngine;
using Verrarium.Data;
using Verrarium.DOTS.Evolution;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Hàm tiện ích để tính khoảng cách giữa hai Genome
    /// và khoảng cách Genus (kết hợp não NEAT + genome).
    /// </summary>
    public static class GenomeDistance
    {
        /// <summary>
        /// Tính khoảng cách giữa hai genome với trọng số theo mức độ ưu tiên:
        /// 1) Đặc tính cơ bản: size, diet, health, color
        /// 2) Đặc tính hoạt động: speed, mouthRange, mouthAngleRange, visionRange, pheromoneType
        /// 3) Đặc tính sinh sản: growthDuration, growthEnergyThreshold, reproAgeThreshold, reproEnergyThreshold, reproCooldown
        /// 4) Đặc tính phụ: mutationRate, pheromoneCooldown
        /// </summary>
        public static float ComputeGenomeDistance(Genome a, Genome b)
        {
            float distance = 0f;

            // 1) ĐẶC TÍNH CƠ BẢN (trọng số rất cao)

            // size: 0.1 – 2.5 (kích thước tối đa tăng nhẹ)
            distance += 1.2f * Abs01(
                Norm(a.size, 0.1f, 2.5f) -
                Norm(b.size, 0.1f, 2.5f));

            // diet: 0 – 1
            distance += 1.2f * Mathf.Abs(a.diet - b.diet);

            // health: 50 – 800
            distance += 1.0f * Abs01(
                Norm(a.health, 50f, 800f) -
                Norm(b.health, 50f, 800f));

            // color: trung bình chênh lệch 3 kênh
            float colorDiff =
                (Mathf.Abs(a.color.r - b.color.r) +
                 Mathf.Abs(a.color.g - b.color.g) +
                 Mathf.Abs(a.color.b - b.color.b)) / 3f;
            distance += 0.8f * colorDiff;

            // 2) ĐẶC TÍNH HOẠT ĐỘNG (cao)

            // speed: 0.1 – 3.0
            distance += 0.9f * Abs01(
                Norm(a.speed, 0.1f, 3.0f) -
                Norm(b.speed, 0.1f, 3.0f));

            // mouthRange: 0.5 – 3.0
            distance += 0.7f * Abs01(
                Norm(a.mouthRange, 0.5f, 3.0f) -
                Norm(b.mouthRange, 0.5f, 3.0f));

            // mouthAngleRange: 30 – 180
            distance += 0.5f * Abs01(
                Norm(a.mouthAngleRange, 30f, 180f) -
                Norm(b.mouthAngleRange, 30f, 180f));

            // visionRange: 1 – 15
            distance += 0.9f * Abs01(
                Norm(a.visionRange, 1f, 15f) -
                Norm(b.visionRange, 1f, 15f));

            // pheromoneType: khác nhau = 1, giống = 0
            float pherTypeDiff = a.pheromoneType == b.pheromoneType ? 0f : 1f;
            distance += 0.6f * pherTypeDiff;

            // 3) ĐẶC TÍNH SINH SẢN (trung bình)

            // growthDuration: 5 – 60
            distance += 0.5f * Abs01(
                Norm(a.growthDuration, 5f, 60f) -
                Norm(b.growthDuration, 5f, 60f));

            // growthEnergyThreshold: 10 – 120
            distance += 0.5f * Abs01(
                Norm(a.growthEnergyThreshold, 10f, 120f) -
                Norm(b.growthEnergyThreshold, 10f, 120f));

            // reproAgeThreshold: 5 – 60
            distance += 0.6f * Abs01(
                Norm(a.reproAgeThreshold, 5f, 60f) -
                Norm(b.reproAgeThreshold, 5f, 60f));

            // reproEnergyThreshold: 20 – 150
            distance += 0.8f * Abs01(
                Norm(a.reproEnergyThreshold, 20f, 150f) -
                Norm(b.reproEnergyThreshold, 20f, 150f));

            // reproCooldown: 5 – 120
            distance += 0.5f * Abs01(
                Norm(a.reproCooldown, 5f, 120f) -
                Norm(b.reproCooldown, 5f, 120f));

            // 4) ĐẶC TÍNH PHỤ (thấp)

            // mutationRate: 0.5 – 5
            distance += 0.3f * Abs01(
                Norm(a.mutationRate, 0.5f, 5f) -
                Norm(b.mutationRate, 0.5f, 5f));

            // pheromoneCooldown: 0.1 – 10
            distance += 0.3f * Abs01(
                Norm(a.pheromoneCooldown, 0.1f, 10f) -
                Norm(b.pheromoneCooldown, 0.1f, 10f));

            // pheromoneLifetime: 0.1 – 10
            distance += 0.3f * Abs01(
                Norm(a.pheromoneLifetime, 0.1f, 10f) -
                Norm(b.pheromoneLifetime, 0.1f, 10f));

            return distance;
        }

        /// <summary>
        /// Tính khoảng cách Genus giữa hai sinh vật:
        /// d_genus = alpha * d_brain + beta * d_genome
        /// Trong đó d_brain là compatibility distance NEAT,
        /// d_genome là khoảng cách genome ở trên.
        /// </summary>
        public static float ComputeGenusDistance(
            NEATNetwork brainA, Genome genomeA,
            NEATNetwork brainB, Genome genomeB,
            SpeciationSystem speciationSystem)
        {
            if (brainA == null || brainB == null || speciationSystem == null)
            {
                return ComputeGenomeDistance(genomeA, genomeB);
            }

            // d_brain: compatibility distance NEAT hiện tại
            float dBrain = speciationSystem.ComputeCompatibilityDistance(brainA, brainB);

            // d_genome: theo hàm trên
            float dGenome = ComputeGenomeDistance(genomeA, genomeB);

            // Ưu tiên não cho Genus, nhưng genome vẫn có ảnh hưởng rõ
            const float ALPHA_BRAIN = 1.0f;  // não
            const float BETA_GENOME = 0.4f;  // genome

            return ALPHA_BRAIN * dBrain + BETA_GENOME * dGenome;
        }

        // --- Helpers ---

        private static float Norm(float v, float min, float max)
        {
            if (max <= min) return 0f;
            return Mathf.Clamp01((v - min) / (max - min));
        }

        private static float Abs01(float v) => Mathf.Abs(v);
    }
}

