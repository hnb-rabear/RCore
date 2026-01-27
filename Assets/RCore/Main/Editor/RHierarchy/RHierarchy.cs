using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.RHierarchy
{
    [InitializeOnLoad]
    public class RHierarchy
    {
        static RHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (!RHierarchySettings.IsEnable) return;
            
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            // Handle Separator (Background drawing)
            if (RHierarchySettings.IsSeparatorEnabled)
                RSeparator.Draw(selectionRect, gameObject);

            // Calculate Rects
            Rect curRect = new Rect(selectionRect);
            curRect.x = selectionRect.x + selectionRect.width;

            // Draw Right-aligned components
            var order = RHierarchySettings.ComponentOrder;
            foreach (var type in order)
            {
                switch (type)
                {
                    case RHierarchySettings.RComponentType.Vertices:
                        if (RHierarchySettings.IsVerticesEnabled) RVertices.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.ChildrenCount:
                        if (RHierarchySettings.IsChildrenCountEnabled) RChildrenCount.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.Components:
                        if (RHierarchySettings.IsComponentsEnabled) RComponents.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.Tag:
                        if (RHierarchySettings.IsTagEnabled) RTag.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.Layer:
                        if (RHierarchySettings.IsLayerEnabled) RLayer.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.Static:
                        if (RHierarchySettings.IsStaticEnabled) RStatic.Draw(gameObject, ref curRect);
                        break;
                    case RHierarchySettings.RComponentType.Visibility:
                        if (RHierarchySettings.IsVisibilityEnabled) RVisibility.Draw(gameObject, ref curRect);
                        break;
                }
            }

            // Draw Left-aligned components (Icon)
            if (RHierarchySettings.IsMonoBehaviourIconEnabled)
                RMonoIcon.Draw(selectionRect, gameObject);
        }
    }

    public static class RSeparator
    {
        public static void Draw(Rect selectionRect, GameObject gameObject)
        {
            // Simple row shading
            if (RHierarchySettings.ShowRowShading)
            {
                float y = selectionRect.y;
                float index = y / selectionRect.height;
                if (Mathf.FloorToInt(index) % 2 == 0)
                {
                    EditorGUI.DrawRect(new Rect(0, selectionRect.y, selectionRect.width + selectionRect.x, selectionRect.height), RHierarchySettings.EvenRowColor);
                }
                else
                {
                    EditorGUI.DrawRect(new Rect(0, selectionRect.y, selectionRect.width + selectionRect.x, selectionRect.height), RHierarchySettings.OddRowColor);
                }
            }
            // Separator line
            if (RHierarchySettings.ShowSeparatorLine)
            {
                EditorGUI.DrawRect(new Rect(0, selectionRect.y, selectionRect.width + selectionRect.x, 1), RHierarchySettings.SeparatorColor);
            }
        }
    }

    public static class RMonoIcon
    {
        public static void Draw(Rect selectionRect, GameObject gameObject)
        {
            // Find custom script
            var components = gameObject.GetComponents<MonoBehaviour>();
            bool hasScript = false;
            foreach (var c in components)
            {
                if (c == null) continue;
                if (c.GetType().Assembly.GetName().Name != "UnityEngine" && !c.GetType().FullName.StartsWith("UnityEngine."))
                {
                    hasScript = true;
                    break;
                }
            }

            if (hasScript)
            {
                Rect iconRect = new Rect(selectionRect.x - 16, selectionRect.y, 16, 16);
                var icon = EditorGUIUtility.IconContent("cs Script Icon");
                GUI.DrawTexture(iconRect, icon.image);
            }
        }
    }

    public static class RVisibility
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            rect.x -= 18;
            rect.width = 18;
            
            bool isActive = gameObject.activeSelf;
            bool isVisible = gameObject.activeInHierarchy;
            
            var iconName = isVisible ? "d_scenevis_visible" : "d_scenevis_hidden";
            if (!isVisible && isActive) iconName = "d_scenevis_hidden"; // Parent hidden
            
            Color color = GUI.color;
            if (isVisible) GUI.color = RHierarchySettings.ActiveColor;
            else GUI.color = RHierarchySettings.InactiveColor;

            var icon = EditorGUIUtility.IconContent(iconName);
            if (GUI.Button(rect, icon.image, GUIStyle.none))
            {
                Undo.RecordObject(gameObject, "Toggle Visibility");
                gameObject.SetActive(!isActive);
                EditorUtility.SetDirty(gameObject);
            }
            GUI.color = color;
        }
    }

    public static class RVertices
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            int verts = 0;
            int tris = 0;
            
            var filters = gameObject.GetComponents<MeshFilter>();
            foreach(var f in filters)
            {
                if(f.sharedMesh != null)
                {
                    verts += f.sharedMesh.vertexCount;
                    tris += f.sharedMesh.triangles.Length / 3;
                }
            }
            var skinned = gameObject.GetComponents<SkinnedMeshRenderer>();
            foreach(var s in skinned)
            {
                if(s.sharedMesh != null)
                {
                    verts += s.sharedMesh.vertexCount;
                    tris += s.sharedMesh.triangles.Length / 3;
                }
            }

            if (verts == 0 && tris == 0) return;

            string label = $"{GetCountString(verts)} / {GetCountString(tris)}";
            float width = 60; // Approximate
            rect.x -= width;
            rect.width = width;
            
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.alignment = TextAnchor.MiddleRight;
            style.normal.textColor = RHierarchySettings.VerticesLabelColor;
            
            GUI.Label(rect, label, style);
        }

        private static string GetCountString(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 1000000) return (count / 1000f).ToString("F1") + "k";
            return (count / 1000000f).ToString("F1") + "M";
        }
    }

    public static class RChildrenCount
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            int count = gameObject.transform.childCount;
            if (count == 0) return;
            
            rect.x -= 24;
            rect.width = 24;
            
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.alignment = TextAnchor.MiddleRight;
            style.normal.textColor = RHierarchySettings.ChildrenCountLabelColor;
            
            GUI.Label(rect, count.ToString(), style);
        }
    }

    public static class RTag
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            string tag = gameObject.tag;
            bool isUntagged = tag == "Untagged";
            
            if (isUntagged && !RHierarchySettings.ShowAlwaysTagLayer) return;

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.fontSize = 8;
            style.alignment = TextAnchor.MiddleRight;
            style.normal.textColor = RHierarchySettings.TagLabelColor;
            
            float contentWidth = style.CalcSize(new GUIContent(tag)).x + 5;
            if (contentWidth > 60) contentWidth = 60;
            if (contentWidth < 20) contentWidth = 20;

            rect.x -= contentWidth;
            rect.width = contentWidth;
            
            GUI.Label(rect, tag, style);
            
            // Interaction
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
                {
                    menu.AddItem(new GUIContent(t), t == tag, () => 
                    {
                        Undo.RecordObject(gameObject, "Change Tag");
                        gameObject.tag = t;
                        EditorUtility.SetDirty(gameObject);
                    });
                }
                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    }

    public static class RLayer
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            int layer = gameObject.layer;
            string layerName = LayerMask.LayerToName(layer);
            bool isDefaultLayer = layer == 0;
            
            if (isDefaultLayer && !RHierarchySettings.ShowAlwaysTagLayer) return;
            
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.fontSize = 8;
            style.alignment = TextAnchor.MiddleRight;
            style.normal.textColor = RHierarchySettings.LayerLabelColor;
            
            float contentWidth = style.CalcSize(new GUIContent(layerName)).x + 5;
            if (contentWidth > 60) contentWidth = 60;
             if (contentWidth < 20) contentWidth = 20;

            rect.x -= contentWidth;
            rect.width = contentWidth;
            
            GUI.Label(rect, layerName, style);
            
            // Interaction
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var l in UnityEditorInternal.InternalEditorUtility.layers)
                {
                    menu.AddItem(new GUIContent(l), l == layerName, () => 
                    {
                        Undo.RecordObject(gameObject, "Change Layer");
                        gameObject.layer = LayerMask.NameToLayer(l);
                        EditorUtility.SetDirty(gameObject);
                    });
                }
                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    }

    public static class RComponents
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            var components = gameObject.GetComponents<Component>();
            List<Component> toDraw = new List<Component>();
            foreach(var c in components)
            {
                if (c == null) continue;
                if (c is Transform) continue;
                toDraw.Add(c);
            }
            
            float iconSize = 16;
            
            foreach(var c in toDraw)
            {
                rect.x -= iconSize;
                rect.width = iconSize;
                
                var content = EditorGUIUtility.ObjectContent(c, null);
                if (content.image != null)
                {
                    // Check if enabled
                    bool isEnabled = true;
                    if (c is Behaviour behaviour) isEnabled = behaviour.enabled;
                    // Renderer also has enabled
                    else if (c is Renderer renderer) isEnabled = renderer.enabled;
                    else if (c is Collider collider) isEnabled = collider.enabled;

                    Color originalColor = GUI.color;
                    if (!isEnabled) GUI.color = new Color(1, 1, 1, 0.5f);

                    GUI.DrawTexture(rect, content.image);

                    GUI.color = originalColor;
                    
                    // Interaction
                    if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0) // Left click to toggle
                        {
                             if (c is Behaviour b)
                             {
                                 Undo.RecordObject(b, "Toggle Component");
                                 b.enabled = !b.enabled;
                                 EditorUtility.SetDirty(b);
                             }
                             else if (c is Renderer r)
                             {
                                 Undo.RecordObject(r, "Toggle Component");
                                 r.enabled = !r.enabled;
                                 EditorUtility.SetDirty(r);
                             }
                             else if (c is Collider col)
                             {
                                 Undo.RecordObject(col, "Toggle Component");
                                 col.enabled = !col.enabled;
                                 EditorUtility.SetDirty(col);
                             }
                        }
                        Event.current.Use();
                    }
                }
            }
            // Add spacing
            rect.x -= 4;
        }
    }

    public static class RStatic
    {
        public static void Draw(GameObject gameObject, ref Rect rect)
        {
            if (!gameObject.isStatic) return;

            string label = "S";
            
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            style.fontSize = 8;
            style.alignment = TextAnchor.MiddleRight;
            style.normal.textColor = RHierarchySettings.StaticLabelColor;
            style.fontStyle = FontStyle.Bold;

            float width = style.CalcSize(new GUIContent(label)).x + 5;
            if (width < 14) width = 14; 
            
            rect.x -= width;
            rect.width = width;

            GUI.Label(rect, label, style);
            
            // Interaction
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Toggle Static"), true, () => 
                {
                    Undo.RecordObject(gameObject, "Toggle Static");
                    gameObject.isStatic = !gameObject.isStatic;
                    EditorUtility.SetDirty(gameObject);
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    }
}
