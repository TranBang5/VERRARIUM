#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Verrarium.World;

namespace Verrarium.Editor
{
    /// <summary>
    /// Helper để tạo HexGrid từ menu
    /// </summary>
    public class HexGridCreator
    {
        [MenuItem("Verrarium/Create Hex Grid")]
        public static void CreateHexGrid()
        {
            // Kiểm tra xem đã có HexGrid chưa
            HexGrid existingGrid = Object.FindFirstObjectByType<HexGrid>();
            if (existingGrid != null)
            {
                if (!EditorUtility.DisplayDialog("Hex Grid Exists", 
                    "A HexGrid already exists in the scene. Do you want to create another one?", 
                    "Yes", "No"))
                {
                    return;
                }
            }

            // Tạo GameObject
            GameObject hexGridObj = new GameObject("HexGrid");
            HexGrid hexGrid = hexGridObj.AddComponent<HexGrid>();

            // Generate grid
            hexGrid.GenerateGrid();

            // Select trong hierarchy
            Selection.activeGameObject = hexGridObj;

            Debug.Log("Hex Grid created! Configure it in the Inspector and click 'Regenerate Grid' if needed.");
        }
    }
}
#endif

