using System.Collections.Generic;

namespace Verrarium.Evolution
{
    /// <summary>
    /// Theo dõi các đổi mới (innovation numbers) để đảm bảo tính nhất quán trong tiến hóa
    /// Singleton pattern để chia sẻ giữa tất cả các mạng
    /// </summary>
    public class InnovationTracker
    {
        private static InnovationTracker instance;
        public static InnovationTracker Instance
        {
            get
            {
                if (instance == null)
                    instance = new InnovationTracker();
                return instance;
            }
        }

        private int nextInnovationNumber = 1;
        private Dictionary<string, int> innovationMap; // Map (fromId,toId) -> innovationNumber

        private InnovationTracker()
        {
            innovationMap = new Dictionary<string, int>();
        }

        /// <summary>
        /// Lấy hoặc tạo innovation number cho một kết nối mới
        /// </summary>
        public int GetInnovationNumber(int fromNeuronId, int toNeuronId)
        {
            string key = $"{fromNeuronId}_{toNeuronId}";
            
            if (innovationMap.ContainsKey(key))
            {
                return innovationMap[key];
            }
            else
            {
                int innovation = nextInnovationNumber++;
                innovationMap[key] = innovation;
                return innovation;
            }
        }

        /// <summary>
        /// Reset tracker (dùng khi bắt đầu giả lập mới)
        /// </summary>
        public void Reset()
        {
            nextInnovationNumber = 1;
            innovationMap.Clear();
        }
    }
}

