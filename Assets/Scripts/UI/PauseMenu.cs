using UnityEngine;
using UnityEngine.UI;
using Verrarium.Core;

namespace Verrarium.UI
{
    /// <summary>
    /// Menu pause với các option Save, Load, Resume, Exit
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;

        [Header("Sub Menus")]
        [SerializeField] private SaveMenu saveMenu;
        [SerializeField] private LoadMenu loadMenu;

        private SimulationSupervisor supervisor;
        private bool isPaused = false;

        private void Awake()
        {
            supervisor = SimulationSupervisor.Instance;
            
            if (supervisor == null)
            {
                Debug.LogWarning("PauseMenu: SimulationSupervisor.Instance is null in Awake!");
            }
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
                Debug.Log("PauseMenu: Panel initialized and hidden");
            }
            else
            {
                Debug.LogError("PauseMenu: pauseMenuPanel is null! Please assign it in Inspector.");
            }

            SetupButtons();
        }

        private void Update()
        {
            // Kiểm tra nút ESC để toggle pause
            if (IsKeyDown(KeyCode.Escape))
            {
                Debug.Log($"PauseMenu Update: ESC pressed, isPaused = {isPaused}, pauseMenuPanel = {(pauseMenuPanel != null ? "assigned" : "NULL")}");
                
                if (isPaused)
                {
                    // Nếu đang ở sub menu, quay lại pause menu
                    if (saveMenu != null && saveMenu.IsVisible())
                    {
                        saveMenu.Hide();
                        Show();
                    }
                    else if (loadMenu != null && loadMenu.IsVisible())
                    {
                        loadMenu.Hide();
                        Show();
                    }
                    else
                    {
                        // Resume game
                        Resume();
                    }
                }
                else
                {
                    // Pause game
                    Debug.Log("PauseMenu Update: Calling Pause()");
                    Pause();
                }
            }
        }

        private void SetupButtons()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);
            
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        /// <summary>
        /// Hiển thị pause menu
        /// </summary>
        public void Show()
        {
            isPaused = true;
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                Debug.Log("PauseMenu: Panel activated");
            }
            else
            {
                Debug.LogWarning("PauseMenu: pauseMenuPanel is null! Please assign it in Inspector.");
            }
            
            if (supervisor != null)
            {
                supervisor.SetPaused(true);
            }
            else
            {
                Debug.LogWarning("PauseMenu: supervisor is null!");
            }
        }

        /// <summary>
        /// Ẩn pause menu và resume game
        /// </summary>
        public void Resume()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            if (saveMenu != null)
                saveMenu.Hide();
            
            if (loadMenu != null)
                loadMenu.Hide();
            
            isPaused = false;
            
            if (supervisor != null)
                supervisor.SetPaused(false);
        }

        /// <summary>
        /// Pause game và hiển thị menu
        /// </summary>
        public void Pause()
        {
            Show();
        }
        
        /// <summary>
        /// Kiểm tra menu có đang hiển thị không
        /// </summary>
        public bool IsVisible()
        {
            return pauseMenuPanel != null && pauseMenuPanel.activeSelf;
        }

        private void OnSaveClicked()
        {
            if (saveMenu != null)
            {
                Hide();
                saveMenu.Show();
            }
        }

        private void OnLoadClicked()
        {
            if (loadMenu != null)
            {
                Hide();
                loadMenu.Show();
            }
        }

        private void OnExitClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void Hide()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Kiểm tra key press (hỗ trợ cả Input System và Legacy Input)
        /// </summary>
        private bool IsKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.InputSystem.Keyboard.current == null)
                return false;

            if (!System.Enum.TryParse(keyCode.ToString(), true, out UnityEngine.InputSystem.Key key))
                return false;

            var control = UnityEngine.InputSystem.Keyboard.current[key];
            return control != null && control.wasPressedThisFrame;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }
    }
}

