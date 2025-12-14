using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Verrarium.Core;
using Verrarium.Save;

namespace Verrarium.UI
{
    /// <summary>
    /// Menu load để hiển thị và load save files
    /// </summary>
    public class LoadMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadMenuPanel;
        [SerializeField] private Transform saveFilesContainer;
        [SerializeField] private GameObject saveFileItemPrefab;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI noSavesText;

        private SimulationSupervisor supervisor;

        private void Awake()
        {
            supervisor = SimulationSupervisor.Instance;
            
            if (loadMenuPanel != null)
                loadMenuPanel.SetActive(false);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Hide);
        }

        /// <summary>
        /// Hiển thị load menu
        /// </summary>
        public void Show()
        {
            if (loadMenuPanel != null)
                loadMenuPanel.SetActive(true);

            RefreshSaveFiles();
        }

        /// <summary>
        /// Ẩn load menu
        /// </summary>
        public void Hide()
        {
            if (loadMenuPanel != null)
                loadMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Kiểm tra menu có đang hiển thị không
        /// </summary>
        public bool IsVisible()
        {
            return loadMenuPanel != null && loadMenuPanel.activeSelf;
        }

        /// <summary>
        /// Refresh danh sách save files
        /// </summary>
        private void RefreshSaveFiles()
        {
            if (saveFilesContainer == null || saveFileItemPrefab == null)
                return;

            // Xóa các items cũ
            foreach (Transform child in saveFilesContainer)
            {
                Destroy(child.gameObject);
            }

            // Lấy danh sách save files (autosave sẽ tự động ở đầu)
            var saveFiles = SimulationSaveSystem.GetSaveFiles();

            if (saveFiles.Length == 0)
            {
                if (noSavesText != null)
                {
                    noSavesText.gameObject.SetActive(true);
                    noSavesText.text = "No save files found";
                }
                return;
            }

            if (noSavesText != null)
                noSavesText.gameObject.SetActive(false);

            // Tạo UI items cho mỗi save file
            // Autosave sẽ tự động ở đầu do đã được sắp xếp trong GetSaveFiles()
            foreach (var saveFile in saveFiles)
            {
                GameObject itemObj = Instantiate(saveFileItemPrefab, saveFilesContainer);
                SaveFileItem item = itemObj.GetComponent<SaveFileItem>();
                
                if (item == null)
                    item = itemObj.AddComponent<SaveFileItem>();

                item.Initialize(saveFile, OnLoadClicked);
            }
        }

        /// <summary>
        /// Xử lý khi click load
        /// </summary>
        private void OnLoadClicked(SaveFileInfo saveFile)
        {
            if (supervisor == null)
                return;

            // Load save data
            SimulationSaveData saveData = SimulationSaveSystem.Load(saveFile.saveName);
            
            if (saveData == null)
            {
                Debug.LogError($"Failed to load save: {saveFile.saveName}");
                return;
            }

            // Load simulation
            supervisor.LoadFromSaveData(saveData);
            
            Debug.Log($"Game loaded: {saveFile.displayName}");
            Hide();
        }
    }

    /// <summary>
    /// Component cho save file item trong load menu
    /// </summary>
    public class SaveFileItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI fileNameText;
        [SerializeField] private TextMeshProUGUI saveTimeText;
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI simulationTimeText;
        [SerializeField] private Button deleteButton;

        private SaveFileInfo saveFile;
        private System.Action<SaveFileInfo> onLoadClicked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (button != null)
                button.onClick.AddListener(() => onLoadClicked?.Invoke(saveFile));

            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        public void Initialize(SaveFileInfo fileInfo, System.Action<SaveFileInfo> onLoad)
        {
            saveFile = fileInfo;
            onLoadClicked = onLoad;

            if (fileNameText != null)
            {
                fileNameText.text = fileInfo.displayName;
                // Highlight autosave với màu khác
                if (fileInfo.isAutosave)
                {
                    fileNameText.color = new Color(0.8f, 0.9f, 1f, 1f); // Light blue
                    fileNameText.fontStyle = FontStyles.Bold;
                }
                else
                {
                    fileNameText.color = Color.white;
                    fileNameText.fontStyle = FontStyles.Normal;
                }
            }

            if (saveTimeText != null)
                saveTimeText.text = fileInfo.saveTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (populationText != null)
                populationText.text = $"Population: {fileInfo.currentPopulation}";

            if (simulationTimeText != null)
            {
                int minutes = Mathf.FloorToInt(fileInfo.simulationTime / 60f);
                int seconds = Mathf.FloorToInt(fileInfo.simulationTime % 60f);
                simulationTimeText.text = $"Time: {minutes:D2}:{seconds:D2}";
            }
        }

        private void OnDeleteClicked()
        {
            if (saveFile == null)
                return;

            // Confirm delete
            bool confirmed = true; // Có thể thêm confirmation dialog ở đây
            
            if (confirmed)
            {
                SimulationSaveSystem.Delete(saveFile.saveName);
                Destroy(gameObject);
            }
        }
    }
}

