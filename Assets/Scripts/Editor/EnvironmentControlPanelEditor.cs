#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using Verrarium.UI.Layout;

namespace Verrarium.Editor
{
    /// <summary>
    /// Editor helper để tự động tạo Environment Control Panel
    /// </summary>
    public class EnvironmentControlPanelEditor : EditorWindow
    {
        [MenuItem("Verrarium/Create Environment Control Panel")]
        public static void CreateEnvironmentControlPanel()
        {
            // Tìm hoặc tạo Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Tạo Top Bar Panel
            GameObject topBar = new GameObject("EnvironmentControlPanel");
            topBar.transform.SetParent(canvas.transform, false);
            
            RectTransform topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 200);
            topBarRect.anchoredPosition = new Vector2(0, 0);
            
            Image topBarBg = topBar.AddComponent<Image>();
            topBarBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Toggle Button
            GameObject toggleBtn = CreateToggleButton(topBar);
            
            // Control Panel (nội dung)
            GameObject controlPanel = CreateControlPanel(topBar);

            // Thêm script
            UI.EnvironmentControlPanel panelScript = topBar.AddComponent<UI.EnvironmentControlPanel>();
            
            // Gán references (sẽ cần gán thủ công một số)
            Selection.activeGameObject = topBar;
            Debug.Log("Environment Control Panel created! Please assign references manually.");
        }

        private static GameObject CreateToggleButton(GameObject parent)
        {
            GameObject toggleBtn = new GameObject("ToggleButton");
            toggleBtn.transform.SetParent(parent.transform, false);
            
            RectTransform toggleRect = toggleBtn.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.5f, 1);
            toggleRect.anchorMax = new Vector2(0.5f, 1);
            toggleRect.pivot = new Vector2(0.5f, 1);
            toggleRect.sizeDelta = new Vector2(40, 30);
            toggleRect.anchoredPosition = new Vector2(0, -5);

            Button button = toggleBtn.AddComponent<Button>();
            Image bg = toggleBtn.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.4f, 1f);

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(toggleBtn.transform, false);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = Vector2.zero;
            arrowRect.anchorMax = Vector2.one;
            arrowRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI arrowText = arrow.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 16;
            arrowText.alignment = TextAlignmentOptions.Center;

            return toggleBtn;
        }

        private static GameObject CreateControlPanel(GameObject parent)
        {
            GameObject panel = new GameObject("ControlPanel");
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(0, -40);
            panelRect.anchoredPosition = new Vector2(0, -5);

            VerticalLayoutGroup rootLayout = panel.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12;
            rootLayout.padding = new RectOffset(18, 18, 20, 18);
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            GameObject gridContainer = new GameObject("SectionsGrid");
            gridContainer.transform.SetParent(panel.transform, false);

            RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.sizeDelta = Vector2.zero;

            ResponsiveGridLayout gridLayout = gridContainer.AddComponent<ResponsiveGridLayout>();
            gridLayout.MinColumns = 1;
            gridLayout.MaxColumns = 2;
            gridLayout.MinCellWidth = 300f;
            gridLayout.CellHeight = 170f;
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.padding = new RectOffset(0, 0, 0, 0);

            LayoutElement gridLayoutElement = gridContainer.AddComponent<LayoutElement>();
            gridLayoutElement.flexibleHeight = 1f;

            // Population Section
            CreateSection(gridContainer, "Population", new string[] { "Target Population", "Max Population" });
            
            // Resource Section
            CreateSection(gridContainer, "Resources", new string[] { "Spawn Interval", "Plants Per Spawn" });
            
            // World Section
            CreateSection(gridContainer, "World", new string[] { "Size X", "Size Y" });

            return panel;
        }

        private static GameObject CreateSection(GameObject parent, string title, string[] controls)
        {
            // Section Container
            GameObject section = new GameObject(title + "Section");
            section.transform.SetParent(parent.transform, false);
            
            Image background = section.AddComponent<Image>();
            background.color = new Color(0.13f, 0.16f, 0.2f, 0.95f);

            LayoutElement cardLayout = section.AddComponent<LayoutElement>();
            cardLayout.minHeight = 150;

            VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 8;
            sectionLayout.padding = new RectOffset(14, 14, 14, 14);
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(section.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;

            // Divider
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(section.transform, false);
            RectTransform dividerRect = divider.AddComponent<RectTransform>();
            dividerRect.sizeDelta = new Vector2(0, 1);
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.05f);

            // Controls
            foreach (string controlName in controls)
            {
                CreateControl(section, controlName);
            }

            return section;
        }

        private static void CreateControl(GameObject parent, string controlName)
        {
            GameObject control = new GameObject(controlName + "Control");
            control.transform.SetParent(parent.transform, false);

            HorizontalLayoutGroup layout = control.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            LayoutElement controlLayout = control.AddComponent<LayoutElement>();
            controlLayout.minHeight = 38;

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(control.transform, false);
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = controlName + ":";
            labelText.fontSize = 14;
            labelText.color = Color.white;

            LayoutElement labelLayout = label.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 150;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(control.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            
            LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1;

            // Slider Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            slider.targetGraphic = bgImage;

            // Slider Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            slider.fillRect = fillRect;

            // Slider Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(bg.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.handleRect = handleRect;

            // Value Text
            GameObject valueText = new GameObject("ValueText");
            valueText.transform.SetParent(control.transform, false);
            TextMeshProUGUI valueTextComp = valueText.AddComponent<TextMeshProUGUI>();
            valueTextComp.text = "0";
            valueTextComp.fontSize = 14;
            valueTextComp.color = Color.yellow;

            LayoutElement valueLayout = valueText.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 80;
        }
    }
}
#endif

