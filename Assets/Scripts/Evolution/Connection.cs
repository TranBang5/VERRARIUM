using UnityEngine;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Đại diện cho một kết nối (synapse) giữa hai nơ-ron trong mạng NEAT
    /// </summary>
    [System.Serializable]
    public class Connection
    {
        public int innovationNumber; // Số đổi mới toàn cục
        public int fromNeuronId; // ID nơ-ron nguồn
        public int toNeuronId; // ID nơ-ron đích
        public float weight; // Trọng số
        public bool enabled; // Kết nối có được kích hoạt không

        public Connection(int innovationNumber, int fromNeuronId, int toNeuronId, float weight, bool enabled = true)
        {
            this.innovationNumber = innovationNumber;
            this.fromNeuronId = fromNeuronId;
            this.toNeuronId = toNeuronId;
            this.weight = weight;
            this.enabled = enabled;
        }

        /// <summary>
        /// Sao chép kết nối
        /// </summary>
        public Connection(Connection other)
        {
            this.innovationNumber = other.innovationNumber;
            this.fromNeuronId = other.fromNeuronId;
            this.toNeuronId = other.toNeuronId;
            this.weight = other.weight;
            this.enabled = other.enabled;
        }
    }
}

