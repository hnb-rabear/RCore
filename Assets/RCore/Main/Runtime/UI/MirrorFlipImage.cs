using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
    public enum MirrorAxis
    {
        Horizontal,
        Vertical,
        Both
    }

    [RequireComponent(typeof(RectTransform))]
    public class MirrorFlipImage : Image
    {
        public bool flipHorizontal;
        public bool mirrorEnabled;
        public MirrorAxis mirrorAxis = MirrorAxis.Horizontal;

        readonly List<UIVertex> m_stream = new List<UIVertex>();
        readonly List<UIVertex> m_result = new List<UIVertex>();

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            if (!flipHorizontal && !mirrorEnabled)
                return;

            Rect rect = GetPixelAdjustedRect();

            m_stream.Clear();
            toFill.GetUIVertexStream(m_stream);

            if (flipHorizontal)
                FlipHorizontal(m_stream, rect);

            if (mirrorEnabled)
            {
                m_result.Clear();
                BuildMirror(m_stream, rect, mirrorAxis, m_result);

                toFill.Clear();
                toFill.AddUIVertexTriangleStream(m_result);
            }
            else
            {
                toFill.Clear();
                toFill.AddUIVertexTriangleStream(m_stream);
            }
        }

        static void FlipHorizontal(List<UIVertex> stream, Rect rect)
        {
            float mirrorSum = rect.xMin + rect.xMax;
            for (int i = 0; i < stream.Count; i++)
            {
                UIVertex v = stream[i];
                Vector3 pos = v.position;
                pos.x = mirrorSum - pos.x;
                v.position = pos;
                stream[i] = v;
            }
        }

        static void BuildMirror(List<UIVertex> stream, Rect rect, MirrorAxis axis, List<UIVertex> result)
        {
            bool mirrorX = axis == MirrorAxis.Horizontal || axis == MirrorAxis.Both;
            bool mirrorY = axis == MirrorAxis.Vertical || axis == MirrorAxis.Both;

            float centerX = mirrorX ? rect.xMin + rect.width * 0.5f : 0f;
            float centerY = mirrorY ? rect.yMin + rect.height * 0.5f : 0f;

            // Only the stretchable center strip is resized to fit the half budget; border-cap
            // regions (sliced corners/edges) keep their original pixel size so curved art isn't distorted.
            AxisMap mapX = mirrorX ? BuildAxisMap(DistinctSorted(stream, true), rect.width * 0.5f) : default;
            AxisMap mapY = mirrorY ? BuildAxisMap(DistinctSorted(stream, false), rect.height * 0.5f) : default;

            List<UIVertex> squished = new List<UIVertex>(stream.Count);
            for (int i = 0; i < stream.Count; i++)
            {
                UIVertex v = stream[i];
                Vector3 pos = v.position;
                if (mirrorX)
                    pos.x = mapX.Map(pos.x);
                if (mirrorY)
                    pos.y = mapY.Map(pos.y);
                v.position = pos;
                squished.Add(v);
            }

            AppendTriangles(squished, result, false, false, centerX, centerY);

            if (mirrorX)
                AppendTriangles(squished, result, true, false, centerX, centerY);

            if (mirrorY)
                AppendTriangles(squished, result, false, true, centerX, centerY);

            if (mirrorX && mirrorY)
                AppendTriangles(squished, result, true, true, centerX, centerY);
        }

        static List<float> s_distinctScratch = new List<float>();

        static List<float> DistinctSorted(List<UIVertex> stream, bool useX)
        {
            const float epsilon = 0.01f;
            s_distinctScratch.Clear();
            for (int i = 0; i < stream.Count; i++)
            {
                float val = useX ? stream[i].position.x : stream[i].position.y;
                bool found = false;
                for (int j = 0; j < s_distinctScratch.Count; j++)
                {
                    if (Mathf.Abs(s_distinctScratch[j] - val) < epsilon)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    s_distinctScratch.Add(val);
            }

            s_distinctScratch.Sort();
            return s_distinctScratch;
        }

        // Maps a source coordinate into the [start, start+halfBudget] range, keeping the outer
        // border-cap widths fixed and only rescaling the inner stretchable strip.
        struct AxisMap
        {
            float m_x0, m_x1, m_x2, m_x3;
            float m_centerNew;
            bool m_hasBorder;

            public static AxisMap Build(List<float> distinct, float halfBudget)
            {
                AxisMap map = default;
                if (distinct.Count < 4)
                {
                    map.m_hasBorder = false;
                    map.m_x0 = distinct.Count > 0 ? distinct[0] : 0f;
                    map.m_centerNew = halfBudget;
                    return map;
                }

                map.m_hasBorder = true;
                map.m_x0 = distinct[0];
                map.m_x1 = distinct[1];
                map.m_x2 = distinct[distinct.Count - 2];
                map.m_x3 = distinct[distinct.Count - 1];

                float capL = map.m_x1 - map.m_x0;
                float capR = map.m_x3 - map.m_x2;
                map.m_centerNew = Mathf.Max(0f, halfBudget - capL - capR);
                return map;
            }

            public float Map(float x)
            {
                if (!m_hasBorder)
                    return m_x0 + (x - m_x0) * 0.5f;

                if (x <= m_x1)
                    return x;
                if (x >= m_x2)
                    return m_x1 + m_centerNew + (x - m_x2);

                float centerOrig = m_x2 - m_x1;
                float t = centerOrig > 0.0001f ? (x - m_x1) / centerOrig : 0f;
                return m_x1 + t * m_centerNew;
            }
        }

        static AxisMap BuildAxisMap(List<float> distinct, float halfBudget) => AxisMap.Build(distinct, halfBudget);

        static void AppendTriangles(List<UIVertex> source, List<UIVertex> result, bool flipX, bool flipY,
            float centerX, float centerY)
        {
            if (!flipX && !flipY)
            {
                result.AddRange(source);
                return;
            }

            for (int i = 0; i < source.Count; i += 3)
            {
                UIVertex a = Mirror(source[i], flipX, flipY, centerX, centerY);
                UIVertex b = Mirror(source[i + 1], flipX, flipY, centerX, centerY);
                UIVertex c = Mirror(source[i + 2], flipX, flipY, centerX, centerY);

                // Mirroring one or three axes flips triangle winding; swap two verts to restore it.
                result.Add(a);
                result.Add(c);
                result.Add(b);
            }
        }

        static UIVertex Mirror(UIVertex v, bool flipX, bool flipY, float centerX, float centerY)
        {
            Vector3 pos = v.position;
            if (flipX)
                pos.x = 2f * centerX - pos.x;
            if (flipY)
                pos.y = 2f * centerY - pos.y;
            v.position = pos;
            return v;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MirrorFlipImage)), CanEditMultipleObjects]
        public class MirrorFlipImageEditor : ImageEditor
        {
            SerializedProperty m_flipHorizontal;
            SerializedProperty m_mirrorEnabled;
            SerializedProperty m_mirrorAxis;

            protected override void OnEnable()
            {
                base.OnEnable();
                m_flipHorizontal = serializedObject.FindProperty("flipHorizontal");
                m_mirrorEnabled = serializedObject.FindProperty("mirrorEnabled");
                m_mirrorAxis = serializedObject.FindProperty("mirrorAxis");
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                serializedObject.Update();
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_flipHorizontal);
                EditorGUILayout.PropertyField(m_mirrorEnabled);
                if (m_mirrorEnabled.boolValue)
                    EditorGUILayout.PropertyField(m_mirrorAxis);
                serializedObject.ApplyModifiedProperties();
            }

            [MenuItem("CONTEXT/Image/Swap MirrorFlipImage")]
            static void SwapMirrorFlipImage(MenuCommand command)
            {
                Image image = command.context as Image;
                if (image == null)
                    return;

                GameObject go = image.gameObject;
                bool toMirrorFlip = !(image is MirrorFlipImage);
                MonoScript script = toMirrorFlip ? GetMonoScript<MirrorFlipImage>() : GetMonoScript<Image>();
                if (script == null)
                    return;

                Undo.RegisterCompleteObjectUndo(image, toMirrorFlip ? "Swap To MirrorFlipImage" : "Swap To Image");

                var serialized = new SerializedObject(image);
                serialized.FindProperty("m_Script").objectReferenceValue = script;
                serialized.ApplyModifiedProperties();

                EditorUtility.SetDirty(go);
                EditorApplication.delayCall += () => go.GetComponent<Image>()?.SetVerticesDirty();
            }

            static MonoScript GetMonoScript<T>() where T : MonoBehaviour
            {
                GameObject temp = new GameObject { hideFlags = HideFlags.HideAndDontSave };
                MonoScript script = MonoScript.FromMonoBehaviour(temp.AddComponent<T>());
                DestroyImmediate(temp);
                return script;
            }
        }
#endif
    }
}
