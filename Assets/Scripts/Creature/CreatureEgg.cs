using UnityEngine;
using Verrarium.Core;
using Verrarium.Data;
using Verrarium.Evolution;

namespace Verrarium.Creature
{
    /// <summary>
    /// Quản lý trứng sinh vật - chờ ấp và nở
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CreatureEgg : MonoBehaviour
    {
        [Header("Egg Settings")]
        [SerializeField] private float incubationDuration = 7f; // Thời gian ấp trứng (giây)
        
        private Genome genome;
        private NEATNetwork brain;
        private CreatureLineageRecord lineageRecord;
        private float spawnTime;
        private bool isHatched = false;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Tạo sprite đơn giản cho trứng nếu chưa có
            if (spriteRenderer.sprite == null)
            {
                CreateSimpleEggSprite();
            }
        }

        public void Initialize(Genome childGenome, NEATNetwork childBrain, CreatureLineageRecord lineageRecord)
        {
            this.genome = childGenome;
            this.brain = childBrain;
            this.lineageRecord = lineageRecord;
            this.spawnTime = Time.time;
            
            // Set màu cho trứng giống với sinh vật tương lai nhưng nhạt hơn
            if (spriteRenderer != null)
            {
                Color eggColor = childGenome.color;
                eggColor.a = 0.8f; // Hơi trong suốt
                spriteRenderer.color = eggColor;
                
                // Scale nhỏ hơn sinh vật trưởng thành một chút
                transform.localScale = Vector3.one * childGenome.size * 0.8f;
            }
        }

        private void Update()
        {
            if (isHatched) return;

            // Kiểm tra thời gian nở
            if (Time.time >= spawnTime + incubationDuration)
            {
                Hatch();
            }
            else
            {
                // Hiệu ứng rung nhẹ khi sắp nở
                float progress = (Time.time - spawnTime) / incubationDuration;
                if (progress > 0.8f)
                {
                    float shakeAmount = (progress - 0.8f) * 0.1f;
                    transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 20f) * shakeAmount * 50f);
                }
            }
        }

        private void Hatch()
        {
            isHatched = true;

            if (SimulationSupervisor.Instance != null)
            {
                // Gọi Supervisor để sinh sinh vật thực sự từ vị trí quả trứng
                GameObject creatureObj = SimulationSupervisor.Instance.SpawnCreature(transform.position, genome, brain, lineageRecord);
                
                // Thêm hiệu ứng nở trứng nếu cần (ví dụ: particle system)
            }

            Destroy(gameObject);
        }
        
        private void CreateSimpleEggSprite()
        {
            Texture2D texture = new Texture2D(32, 32);
            Vector2 center = new Vector2(16, 16);
            float radiusX = 12f;
            float radiusY = 15f; // Hình oval

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Ellipse equation: (x-h)^2/a^2 + (y-k)^2/b^2 <= 1
                    float dx = x - center.x;
                    float dy = y - center.y;
                    
                    if ((dx * dx) / (radiusX * radiusX) + (dy * dy) / (radiusY * radiusY) <= 1f)
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
    }
}





