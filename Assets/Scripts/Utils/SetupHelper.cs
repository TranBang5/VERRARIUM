using UnityEngine;

namespace Verrarium.Utils
{
    /// <summary>
    /// Helper script để tự động setup các component cần thiết
    /// Chạy trong Editor mode để kiểm tra và tạo các component thiếu
    /// </summary>
    [System.Serializable]
    public static class SetupHelper
    {
        /// <summary>
        /// Kiểm tra và setup Creature GameObject
        /// </summary>
        public static void SetupCreature(GameObject creatureObj)
        {
            // Rigidbody2D
            Rigidbody2D rb = creatureObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = creatureObj.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;

            // CircleCollider2D
            CircleCollider2D collider = creatureObj.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = creatureObj.AddComponent<CircleCollider2D>();
            }
            collider.radius = 0.5f;

            // SpriteRenderer
            SpriteRenderer sr = creatureObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = creatureObj.AddComponent<SpriteRenderer>();
            }

            // CreatureController
            Creature.CreatureController controller = creatureObj.GetComponent<Creature.CreatureController>();
            if (controller == null)
            {
                controller = creatureObj.AddComponent<Creature.CreatureController>();
            }
        }

        /// <summary>
        /// Kiểm tra và setup Resource GameObject
        /// </summary>
        public static void SetupResource(GameObject resourceObj, Resources.ResourceType type, float energyValue = 50f)
        {
            // CircleCollider2D (trigger)
            CircleCollider2D collider = resourceObj.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = resourceObj.AddComponent<CircleCollider2D>();
            }
            collider.isTrigger = true;
            collider.radius = 0.3f;

            // SpriteRenderer
            SpriteRenderer sr = resourceObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = resourceObj.AddComponent<SpriteRenderer>();
            }
            sr.color = type == Resources.ResourceType.Plant ? Color.green : Color.red;

            // Resource component
            Resources.Resource resource = resourceObj.GetComponent<Resources.Resource>();
            if (resource == null)
            {
                resource = resourceObj.AddComponent<Resources.Resource>();
            }
        }
    }
}

