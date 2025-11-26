using UnityEngine;

namespace Verrarium.Utils
{
    /// <summary>
    /// Các hàm tiện ích toán học
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Chuẩn hóa góc về phạm vi [-180, 180]
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Tính góc từ vị trí này đến vị trí khác, trả về trong phạm vi [-1, 1]
        /// -1 = bên trái, 1 = bên phải
        /// </summary>
        public static float AngleToTarget(Vector2 from, Vector2 to, Vector2 forward)
        {
            Vector2 direction = (to - from).normalized;
            float angle = Vector2.SignedAngle(forward, direction);
            return Mathf.Clamp(angle / 180f, -1f, 1f);
        }

        /// <summary>
        /// Chuẩn hóa khoảng cách về phạm vi [0, 1]
        /// </summary>
        public static float NormalizeDistance(float distance, float maxDistance)
        {
            return Mathf.Clamp01(distance / maxDistance);
        }
    }
}

