using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Verrarium.Core
{
    /// <summary>
    /// UI hiển thị thống kê giả lập
    /// </summary>
    public class SimulationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI bornText;
        [SerializeField] private TextMeshProUGUI diedText;
        [SerializeField] private TextMeshProUGUI timeText;

        private SimulationSupervisor supervisor;

        private void Start()
        {
            supervisor = SimulationSupervisor.Instance;
        }

        private void Update()
        {
            if (supervisor == null) return;

            if (populationText != null)
                populationText.text = $"Population: {supervisor.CurrentPopulation}";

            if (bornText != null)
                bornText.text = $"Born: {supervisor.TotalBorn}";

            if (diedText != null)
                diedText.text = $"Died: {supervisor.TotalDied}";

            if (timeText != null)
            {
                float time = supervisor.SimulationTime;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }
}

