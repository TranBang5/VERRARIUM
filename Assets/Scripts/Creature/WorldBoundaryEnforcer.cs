using UnityEngine;

namespace Verrarium.Creature
{
    /// <summary>
    /// Component để đảm bảo creatures không đi quá xa khỏi world bounds
    /// Đẩy creatures quay lại nếu chúng đi ra ngoài
    /// </summary>
    public class WorldBoundaryEnforcer : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Vector2 worldSize;
        private float boundaryMargin = 0.5f; // Margin để đẩy creatures quay lại trước khi chạm border

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody2D>();
            }
        }

        private void Start()
        {
            if (Core.SimulationSupervisor.Instance != null)
            {
                worldSize = Core.SimulationSupervisor.Instance.WorldSize;
            }
        }

        private void FixedUpdate()
        {
            if (rb == null || Core.SimulationSupervisor.Instance == null) return;

            // Cập nhật world size nếu thay đổi
            worldSize = Core.SimulationSupervisor.Instance.WorldSize;

            Vector2 position = transform.position;
            Vector2 clampedPosition = position;
            bool needsClamp = false;

            // Kiểm tra và clamp position về trong bounds
            float halfWidth = worldSize.x / 2f;
            float halfHeight = worldSize.y / 2f;

            if (position.x < -halfWidth + boundaryMargin)
            {
                clampedPosition.x = -halfWidth + boundaryMargin;
                needsClamp = true;
            }
            else if (position.x > halfWidth - boundaryMargin)
            {
                clampedPosition.x = halfWidth - boundaryMargin;
                needsClamp = true;
            }

            if (position.y < -halfHeight + boundaryMargin)
            {
                clampedPosition.y = -halfHeight + boundaryMargin;
                needsClamp = true;
            }
            else if (position.y > halfHeight - boundaryMargin)
            {
                clampedPosition.y = halfHeight - boundaryMargin;
                needsClamp = true;
            }

            // Nếu cần clamp, đẩy creature quay lại
            if (needsClamp)
            {
                transform.position = clampedPosition;
                
                // Đảo ngược velocity để đẩy creature quay lại
                Vector2 directionToCenter = (Vector2.zero - clampedPosition).normalized;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, directionToCenter * rb.linearVelocity.magnitude * 0.5f, 0.3f);
            }
        }
    }
}

