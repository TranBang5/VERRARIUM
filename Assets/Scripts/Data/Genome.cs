using UnityEngine;

namespace Verrarium.Data
{
    /// <summary>
    /// Bộ gen của một sinh vật - xác định tất cả các đặc điểm vật lý và sinh lý
    /// Dựa trên Bảng 1 trong tài liệu thiết kế
    /// </summary>
    [System.Serializable]
    public struct Genome
    {
        [Header("Physical Traits")]
        public float size;                    // Bán kính cơ bản của sinh vật
        public Color color;                   // Màu sắc nhận diện
        public float speed;                   // Hệ số nhân cho lực đẩy

        [Header("Metabolic Traits")]
        public float diet;                    // [0.0, 1.0] - 0 = ăn thực vật, 1 = ăn thịt, 0.5 = ăn tạp
        public float health;                  // Điểm máu tối đa

        [Header("Growth Traits")]
        public float growthDuration;         // Thời gian (giây) để đạt trưởng thành hoàn toàn
        public float growthEnergyThreshold;  // Năng lượng cần để bắt đầu tăng trưởng

        [Header("Reproduction Traits")]
        public float reproAgeThreshold;      // Tuổi tối thiểu để sinh sản (giây)
        public float reproEnergyThreshold;   // Năng lượng tối thiểu để đẻ trứng
        public float reproCooldown;          // Thời gian hồi chiêu giữa các lần đẻ trứng (giây)

        [Header("Sensory Traits")]
        public float visionRange;            // Bán kính phát hiện đối tượng

        [Header("Behavioral Traits")]
        public PheromoneType pheromoneType;  // Loại pheromone phát ra

        [Header("Evolution Traits")]
        public float mutationRate;           // Tỷ lệ đột biến trung bình (lambda cho Poisson)

        /// <summary>
        /// Tạo một bộ gen mặc định cho sinh vật ban đầu
        /// </summary>
        public static Genome CreateDefault()
        {
            return new Genome
            {
                size = 0.5f,
                color = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f)),
                speed = 1f,
                diet = Random.Range(0f, 1f),
                health = 100f,
                growthDuration = 20f,
                growthEnergyThreshold = 50f,
                reproAgeThreshold = 10f,
                reproEnergyThreshold = 60f,
                reproCooldown = 10f,
                visionRange = 5f,
                pheromoneType = (PheromoneType)Random.Range(0, 3),
                mutationRate = 2f
            };
        }

        /// <summary>
        /// Sao chép bộ gen và áp dụng đột biến
        /// </summary>
        public static Genome Mutate(Genome parent, float mutationStrength = 0.1f)
        {
            Genome child = parent;

            // Đột biến size
            if (Random.value < 0.3f)
                child.size = Mathf.Max(0.1f, child.size + Random.Range(-mutationStrength, mutationStrength));

            // Đột biến speed
            if (Random.value < 0.3f)
                child.speed = Mathf.Max(0.1f, child.speed + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f));

            // Đột biến diet
            if (Random.value < 0.2f)
                child.diet = Mathf.Clamp01(child.diet + Random.Range(-mutationStrength, mutationStrength));

            // Đột biến health
            if (Random.value < 0.2f)
                child.health = Mathf.Max(10f, child.health + Random.Range(-mutationStrength * 50f, mutationStrength * 50f));

            // Đột biến growthDuration
            if (Random.value < 0.2f)
                child.growthDuration = Mathf.Max(1f, child.growthDuration + Random.Range(-mutationStrength * 5f, mutationStrength * 5f));

            // Đột biến growthEnergyThreshold
            if (Random.value < 0.2f)
                child.growthEnergyThreshold = Mathf.Max(10f, child.growthEnergyThreshold + Random.Range(-mutationStrength * 20f, mutationStrength * 20f));

            // Đột biến reproAgeThreshold
            if (Random.value < 0.2f)
                child.reproAgeThreshold = Mathf.Max(1f, child.reproAgeThreshold + Random.Range(-mutationStrength * 2f, mutationStrength * 2f));

            // Đột biến reproEnergyThreshold
            if (Random.value < 0.2f)
                child.reproEnergyThreshold = Mathf.Max(20f, child.reproEnergyThreshold + Random.Range(-mutationStrength * 30f, mutationStrength * 30f));

            // Đột biến reproCooldown
            if (Random.value < 0.2f)
                child.reproCooldown = Mathf.Max(1f, child.reproCooldown + Random.Range(-mutationStrength * 2f, mutationStrength * 2f));

            // Đột biến visionRange
            if (Random.value < 0.2f)
                child.visionRange = Mathf.Max(1f, child.visionRange + Random.Range(-mutationStrength * 2f, mutationStrength * 2f));

            // Đột biến pheromoneType
            if (Random.value < 0.1f)
                child.pheromoneType = (PheromoneType)Random.Range(0, 3);

            // Đột biến mutationRate (meta-evolution)
            if (Random.value < 0.1f)
                child.mutationRate = Mathf.Max(0.5f, child.mutationRate + Random.Range(-mutationStrength * 1f, mutationStrength * 1f));

            // Đột biến màu sắc nhẹ
            if (Random.value < 0.3f)
            {
                child.color = new Color(
                    Mathf.Clamp01(parent.color.r + Random.Range(-0.2f, 0.2f)),
                    Mathf.Clamp01(parent.color.g + Random.Range(-0.2f, 0.2f)),
                    Mathf.Clamp01(parent.color.b + Random.Range(-0.2f, 0.2f))
                );
            }

            return child;
        }
    }

    public enum PheromoneType
    {
        Red,
        Green,
        Blue
    }
}

