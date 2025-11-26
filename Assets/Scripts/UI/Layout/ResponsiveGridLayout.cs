using UnityEngine;
using UnityEngine.UI;

namespace Verrarium.UI.Layout
{
    /// <summary>
    /// GridLayoutGroup tùy biến để tự động co giãn theo chiều rộng panel.
    /// </summary>
    [ExecuteAlways]
    public class ResponsiveGridLayout : GridLayoutGroup
    {
        [SerializeField, Min(1)] private int minColumns = 1;
        [SerializeField, Min(1)] private int maxColumns = 2;
        [SerializeField, Min(50f)] private float minCellWidth = 320f;
        [SerializeField, Min(80f)] private float cellHeight = 150f;

        public int MinColumns
        {
            get => minColumns;
            set => minColumns = Mathf.Max(1, value);
        }

        public int MaxColumns
        {
            get => maxColumns;
            set => maxColumns = Mathf.Max(1, value);
        }

        public float MinCellWidth
        {
            get => minCellWidth;
            set => minCellWidth = Mathf.Max(50f, value);
        }

        public float CellHeight
        {
            get => cellHeight;
            set => cellHeight = Mathf.Max(80f, value);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            UpdateCellSize();
        }

        public override void SetLayoutHorizontal()
        {
            UpdateCellSize();
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            UpdateCellSize();
            base.SetLayoutVertical();
        }

        private void UpdateCellSize()
        {
            if (!isActiveAndEnabled)
                return;

            float availableWidth = rectTransform.rect.width - padding.horizontal;
            int targetColumns = Mathf.Clamp(
                Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing.x) / (minCellWidth + spacing.x))),
                minColumns,
                maxColumns);

            float totalSpacing = spacing.x * (targetColumns - 1);
            float calculatedWidth = (availableWidth - totalSpacing) / targetColumns;

            constraint = Constraint.FixedColumnCount;
            constraintCount = targetColumns;
            cellSize = new Vector2(Mathf.Max(minCellWidth, calculatedWidth), cellHeight);
        }
    }
}


