using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Verrarium.Core;

namespace Verrarium.UI
{
    /// <summary>
    /// Panel điều khiển môi trường ở top bar - cho phép điều chỉnh các thông số giả lập
    /// </summary>
    public class EnvironmentControlPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject controlPanel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private RectTransform toggleArrow; // Arrow RectTransform để xoay
        private TextMeshProUGUI toggleArrowText;

        [Header("Population Controls")]
        [SerializeField] private Slider targetPopulationSlider;
        [SerializeField] private TextMeshProUGUI targetPopulationValueText;
        [SerializeField] private Slider maxPopulationSlider;
        [SerializeField] private TextMeshProUGUI maxPopulationValueText;

        [Header("Resource Controls")]
        [SerializeField] private Slider resourceSpawnIntervalSlider;
        [SerializeField] private TextMeshProUGUI resourceSpawnIntervalValueText;
        [SerializeField] private Slider plantsPerSpawnSlider;
        [SerializeField] private TextMeshProUGUI plantsPerSpawnValueText;

        [Header("World Controls")]
        [SerializeField] private Slider worldSizeXSlider;
        [SerializeField] private TextMeshProUGUI worldSizeXValueText;
        [SerializeField] private Slider worldSizeYSlider;
        [SerializeField] private TextMeshProUGUI worldSizeYValueText;

        [Header("Advanced Controls")]
        [SerializeField] private GameObject advancedSection;
        [SerializeField] private Button advancedToggleButton;
        [SerializeField] private Slider baseMetabolicRateSlider;
        [SerializeField] private TextMeshProUGUI baseMetabolicRateValueText;

        private SimulationSupervisor supervisor;
        private bool isExpanded = false;
        private bool advancedExpanded = false;

        private void Start()
        {
            supervisor = SimulationSupervisor.Instance;

            if (supervisor == null)
            {
                Debug.LogWarning("SimulationSupervisor not found!");
                return;
            }

            SetupControls();
            SetupButtons();
            LoadCurrentValues();

            // Đặt trạng thái mặc định là đóng
            SetPanelExpanded(false);
        }

        /// <summary>
        /// Setup các controls với giá trị min/max
        /// </summary>
        private void SetupControls()
        {
            // Target Population
            if (targetPopulationSlider != null)
            {
                targetPopulationSlider.minValue = 10;
                targetPopulationSlider.maxValue = 200;
                targetPopulationSlider.onValueChanged.AddListener(OnTargetPopulationChanged);
            }

            // Max Population
            if (maxPopulationSlider != null)
            {
                maxPopulationSlider.minValue = 20;
                maxPopulationSlider.maxValue = 500;
                maxPopulationSlider.onValueChanged.AddListener(OnMaxPopulationChanged);
            }

            // Resource Spawn Interval
            if (resourceSpawnIntervalSlider != null)
            {
                resourceSpawnIntervalSlider.minValue = 0.5f;
                resourceSpawnIntervalSlider.maxValue = 10f;
                resourceSpawnIntervalSlider.onValueChanged.AddListener(OnResourceSpawnIntervalChanged);
            }

            // Plants Per Spawn
            if (plantsPerSpawnSlider != null)
            {
                plantsPerSpawnSlider.minValue = 1;
                plantsPerSpawnSlider.maxValue = 20;
                plantsPerSpawnSlider.wholeNumbers = true;
                plantsPerSpawnSlider.onValueChanged.AddListener(OnPlantsPerSpawnChanged);
            }

            // World Size X
            if (worldSizeXSlider != null)
            {
                worldSizeXSlider.minValue = 10f;
                worldSizeXSlider.maxValue = 50f;
                worldSizeXSlider.onValueChanged.AddListener(OnWorldSizeXChanged);
            }

            // World Size Y
            if (worldSizeYSlider != null)
            {
                worldSizeYSlider.minValue = 10f;
                worldSizeYSlider.maxValue = 50f;
                worldSizeYSlider.onValueChanged.AddListener(OnWorldSizeYChanged);
            }

            // Base Metabolic Rate
            if (baseMetabolicRateSlider != null)
            {
                baseMetabolicRateSlider.minValue = 0.1f;
                baseMetabolicRateSlider.maxValue = 5f;
                baseMetabolicRateSlider.onValueChanged.AddListener(OnBaseMetabolicRateChanged);
            }
        }

        /// <summary>
        /// Setup các buttons
        /// </summary>
        private void SetupButtons()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(TogglePanel);

            if (advancedToggleButton != null)
                advancedToggleButton.onClick.AddListener(ToggleAdvanced);
        }

        /// <summary>
        /// Load giá trị hiện tại từ SimulationSupervisor
        /// </summary>
        private void LoadCurrentValues()
        {
            if (supervisor == null) return;

            // Load từ supervisor
            if (targetPopulationSlider != null)
                targetPopulationSlider.value = supervisor.GetTargetPopulationSize();
            
            if (maxPopulationSlider != null)
                maxPopulationSlider.value = supervisor.GetMaxPopulationSize();
            
            if (resourceSpawnIntervalSlider != null)
                resourceSpawnIntervalSlider.value = supervisor.GetResourceSpawnInterval();
            
            if (plantsPerSpawnSlider != null)
                plantsPerSpawnSlider.value = supervisor.GetPlantsPerSpawn();
            
            Vector2 worldSize = supervisor.GetWorldSize();
            if (worldSizeXSlider != null)
                worldSizeXSlider.value = worldSize.x;
            
            if (worldSizeYSlider != null)
                worldSizeYSlider.value = worldSize.y;

            // Cập nhật text values
            UpdateAllValueTexts();
        }

        /// <summary>
        /// Cập nhật tất cả value texts
        /// </summary>
        private void UpdateAllValueTexts()
        {
            if (targetPopulationSlider != null && targetPopulationValueText != null)
                targetPopulationValueText.text = Mathf.RoundToInt(targetPopulationSlider.value).ToString();
            
            if (maxPopulationSlider != null && maxPopulationValueText != null)
                maxPopulationValueText.text = Mathf.RoundToInt(maxPopulationSlider.value).ToString();
            
            if (resourceSpawnIntervalSlider != null && resourceSpawnIntervalValueText != null)
                resourceSpawnIntervalValueText.text = resourceSpawnIntervalSlider.value.ToString("F1") + "s";
            
            if (plantsPerSpawnSlider != null && plantsPerSpawnValueText != null)
                plantsPerSpawnValueText.text = Mathf.RoundToInt(plantsPerSpawnSlider.value).ToString();
            
            if (worldSizeXSlider != null && worldSizeXValueText != null)
                worldSizeXValueText.text = worldSizeXSlider.value.ToString("F1");
            
            if (worldSizeYSlider != null && worldSizeYValueText != null)
                worldSizeYValueText.text = worldSizeYSlider.value.ToString("F1");
            
            if (baseMetabolicRateSlider != null && baseMetabolicRateValueText != null)
                baseMetabolicRateValueText.text = baseMetabolicRateSlider.value.ToString("F2");
        }

        private void OnEnable()
        {
            // Load values khi script được enable
            if (supervisor != null)
            {
                LoadCurrentValues();
            }
        }

        /// <summary>
        /// Toggle mở/đóng panel
        /// </summary>
        private void TogglePanel()
        {
            SetPanelExpanded(!isExpanded);
        }

        /// <summary>
        /// Thiết lập trạng thái panel (mở/đóng)
        /// </summary>
        private void SetPanelExpanded(bool expanded)
        {
            isExpanded = expanded;

            if (controlPanel != null)
                controlPanel.SetActive(isExpanded);

            bool hasTextArrow = TryCacheToggleArrowText();

            if (toggleArrow != null && !hasTextArrow)
                toggleArrow.rotation = Quaternion.Euler(0, 0, isExpanded ? 0 : 180);

            if (toggleArrowText != null)
                toggleArrowText.text = isExpanded ? "^" : "v";
        }

        /// <summary>
        /// Toggle advanced section
        /// </summary>
        private void ToggleAdvanced()
        {
            advancedExpanded = !advancedExpanded;
            if (advancedSection != null)
                advancedSection.SetActive(advancedExpanded);
        }

        private bool TryCacheToggleArrowText()
        {
            if (toggleArrowText != null)
                return true;

            if (toggleArrow != null)
                toggleArrowText = toggleArrow.GetComponent<TextMeshProUGUI>();

            return toggleArrowText != null;
        }

        // Event handlers cho các controls

        private void OnTargetPopulationChanged(float value)
        {
            if (supervisor != null)
            {
                supervisor.SetTargetPopulationSize(Mathf.RoundToInt(value));
            }
            if (targetPopulationValueText != null)
                targetPopulationValueText.text = Mathf.RoundToInt(value).ToString();
        }

        private void OnMaxPopulationChanged(float value)
        {
            if (supervisor != null)
            {
                supervisor.SetMaxPopulationSize(Mathf.RoundToInt(value));
            }
            if (maxPopulationValueText != null)
                maxPopulationValueText.text = Mathf.RoundToInt(value).ToString();
        }

        private void OnResourceSpawnIntervalChanged(float value)
        {
            if (supervisor != null)
            {
                supervisor.SetResourceSpawnInterval(value);
            }
            if (resourceSpawnIntervalValueText != null)
                resourceSpawnIntervalValueText.text = value.ToString("F1") + "s";
        }

        private void OnPlantsPerSpawnChanged(float value)
        {
            int intValue = Mathf.RoundToInt(value);
            if (supervisor != null)
            {
                supervisor.SetPlantsPerSpawn(intValue);
            }
            if (plantsPerSpawnValueText != null)
                plantsPerSpawnValueText.text = intValue.ToString();
        }

        private void OnWorldSizeXChanged(float value)
        {
            if (supervisor != null)
            {
                Vector2 currentSize = supervisor.GetWorldSize();
                supervisor.SetWorldSize(new Vector2(value, currentSize.y));
            }
            if (worldSizeXValueText != null)
                worldSizeXValueText.text = value.ToString("F1");
        }

        private void OnWorldSizeYChanged(float value)
        {
            if (supervisor != null)
            {
                Vector2 currentSize = supervisor.GetWorldSize();
                supervisor.SetWorldSize(new Vector2(currentSize.x, value));
            }
            if (worldSizeYValueText != null)
                worldSizeYValueText.text = value.ToString("F1");
        }

        private void OnBaseMetabolicRateChanged(float value)
        {
            if (supervisor != null)
            {
                supervisor.SetBaseMetabolicRate(value);
            }
            if (baseMetabolicRateValueText != null)
                baseMetabolicRateValueText.text = value.ToString("F2");
        }

        /// <summary>
        /// Cập nhật giá trị sliders từ supervisor (gọi từ supervisor khi thay đổi)
        /// </summary>
        public void UpdateFromSupervisor()
        {
            if (supervisor == null) return;

            // Cập nhật sliders nếu có thay đổi từ bên ngoài
            // (có thể gọi từ SimulationSupervisor khi có thay đổi)
        }
    }
}

