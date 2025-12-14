using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Verrarium.Core;
using Verrarium.Save;

namespace Verrarium.UI
{
    /// <summary>
    /// Menu save với 20 save slots và input field cho tên
    /// </summary>
    public class SaveMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject saveMenuPanel;
        [SerializeField] private Transform saveSlotsContainer;
        [SerializeField] private GameObject saveSlotPrefab;
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button confirmSaveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI selectedSlotText;

        private const int MAX_SAVE_SLOTS = 20;
        private int selectedSlotIndex = -1;
        private SaveSlotButton[] saveSlots = new SaveSlotButton[MAX_SAVE_SLOTS];
        private SimulationSupervisor supervisor;

        private void Awake()
        {
            supervisor = SimulationSupervisor.Instance;
            
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);

            SetupUI();
        }

        private void SetupUI()
        {
            if (saveSlotsContainer == null || saveSlotPrefab == null)
                return;

            // Tạo 20 save slots
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                GameObject slotObj = null;
                try
                {
                    slotObj = Instantiate(saveSlotPrefab, saveSlotsContainer);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to instantiate save slot prefab: {e.Message}");
                    // Tạo slot mới nếu prefab bị lỗi
                    slotObj = new GameObject($"SaveSlot_{i}");
                    slotObj.transform.SetParent(saveSlotsContainer, false);
                    slotObj.AddComponent<RectTransform>();
                }
                
                if (slotObj == null) continue;
                
                SaveSlotButton slotButton = slotObj.GetComponent<SaveSlotButton>();
                
                if (slotButton == null)
                    slotButton = slotObj.AddComponent<SaveSlotButton>();

                int slotIndex = i;
                slotButton.Initialize(slotIndex, $"Save Slot {i + 1}", OnSlotSelected);
                saveSlots[i] = slotButton;
            }

            if (confirmSaveButton != null)
                confirmSaveButton.onClick.AddListener(OnConfirmSave);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Hide);

            if (saveNameInput != null)
                saveNameInput.onValueChanged.AddListener(OnSaveNameChanged);

            RefreshSaveSlots();
        }

        /// <summary>
        /// Hiển thị save menu
        /// </summary>
        public void Show()
        {
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(true);

            selectedSlotIndex = -1;
            RefreshSaveSlots();
            
            if (saveNameInput != null)
            {
                saveNameInput.text = "";
                saveNameInput.interactable = false;
            }

            if (confirmSaveButton != null)
                confirmSaveButton.interactable = false;

            if (selectedSlotText != null)
                selectedSlotText.text = "Select a save slot";
        }

        /// <summary>
        /// Ẩn save menu
        /// </summary>
        public void Hide()
        {
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Kiểm tra menu có đang hiển thị không
        /// </summary>
        public bool IsVisible()
        {
            return saveMenuPanel != null && saveMenuPanel.activeSelf;
        }

        private void OnSlotSelected(int slotIndex)
        {
            selectedSlotIndex = slotIndex;

            // Update UI
            for (int i = 0; i < saveSlots.Length; i++)
            {
                if (saveSlots[i] != null)
                    saveSlots[i].SetSelected(i == slotIndex);
            }

            // Enable input field
            if (saveNameInput != null)
            {
                saveNameInput.interactable = true;
                
                // Load existing save name if available
                var saveFiles = SimulationSaveSystem.GetSaveFiles();
                var existingSave = saveFiles.FirstOrDefault(s => s.saveName == $"save_{slotIndex}");
                if (existingSave != null)
                {
                    saveNameInput.text = existingSave.displayName;
                }
                else
                {
                    saveNameInput.text = $"Save Slot {slotIndex + 1}";
                }
            }

            if (confirmSaveButton != null)
                confirmSaveButton.interactable = true;

            if (selectedSlotText != null)
                selectedSlotText.text = $"Selected: Slot {slotIndex + 1}";
        }

        private void OnSaveNameChanged(string newName)
        {
            // Validate save name
            if (confirmSaveButton != null)
            {
                bool isValid = !string.IsNullOrWhiteSpace(newName) && 
                              SimulationSaveSystem.IsValidSaveName(newName);
                confirmSaveButton.interactable = isValid && selectedSlotIndex >= 0;
            }
        }

        private void OnConfirmSave()
        {
            if (supervisor == null || selectedSlotIndex < 0)
                return;

            string saveName = saveNameInput != null ? saveNameInput.text : $"Save Slot {selectedSlotIndex + 1}";
            
            if (string.IsNullOrWhiteSpace(saveName))
            {
                Debug.LogWarning("Save name cannot be empty!");
                return;
            }

            if (!SimulationSaveSystem.IsValidSaveName(saveName))
            {
                Debug.LogWarning("Invalid save name! Contains invalid characters.");
                return;
            }

            // Tạo unique save name với slot index
            string uniqueSaveName = $"save_{selectedSlotIndex}_{saveName}";

            // Save game
            bool success = SimulationSaveSystem.Save(uniqueSaveName, supervisor);
            
            if (success)
            {
                Debug.Log($"Game saved to slot {selectedSlotIndex + 1}: {saveName}");
                RefreshSaveSlots();
                Hide();
            }
            else
            {
                Debug.LogError("Failed to save game!");
            }
        }

        /// <summary>
        /// Refresh save slots với thông tin mới nhất
        /// </summary>
        private void RefreshSaveSlots()
        {
            var saveFiles = SimulationSaveSystem.GetSaveFiles();
            
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                if (saveSlots[i] == null) continue;

                // Tìm save file cho slot này
                var slotSave = saveFiles.FirstOrDefault(s => s.saveName.StartsWith($"save_{i}_"));
                
                if (slotSave != null)
                {
                    saveSlots[i].UpdateInfo(slotSave.displayName, slotSave.saveTime, slotSave.currentPopulation);
                }
                else
                {
                    saveSlots[i].UpdateInfo($"Save Slot {i + 1}", null, 0);
                }
            }
        }
    }

    /// <summary>
    /// Component cho save slot button
    /// </summary>
    public class SaveSlotButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI slotNameText;
        [SerializeField] private TextMeshProUGUI saveTimeText;
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private Image backgroundImage;

        private int slotIndex;
        private Action<int> onSelected;
        private bool isSelected = false;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (button != null)
                button.onClick.AddListener(() => onSelected?.Invoke(slotIndex));
        }

        public void Initialize(int index, string defaultName, Action<int> onSelectedCallback)
        {
            slotIndex = index;
            onSelected = onSelectedCallback;
            
            if (slotNameText != null)
                slotNameText.text = defaultName;
        }

        public void UpdateInfo(string displayName, DateTime? saveTime, int population)
        {
            if (slotNameText != null)
                slotNameText.text = displayName;

            if (saveTimeText != null)
            {
                if (saveTime.HasValue)
                    saveTimeText.text = saveTime.Value.ToString("yyyy-MM-dd HH:mm");
                else
                    saveTimeText.text = "Empty";
            }

            if (populationText != null)
            {
                if (population > 0)
                    populationText.text = $"Pop: {population}";
                else
                    populationText.text = "";
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? new Color(0.3f, 0.6f, 1f, 0.5f) : Color.white;
            }
        }
    }
}

