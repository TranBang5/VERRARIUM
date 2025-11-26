using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Verrarium.UI
{
    /// <summary>
    /// Helper class để tạo UI elements cho CreatureInspector
    /// Có thể gọi từ Editor hoặc runtime
    /// </summary>
    public static class CreatureInspectorHelper
    {
        private static TMP_FontAsset defaultFont;

        public static TMP_FontAsset GetDefaultFont()
        {
            if (defaultFont != null) return defaultFont;

            // 1. Try Resources.Load standard paths
            defaultFont = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
                defaultFont = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts/LiberationSans SDF");

            // 2. Try FindObjectsOfTypeAll (slow but reliable in editor)
            if (defaultFont == null)
            {
                var fonts = UnityEngine.Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts != null && fonts.Length > 0)
                {
                    // Prefer one that is not null and has "Liberation" in name if possible
                    foreach (var f in fonts)
                    {
                        if (f != null && f.name.Contains("Liberation"))
                        {
                            defaultFont = f;
                            break;
                        }
                    }
                    if (defaultFont == null) defaultFont = fonts[0];
                }
            }
            
            // 3. Last resort: TMP_Settings default font
            if (defaultFont == null && TMP_Settings.defaultFontAsset != null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }

            if (defaultFont == null)
            {
                Debug.LogWarning("[CreatureInspectorHelper] Could not find any TMP_FontAsset. Text might not render.");
            }
            else
            {
                // Debug.Log($"[CreatureInspectorHelper] Using font: {defaultFont.name}");
            }

            return defaultFont;
        }

        private static void SetupText(TextMeshProUGUI textComponent, string content, int fontSize, Color color)
        {
            var font = GetDefaultFont();
            if (font != null) textComponent.font = font;
            
            textComponent.text = content;
            textComponent.fontSize = fontSize;
            textComponent.color = color; 
            textComponent.raycastTarget = false;
            textComponent.overflowMode = TextOverflowModes.Overflow; 
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            
            // Force reset scale & z-pos just in case
            textComponent.rectTransform.localScale = Vector3.one;
            Vector3 pos = textComponent.rectTransform.localPosition;
            textComponent.rectTransform.localPosition = new Vector3(pos.x, pos.y, 0f);
        }


        /// <summary>
        /// Helper set layer cho object và children
        /// </summary>
        private static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }

        private static int GetUILayer()
        {
            return LayerMask.NameToLayer("UI");
        }

        /// <summary>
        /// Tạo ScrollView đơn giản
        /// </summary>
        public static Transform CreateScrollView(Transform parent, string name)
        {
            GameObject scrollObj = new GameObject(name, typeof(RectTransform));
            scrollObj.layer = GetUILayer();
            scrollObj.transform.SetParent(parent, false);
            
            // ScrollRect
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 10f;
            
            RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.sizeDelta = Vector2.zero;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.layer = GetUILayer();
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            
            // Mask - Removing RectMask2D temporarily to debug visibility
            // viewport.AddComponent<RectMask2D>();
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0); // Transparent but raycastable if needed, or just invisible container
            viewportImg.maskable = true;
            viewport.AddComponent<RectMask2D>(); // Re-adding mask but with Image component might help? No, RectMask2D works on RectTransform.
            // Let's TRY WITHOUT MASK first.
            // UnityEngine.Object.DestroyImmediate(viewport.GetComponent<RectMask2D>());
            // UnityEngine.Object.DestroyImmediate(viewport.GetComponent<Image>());
            // Re-enable mask
            if (viewport.GetComponent<RectMask2D>() == null)
                 viewport.AddComponent<RectMask2D>();

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.layer = GetUILayer();
            if (name.Contains("Genome")) content.name = "GenomeContent";
            else if (name.Contains("Brain")) content.name = "BrainContent";
            
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0); 
            
            // Nếu là BrainContent và ta muốn vẽ graph, ta có thể không cần VerticalLayoutGroup
            // Nhưng logic cũ dùng VerticalLayoutGroup.
            // Ta sẽ thêm VerticalLayoutGroup mặc định, nếu cần vẽ graph ta sẽ Destroy nó sau.
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 5f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            
            // Fix: Mặc định disable LayoutGroup để tránh conflict với ContentSizeFitter nếu không có element layout
            // Khi thêm element (như GenomeRow), chúng ta sẽ enable lại nếu cần, hoặc để nó tự update.
            // Tuy nhiên, ContentSizeFitter thường yêu cầu LayoutGroup hoạt động để tính size.
            // Vấn đề log cho thấy SizeDelta Height = 0.
            // Điều này xảy ra khi ContentSizeFitter (PreferredSize) thấy nội dung rỗng hoặc nội dung không báo cáo height.

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Link references
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return content.transform;
        }

        /// <summary>
        /// Tạo một row hiển thị gen (Label: Value)
        /// </summary>
        public static GameObject CreateGenomeRow(Transform parent, string label, string value)
        {
            GameObject row = new GameObject("GenomeRow");
            row.layer = GetUILayer();
            row.transform.SetParent(parent, false);
            
            // Debug log
            // Debug.Log($"[CreatureInspectorHelper] Creating GenomeRow: {label} = {value}");

            // Layout
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;  // Changed to true
            layout.childForceExpandHeight = true; // Changed to true

            // ContentSizeFitter is redundant/conflicting if we are inside a VerticalLayoutGroup that controls height.
            // Better to use LayoutElement to tell parent our preferred size.
            // ContentSizeFitter fitter = row.AddComponent<ContentSizeFitter>();
            // fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            LayoutElement rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = 25f; // Explicitly set min height for the row
            rowLE.preferredHeight = 25f;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.layer = GetUILayer();
            labelObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            SetupText(labelText, label + ":", 14, Color.white);
            labelText.alignment = TextAlignmentOptions.Left;

            LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 200f;

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.layer = GetUILayer();
            valueObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            SetupText(valueText, value, 14, new Color(1f, 0.8f, 0.2f)); // Vàng sáng
            valueText.alignment = TextAlignmentOptions.Left;

            LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            // Đảm bảo min height để layout hoạt động
            valueLayout.minHeight = 20f;
            
            // Thêm min height cho label layout
            labelLayout.minHeight = 20f;
            
            // Đảm bảo transform chuẩn
            row.transform.localScale = Vector3.one;
            row.transform.localPosition = Vector3.zero;

            return row;
        }


        /// <summary>
        /// Tạo một display cho nơ-ron (List View)
        /// </summary>
        public static GameObject CreateNeuronDisplay(Transform parent, string info)
        {
            GameObject display = new GameObject("NeuronDisplay");
            display.layer = GetUILayer();
            display.transform.SetParent(parent, false);

            // Background
            Image bg = display.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            // Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.layer = GetUILayer();
            textObj.transform.SetParent(display.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            SetupText(text, info, 12, Color.white);
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(5, 2, 5, 2);

            // Layout
            LayoutElement layout = display.AddComponent<LayoutElement>();
            layout.preferredHeight = 25f;

            return display;
        }

        /// <summary>
        /// Tạo một display cho kết nối (List View)
        /// </summary>
        public static GameObject CreateConnectionDisplay(Transform parent, string info)
        {
            GameObject display = new GameObject("ConnectionDisplay");
            display.layer = GetUILayer();
            display.transform.SetParent(parent, false);

            // Background
            Image bg = display.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            // Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.layer = GetUILayer();
            textObj.transform.SetParent(display.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            SetupText(text, info, 11, Color.white);
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(5, 2, 5, 2);

            // Layout
            LayoutElement layout = display.AddComponent<LayoutElement>();
            layout.preferredHeight = 20f;

            return display;
        }
        
        // --- Graph Visualization Helpers ---

        public static GameObject CreateNodeUI(Transform parent, Vector2 position, float size, Color color, string textContent)
        {
            GameObject node = new GameObject("Node_" + textContent);
            node.layer = GetUILayer();
            node.transform.SetParent(parent, false);
            
            RectTransform rect = node.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;
            
            // Force reset local scale
            node.transform.localScale = Vector3.one;
            
            Image img = node.AddComponent<Image>();
            img.color = color;
            // Make it circle-ish if we have a sprite, otherwise square. Default sprite usually rect.
            // Can try to load knob/circle if available, or just square.
            
            // Text overlay
            GameObject textObj = new GameObject("Text");
            textObj.layer = GetUILayer();
            textObj.transform.SetParent(node.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            SetupText(text, textContent, 10, Color.black);
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            
            return node;
        }

        public static void CreateLineConnection(Transform parent, Vector2 start, Vector2 end, float width, Color color)
        {
            GameObject line = new GameObject("Line");
            line.layer = GetUILayer();
            line.transform.SetParent(parent, false);
            line.transform.SetAsFirstSibling(); // Draw behind nodes
            
            RectTransform rect = line.AddComponent<RectTransform>();
            Vector2 dir = (end - start).normalized;
            float distance = Vector2.Distance(start, end);
            
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(distance, width);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = start;
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            // Force reset scale (rotation is set above, so don't touch rot, but scale can be wacky)
            rect.localScale = Vector3.one; 
            
            Image img = line.AddComponent<Image>();
            img.color = color;
        }
    }
}
