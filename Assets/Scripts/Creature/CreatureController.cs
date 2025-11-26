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

        [Header("Metabolism")]
        [SerializeField] private float baseMetabolicRate = 0.8f; // Năng lượng tiêu thụ mỗi giây
        [SerializeField] private float movementEnergyCost = 0.5f; // Năng lượng tiêu thụ khi di chuyển
        [SerializeField] private float agingStartMaturity = 0.8f; // Bắt đầu lão hóa khi đạt 80% trưởng thành
        [SerializeField] private float agingDamageRate = 1.0f; // Sát thương mỗi giây do lão hóa

        [Header("Neural Network")]
        private NEATNetwork brain;
        private const int INPUT_COUNT = 10; // 10 đầu vào (đã tắt pheromone tạm thời)
        private const int OUTPUT_COUNT = 7; // Theo Bảng 2

        // Neural Network outputs
        private float accelerateOutput = 0f;
        private float rotateOutput = 0f;
        private float layEggOutput = 0f;
        private float growthOutput = 0f;
        private float healOutput = 0f;
        private float attackOutput = 0f;
        private float eatOutput = 0f;

        // Sensory data (inputs cho Neural Network)
        private float[] neuralInputs = new float[INPUT_COUNT];

        // Tài nguyên và sinh vật gần nhất
        private Resource closestPlant = null;
        private Resource closestMeat = null;
        private CreatureController closestCreature = null;
        private float lastEatTime = 0f;
        private const float EAT_COOLDOWN = 0.5f;
        private float lastReproduceTime = 0f;

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
            
            // Khởi tạo hoặc sao chép bộ não
            if (initialBrain != null)
            {
                brain = new NEATNetwork(initialBrain);
            }
            else
            {
                brain = new NEATNetwork(INPUT_COUNT, OUTPUT_COUNT);
            }
            
            ApplyGenomeToPhysics();
            energy = maxEnergy;
            currentHealth = genome.health;
            maturity = 0f;
            age = 0f;
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

        private void FixedUpdate()
        {
            if (genome.size == 0) return; // Chưa được khởi tạo

            age += Time.fixedDeltaTime;

            // Sense - Thu thập thông tin cảm giác (tạm thời random, sẽ thay bằng Neural Network)
            Sense();

            // Think - Tính toán Neural Network (tạm thời random)
            Think();

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
        /// Thu thập thông tin cảm giác - tất cả các đầu vào theo Bảng 2
        /// </summary>
        private void Sense()
        {
            if (SimulationSupervisor.Instance == null) return;

            Vector2 position = transform.position;
            Vector2 forward = transform.up;

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

            // Pheromone inputs đã được tắt tạm thời
            // (Có thể bật lại sau khi tích hợp rtNEAT đầy đủ)
        }

        /// <summary>
        /// Tính toán Neural Network - kích hoạt mạng với các đầu vào cảm giác
        /// </summary>
        private void Think()
        {
            if (brain == null) return;

            // Tính toán đầu ra từ mạng nơ-ron
            float[] outputs = brain.Compute(neuralInputs);

            // Gán các đầu ra (theo thứ tự trong Bảng 2)
            accelerateOutput = outputs[0];      // [0.0, 1.0]
            rotateOutput = (outputs[1] - 0.5f) * 2f; // Chuyển [0,1] thành [-1,1]
            layEggOutput = outputs[2];          // [0.0, 1.0]
            growthOutput = outputs[3];          // [0.0, 1.0]
            healOutput = outputs[4];             // [0.0, 1.0]
            attackOutput = outputs[5];          // [0.0, 1.0]
            eatOutput = outputs[6];             // [0.0, 1.0]
        }

        /// <summary>
        /// Thực thi hành động dựa trên Neural Network outputs
        /// </summary>
        private void Act()
        {
            // Di chuyển
            if (accelerateOutput > 0.1f)
            {
                Vector2 force = transform.up * accelerateOutput * genome.speed * 5f;
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

            // Pheromone đã được tắt tạm thời
            // EmitPheromone();
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

            float distance = Vector2.Distance(transform.position, targetResource.transform.position);
            if (distance > 1.5f) return; // Quá xa

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
                lastEatTime = Time.time;

                if (SimulationSupervisor.Instance != null)
                {
                    SimulationSupervisor.Instance.RemoveResource(targetResource);
                }

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
            
            // Tiêu thụ năng lượng
            float growthCost = 2f * Time.fixedDeltaTime;
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
            if (maturity < 0.8f) return;

            // Tiêu thụ năng lượng để sinh sản
            float reproductionCost = genome.reproEnergyThreshold * 0.5f;
            energy -= reproductionCost;
            lastReproduceTime = Time.time;

            // Tạo trứng
            if (SimulationSupervisor.Instance != null)
            {
                Vector2 eggPosition = (Vector2)transform.position + Random.insideUnitCircle * 1f;
                SimulationSupervisor.Instance.OnCreatureReproduction(this, eggPosition, genome);
            }
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

            // Mất máu nếu hết năng lượng
            if (energy <= 0f)
            {
                currentHealth -= 5f * Time.fixedDeltaTime;
                energy = 0f;
            }
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
            if (currentHealth <= 0f || energy <= 0f && currentHealth <= 0f)
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

