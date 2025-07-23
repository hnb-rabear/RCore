using RCore.Inspector;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
    public class UIGridScaler : MonoBehaviour
    {
        public enum ScaleType
        {
            None,
            Percent,
            Pixel
        }

        public GridLayoutGroup mGridLayoutGroup;
        public bool autoScaleWidth;
        [Range(0, 100)] public float cellMinWidth;
        [Range(0, 100)] public float cellMaxWidth;
        public bool autoScaleHeight;
        [Range(0, 100)] public float cellMinHeight;
        [Range(0, 100)] public float cellMaxHeight;

        private void OnEnable()
        {
            Validate();
        }

        [InspectorButton]
        private void Validate()
        {
            if (mGridLayoutGroup == null)
                mGridLayoutGroup = GetComponent<GridLayoutGroup>();

            var cellSize = mGridLayoutGroup.cellSize;
            if (autoScaleWidth)
            {
                var rectTransform = transform as RectTransform;
                var size = rectTransform.sizeDelta;
                float minWidth = size.x * cellMinWidth / 100f;
                float maxWidth = size.x * cellMaxWidth / 100f;
                if (cellSize.x > maxWidth) cellSize.x = maxWidth;
                if (cellSize.x < minWidth) cellSize.x = minWidth;
                mGridLayoutGroup.cellSize = cellSize;
            }

            if (autoScaleHeight)
            {
                var rectTransform = transform as RectTransform;
                var size = rectTransform.sizeDelta;
                float minHeight = size.y * cellMinHeight / 100f;
                float maxHeight = size.y * cellMaxHeight / 100f;
                if (cellSize.x > maxHeight) cellSize.x = maxHeight;
                if (cellSize.y < minHeight) cellSize.y = minHeight;
                mGridLayoutGroup.cellSize = cellSize;
            }
        }
    }
}