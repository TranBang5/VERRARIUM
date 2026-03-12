using UnityEngine;

namespace Verrarium.Utils
{
    /// <summary>
    /// Visual đám mây pheromone xuất hiện tại vị trí nhả (đuôi sinh vật) với hiệu ứng mờ dần.
    /// Tuổi thọ ngắn, tự trả về pool thông qua callback.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PheromoneCloud : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private AnimationCurve alphaOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private float startScale = 0.4f;
        [SerializeField] private float endScale = 0.9f;

        private SpriteRenderer spriteRenderer;
        private float age;
        private Color baseColor;
        private System.Action<PheromoneCloud> releaseCallback;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateCircleSprite();
            }

            spriteRenderer.sortingOrder = -5;
        }

        public void Initialize(Vector2 position, Color color, float strength, System.Action<PheromoneCloud> onRelease, float lifetimeSeconds)
        {
            transform.position = position;

            baseColor = color;
            float intensity = Mathf.Clamp01(strength);
            baseColor.a = Mathf.Clamp01(baseColor.a * Mathf.Lerp(0.4f, 1f, intensity));

            age = 0f;
            if (lifetimeSeconds > 0f)
            {
                lifetime = lifetimeSeconds;
            }
            releaseCallback = onRelease;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.0001f, lifetime));

            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = Vector3.one * scale;

            float alphaT = alphaOverLifetime.Evaluate(t);
            Color c = baseColor;
            c.a *= alphaT;
            spriteRenderer.color = c;

            if (age >= lifetime)
            {
                gameObject.SetActive(false);
                releaseCallback?.Invoke(this);
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(dist / radius);
                    float alpha = 1f - t; // Mờ dần ra ngoài

                    if (dist <= radius)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}

