using System.Collections.Generic;
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
        public float mouthAngle;              // Góc của miệng so với hướng forward (độ) - 0 = phía trước, 90 = bên phải, -90 = bên trái
        public float mouthRange;              // Tầm với của miệng (khoảng cách tối đa có thể ăn)
        public float mouthAngleRange;         // Góc mở của miệng (độ) - phạm vi góc có thể ăn

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
        public float pheromoneCooldown;      // Thời gian nghỉ giữa các lần nhả pheromone (giây)
        public float pheromoneLifetime;      // Thời gian tồn tại “đám mây” pheromone (giây)

        [Header("Evolution Traits")]
        public float mutationRate;           // Tỷ lệ đột biến GENOME trung bình (lambda cho Poisson)
        public float brainMutationRate;      // Tỷ lệ đột biến BRAIN (NEAT) trung bình (lambda cho Poisson). Nếu = 0 (save cũ) sẽ fallback = mutationRate ở runtime.

        /// <summary>
        /// Tạo một bộ gen mặc định cho sinh vật ban đầu
        /// </summary>
        public static Genome CreateDefault()
        {
            return new Genome
            {
                // Các sinh vật khởi tạo đều đồng nhất để quan sát tiến hoá rõ hơn
                size = 0.5f,
                color = Color.white,
                speed = 1f,
                mouthAngle = 0f, // Mặc định miệng ở phía trước
                mouthRange = 1.5f, // Tầm với miệng
                mouthAngleRange = 60f, // Góc mở 60 độ mỗi bên (tổng 120 độ)
                diet = 0f, // Mặc định ăn thực vật (plant)
                health = 450f, // Tăng từ 250f lên 450f - sống lâu hơn rất nhiều
                growthDuration = 30f, // Tăng từ 12f lên 30f - trưởng thành chậm hơn, sống lâu hơn
                growthEnergyThreshold = 40f, // Giảm từ 50f xuống 40f - dễ tăng trưởng hơn
                reproAgeThreshold = 20f, // Tăng từ 15s lên 20s - phải già hơn mới sinh sản
                reproEnergyThreshold = 75f, // Tăng từ 60f lên 75f - cần nhiều năng lượng hơn
                reproCooldown = 40f, // Tăng từ 20s lên 40s - chờ lâu hơn giữa các lần đẻ trứng
                visionRange = 5f,
                pheromoneType = (PheromoneType)Random.Range(0, 3),
                pheromoneCooldown = Random.Range(0.5f, 3f),
                pheromoneLifetime = Random.Range(5f, 8f),
                mutationRate = 2f,
                brainMutationRate = 2f
            };
        }

        /// <summary>
        /// Sao chép bộ gen và áp dụng đột biến.
        /// Mỗi thuộc tính (size, color, speed, ...) chỉ được đột biến tối đa 1 lần:
        /// chọn ngẫu nhiên numMutationEvents thuộc tính khác nhau và áp dụng đúng 1 đột biến cho từng thuộc tính đó.
        /// </summary>
        public static Genome Mutate(
            Genome parent,
            float mutationStrength = 0.1f,
            int numMutationEvents = 1,
            List<int> mutatedTraitIndices = null)
        {
            Genome child = parent;
            const int traitCount = 18; // size, speed, mouthRange, mouthAngleRange, diet, health, growthDuration, growthEnergyThreshold, reproAgeThreshold, reproEnergyThreshold, reproCooldown, visionRange, pheromoneType, pheromoneCooldown, pheromoneLifetime, mutationRate, brainMutationRate, color

            if (numMutationEvents <= 0) return child;

            // Danh sách chỉ số thuộc tính có thể đột biến (mouthAngle không đột biến)
            var indices = new List<int>(traitCount);
            for (int i = 0; i < traitCount; i++)
                indices.Add(i);

            // Xáo trộn và lấy tối đa numMutationEvents thuộc tính khác nhau
            int n = Mathf.Min(numMutationEvents, traitCount);
            for (int i = 0; i < n; i++)
            {
                int j = Random.Range(i, indices.Count);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }

            for (int k = 0; k < n; k++)
            {
                switch (indices[k])
                {
                    case 0:  child.size = Mathf.Clamp(child.size + Random.Range(-mutationStrength, mutationStrength), 0.1f, 2.5f); break;
                    case 1:  child.speed = Mathf.Max(0.1f, child.speed + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f)); break;
                    case 2:  child.mouthRange = Mathf.Max(0.5f, child.mouthRange + Random.Range(-mutationStrength * 0.5f, mutationStrength * 0.5f)); break;
                    case 3:  child.mouthAngleRange = Mathf.Clamp(child.mouthAngleRange + Random.Range(-mutationStrength * 20f, mutationStrength * 20f), 30f, 180f); break;
                    case 4:  child.diet = Mathf.Clamp01(child.diet + Random.Range(-mutationStrength, mutationStrength)); break;
                    case 5:  child.health = Mathf.Max(10f, child.health + Random.Range(-mutationStrength * 50f, mutationStrength * 50f)); break;
                    case 6:  child.growthDuration = Mathf.Max(1f, child.growthDuration + Random.Range(-mutationStrength * 5f, mutationStrength * 5f)); break;
                    case 7:  child.growthEnergyThreshold = Mathf.Max(10f, child.growthEnergyThreshold + Random.Range(-mutationStrength * 20f, mutationStrength * 20f)); break;
                    case 8:  child.reproAgeThreshold = Mathf.Max(1f, child.reproAgeThreshold + Random.Range(-mutationStrength * 2f, mutationStrength * 2f)); break;
                    case 9:  child.reproEnergyThreshold = Mathf.Max(20f, child.reproEnergyThreshold + Random.Range(-mutationStrength * 30f, mutationStrength * 30f)); break;
                    case 10: child.reproCooldown = Mathf.Max(1f, child.reproCooldown + Random.Range(-mutationStrength * 2f, mutationStrength * 2f)); break;
                    case 11: child.visionRange = Mathf.Max(1f, child.visionRange + Random.Range(-mutationStrength * 2f, mutationStrength * 2f)); break;
                    case 12: child.pheromoneType = (PheromoneType)Random.Range(0, 3); break;
                    case 13: child.pheromoneCooldown = Mathf.Max(0.1f, child.pheromoneCooldown + Random.Range(-mutationStrength * 2f, mutationStrength * 2f)); break;
                    case 14: child.pheromoneLifetime = Mathf.Max(0.1f, child.pheromoneLifetime + Random.Range(-mutationStrength * 2f, mutationStrength * 2f)); break;
                    case 15: child.mutationRate = Mathf.Max(0.5f, child.mutationRate + Random.Range(-mutationStrength * 1f, mutationStrength * 1f)); break;
                    case 16: child.brainMutationRate = Mathf.Max(0.0f, child.brainMutationRate + Random.Range(-mutationStrength * 1f, mutationStrength * 1f)); break;
                    case 17: // color: chỉ đột biến một kênh R/G/B
                        {
                            float r = child.color.r, g = child.color.g, b = child.color.b;
                            int ch = Random.Range(0, 3);
                            float delta = Random.Range(-0.2f, 0.2f);
                            if (ch == 0) r = Mathf.Clamp01(r + delta);
                            else if (ch == 1) g = Mathf.Clamp01(g + delta);
                            else b = Mathf.Clamp01(b + delta);
                            child.color = new Color(r, g, b, child.color.a);
                        }
                        break;
                }

                mutatedTraitIndices?.Add(indices[k]);
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

