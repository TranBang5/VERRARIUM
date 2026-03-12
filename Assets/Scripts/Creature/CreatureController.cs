using UnityEngine;
using Verrarium.Data;
using Verrarium.Resources;
using Verrarium.Core;
using Verrarium.Evolution;
using Verrarium.Utils;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Verrarium.Creature
{
    /// <summary>
    /// Controller cho một sinh vật - quản lý trạng thái, hành vi và vòng đời
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(CreatureClickHandler))]
    public class CreatureController : MonoBehaviour
    {
        [Header("Components")]
        private Rigidbody2D rb;
        private new CircleCollider2D collider; // 'new' để ẩn Component.collider
        private SpriteRenderer spriteRenderer;

        [Header("Genome")]
        private Genome genome;
        private CreatureLineageRecord lineageRecord;

        [Header("State")]
        private float energy = 100f;
        private float maxEnergy = 100f;
        private float currentHealth = 100f;
        private float maturity = 0f; // 0 = mới sinh, 1 = trưởng thành
        private float age = 0f;
        private bool isPaused = false;

        [Header("Metabolism")]
        [SerializeField] private float baseMetabolicRate = 0.12f; // Giảm từ 0.2f xuống 0.12f - tiêu thụ ít năng lượng hơn nhiều, sống lâu hơn
        [SerializeField] private float movementEnergyCost = 0.15f; // Giảm từ 0.2f xuống 0.15f - di chuyển ít tốn năng lượng hơn
        [SerializeField] private float agingStartMaturity = 0.99f; // Tăng từ 0.98f lên 0.99f - lão hóa muộn hơn nữa
        [SerializeField] private float agingDamageRate = 0.3f; // Giảm từ 0.5f xuống 0.3f - lão hóa ít sát thương hơn nhiều, sống lâu hơn
        
        [Header("Starvation")]
        [SerializeField] private float starvationThreshold = 0.25f; // Giảm từ 0.3f xuống 0.25f - cho phép năng lượng thấp hơn trước khi đói
        [SerializeField] private float starvationDamageRate = 6f; // Giảm từ 8f xuống 6f - chết đói chậm hơn một chút

        [Header("Trample Settings")]
        [SerializeField] private float trampleVelocityThreshold = 1.0f; // Ngưỡng tốc độ tối thiểu để gây trample damage lên sinh vật khác
        [SerializeField] private float trampleDamageBase = 5f;          // Hệ số sát thương cơ bản khi giẫm lên sinh vật khác
        [SerializeField] private float selfTrampleDamageFactor = 0.3f;  // Tỷ lệ sát thương phản lại chính mình khi va chạm mạnh
        [SerializeField, Min(0f)] private float trampleInvincibilitySeconds = 0.25f; // I-frame sau khi bị va chạm (trample), tránh bị trừ máu liên tục
        private float lastTrampleDamageTime = -999f;

        [Header("Neural Network")]
        private NEATNetwork brain;
        // 10 đầu vào cơ bản + 3 đầu vào pheromone (R,G,B) + 1 đầu vào grayscale màu sinh vật gần nhất
        private const int INPUT_COUNT = 14;
        // 7 output hành vi cơ bản + 1 output thả pheromone
        private const int OUTPUT_COUNT = 8;

        // Neural Network outputs
        private float accelerateOutput = 0f;
        private float rotateOutput = 0f;
        private float layEggOutput = 0f;
        private float growthOutput = 0f;
        private float healOutput = 0f;
        private float attackOutput = 0f;
        private float eatOutput = 0f;
        private float pheromoneOutput = 0f;

        // Sensory data (inputs cho Neural Network)
        private float[] neuralInputs = new float[INPUT_COUNT];

        // Tài nguyên và sinh vật gần nhất
        private Resource closestPlant = null;
        private Resource closestMeat = null;
        private CreatureController closestCreature = null;
        private float lastEatTime = 0f;
        private const float EAT_COOLDOWN = 0.5f;
        private float lastReproduceTime = 0f;
        private float lastPheromoneEmitTime = 0f;

        [Header("Evolution Stats")]
        // Tổng năng lượng đã ăn được trong suốt vòng đời (dùng cho proxy fitness)
        private float totalEnergyGained = 0f;
        // Số con đã sinh ra (offspring count)
        private int offspringCount = 0;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            collider = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (GetComponent<CreatureClickHandler>() == null)
                gameObject.AddComponent<CreatureClickHandler>();

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Khởi tạo sinh vật với bộ gen
        /// </summary>
        public void Initialize(Genome initialGenome, NEATNetwork initialBrain = null)
        {
            genome = initialGenome;

            // Backward-compat: save cũ chưa có brainMutationRate -> mặc định theo mutationRate
            if (genome.brainMutationRate <= 0f)
                genome.brainMutationRate = genome.mutationRate;
            
            // Khởi tạo hoặc sao chép bộ não
            if (initialBrain != null)
            {
                // Dùng trực tiếp instance được truyền vào để speciation/fitnessMap cùng tham chiếu.
                // (Bản thân quá trình sinh sản đã clone từ parentBrain trước khi truyền vào.)
                brain = initialBrain;
            }
            else
            {
                brain = new NEATNetwork(INPUT_COUNT, OUTPUT_COUNT);
            }
            
            ApplyGenomeToPhysics();
            // Đặt năng lượng ban đầu thấp hơn ngưỡng sinh sản để sinh vật mới sinh không thể đẻ trứng ngay
            // Đảm bảo năng lượng ban đầu không vượt quá 60% ngưỡng sinh sản
            energy = Mathf.Min(maxEnergy, genome.reproEnergyThreshold * 0.6f);
            currentHealth = genome.health;
            maturity = 0f;
            age = 0f;
            
            // Thêm WorldBoundaryEnforcer nếu chưa có và world border được bật
            if (SimulationSupervisor.Instance != null && 
                SimulationSupervisor.Instance.EnableWorldBorder &&
                GetComponent<WorldBoundaryEnforcer>() == null)
            {
                gameObject.AddComponent<WorldBoundaryEnforcer>();
            }

            // Đăng ký với BrainUpdateManager nếu cần
            // BrainUpdateManager.Instance?.RegisterCreature(this);
        }

        /// <summary>
        /// Áp dụng bộ gen lên các thuộc tính vật lý
        /// </summary>
        private void ApplyGenomeToPhysics()
        {
            // Kích thước
            float currentSize = genome.size * (1f + maturity * 0.5f); // Có thể lớn hơn 50% khi trưởng thành
            transform.localScale = Vector3.one * currentSize;

            // Khối lượng (tỷ lệ với thể tích = size^2)
            if (rb != null)
            {
                rb.mass = currentSize * currentSize;
            }

            // Collider radius
            if (collider != null)
            {
                collider.radius = 0.5f; // Normalized, scale sẽ làm phần còn lại
            }

            // Màu sắc
            if (spriteRenderer != null)
            {
                spriteRenderer.color = genome.color;
                
                // Tạo sprite đơn giản nếu chưa có
                if (spriteRenderer.sprite == null)
                {
                    CreateSimpleSprite();
                }
            }
        }

        /// <summary>
        /// Tạo sprite đơn giản (hình tròn)
        /// </summary>
        private void CreateSimpleSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            Vector2 center = new Vector2(16, 16);
            float radius = 14f;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }

        private bool brainUpdateEnabled = true; // Cho phép brain update mỗi frame (có thể tắt để dùng time-slicing)
        private int brainUpdateFrameSkip = 0; // Skip brain update mỗi N frames

        private void FixedUpdate()
        {
            // Không cập nhật nếu đang pause
            if (isPaused) return;

            if (genome.size == 0) return; // Chưa được khởi tạo

            age += Time.fixedDeltaTime;

            // Sense - Thu thập thông tin cảm giác
            Sense();

            // Think - Tính toán Neural Network (có thể skip để tối ưu)
            if (brainUpdateEnabled)
            {
                brainUpdateFrameSkip++;
                // Update brain mỗi 2-3 frames để giảm tải
                if (brainUpdateFrameSkip >= 2)
                {
            Think();
                    brainUpdateFrameSkip = 0;
                }
                else
                {
                    // Sử dụng outputs cũ
                }
            }

            // Act - Thực thi hành động
            Act();

            // Metabolism - Tiêu thụ năng lượng
            UpdateMetabolism();

            // Lão hóa
            UpdateAging();

            // Kiểm tra chết
            CheckDeath();
        }

        /// <summary>
        /// Update chỉ brain (dùng cho time-slicing)
        /// </summary>
        public void UpdateBrainOnly()
        {
            if (genome.size == 0) return;
            Sense();
            Think();
        }

        /// <summary>
        /// Thu thập thông tin cảm giác - tất cả các đầu vào theo Bảng 2 (mở rộng với grayscale sinh vật gần nhất)
        /// </summary>
        private void Sense()
        {
            if (SimulationSupervisor.Instance == null) return;

            Vector2 position = transform.position;
            Vector2 forward = transform.right; // Front là bên phải của sprite

            // Tìm các đối tượng gần nhất
            closestPlant = SimulationSupervisor.Instance.FindClosestResource(position, ResourceType.Plant, genome.visionRange);
            closestMeat = SimulationSupervisor.Instance.FindClosestResource(position, ResourceType.Meat, genome.visionRange);
            closestCreature = SimulationSupervisor.Instance.FindClosestCreature(position, this, genome.visionRange);

            // Tính toán các đầu vào Neural Network (theo Bảng 2)
            int index = 0;

            // 0: EnergyRatio [0.0, 1.0]
            neuralInputs[index++] = EnergyRatio;

            // 1: Maturity [0.0, 1.0]
            neuralInputs[index++] = maturity;

            // 2: HealthRatio [0.0, 1.0]
            neuralInputs[index++] = HealthRatio;

            // 3: Age (chuẩn hóa)
            neuralInputs[index++] = Mathf.Clamp01(age / 100f); // Chuẩn hóa về [0,1]

            // 4: DistToClosestPlant [0.0, 1.0]
            if (closestPlant != null)
            {
                float dist = Vector2.Distance(position, closestPlant.transform.position);
                neuralInputs[index++] = MathUtils.NormalizeDistance(dist, genome.visionRange);
            }
            else
            {
                neuralInputs[index++] = 1f; // Không thấy = ở rìa tầm nhìn
            }

            // 5: AngleToClosestPlant [-1, 1]
            if (closestPlant != null)
            {
                neuralInputs[index++] = MathUtils.AngleToTarget(position, closestPlant.transform.position, forward);
            }
            else
            {
                neuralInputs[index++] = 0f;
            }

            // 6: DistToClosestMeat [0.0, 1.0]
            if (closestMeat != null)
            {
                float dist = Vector2.Distance(position, closestMeat.transform.position);
                neuralInputs[index++] = MathUtils.NormalizeDistance(dist, genome.visionRange);
            }
            else
            {
                neuralInputs[index++] = 1f;
            }

            // 7: AngleToClosestMeat [-1, 1]
            if (closestMeat != null)
            {
                neuralInputs[index++] = MathUtils.AngleToTarget(position, closestMeat.transform.position, forward);
            }
            else
            {
                neuralInputs[index++] = 0f;
            }

            // 8: DistToClosestCreature [0.0, 1.0]
            if (closestCreature != null)
            {
                float dist = Vector2.Distance(position, closestCreature.transform.position);
                neuralInputs[index++] = MathUtils.NormalizeDistance(dist, genome.visionRange);
            }
            else
            {
                neuralInputs[index++] = 1f;
            }

            // 9: AngleToClosestCreature [-1, 1]
            if (closestCreature != null)
            {
                neuralInputs[index++] = MathUtils.AngleToTarget(position, closestCreature.transform.position, forward);
            }
            else
            {
                neuralInputs[index++] = 0f;
            }

            // 10: Grayscale màu của sinh vật gần nhất [0.0, 1.0]
            if (closestCreature != null)
            {
                var otherGenome = closestCreature.GetGenome();
                Color otherColor = otherGenome.color;
                float gray = 0.299f * otherColor.r + 0.587f * otherColor.g + 0.114f * otherColor.b;
                neuralInputs[index++] = Mathf.Clamp01(gray);
            }
            else
            {
                neuralInputs[index++] = 0f;
            }

            // 11-13: Pheromone strengths (Red, Green, Blue) tại vùng "miệng" của sinh vật
            if (SimulationSupervisor.Instance.EnablePheromones && SimulationSupervisor.Instance.HexGrid != null)
            {
                // Vị trí cảm biến pheromone đặt gần miệng (phía trước thân)
                float currentSize = genome.size * (1f + maturity * 0.5f);
                Vector2 mouthPos = position + forward * (currentSize * 0.8f);

                // Red, Green, Blue đọc trực tiếp từ HexGrid
                var hexGrid = SimulationSupervisor.Instance.HexGrid;
                neuralInputs[index++] = hexGrid.GetPheromoneStrengthAtWorld(mouthPos, 0);
                neuralInputs[index++] = hexGrid.GetPheromoneStrengthAtWorld(mouthPos, 1);
                neuralInputs[index++] = hexGrid.GetPheromoneStrengthAtWorld(mouthPos, 2);
            }
            else
            {
                neuralInputs[index++] = 0f;
                neuralInputs[index++] = 0f;
                neuralInputs[index++] = 0f;
            }
        }

        /// <summary>
        /// Tính toán Neural Network - kích hoạt mạng với các đầu vào cảm giác
        /// Hỗ trợ tương thích với các bộ não cũ có inputCount nhỏ hơn (ví dụ save cũ).
        /// </summary>
        private void Think()
        {
            if (brain == null) return;

            // Chuẩn bị mảng input phù hợp với brain.InputCount để tránh lỗi khi load save cũ
            int brainInputCount = brain.InputCount;
            float[] inputsForBrain = new float[brainInputCount];
            int copyCount = Mathf.Min(brainInputCount, neuralInputs.Length);
            for (int i = 0; i < copyCount; i++)
            {
                inputsForBrain[i] = neuralInputs[i];
            }
            // Nếu brain mong đợi nhiều input hơn mảng hiện tại, các vị trí còn lại giữ giá trị 0

            // Tính toán đầu ra từ mạng nơ-ron
            float[] outputs = brain.Compute(inputsForBrain);

            // Gán các đầu ra (theo thứ tự trong Bảng 2)
            accelerateOutput = outputs[0];                // [0.0, 1.0]
            rotateOutput = (outputs[1] - 0.5f) * 2f;      // Chuyển [0,1] thành [-1,1]
            layEggOutput = outputs[2];                    // [0.0, 1.0]
            growthOutput = outputs[3];                    // [0.0, 1.0]
            healOutput = outputs[4];                      // [0.0, 1.0]
            attackOutput = outputs[5];                    // [0.0, 1.0]
            eatOutput = outputs[6];                       // [0.0, 1.0]
            pheromoneOutput = outputs[7];                 // [0.0, 1.0] - điều khiển thả pheromone
        }

        /// <summary>
        /// Thực thi hành động dựa trên Neural Network outputs
        /// </summary>
        private void Act()
        {
            // Di chuyển
            if (accelerateOutput > 0.1f)
            {
                Vector2 force = transform.right * accelerateOutput * genome.speed * 2.5f; // Front là bên phải - di chuyển chậm hơn 50%
                rb.AddForce(force);
                energy -= movementEnergyCost * Time.fixedDeltaTime;
            }

            // Xoay
            if (Mathf.Abs(rotateOutput) > 0.1f)
            {
                float torque = rotateOutput * 25f;
                rb.AddTorque(torque);
                energy -= movementEnergyCost * 0.5f * Time.fixedDeltaTime;
            }

            // Ăn
            if (eatOutput > 0.5f && Time.time - lastEatTime > EAT_COOLDOWN)
            {
                // Kiểm tra xem có tài nguyên gần đó không
                if (closestPlant != null || closestMeat != null)
                {
                    TryEat();
                }
            }

            // Tăng trưởng
            if (growthOutput > 0.5f && maturity < 1f)
            {
                Grow();
            }

            // Sinh sản
            if (layEggOutput > 0.5f)
            {
                TryReproduce();
            }

            // Hồi máu
            if (healOutput > 0.5f)
            {
                Heal();
            }

            // Phát pheromone phía sau sinh vật khi output đủ lớn và hết cooldown
            if (pheromoneOutput > 0.5f)
            {
                EmitPheromone();
            }
        }

        /// <summary>
        /// Cố gắng ăn tài nguyên gần đó
        /// </summary>
        private void TryEat()
        {
            // Chọn tài nguyên dựa trên diet
            Resource targetResource = null;
            if (genome.diet < 0.3f)
                targetResource = closestPlant;
            else if (genome.diet > 0.7f)
                targetResource = closestMeat;
            else
            {
                // Ăn tạp - chọn cái gần nhất
                if (closestPlant != null && closestMeat != null)
                {
                    float distPlant = Vector2.Distance(transform.position, closestPlant.transform.position);
                    float distMeat = Vector2.Distance(transform.position, closestMeat.transform.position);
                    targetResource = distPlant < distMeat ? closestPlant : closestMeat;
                }
                else if (closestPlant != null)
                    targetResource = closestPlant;
                else if (closestMeat != null)
                    targetResource = closestMeat;
            }

            if (targetResource == null) return;

            // Kiểm tra xem thức ăn có nằm ở phía miệng không
            if (!IsFoodInMouthRange(targetResource))
            {
                return; // Thức ăn không nằm ở phía miệng
            }

            // Kiểm tra diet
            bool canEat = false;
            if (genome.diet < 0.3f && targetResource.Type == ResourceType.Plant)
                canEat = true;
            else if (genome.diet > 0.7f && targetResource.Type == ResourceType.Meat)
                canEat = true;
            else if (genome.diet >= 0.3f && genome.diet <= 0.7f)
                canEat = true; // Ăn tạp

            if (canEat)
            {
                float energyGained = targetResource.Consume();
            energy = Mathf.Min(maxEnergy, energy + energyGained);
            totalEnergyGained += energyGained;
                lastEatTime = Time.time;

                // Cập nhật references
                if (targetResource == closestPlant) closestPlant = null;
                if (targetResource == closestMeat) closestMeat = null;
            }
        }

        /// <summary>
        /// Tăng trưởng
        /// </summary>
        private void Grow()
        {
            float growthRate = Time.fixedDeltaTime / genome.growthDuration;
            maturity = Mathf.Min(1f, maturity + growthRate);
            
            // Tiêu thụ năng lượng (giảm cost để dễ tăng trưởng hơn)
            float growthCost = 1.5f * Time.fixedDeltaTime; // Giảm từ 2f xuống 1.5f
            energy -= growthCost;

            // Cập nhật kích thước
            ApplyGenomeToPhysics();
        }

        /// <summary>
        /// Cố gắng sinh sản
        /// </summary>
        private void TryReproduce()
        {
            if (Time.time - lastReproduceTime < genome.reproCooldown) return; // Kiểm tra cooldown từ gen
            if (age < genome.reproAgeThreshold) return;
            if (energy < genome.reproEnergyThreshold) return;
            if (maturity < 0.85f) return; // Tăng từ 0.75f lên 0.85f - phải trưởng thành hơn mới sinh sản

            // Kiểm tra population limit
            if (SimulationSupervisor.Instance != null)
            {
                int currentPopulation = SimulationSupervisor.Instance.CurrentPopulation;
                int maxPopulation = SimulationSupervisor.Instance.GetMaxPopulationSize();
                
                // Nếu đã đạt max population, không cho sinh sản
                if (currentPopulation >= maxPopulation) return;
                
                // Density-based reproduction penalty: khó sinh sản hơn khi population cao
                int targetPopulation = SimulationSupervisor.Instance.GetTargetPopulationSize();
                float populationRatio = (float)currentPopulation / maxPopulation;
                
                // Nếu population > 80% max, giảm 50% cơ hội sinh sản
                if (populationRatio > 0.8f)
                {
                    if (Random.value > 0.5f) return; // 50% chance bị từ chối
                }
                // Nếu population > 60% max, giảm 25% cơ hội sinh sản
                else if (populationRatio > 0.6f)
                {
                    if (Random.value > 0.75f) return; // 25% chance bị từ chối
                }
            }

            // Tiêu thụ năng lượng để sinh sản
            float reproductionCost = genome.reproEnergyThreshold * 0.6f; // Tăng từ 0.5f lên 0.6f - tốn nhiều năng lượng hơn
            energy -= reproductionCost;
            lastReproduceTime = Time.time;

            // Tạo trứng
            if (SimulationSupervisor.Instance != null)
            {
                Vector2 eggPosition = (Vector2)transform.position + Random.insideUnitCircle * 1f;
                SimulationSupervisor.Instance.OnCreatureReproduction(this, eggPosition, genome);
                offspringCount++;
            }
        }

        /// <summary>
        /// Kiểm tra xem thức ăn có nằm ở phía miệng không
        /// </summary>
        private bool IsFoodInMouthRange(Resource food)
        {
            if (food == null) return false;

            Vector2 creaturePosition = transform.position;
            Vector2 foodPosition = food.transform.position;
            
            // Tính size hiện tại (có thể lớn hơn khi trưởng thành)
            float currentSize = genome.size * (1f + maturity * 0.5f);
            
            // Tính vị trí miệng (luôn ở phía trước, không thay đổi angle)
            Vector2 forward = transform.right; // Front là bên phải của sprite
            Vector2 mouthPosition = creaturePosition + forward * (currentSize * 0.8f); // Miệng ở gần rìa cơ thể phía trước

            // Mouth range scale theo size (creature lớn hơn = miệng xa hơn)
            float effectiveMouthRange = genome.mouthRange * currentSize;

            // Khoảng cách từ miệng đến thức ăn
            float distanceToFood = Vector2.Distance(mouthPosition, foodPosition);
            if (distanceToFood > effectiveMouthRange)
            {
                return false; // Quá xa
            }

            // Tính hướng từ miệng đến thức ăn
            Vector2 directionToFood = (foodPosition - mouthPosition).normalized;
            
            // Hướng của miệng luôn là forward (phía trước)
            Vector2 mouthForward = forward;
            
            // Tính góc giữa hướng miệng và hướng đến thức ăn
            float angleToFood = Vector2.SignedAngle(mouthForward, directionToFood);
            float halfMouthAngleRange = genome.mouthAngleRange / 2f;

            // Kiểm tra xem thức ăn có nằm trong góc mở của miệng không
            return Mathf.Abs(angleToFood) <= halfMouthAngleRange;
        }

        /// <summary>
        /// Hồi máu
        /// </summary>
        private void Heal()
        {
            if (currentHealth >= genome.health) return;
            if (energy < 10f) return;

            float healAmount = 5f * Time.fixedDeltaTime;
            currentHealth = Mathf.Min(genome.health, currentHealth + healAmount);
            energy -= 2f * Time.fixedDeltaTime;
        }

        /// <summary>
        /// Cập nhật trao đổi chất
        /// </summary>
        private void UpdateMetabolism()
        {
            // Năng lượng tối đa phụ thuộc vào kích thước
            maxEnergy = genome.size * 100f * (1f + maturity * 0.5f);

            // Tiêu thụ năng lượng cơ bản
            float metabolicCost = baseMetabolicRate * Time.fixedDeltaTime;
            energy -= metabolicCost;

            // Giới hạn energy không âm
            if (energy < 0f)
            {
                energy = 0f;
            }

            // Tính toán sát thương đói (starvation damage)
            UpdateStarvation();
        }

        /// <summary>
        /// Cập nhật sát thương đói - sinh vật chết nhanh hơn khi thiếu năng lượng
        /// </summary>
        private void UpdateStarvation()
        {
            if (maxEnergy <= 0f) return;

            float energyRatio = energy / maxEnergy;
            
            // Chỉ bị đói khi energy dưới ngưỡng
            if (energyRatio < starvationThreshold)
            {
                // Tính damage dựa trên mức độ đói
                // Khi energy = 0: damage = starvationDamageRate
                // Khi energy = starvationThreshold: damage = 0
                // Damage tăng tuyến tính khi energy giảm
                float starvationLevel = 1f - (energyRatio / starvationThreshold); // 0 khi ở threshold, 1 khi energy = 0
                float damage = starvationDamageRate * starvationLevel * Time.fixedDeltaTime;
                
                currentHealth -= damage;
            }
            // Nếu có đủ năng lượng (>= threshold), không bị damage từ đói
        }

        /// <summary>
        /// Cập nhật lão hóa - Giảm máu khi đã trưởng thành
        /// </summary>
        private void UpdateAging()
        {
            if (maturity >= agingStartMaturity)
            {
                // Lão hóa càng nhanh khi càng già (maturity càng gần 1 hoặc hơn)
                float agingFactor = (maturity - agingStartMaturity) / (1f - agingStartMaturity);
                // Nếu maturity vượt quá 1 (do float hoặc logic khác), aging càng mạnh
                if (maturity > 1f) agingFactor = 1f + (maturity - 1f) * 5f; 
                
                float damage = agingDamageRate * agingFactor * Time.fixedDeltaTime;
                currentHealth -= damage;
            }
        }

        /// <summary>
        /// Kiểm tra và xử lý cái chết
        /// </summary>
        private void CheckDeath()
        {
            if (currentHealth <= 0f || (energy <= 0f && currentHealth <= 0f))
            {
                Die();
            }
        }

        /// <summary>
        /// Xử lý cái chết
        /// </summary>
        private void Die()
        {
            if (SimulationSupervisor.Instance != null)
            {
                SimulationSupervisor.Instance.OnCreatureDeath(this, transform.position, genome.size);
            }
            CreatureLineageRegistry.Unbind(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            CreatureLineageRegistry.Unbind(this);
            // Hủy đăng ký với BrainUpdateManager
            // BrainUpdateManager.Instance?.UnregisterCreature(this);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isPaused || rb == null) return;

            // Tính tốc độ hiệu dụng dựa trên vận tốc tịnh tiến và quay
            float linearSpeed = rb.linearVelocity.magnitude;
            float angularContribution = Mathf.Abs(rb.angularVelocity) * 0.01f;
            float effectiveSpeed = linearSpeed + angularContribution;

            if (effectiveSpeed <= 0f)
                return;

            // Giẫm lên trứng: chỉ cần có vận tốc là trứng vỡ
            var egg = collision.collider.GetComponent<CreatureEgg>();
            if (egg != null)
            {
                egg.BreakEgg();
            }

            // Giẫm lên sinh vật khác: chỉ gây sát thương nếu đủ nhanh
            var otherCreature = collision.collider.GetComponent<CreatureController>();
            if (otherCreature != null && otherCreature != this && effectiveSpeed >= trampleVelocityThreshold)
            {
                float damage = trampleDamageBase * effectiveSpeed;
                otherCreature.ReceiveTrampleDamage(damage);

                // Một phần sát thương phản lại chính mình
                float selfDamage = damage * selfTrampleDamageFactor;
                ReceiveTrampleDamage(selfDamage);
            }
        }

        /// <summary>
        /// Nhận sát thương từ cơ chế giẫm đạp
        /// </summary>
        public void ReceiveTrampleDamage(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f)
                return;

            if (trampleInvincibilitySeconds > 0f && Time.time - lastTrampleDamageTime < trampleInvincibilitySeconds)
                return;

            lastTrampleDamageTime = Time.time;
            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Lấy bộ não để sao chép khi sinh sản
        /// </summary>
        public NEATNetwork GetBrain() => brain;

        /// <summary>
        /// Đặt base metabolic rate (dùng cho điều chỉnh từ UI)
        /// </summary>
        public void SetBaseMetabolicRate(float value)
        {
            baseMetabolicRate = Mathf.Clamp(value, 0.1f, 5f);
        }

        // Getters
        public Genome GetGenome() => genome;
        public float Energy => energy;
        public float MaxEnergy => maxEnergy;
        public float Health => currentHealth;
        public float MaxHealth => genome.health;
        public float Maturity => maturity;
        public float Age => age;
        public float EnergyRatio => maxEnergy > 0 ? energy / maxEnergy : 0f;
        public float HealthRatio => genome.health > 0 ? currentHealth / genome.health : 0f;
        public void SetLineageRecord(CreatureLineageRecord record) => lineageRecord = record;
        public CreatureLineageRecord GetLineageRecord() => lineageRecord;
        public float TotalEnergyGained => totalEnergyGained;
        public int OffspringCount => offspringCount;

        /// <summary>
        /// Set pause state cho creature
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        /// <summary>
        /// Set state từ save data (dùng cho load game)
        /// </summary>
        public void SetStateFromSave(float energy, float maxEnergy, float health, float maturity, float age, int offspringCount = 0)
        {
            this.energy = energy;
            this.maxEnergy = maxEnergy;
            this.currentHealth = health;
            this.maturity = maturity;
            this.age = age;
            this.offspringCount = Mathf.Max(0, offspringCount);
        }

        /// <summary>
        /// Phát pheromone dựa trên loại pheromone trong gen
        /// </summary>
        private void EmitPheromone()
        {
            if (SimulationSupervisor.Instance == null ||
                !SimulationSupervisor.Instance.EnablePheromones ||
                SimulationSupervisor.Instance.HexGrid == null)
            {
                return;
            }

            // Kiểm tra cooldown theo bộ gen
            float cooldown = Mathf.Max(0.1f, genome.pheromoneCooldown);
            if (Time.time - lastPheromoneEmitTime < cooldown)
            {
                return;
            }

            int typeIndex = (int)genome.pheromoneType; // Red=0, Green=1, Blue=2
            typeIndex = Mathf.Clamp(typeIndex, 0, 2);

            // Vị trí thả pheromone ở phía sau sinh vật (đuôi)
            float currentSize = genome.size * (1f + maturity * 0.5f);
            Vector2 backward = - (Vector2)transform.right; // Phía sau, ngược với hướng mặt
            Vector2 emitPos = (Vector2)transform.position + backward * (currentSize * 0.8f);

            // Lượng pheromone phát ra mỗi lần phụ thuộc vào kích thước, cường độ output và gene pheromoneLifetime
            float lifetimeFactor = Mathf.Clamp(genome.pheromoneLifetime / 2f, 0.5f, 2f);
            float amount = pheromoneOutput * genome.size * lifetimeFactor;
            if (amount > 0f)
            {
                // Ghi pheromone trực tiếp vào HexGrid
                SimulationSupervisor.Instance.HexGrid.AddPheromoneAtWorld(emitPos, typeIndex, amount);

                lastPheromoneEmitTime = Time.time;

                // Hiệu ứng đám mây pheromone mờ tại đuôi sinh vật
                if (PheromoneEmitCloudManager.Instance != null)
                {
                    float cloudLifetime = Mathf.Clamp(genome.pheromoneLifetime, 0.2f, 5f);
                    PheromoneEmitCloudManager.Instance.SpawnCloud(emitPos, genome.pheromoneType, amount, cloudLifetime);
                }
            }
        }
    }
}

namespace Verrarium.Creature
{
    /// <summary>
    /// Bridges EventSystem pointer clicks to gameplay logic.
    /// Requires a Collider2D and a Physics2DRaycaster on the camera.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CreatureClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public static event System.Action<CreatureController> OnCreatureClicked;

        private CreatureController controller;

        private void Awake()
        {
            controller = GetComponent<CreatureController>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (controller == null)
                controller = GetComponent<CreatureController>();

            if (controller != null)
                OnCreatureClicked?.Invoke(controller);
        }
    }
}

