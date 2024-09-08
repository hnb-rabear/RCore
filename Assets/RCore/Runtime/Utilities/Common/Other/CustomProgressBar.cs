/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Common
{
    [System.Serializable]
    public class CustomProgressBar
    {
        public SpriteRenderer sprProgressBar;

        private SpriteRenderer _sprProgressBarValue;
        private SpriteRenderer mSprProgressBarValue
        {
            get
            {
                if (_sprProgressBarValue == null)
                    foreach (Transform t in sprProgressBar.transform)
                    {
                        _sprProgressBarValue = t.GetComponentInChildren<SpriteRenderer>();
                        if (_sprProgressBarValue != null)
                            break;
                    }
                return _sprProgressBarValue;
            }
        }
        public float fillAmount
        {
            set
            {
                if (value < 0) value = 0;
                if (value > 1) value = 1;

                var scale = mSprProgressBarValue.transform.localScale;
                scale.x = value;
                mSprProgressBarValue.transform.localScale = scale;
            }
            get => mSprProgressBarValue.transform.localScale.x;
        }
        public void SetActive(bool pValue)
        {
            sprProgressBar.gameObject.SetActive(pValue);
        }
        public Color bgColor
        {
            get => sprProgressBar.color;
            set => sprProgressBar.color = value;
        }
        public Color valueColor
        {
            get => mSprProgressBarValue.color;
            set => mSprProgressBarValue.color = value;
        }
        public Transform transform => sprProgressBar.transform;

        public GameObject gameObject => sprProgressBar.gameObject;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(CustomProgressBar))]
    public class CustomProgressBarDrawner : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);
            var progressBar = property.FindPropertyRelative("sprProgressBar");
            EditorGUI.PropertyField(position, progressBar, label, false);
        }
    }
#endif
}