#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Verrarium.UI;

namespace Verrarium.Editor
{
    /// <summary>
    /// Menu command để tự động tạo Pause Menu và Save/Load Menu UI trong hierarchy
    /// </summary>
    public static class PauseMenuCreator
    {
        private const string MenuPath = "Verrarium/Create Pause & Save/Load Menu";

        [MenuItem(MenuPath, priority = 200)]
        public static void CreatePauseMenu()
        {
            // Tìm hoặc tạo Canvas
            Canvas canvas = FindOrCreateCanvas();

            // Tạo Pause Menu
            GameObject pauseMenuObj = CreatePauseMenu(canvas.transform);
            
            // Tạo Save Menu
            GameObject saveMenuObj = CreateSaveMenu(canvas.transform);
            
            // Tạo Load Menu
            GameObject loadMenuObj = CreateLoadMenu(canvas.transform);

            // Setup references
            PauseMenu pauseMenu = pauseMenuObj.GetComponent<PauseMenu>();
            SaveMenu saveMenu = saveMenuObj.GetComponent<SaveMenu>();
            LoadMenu loadMenu = loadMenuObj.GetComponent<LoadMenu>();

            // Link references using SerializedObject
            SerializedObject pauseMenuSO = new SerializedObject(pauseMenu);
            pauseMenuSO.FindProperty("saveMenu").objectReferenceValue = saveMenu;
            pauseMenuSO.FindProperty("loadMenu").objectReferenceValue = loadMenu;
            pauseMenuSO.ApplyModifiedProperties();

            Selection.activeGameObject = pauseMenuObj;
            EditorUtility.DisplayDialog(
                "Pause & Save/Load Menu",
                "Đã tạo thành công Pause Menu, Save Menu và Load Menu!\n\n" +
                "Các components đã được link tự động. Bạn có thể chỉnh sửa UI trong hierarchy.",
                "OK");
        }

        private static Canvas FindOrCreateCanvas()
        {
            // Tìm Canvas ở root level (không phải trong child objects)
            Canvas canvas = null;
            
#if UNITY_2023_1_OR_NEWER
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
            Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>();
#endif
            
            // Tìm Canvas ở root level (parent là null hoặc không có parent là UI element)
            foreach (Canvas c in allCanvases)
            {
                if (c.transform.parent == null || c.transform.parent.GetComponent<Canvas>() == null)
                {
                    canvas = c;
                    break;
                }
            }
            
            // Nếu không tìm thấy Canvas ở root, tạo mới
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Tạo EventSystem nếu chưa có
#if UNITY_2023_1_OR_NEWER
                if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
#else
                if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
#endif
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            return canvas;
        }

        private static GameObject CreatePauseMenu(Transform parent)
        {
            // Main Panel
            GameObject panel = CreateUIElement("PauseMenuPanel", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Background với gradient effect (dark với blur)
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            // Add PauseMenu component
            PauseMenu pauseMenu = panel.AddComponent<PauseMenu>();

            // Main content panel với border
            GameObject contentPanel = CreateUIElement("ContentPanel", panel.transform);
            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(450, 500);
            contentRect.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            
            // Thêm shadow effect bằng cách tạo một panel phía sau
            GameObject shadowPanel = CreateUIElement("ShadowPanel", contentPanel.transform);
            shadowPanel.transform.SetAsFirstSibling();
            RectTransform shadowRect = shadowPanel.GetComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.sizeDelta = new Vector2(10, 10);
            shadowRect.anchoredPosition = new Vector2(5, -5);
            Image shadowImage = shadowPanel.AddComponent<Image>();
            shadowImage.color = new Color(0, 0, 0, 0.5f);

            // Title với shadow
            GameObject title = CreateText("Title", contentPanel.transform, "PAUSE MENU", 42);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.88f);
            titleRect.anchorMax = new Vector2(0.5f, 0.88f);
            titleRect.sizeDelta = new Vector2(400, 70);
            titleRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.95f, 0.8f, 1f); // Warm white
            AddTextShadow(title);

            // Buttons Container
            GameObject buttonsContainer = CreateUIElement("ButtonsContainer", contentPanel.transform);
            RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.45f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.45f);
            buttonsRect.sizeDelta = new Vector2(380, 320);
            buttonsRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layoutGroup = buttonsContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 18;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter sizeFitter = buttonsContainer.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Save Button - Blue gradient
            Button saveButton = CreateStyledButton("SaveButton", buttonsContainer.transform, "SAVE", 
                new Color(0.2f, 0.5f, 0.9f, 1f), new Color(0.1f, 0.4f, 0.8f, 1f));
            saveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);

            // Load Button - Green gradient
            Button loadButton = CreateStyledButton("LoadButton", buttonsContainer.transform, "LOAD",
                new Color(0.3f, 0.7f, 0.4f, 1f), new Color(0.2f, 0.6f, 0.3f, 1f));
            loadButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);

            // Resume Button - Orange gradient
            Button resumeButton = CreateStyledButton("ResumeButton", buttonsContainer.transform, "RESUME",
                new Color(0.9f, 0.6f, 0.2f, 1f), new Color(0.8f, 0.5f, 0.1f, 1f));
            resumeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);

            // Exit Button - Red gradient
            Button exitButton = CreateStyledButton("ExitButton", buttonsContainer.transform, "EXIT",
                new Color(0.8f, 0.3f, 0.3f, 1f), new Color(0.7f, 0.2f, 0.2f, 1f));
            exitButton.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 60);

            // Link references
            SerializedObject pauseMenuSO = new SerializedObject(pauseMenu);
            pauseMenuSO.FindProperty("pauseMenuPanel").objectReferenceValue = panel;
            pauseMenuSO.FindProperty("saveButton").objectReferenceValue = saveButton;
            pauseMenuSO.FindProperty("loadButton").objectReferenceValue = loadButton;
            pauseMenuSO.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            pauseMenuSO.FindProperty("exitButton").objectReferenceValue = exitButton;
            pauseMenuSO.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateSaveMenu(Transform parent)
        {
            // Main Panel
            GameObject panel = CreateUIElement("SaveMenuPanel", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Background với gradient
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // Add SaveMenu component
            SaveMenu saveMenu = panel.AddComponent<SaveMenu>();

            // Main content panel
            GameObject contentPanel = CreateUIElement("ContentPanel", panel.transform);
            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.05f);
            contentRect.anchorMax = new Vector2(0.95f, 0.95f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            // Title với shadow
            GameObject title = CreateText("Title", contentPanel.transform, "SAVE GAME", 38);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.94f);
            titleRect.anchorMax = new Vector2(0.5f, 0.94f);
            titleRect.sizeDelta = new Vector2(500, 60);
            titleRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.9f, 0.9f, 1f, 1f);
            AddTextShadow(title);

            // Save Name Input
            GameObject inputContainer = CreateUIElement("InputContainer", panel.transform);
            RectTransform inputRect = inputContainer.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.85f);
            inputRect.anchorMax = new Vector2(0.5f, 0.85f);
            inputRect.sizeDelta = new Vector2(500, 60);
            inputRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup inputLayout = inputContainer.AddComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 10;
            inputLayout.childControlWidth = true;
            inputLayout.childControlHeight = true;
            inputLayout.childForceExpandWidth = false;
            inputLayout.childForceExpandHeight = true;

            GameObject label = CreateText("Label", inputContainer.transform, "Save Name:", 20);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 40);

            GameObject inputFieldObj = CreateUIElement("SaveNameInput", inputContainer.transform);
            TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
            inputFieldObj.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 45);

            Image inputBg = inputFieldObj.AddComponent<Image>();
            inputBg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            
            // Border cho input field
            GameObject inputBorder = CreateUIElement("Border", inputFieldObj.transform);
            inputBorder.transform.SetAsFirstSibling();
            RectTransform borderRect = inputBorder.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(-2, -2);
            borderRect.anchoredPosition = Vector2.zero;
            Image borderImage = inputBorder.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.5f, 0.7f, 1f);

            GameObject textArea = CreateUIElement("TextArea", inputFieldObj.transform);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = Vector2.zero;
            textAreaRect.anchoredPosition = Vector2.zero;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);

            GameObject textObj = CreateText("Text", textArea.transform, "", 18);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textObj.GetComponent<TextMeshProUGUI>().color = Color.black;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = textObj.GetComponent<TextMeshProUGUI>();

            GameObject placeholderObj = CreateText("Placeholder", textArea.transform, "Enter save name...", 18);
            placeholderObj.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            inputField.placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();

            // Selected Slot Text
            GameObject selectedSlotText = CreateText("SelectedSlotText", contentPanel.transform, "Select a save slot", 20);
            RectTransform selectedRect = selectedSlotText.GetComponent<RectTransform>();
            selectedRect.anchorMin = new Vector2(0.5f, 0.80f);
            selectedRect.anchorMax = new Vector2(0.5f, 0.80f);
            selectedRect.sizeDelta = new Vector2(500, 35);
            selectedRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI selectedText = selectedSlotText.GetComponent<TextMeshProUGUI>();
            selectedText.alignment = TextAlignmentOptions.Center;
            selectedText.color = new Color(0.8f, 0.85f, 1f, 1f);
            AddTextShadow(selectedSlotText);

            // Save Slots Container (ScrollView)
            GameObject scrollView = CreateUIElement("SaveSlotsScrollView", contentPanel.transform);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.08f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.92f, 0.72f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;

            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            // Border cho scroll view
            GameObject scrollBorder = CreateUIElement("Border", scrollView.transform);
            scrollBorder.transform.SetAsFirstSibling();
            RectTransform scrollBorderRect = scrollBorder.GetComponent<RectTransform>();
            scrollBorderRect.anchorMin = Vector2.zero;
            scrollBorderRect.anchorMax = Vector2.one;
            scrollBorderRect.sizeDelta = new Vector2(-2, -2);
            scrollBorderRect.anchoredPosition = Vector2.zero;
            Image scrollBorderImage = scrollBorder.AddComponent<Image>();
            scrollBorderImage.color = new Color(0.3f, 0.3f, 0.4f, 0.6f);
            
            ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();

            GameObject viewport = CreateUIElement("Viewport", scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            viewport.AddComponent<Image>();
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIElement("Content", viewport.transform);
            RectTransform contentRectSlots = content.GetComponent<RectTransform>();
            contentRectSlots.anchorMin = new Vector2(0.5f, 1f);
            contentRectSlots.anchorMax = new Vector2(0.5f, 1f);
            contentRectSlots.pivot = new Vector2(0.5f, 1f);
            contentRectSlots.sizeDelta = new Vector2(0, 0);

            GridLayoutGroup gridLayout = content.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 100);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRectComponent.content = contentRectSlots;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;

            // Buttons
            GameObject buttonsContainer = CreateUIElement("ButtonsContainer", panel.transform);
            RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.5f, 0.02f);
            buttonsRect.anchorMax = new Vector2(0.5f, 0.02f);
            buttonsRect.sizeDelta = new Vector2(400, 50);
            buttonsRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childControlHeight = false;
            buttonsLayout.childForceExpandWidth = false;
            buttonsLayout.childForceExpandHeight = false;

            Button confirmButton = CreateStyledButton("ConfirmButton", buttonsContainer.transform, "CONFIRM",
                new Color(0.2f, 0.7f, 0.3f, 1f), new Color(0.15f, 0.6f, 0.25f, 1f));
            confirmButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 45);

            Button cancelButton = CreateStyledButton("CancelButton", buttonsContainer.transform, "CANCEL",
                new Color(0.6f, 0.2f, 0.2f, 1f), new Color(0.5f, 0.15f, 0.15f, 1f));
            cancelButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 45);

            // Create Save Slot Prefab (template)
            GameObject slotPrefab = CreateSaveSlotPrefab();
            slotPrefab = SaveAsPrefab(slotPrefab, "SaveSlotPrefab");

            // Link references
            SerializedObject saveMenuSO = new SerializedObject(saveMenu);
            saveMenuSO.FindProperty("saveMenuPanel").objectReferenceValue = panel;
            saveMenuSO.FindProperty("saveSlotsContainer").objectReferenceValue = content.transform;
            saveMenuSO.FindProperty("saveSlotPrefab").objectReferenceValue = slotPrefab;
            saveMenuSO.FindProperty("saveNameInput").objectReferenceValue = inputField;
            saveMenuSO.FindProperty("confirmSaveButton").objectReferenceValue = confirmButton;
            saveMenuSO.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            saveMenuSO.FindProperty("selectedSlotText").objectReferenceValue = selectedSlotText.GetComponent<TextMeshProUGUI>();
            saveMenuSO.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateLoadMenu(Transform parent)
        {
            // Main Panel
            GameObject panel = CreateUIElement("LoadMenuPanel", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Background với gradient
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // Add LoadMenu component
            LoadMenu loadMenu = panel.AddComponent<LoadMenu>();

            // Main content panel
            GameObject contentPanel = CreateUIElement("ContentPanel", panel.transform);
            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.05f);
            contentRect.anchorMax = new Vector2(0.95f, 0.95f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            // Title với shadow
            GameObject title = CreateText("Title", contentPanel.transform, "LOAD GAME", 38);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.94f);
            titleRect.anchorMax = new Vector2(0.5f, 0.94f);
            titleRect.sizeDelta = new Vector2(500, 60);
            titleRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.9f, 0.9f, 1f, 1f);
            AddTextShadow(title);

            // No Saves Text
            GameObject noSavesText = CreateText("NoSavesText", panel.transform, "No save files found", 24);
            RectTransform noSavesRect = noSavesText.GetComponent<RectTransform>();
            noSavesRect.anchorMin = new Vector2(0.5f, 0.5f);
            noSavesRect.anchorMax = new Vector2(0.5f, 0.5f);
            noSavesRect.sizeDelta = new Vector2(400, 50);
            noSavesRect.anchoredPosition = Vector2.zero;
            noSavesText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            noSavesText.SetActive(false);

            // Save Files Container (ScrollView)
            GameObject scrollView = CreateUIElement("SaveFilesScrollView", panel.transform);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.9f, 0.85f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;

            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            // Border cho scroll view
            GameObject scrollBorder = CreateUIElement("Border", scrollView.transform);
            scrollBorder.transform.SetAsFirstSibling();
            RectTransform scrollBorderRect = scrollBorder.GetComponent<RectTransform>();
            scrollBorderRect.anchorMin = Vector2.zero;
            scrollBorderRect.anchorMax = Vector2.one;
            scrollBorderRect.sizeDelta = new Vector2(-2, -2);
            scrollBorderRect.anchoredPosition = Vector2.zero;
            Image scrollBorderImage = scrollBorder.AddComponent<Image>();
            scrollBorderImage.color = new Color(0.3f, 0.3f, 0.4f, 0.6f);
            
            ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();

            GameObject viewport = CreateUIElement("Viewport", scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            viewport.AddComponent<Image>();
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIElement("Content", viewport.transform);
            RectTransform contentRectFiles = content.GetComponent<RectTransform>();
            contentRectFiles.anchorMin = new Vector2(0.5f, 1f);
            contentRectFiles.anchorMax = new Vector2(0.5f, 1f);
            contentRectFiles.pivot = new Vector2(0.5f, 1f);
            contentRectFiles.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup verticalLayout = content.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 10;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRectComponent.content = contentRectFiles;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;

            // Cancel Button
            Button cancelButton = CreateStyledButton("CancelButton", contentPanel.transform, "CANCEL",
                new Color(0.6f, 0.2f, 0.2f, 1f), new Color(0.5f, 0.15f, 0.15f, 1f));
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.03f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.03f);
            cancelRect.sizeDelta = new Vector2(180, 45);
            cancelRect.anchoredPosition = Vector2.zero;

            // Create Save File Item Prefab (template)
            GameObject fileItemPrefab = CreateSaveFileItemPrefab();
            fileItemPrefab = SaveAsPrefab(fileItemPrefab, "SaveFileItemPrefab");

            // Link references
            SerializedObject loadMenuSO = new SerializedObject(loadMenu);
            loadMenuSO.FindProperty("loadMenuPanel").objectReferenceValue = panel;
            loadMenuSO.FindProperty("saveFilesContainer").objectReferenceValue = content.transform;
            loadMenuSO.FindProperty("saveFileItemPrefab").objectReferenceValue = fileItemPrefab;
            loadMenuSO.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            loadMenuSO.FindProperty("noSavesText").objectReferenceValue = noSavesText.GetComponent<TextMeshProUGUI>();
            loadMenuSO.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateSaveSlotPrefab()
        {
            GameObject slot = CreateUIElement("SaveSlotPrefab");
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(200, 100);

            Image slotBg = slot.AddComponent<Image>();
            slotBg.color = new Color(0.2f, 0.25f, 0.3f, 0.9f);
            
            // Border cho slot
            GameObject slotBorder = CreateUIElement("Border", slot.transform);
            slotBorder.transform.SetAsFirstSibling();
            RectTransform slotBorderRect = slotBorder.GetComponent<RectTransform>();
            slotBorderRect.anchorMin = Vector2.zero;
            slotBorderRect.anchorMax = Vector2.one;
            slotBorderRect.sizeDelta = new Vector2(-2, -2);
            slotBorderRect.anchoredPosition = Vector2.zero;
            Image borderImage = slotBorder.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.5f, 0.6f, 0.8f);

            Button button = slot.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            button.colors = colors;

            SaveSlotButton slotButton = slot.AddComponent<SaveSlotButton>();

            // Slot Name
            GameObject nameText = CreateText("SlotNameText", slot.transform, "Save Slot 1", 18);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.6f);
            nameRect.anchorMax = new Vector2(0.95f, 0.95f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.anchoredPosition = Vector2.zero;
            nameText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            // Save Time
            GameObject timeText = CreateText("SaveTimeText", slot.transform, "Empty", 14);
            RectTransform timeRect = timeText.GetComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0.05f, 0.3f);
            timeRect.anchorMax = new Vector2(0.95f, 0.6f);
            timeRect.sizeDelta = Vector2.zero;
            timeRect.anchoredPosition = Vector2.zero;
            timeText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            timeText.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            // Population
            GameObject popText = CreateText("PopulationText", slot.transform, "", 14);
            RectTransform popRect = popText.GetComponent<RectTransform>();
            popRect.anchorMin = new Vector2(0.05f, 0.05f);
            popRect.anchorMax = new Vector2(0.95f, 0.3f);
            popRect.sizeDelta = Vector2.zero;
            popRect.anchoredPosition = Vector2.zero;
            popText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            popText.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.8f, 1f);

            // Background Image for selection highlight
            GameObject bgImageObj = CreateUIElement("BackgroundImage", slot.transform);
            RectTransform bgRect = bgImageObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            Image bgImage = bgImageObj.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImageObj.SetActive(false);

            // Link references
            SerializedObject slotButtonSO = new SerializedObject(slotButton);
            slotButtonSO.FindProperty("button").objectReferenceValue = button;
            slotButtonSO.FindProperty("slotNameText").objectReferenceValue = nameText.GetComponent<TextMeshProUGUI>();
            slotButtonSO.FindProperty("saveTimeText").objectReferenceValue = timeText.GetComponent<TextMeshProUGUI>();
            slotButtonSO.FindProperty("populationText").objectReferenceValue = popText.GetComponent<TextMeshProUGUI>();
            slotButtonSO.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            slotButtonSO.ApplyModifiedProperties();

            return slot;
        }

        private static GameObject CreateSaveFileItemPrefab()
        {
            GameObject item = CreateUIElement("SaveFileItemPrefab");
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 120);

            Image itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.18f, 0.2f, 0.25f, 0.95f);
            
            // Border cho item
            GameObject itemBorder = CreateUIElement("Border", item.transform);
            itemBorder.transform.SetAsFirstSibling();
            RectTransform itemBorderRect = itemBorder.GetComponent<RectTransform>();
            itemBorderRect.anchorMin = Vector2.zero;
            itemBorderRect.anchorMax = Vector2.one;
            itemBorderRect.sizeDelta = new Vector2(-2, -2);
            itemBorderRect.anchoredPosition = Vector2.zero;
            Image borderImage = itemBorder.AddComponent<Image>();
            borderImage.color = new Color(0.35f, 0.4f, 0.5f, 0.7f);

            Button button = item.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.95f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            button.colors = colors;

            SaveFileItem fileItem = item.AddComponent<SaveFileItem>();

            HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Left side - File info
            GameObject leftContainer = CreateUIElement("LeftContainer", item.transform);
            RectTransform leftRect = leftContainer.GetComponent<RectTransform>();
            leftRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup leftLayout = leftContainer.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 5;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = false;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;

            ContentSizeFitter leftFitter = leftContainer.AddComponent<ContentSizeFitter>();
            leftFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            leftFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // File Name
            GameObject fileNameText = CreateText("FileNameText", leftContainer.transform, "Save Name", 20);
            fileNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 25);
            fileNameText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

            // Save Time
            GameObject saveTimeText = CreateText("SaveTimeText", leftContainer.transform, "2024-01-01 12:00:00", 16);
            saveTimeText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            saveTimeText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            saveTimeText.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            // Population
            GameObject populationText = CreateText("PopulationText", leftContainer.transform, "Population: 100", 16);
            populationText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            populationText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            populationText.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.8f, 1f);

            // Simulation Time
            GameObject simTimeText = CreateText("SimulationTimeText", leftContainer.transform, "Time: 10:30", 16);
            simTimeText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            simTimeText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            simTimeText.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            // Right side - Delete Button
            Button deleteButton = CreateButton("DeleteButton", item.transform, "DELETE");
            GameObject deleteButtonObj = deleteButton.gameObject;
            RectTransform deleteRect = deleteButtonObj.GetComponent<RectTransform>();
            deleteRect.sizeDelta = new Vector2(80, 40);

            Image deleteBg = deleteButtonObj.GetComponent<Image>();
            deleteBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            ColorBlock deleteColors = deleteButton.colors;
            deleteColors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            deleteColors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            deleteColors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            deleteButton.colors = deleteColors;

            // Link references
            SerializedObject fileItemSO = new SerializedObject(fileItem);
            fileItemSO.FindProperty("button").objectReferenceValue = button;
            fileItemSO.FindProperty("fileNameText").objectReferenceValue = fileNameText.GetComponent<TextMeshProUGUI>();
            fileItemSO.FindProperty("saveTimeText").objectReferenceValue = saveTimeText.GetComponent<TextMeshProUGUI>();
            fileItemSO.FindProperty("populationText").objectReferenceValue = populationText.GetComponent<TextMeshProUGUI>();
            fileItemSO.FindProperty("simulationTimeText").objectReferenceValue = simTimeText.GetComponent<TextMeshProUGUI>();
            fileItemSO.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            fileItemSO.ApplyModifiedProperties();

            return item;
        }

        private static GameObject CreateUIElement(string name, Transform parent = null)
        {
            GameObject obj = new GameObject(name);
            if (parent != null)
                obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI textComponent = obj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            return obj;
        }

        private static Button CreateButton(string name, Transform parent, string text)
        {
            return CreateStyledButton(name, parent, text, 
                new Color(0.2f, 0.4f, 0.8f, 1f), 
                new Color(0.1f, 0.3f, 0.7f, 1f));
        }

        private static Button CreateStyledButton(string name, Transform parent, string text, 
            Color normalColor, Color pressedColor)
        {
            GameObject obj = CreateUIElement(name, parent);
            
            // Main button image với gradient effect
            Image image = obj.AddComponent<Image>();
            image.color = normalColor;

            // Thêm border effect bằng cách tạo một panel nhỏ hơn bên trong
            GameObject borderObj = CreateUIElement("Border", obj.transform);
            borderObj.transform.SetAsFirstSibling();
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(-4, -4);
            borderRect.anchoredPosition = Vector2.zero;
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(normalColor.r * 1.3f, normalColor.g * 1.3f, normalColor.b * 1.3f, 1f);

            // Shadow
            GameObject shadowObj = CreateUIElement("Shadow", obj.transform);
            shadowObj.transform.SetAsFirstSibling();
            RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.sizeDelta = new Vector2(4, -4);
            shadowRect.anchoredPosition = new Vector2(2, -2);
            Image shadowImage = shadowObj.AddComponent<Image>();
            shadowImage.color = new Color(0, 0, 0, 0.3f);

            Button button = obj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = new Color(
                Mathf.Min(1f, normalColor.r * 1.2f),
                Mathf.Min(1f, normalColor.g * 1.2f),
                Mathf.Min(1f, normalColor.b * 1.2f),
                1f);
            colors.pressedColor = pressedColor;
            colors.selectedColor = normalColor;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Button text với shadow
            GameObject textObj = CreateText("Text", obj.transform, text, 20);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI textComponent = textObj.GetComponent<TextMeshProUGUI>();
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.color = Color.white;
            AddTextShadow(textObj);

            return button;
        }

        private static void AddTextShadow(GameObject textObj)
        {
            // Tạo shadow bằng cách duplicate text và offset
            TextMeshProUGUI mainText = textObj.GetComponent<TextMeshProUGUI>();
            if (mainText == null) return;

            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(textObj.transform);
            shadowObj.transform.SetAsFirstSibling();
            RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.sizeDelta = Vector2.zero;
            shadowRect.anchoredPosition = new Vector2(2, -2);

            TextMeshProUGUI shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
            shadowText.text = mainText.text;
            shadowText.font = mainText.font;
            shadowText.fontSize = mainText.fontSize;
            shadowText.fontStyle = mainText.fontStyle;
            shadowText.alignment = mainText.alignment;
            shadowText.color = new Color(0, 0, 0, 0.5f);
        }

        private static GameObject SaveAsPrefab(GameObject obj, string prefabName)
        {
            // Tạo thư mục Prefabs/UI nếu chưa có
            string prefabPath = "Assets/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                string prefabsPath = "Assets/Prefabs";
                if (!AssetDatabase.IsValidFolder(prefabsPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder(prefabsPath, "UI");
            }

            // Lưu prefab
            string fullPath = $"{prefabPath}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, fullPath);
            
            // Xóa object trong scene (vì đã lưu thành prefab)
            Object.DestroyImmediate(obj);

            return prefab;
        }
    }
}
#endif

