#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Verrarium.UI;

namespace Verrarium.Editor
{
    /// <summary>
    /// Menu command giúp sinh layout popup ngay trong Editor.
    /// </summary>
    public static class CreaturePopupUIMenu
    {
        private const string MenuPath = "Verrarium/Generate Creature Popup UI";

        [MenuItem(MenuPath, priority = 150)]
        public static void GeneratePopupLayout()
        {
            CreaturePopupUI popup = GetTargetPopup();
            if (popup == null)
            {
                EditorUtility.DisplayDialog(
                    "Creature Popup UI",
                    "Không tìm thấy component CreaturePopupUI trong scene hiện tại. Thêm script vào một GameObject trước khi chạy lệnh.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(popup.gameObject, "Generate Creature Popup UI");
            InvokeGenerateLayout(popup);
            Selection.activeObject = popup.gameObject;

            EditorUtility.DisplayDialog(
                "Creature Popup UI",
                "Popup UI đã được tạo/cập nhật. Bạn có thể chỉnh sửa layout trực tiếp trong hierarchy.",
                "OK");
        }

        private static CreaturePopupUI GetTargetPopup()
        {
            CreaturePopupUI popup = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<CreaturePopupUI>()
                : null;

            if (popup != null)
                return popup;

#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<CreaturePopupUI>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<CreaturePopupUI>(true);
#endif
        }

        private static void InvokeGenerateLayout(CreaturePopupUI popup)
        {
            MethodInfo method = typeof(CreaturePopupUI).GetMethod(
                "GenerateLayoutInEditor",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method != null)
            {
                method.Invoke(popup, null);
            }
            else
            {
                popup.SendMessage("GenerateLayoutInEditor", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
#endif










