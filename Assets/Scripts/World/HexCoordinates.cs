using UnityEngine;

namespace Verrarium.World
{
    /// <summary>
    /// Tọa độ hex sử dụng hệ thống axial coordinates (q, r)
    /// </summary>
    [System.Serializable]
    public struct HexCoordinates
    {
        public int q; // Column (x-axis)
        public int r; // Row (y-axis)

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        /// <summary>
        /// Chuyển đổi sang tọa độ world (Unity)
        /// </summary>
        public Vector2 ToWorld(float hexSize)
        {
            float x = hexSize * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2 * r);
            float y = hexSize * (3f / 2f * r);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Chuyển đổi từ tọa độ world sang hex coordinates
        /// </summary>
        public static HexCoordinates FromWorld(Vector2 worldPos, float hexSize)
        {
            float q = (Mathf.Sqrt(3) / 3 * worldPos.x - 1f / 3f * worldPos.y) / hexSize;
            float r = (2f / 3f * worldPos.y) / hexSize;
            return HexRound(q, r);
        }

        /// <summary>
        /// Làm tròn tọa độ hex
        /// </summary>
        private static HexCoordinates HexRound(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                rq = -rr - rs;
            }
            else if (rDiff > sDiff)
            {
                rr = -rq - rs;
            }

            return new HexCoordinates(rq, rr);
        }

        /// <summary>
        /// Tính khoảng cách giữa hai hex
        /// </summary>
        public int DistanceTo(HexCoordinates other)
        {
            return (Mathf.Abs(q - other.q) + 
                   Mathf.Abs(q + r - other.q - other.r) + 
                   Mathf.Abs(r - other.r)) / 2;
        }

        public override bool Equals(object obj)
        {
            if (obj is HexCoordinates)
            {
                HexCoordinates other = (HexCoordinates)obj;
                return q == other.q && r == other.r;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return q.GetHashCode() ^ r.GetHashCode();
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b)
        {
            return a.q == b.q && a.r == b.r;
        }

        public static bool operator !=(HexCoordinates a, HexCoordinates b)
        {
            return !(a == b);
        }
    }
}

