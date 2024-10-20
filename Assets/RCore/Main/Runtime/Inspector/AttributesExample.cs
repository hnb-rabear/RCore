using UnityEngine;
using System.Collections;

namespace RCore.Inspector
{
    public class AttributesExample : MonoBehaviour
    {
	    [Comment("This is comment", "This is tooltip")]
	    [Highlight] public string highlight;
	    [ReadOnly] public string readOnly;
	    [Separator]
	    [TagSelector] public string tagSelector;
	    [SingleLayer] public int singleLayer;
        public Color TestColor;

        [InspectorButton]
        public IEnumerator RunClock(int duration = 3)
        {
            int n = 0;
            while (n < duration)
            {
                Debug.Log(n % 2 == 0 ? "Tick" : "Tack");
                yield return new WaitForSeconds(1.0f);
                n++;
            }
        }

        [InspectorButton]
        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, scale);
        }

        [InspectorButton]
        public void SetScaleX(float scale = 5.0f)
        {
            transform.localScale = new Vector3(scale, transform.localScale.y, transform.localScale.z);
        }

        [InspectorButton]
        public void FloatIntDefault(float floatAsInt = 5)
        {
            Debug.Log("floatAsInt " + floatAsInt);
        }

        [InspectorButton]
        public void EmptyMethod()
        {
        }

        [InspectorButton]
        public void PrintStuff(float floatVal, int intVal, string stringVal, bool boolVal)
        {
            Debug.Log("floatVal " + floatVal);
            Debug.Log("intVal " + intVal);
            Debug.Log("stringVal " + stringVal);
            Debug.Log("boolVal " + boolVal);
        }

        [InspectorButton]
        public void SetMaterialColor(Color color)
        {
            GetComponent<MeshRenderer>().sharedMaterial.color = color;
        }

        [InspectorButton]
        public IEnumerator CountTo(int max = 6)
        {
            int current = 0;
            while (current < max)
            {
                Debug.Log(current++);
                yield return new WaitForSeconds(1.0f);
            }
        }

        [InspectorButton]
        public string ConvertToHex(Color color)
        {
            return "#" + ColorToHex(color);
        }

        [InspectorButton]
        public void PrintNameOf(GameObject go)
        {
            Debug.Log(go.name);
        }

        //TAKEN FROM http://wiki.unity3d.com/index.php?title=HexConverter

        // Note that Color32 and Color implicitly convert to each other. You may pass a Color object to this method without first casting it.
        private string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }
    }
}