#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Verrarium.World;

namespace Verrarium.Editor
{
    /// <summary>
    /// Custom editor cho HexGrid để dễ dàng setup và customize
    /// </summary>
    [CustomEditor(typeof(HexGrid))]
    public class HexGridEditor : UnityEditor.Editor
    {
        private HexGrid hexGrid;
        private bool showCellEditor = false;
        private HexCoordinates selectedCellCoords = new HexCoordinates(0, 0);

        private void OnEnable()
        {
            hexGrid = (HexGrid)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Management", EditorStyles.boldLabel);

            // Regenerate Grid
            if (GUILayout.Button("Regenerate Grid"))
            {
                hexGrid.GenerateGrid();
                EditorUtility.SetDirty(hexGrid);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cell Editor", EditorStyles.boldLabel);

            showCellEditor = EditorGUILayout.Foldout(showCellEditor, "Edit Individual Cells");

            if (showCellEditor)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Chọn cell
                EditorGUILayout.LabelField("Select Cell Coordinates:");
                int q = EditorGUILayout.IntField("Q (Column)", selectedCellCoords.q);
                int r = EditorGUILayout.IntField("R (Row)", selectedCellCoords.r);
                selectedCellCoords = new HexCoordinates(q, r);

                HexCell cell = hexGrid.GetCell(selectedCellCoords);

                if (cell != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Cell Properties:", EditorStyles.boldLabel);

                    // Fertility
                    bool isFertile = EditorGUILayout.Toggle("Is Fertile", cell.isFertile);
                    if (isFertile != cell.isFertile)
                    {
                        hexGrid.SetFertile(selectedCellCoords, isFertile);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    float fertility = EditorGUILayout.Slider("Fertility", cell.fertility, 0f, 1f);
                    if (fertility != cell.fertility)
                    {
                        hexGrid.SetFertility(selectedCellCoords, fertility);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    // Temperature
                    float temperature = EditorGUILayout.Slider("Temperature", cell.temperature, 0f, 1f);
                    if (temperature != cell.temperature)
                    {
                        hexGrid.SetTemperature(selectedCellCoords, temperature);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    // Resource Density
                    float resourceDensity = EditorGUILayout.Slider("Resource Density", cell.resourceDensity, 0f, 2f);
                    if (resourceDensity != cell.resourceDensity)
                    {
                        hexGrid.SetResourceDensity(selectedCellCoords, resourceDensity);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    // Movement Cost
                    float movementCost = EditorGUILayout.Slider("Movement Cost", cell.movementCost, 0.1f, 5f);
                    if (movementCost != cell.movementCost)
                    {
                        hexGrid.SetMovementCost(selectedCellCoords, movementCost);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    // Obstacle
                    bool isObstacle = EditorGUILayout.Toggle("Is Obstacle", cell.isObstacle);
                    if (isObstacle != cell.isObstacle)
                    {
                        hexGrid.SetObstacle(selectedCellCoords, isObstacle);
                        EditorUtility.SetDirty(hexGrid);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"World Position: {cell.WorldPosition}");
                }
                else
                {
                    EditorGUILayout.HelpBox("Cell not found at these coordinates", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bulk Operations", EditorStyles.boldLabel);

            // Create Fertile Areas
            if (GUILayout.Button("Create Random Fertile Areas"))
            {
                CreateRandomFertileAreas();
            }

            // Clear All Fertile
            if (GUILayout.Button("Clear All Fertile"))
            {
                ClearAllFertile();
            }

            // Reset All Cells
            if (GUILayout.Button("Reset All Cells"))
            {
                ResetAllCells();
            }
        }

        private void CreateRandomFertileAreas()
        {
            int numAreas = 5;

            for (int i = 0; i < numAreas; i++)
            {
                HexCell randomCell = hexGrid.GetRandomCell();
                if (randomCell != null)
                {
                    // Đánh dấu cell và neighbors là fertile
                    hexGrid.SetFertile(randomCell.Coordinates, true);
                    
                    var neighbors = hexGrid.GetNeighbors(randomCell.Coordinates);
                    foreach (var neighbor in neighbors)
                    {
                        hexGrid.SetFertile(neighbor.Coordinates, true);
                    }
                }
            }

            EditorUtility.SetDirty(hexGrid);
        }

        private void ClearAllFertile()
        {
            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                hexGrid.SetFertile(cell.Coordinates, false);
            }

            EditorUtility.SetDirty(hexGrid);
        }

        private void ResetAllCells()
        {
            var allCells = hexGrid.GetAllCells();
            foreach (var cell in allCells)
            {
                hexGrid.SetFertility(cell.Coordinates, 0.5f);
                hexGrid.SetTemperature(cell.Coordinates, 0.5f);
                hexGrid.SetResourceDensity(cell.Coordinates, 1.0f);
                hexGrid.SetMovementCost(cell.Coordinates, 1.0f);
                hexGrid.SetObstacle(cell.Coordinates, false);
            }

            EditorUtility.SetDirty(hexGrid);
        }
    }
}
#endif

