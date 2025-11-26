using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Verrarium.CameraControl;
using Verrarium.Creature;
using Verrarium.Data;
using Verrarium.Evolution;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Verrarium.UI
{
    /// <summary>
    /// Runtime popup hiển thị brain/genome khi người chơi click vào sinh vật.
    /// Tự động tạo Canvas + layout nếu chưa có, đồng thời lock camera theo sinh vật.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CreaturePopupUI : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CameraDragController cameraController;
        [SerializeField, Min(1f)] private float zoomThreshold = 18f;
        [SerializeField, Min(1f)] private float focusZoom = 10f;

        [Header("Panel Settings")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
        [SerializeField] private Vector2 panelAnchorMin = new Vector2(0.05f, 0.05f);
        [SerializeField] private Vector2 panelAnchorMax = new Vector2(0.95f, 0.95f);

        [Header("UI References")]
        [Tooltip("Nếu tắt, script sẽ không tự tạo UI lúc chạy. Bạn cần gán các reference bên dưới.")]
        [SerializeField] private bool autoBuildUI = true;
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private RectTransform brainPanel;
        [SerializeField] private RectTransform genomePanel;
        [SerializeField] private RectTransform brainGraphContainer;
        [SerializeField] private Transform brainListRoot;
        [SerializeField] private Transform genomeListRoot;

        [Header("Input")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        private CreatureController currentCreature;
        private bool isVisible;
        private bool selectionChangedThisFrame;

        private readonly Dictionary<int, BrainNodeVisual> nodeVisuals = new();
        private readonly Dictionary<(int from, int to), Image> connectionVisuals = new();

        private static readonly string[] InputNeuronNames =
        {
            "Energy Ratio",
            "Maturity",
            "Health Ratio",
            "Age",
            "Dist Plant",
            "Angle Plant",
            "Dist Meat",
            "Angle Meat",
            "Dist Creature",
            "Angle Creature"
        };

        private static readonly string[] OutputNeuronNames =
        {
            "Accelerate",
            "Rotate",
            "Lay Egg",
            "Growth",
            "Heal",
            "Attack",
            "Eat"
        };

        private struct BrainNodeVisual
        {
            public Image Image;
            public TextMeshProUGUI Label;
            public Color BaseColor;
        }

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (cameraController == null && targetCamera != null)
                cameraController = targetCamera.GetComponent<CameraDragController>();

            EnsureEventSystem();
            EnsurePhysics2DRaycaster();
            BuildUIIfNeeded(autoBuildUI);
            HidePopup();
        }

        private void OnEnable()
        {
            CreatureClickHandler.OnCreatureClicked += HandleCreatureClicked;
        }

        private void OnDisable()
        {
            CreatureClickHandler.OnCreatureClicked -= HandleCreatureClicked;
        }

        private void Update()
        {
            if (!isVisible)
            {
                selectionChangedThisFrame = false;
                return;
            }

            UpdateBrainActivationVisuals();

            if (IsKeyDown(closeKey))
            {
                ClearSelection();
                selectionChangedThisFrame = false;
                return;
            }

            if (!selectionChangedThisFrame && IsMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    selectionChangedThisFrame = false;
                    return;
                }

                if (!ClickedCreatureUnderPointer())
                {
                    ClearSelection();
                    selectionChangedThisFrame = false;
                    return;
                }
            }

            selectionChangedThisFrame = false;
        }

        private void HandleCreatureClicked(CreatureController creature)
        {
            selectionChangedThisFrame = true;
            if (creature == null)
                return;

            currentCreature = creature;
            ShowPopup();
            
            // Force Update Canvas BEFORE populating to ensure non-zero dimensions if possible
            if (popupCanvas != null) Canvas.ForceUpdateCanvases();
            
            RenderBrainGraph(creature.GetBrain());
            PopulateBrainList(creature.GetBrain());
            RefreshGenomeInfo(creature);
            FocusCamera(creature.transform);
        }

        private void ShowPopup()
        {
            if (popupCanvas != null)
                popupCanvas.enabled = true;
            isVisible = true;
        }

        private void HidePopup()
        {
            if (popupCanvas != null)
                popupCanvas.enabled = false;
            isVisible = false;
        }

        private void ClearSelection()
        {
            currentCreature = null;
            HidePopup();
            if (cameraController != null)
                cameraController.ReleaseLock();
        }

        private void FocusCamera(Transform target)
        {
            if (cameraController == null || target == null)
                return;

            float desiredZoom = -1f;
            if (targetCamera != null && targetCamera.orthographic && targetCamera.orthographicSize > zoomThreshold)
            {
                desiredZoom = focusZoom;
            }

            cameraController.FocusOnTarget(target, desiredZoom, true);
        }

        #region Brain Visualization

        private void RenderBrainGraph(NEATNetwork brain)
        {
            nodeVisuals.Clear();
            connectionVisuals.Clear();

            if (brainGraphContainer == null)
                return;

            ClearChildren(brainGraphContainer);

            if (brain == null)
                return;

            List<Neuron> neurons = brain.GetNeurons();
            if (neurons == null || neurons.Count == 0)
                return;

            List<Neuron> inputNeurons = neurons.Where(n => n.type == NeuronType.Input).OrderBy(n => n.id).ToList();
            List<Neuron> hiddenNeurons = neurons.Where(n => n.type == NeuronType.Hidden).OrderBy(n => n.id).ToList();
            List<Neuron> outputNeurons = neurons.Where(n => n.type == NeuronType.Output).OrderBy(n => n.id).ToList();

            float width = Mathf.Abs(brainGraphContainer.rect.width);
            if (width < 50f) width = 620f;
            float height = Mathf.Abs(brainGraphContainer.rect.height);
            if (height < 10f) height = 320f;

            var positions = new Dictionary<int, Vector2>();
            PlaceNeuronColumn(inputNeurons, 0, width, height, positions);
            PlaceNeuronColumn(hiddenNeurons, 1, width, height, positions);
            PlaceNeuronColumn(outputNeurons, 2, width, height, positions);

            foreach (Neuron neuron in neurons)
            {
                if (!positions.TryGetValue(neuron.id, out Vector2 pos))
                    continue;

                Color color = neuron.type switch
                {
                    NeuronType.Input => new Color(0.4f, 0.7f, 1f, 0.9f),
                    NeuronType.Hidden => new Color(0.7f, 0.7f, 0.7f, 0.9f),
                    _ => new Color(1f, 0.55f, 0.2f, 0.9f)
                };

                string nodeLabel = GetNeuronLabel(neuron, brain);
                GameObject node = CreatureInspectorHelper.CreateNodeUI(brainGraphContainer, pos, 42f, color, nodeLabel);
                Image image = node.GetComponent<Image>();
                TextMeshProUGUI label = node.GetComponentInChildren<TextMeshProUGUI>();
                nodeVisuals[neuron.id] = new BrainNodeVisual
                {
                    Image = image,
                    Label = label,
                    BaseColor = color
                };
            }

            foreach (Connection connection in brain.GetConnections())
            {
                if (!positions.TryGetValue(connection.fromNeuronId, out Vector2 start) ||
                    !positions.TryGetValue(connection.toNeuronId, out Vector2 end))
                {
                    continue;
                }

                Image lineImage = CreateConnectionLine(start, end, 4f);
                if (lineImage != null)
                {
                    connectionVisuals[(connection.fromNeuronId, connection.toNeuronId)] = lineImage;
                }
            }
        }

        private void PopulateBrainList(NEATNetwork brain)
        {
            if (brainListRoot == null)
                return;

            ClearChildren(brainListRoot);

            if (brain == null)
                return;

            foreach (Neuron neuron in brain.GetNeurons().OrderBy(n => n.type).ThenBy(n => n.id))
            {
                string info = $"[{neuron.type}] #{neuron.id}  Bias:{neuron.bias:F2}";
                CreatureInspectorHelper.CreateNeuronDisplay(brainListRoot, info);
            }

            foreach (Connection connection in brain.GetConnections())
            {
                string info = $"{connection.fromNeuronId} → {connection.toNeuronId} | w:{connection.weight:F2} | {(connection.enabled ? "ON" : "OFF")}";
                CreatureInspectorHelper.CreateConnectionDisplay(brainListRoot, info);
            }
        }

        private void UpdateBrainActivationVisuals()
        {
            if (currentCreature == null)
                return;

            NEATNetwork brain = currentCreature.GetBrain();
            if (brain == null)
                return;

            foreach (Neuron neuron in brain.GetNeurons())
            {
                if (!nodeVisuals.TryGetValue(neuron.id, out BrainNodeVisual visual) || visual.Image == null)
                    continue;

                float intensity = Mathf.Clamp01(neuron.value);
                Color target = Color.Lerp(visual.BaseColor * 0.6f, Color.cyan, intensity);
                target.a = visual.BaseColor.a;
                visual.Image.color = target;
                if (visual.Label != null)
                {
                    visual.Label.color = intensity > 0.6f ? Color.black : Color.white;
                }
                nodeVisuals[neuron.id] = visual;
            }

            foreach (Connection connection in brain.GetConnections())
            {
                if (!connectionVisuals.TryGetValue((connection.fromNeuronId, connection.toNeuronId), out Image lineImage) || lineImage == null)
                    continue;

                float magnitude = Mathf.Clamp01(Mathf.Abs(connection.weight));
                Color color = connection.weight >= 0f ? Color.green : Color.red;
                color.a = Mathf.Lerp(0.2f, 0.9f, magnitude);
                lineImage.color = color;
            }
        }

        private void PlaceNeuronColumn(IReadOnlyList<Neuron> neurons, int columnIndex, float width, float height, Dictionary<int, Vector2> positions)
        {
            if (neurons == null || neurons.Count == 0)
                return;

            float[] xColumns = new[]
            {
                40f,
                width * 0.5f,
                Mathf.Max(width - 40f, 40f)
            };

            float verticalSpace = Mathf.Max(height - 40f, 40f);
            float spacing = neurons.Count > 1 ? verticalSpace / Mathf.Max(1, neurons.Count - 1) : 0f;
            spacing = Mathf.Max(spacing, 48f);
            float totalSpan = spacing * Mathf.Max(0, neurons.Count - 1);
            float overflow = Mathf.Max(0f, totalSpan - verticalSpace);
            float startOffset = -20f - Mathf.Max(0f, (verticalSpace - totalSpan) * 0.5f);
            if (overflow > 0f)
            {
                startOffset = -20f;
            }

            for (int i = 0; i < neurons.Count; i++)
            {
                float x = xColumns[Mathf.Clamp(columnIndex, 0, xColumns.Length - 1)];
                float y = startOffset - spacing * i;
                positions[neurons[i].id] = new Vector2(x, y);
            }
        }

        #endregion

        #region Genome Tab

        private void RefreshGenomeInfo(CreatureController creature)
        {
            if (genomeListRoot == null)
                return;

            ClearChildren(genomeListRoot);

            if (creature == null)
                return;

            Genome genome = creature.GetGenome();
            CreatureLineageRecord lineage = creature.GetLineageRecord();

            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Energy", $"{creature.Energy:F1}/{creature.MaxEnergy:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Health", $"{creature.Health:F1}/{creature.MaxHealth:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Maturity", $"{creature.Maturity:P0}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Age", $"{creature.Age:F1}s");

            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Size", $"{genome.size:F2}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Speed", $"{genome.speed:F2}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Diet", $"{genome.diet:F2}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Vision", $"{genome.visionRange:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Health Cap", $"{genome.health:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Growth Duration", $"{genome.growthDuration:F1}s");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Growth Energy", $"{genome.growthEnergyThreshold:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Repro Age", $"{genome.reproAgeThreshold:F1}s");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Repro Energy", $"{genome.reproEnergyThreshold:F1}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Repro Cooldown", $"{genome.reproCooldown:F1}s");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Mutation Rate", $"{genome.mutationRate:F2}");
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Pheromone", genome.pheromoneType.ToString());

            string colorHex = ColorUtility.ToHtmlStringRGB(genome.color);
            CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Color", $"#{colorHex}");

            if (lineage != null)
            {
                CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Generation", lineage.GenerationIndex.ToString());
                CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Lineage ID", $"#{lineage.LineageId:0000}");
                CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Genome Code", lineage.GenomeCode);
                CreatureInspectorHelper.CreateGenomeRow(genomeListRoot, "Parent Code", lineage.ParentGenomeCode);
            }
            
            // Force rebuild layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(genomeListRoot as RectTransform);
        }

        #endregion

        #region UI Construction Helpers

        private void BuildUIIfNeeded(bool allowCreate)
        {
            if (HasAllUIReferences())
                return;

            if (!allowCreate)
            {
                Debug.LogWarning("[CreaturePopupUI] UI references are missing but auto-build is disabled. Assign existing hierarchy objects or enable autoBuildUI.", this);
                return;
            }

            GameObject canvasObj = new GameObject("CreaturePopupCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvasObj.transform.SetParent(transform, false);

            popupCanvas = canvasObj.AddComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = new GameObject("CreaturePopupRoot");
            panelObj.layer = canvasObj.layer;
            panelObj.transform.SetParent(canvasObj.transform, false);
            popupPanel = panelObj.AddComponent<RectTransform>();
            popupPanel.anchorMin = panelAnchorMin;
            popupPanel.anchorMax = panelAnchorMax;
            popupPanel.offsetMin = Vector2.zero;
            popupPanel.offsetMax = Vector2.zero;

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            brainPanel = CreateSidePanel(panelObj.transform, "BrainPanel", new Vector2(0f, 0f), new Vector2(0.48f, 1f));
            genomePanel = CreateSidePanel(panelObj.transform, "GenomePanel", new Vector2(0.52f, 0f), new Vector2(1f, 1f));

            BuildBrainTab(brainPanel.transform);
            BuildGenomeTab(genomePanel.transform);
        }

        private bool HasAllUIReferences()
        {
            return popupCanvas != null &&
                   popupPanel != null &&
                   brainPanel != null &&
                   genomePanel != null &&
                   brainGraphContainer != null &&
                   brainListRoot != null &&
                   genomeListRoot != null;
        }

        private void BuildBrainTab(Transform parent)
        {
            GameObject tab = CreateTab(parent, "BrainTab", out RectTransform tabRect);
            VerticalLayoutGroup layout = tab.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateSectionTitle(tab.transform, "Brain / Phenotype");

            GameObject graphObj = new GameObject("PhenotypeGraph");
            graphObj.layer = tab.layer;
            graphObj.transform.SetParent(tab.transform, false);
            brainGraphContainer = graphObj.AddComponent<RectTransform>();
            brainGraphContainer.anchorMin = new Vector2(0, 1);
            brainGraphContainer.anchorMax = new Vector2(1, 1);
            brainGraphContainer.pivot = new Vector2(0, 1);
            brainGraphContainer.sizeDelta = new Vector2(0, 320f);

            Image graphBg = graphObj.AddComponent<Image>();
            graphBg.color = new Color(1f, 1f, 1f, 0.04f);

            LayoutElement graphLayout = graphObj.AddComponent<LayoutElement>();
            graphLayout.preferredHeight = 320f;

            Transform brainScrollContent = CreatureInspectorHelper.CreateScrollView(tab.transform, "BrainConnections");
            brainListRoot = brainScrollContent;
            RectTransform scrollRect = brainScrollContent.parent?.parent as RectTransform;
            if (scrollRect != null)
            {
                LayoutElement scrollLayout = scrollRect.gameObject.AddComponent<LayoutElement>();
                scrollLayout.flexibleHeight = 1f;
                scrollLayout.flexibleWidth = 1f;
            }
        }

        private void BuildGenomeTab(Transform parent)
        {
            GameObject tab = CreateTab(parent, "GenomeTab", out _);
            VerticalLayoutGroup layout = tab.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateSectionTitle(tab.transform, "Genome / Stats");

            Transform genomeScrollContent = CreatureInspectorHelper.CreateScrollView(tab.transform, "GenomeScroll");
            genomeListRoot = genomeScrollContent;
            RectTransform scrollRect = genomeScrollContent.parent?.parent as RectTransform;
            if (scrollRect != null)
            {
                LayoutElement scrollLayout = scrollRect.gameObject.AddComponent<LayoutElement>();
                scrollLayout.flexibleHeight = 1f;
                scrollLayout.flexibleWidth = 1f;
            }
        }

        private RectTransform CreateSidePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = obj.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.05f);

            // Fix: Add explicit width control to the panel itself
            // If the parent (panel) has 0 width, the children (tabs -> scrollviews) will have 0 width.
            // But here anchorMin/Max should handle it. 
            // Wait, if the PopupPanel has 0 width, then this child has 0 width.
            
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true; // Added this

            return rect;
        }

        private GameObject CreateTab(Transform parent, string name, out RectTransform rect)
        {
            GameObject tab = new GameObject(name);
            tab.layer = parent.gameObject.layer;
            tab.transform.SetParent(parent, false);

            rect = tab.AddComponent<RectTransform>();
            // If parent is VerticalLayoutGroup, these anchors are overridden, but good to set.
            rect.anchorMin = new Vector2(0, 0); // Stretch
            rect.anchorMax = new Vector2(1, 1); // Stretch
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            Image background = tab.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.05f);

            LayoutElement layoutElement = tab.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f; // Take all available width
            layoutElement.flexibleHeight = 1f; // Take all available height
            layoutElement.minWidth = 100f; // Minimal width

            return tab;
        }

        private TextMeshProUGUI CreateSectionTitle(Transform parent, string text)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.layer = parent.gameObject.layer;
            titleObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.font = CreatureInspectorHelper.GetDefaultFont();
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = text.ToUpperInvariant();
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;

            LayoutElement layoutElement = titleObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30f;

            return tmp;
        }

        private static string GetNeuronLabel(Neuron neuron, NEATNetwork brain)
        {
            if (neuron == null || brain == null)
                return string.Empty;

            return neuron.type switch
            {
                NeuronType.Input => FormatNodeName(InputNeuronNames, neuron.id),
                NeuronType.Output => FormatNodeName(OutputNeuronNames, neuron.id - brain.InputCount),
                _ => $"#{neuron.id}"
            };
        }

        private static string FormatNodeName(IReadOnlyList<string> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return $"#{index}";

            string name = list[index];
            return name.Replace(" ", "\n");
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
                }
                else
#endif
                {
                    UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
                }
            }
        }

        private Image CreateConnectionLine(Vector2 start, Vector2 end, float width)
        {
            if (brainGraphContainer == null)
                return null;

            GameObject line = new GameObject("Line");
            line.layer = brainGraphContainer.gameObject.layer;
            line.transform.SetParent(brainGraphContainer, false);
            line.transform.SetAsFirstSibling();

            RectTransform rect = line.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);
            rect.sizeDelta = new Vector2(distance, width);
            rect.anchoredPosition = start;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0, 0, angle);

            Image img = line.AddComponent<Image>();
            img.color = Color.white * 0.4f;
            return img;
        }

        private bool ClickedCreatureUnderPointer()
        {
            if (targetCamera == null)
                return false;

            Ray ray = targetCamera.ScreenPointToRay(GetPointerPosition());
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 2000f);
            if (hit.collider == null)
                return false;

            return hit.collider.GetComponent<CreatureController>() != null;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void EnsurePhysics2DRaycaster()
        {
            if (targetCamera == null)
                return;

            if (targetCamera.GetComponent<Physics2DRaycaster>() == null)
            {
                targetCamera.gameObject.AddComponent<Physics2DRaycaster>();
            }
        }

        #endregion

        #region Input Helpers

        private bool IsKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current == null)
                return false;

            if (!Enum.TryParse(keyCode.ToString(), true, out Key key))
                return false;

            KeyControl control = Keyboard.current[key];
            return control != null && control.wasPressedThisFrame;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        private bool IsMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current == null)
                return false;

            return button switch
            {
                0 => Mouse.current.leftButton.wasPressedThisFrame,
                1 => Mouse.current.rightButton.wasPressedThisFrame,
                2 => Mouse.current.middleButton.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        private Vector3 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Generate Popup Layout (Edit-Time)")]
        private void GenerateLayoutInEditor()
        {
            BuildUIIfNeeded(true);
            if (popupCanvas != null)
            {
                EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
        }
#endif
    }
}


