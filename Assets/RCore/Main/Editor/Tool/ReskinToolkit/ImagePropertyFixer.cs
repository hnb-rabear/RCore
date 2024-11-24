using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	[System.Serializable]
	public class ImagePropertyFixer
	{
		private bool m_displayIcon;
		private List<Sprite> m_sprites = new List<Sprite>();
		private List<GameObject> m_targets = new List<GameObject>();
		private YesNoNone m_useSpriteMesh;
		private YesNoNone m_raycastTarget;
		private YesNoNone m_maskable;
		private PerfectRatio m_perfectRatio;

		private bool CanSubmit()
		{
			return m_useSpriteMesh != YesNoNone.None || m_raycastTarget != YesNoNone.None || m_maskable != YesNoNone.None || m_perfectRatio != PerfectRatio.None;
		}

		public void Draw()
		{
			var btnClearSprites = new EditorButton
			{
				label = "Clear sprites",
				onPressed = () => m_sprites.Clear(),
				color = Color.red
			};
			var btnClearTargets = new EditorButton
			{
				label = "Clear targets",
				onPressed = () => this.m_targets.Clear(),
				color = Color.red
			};
			m_sprites ??= new List<Sprite>();
			EditorHelper.DragDropBox<Object>("Sprite or Texture", objs =>
			{
				foreach (var obj in objs)
				{
					if (obj is Texture2D || obj is Sprite)
					{
						string path = AssetDatabase.GetAssetPath(obj);
						var tempSprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
						foreach (var sprite in tempSprites)
							if (!m_sprites.Contains(sprite))
								m_sprites.Add(sprite);
					}
					else if (obj is GameObject)
					{
						var images = (obj as GameObject).GetComponentsInChildren<Image>(true);
						foreach (var image in images)
							if (!m_sprites.Contains(image.sprite))
								m_sprites.Add(image.sprite);
						var spriteRenderers = (obj as GameObject).GetComponentsInChildren<SpriteRenderer>(true);
						foreach (var image in spriteRenderers)
						    if (!m_sprites.Contains(image.sprite))
						        m_sprites.Add(image.sprite);
					}
				}
			});
			m_displayIcon = EditorHelper.Toggle(m_displayIcon, "Display icon");
			EditorHelper.PagesForList(m_sprites.Count, nameof(m_sprites), i =>
			{
				EditorGUILayout.BeginHorizontal();
				{
					if (m_displayIcon)
					{
						m_sprites[i] = (Sprite)EditorHelper.ObjectField<Sprite>(m_sprites[i], "", 0, 60, showAsBox: true);
						EditorHelper.TextField(m_sprites[i] != null ? m_sprites[i].name : "", "", 0, 200);
					}
					else
						m_sprites[i] = (Sprite)EditorHelper.ObjectField<Sprite>(m_sprites[i], "");
					if (EditorHelper.ButtonColor("-", Color.red, 23))
						m_sprites.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}, new IDraw[] { btnClearSprites }, new IDraw[] { btnClearSprites });
			EditorHelper.DragDropBox<Object>("Searching sources", objs =>
			{
				foreach (var obj in objs)
				{
					if (obj is GameObject gameObject)
						if (!m_targets.Contains(obj))
							m_targets.Add(gameObject);
				}
			});
			EditorGUILayout.HelpBox("If the searching sources is empty, the tool will scan all the objects in Assets folder", MessageType.Info);
			EditorHelper.PagesForList(m_targets.Count, nameof(m_targets), i =>
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.ObjectField(m_targets[i], typeof(Object), false);
					if (EditorHelper.ButtonColor("-", Color.red, 23))
						m_targets.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}, new IDraw[] { btnClearTargets }, new IDraw[] { btnClearTargets });

			if (m_sprites.Count > 0)
			{
				m_raycastTarget = EditorHelper.DropdownListEnum(m_raycastTarget, "Raycast target", 100);
				m_useSpriteMesh = EditorHelper.DropdownListEnum(m_useSpriteMesh, "Use sprite mesh", 100);
				m_maskable = EditorHelper.DropdownListEnum(m_maskable, "Maskable", 100);
				m_perfectRatio = EditorHelper.DropdownListEnum(m_perfectRatio, "Perfect ratio", 100);
				if (EditorHelper.Button("Find images and apply"))
				{
					if (!CanSubmit())
					{
						EditorUtility.DisplayDialog("Error", "Please select an option", "OK");
						return;
					}
					m_targets ??= new List<GameObject>();
					if (m_targets.Count == 0)
					{
						bool ok = EditorUtility.DisplayDialog("Error", "If the searching sources is empty, the tool will scan all the objects in Assets folder", "Scan all", "Cancel");
						if (!ok)
							return;
						var assetGUIDs = AssetDatabase.FindAssets("t:GameObject", new string[] { "Assets" });
						foreach (var guiD in assetGUIDs)
						{
							var path = AssetDatabase.GUIDToAssetPath(guiD);
							var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
							m_targets.Add(obj);
						}
					}
					foreach (var target in m_targets)
					{
						bool dirty = false;
						var images = target.GetComponentsInChildren<Image>(true);
						foreach (var image in images)
						{
							if (m_sprites.Contains(image.sprite))
							{
								if (m_raycastTarget != YesNoNone.None)
									image.raycastTarget = m_raycastTarget == YesNoNone.Yes;
								if (m_useSpriteMesh != YesNoNone.None)
									image.useSpriteMesh = m_useSpriteMesh == YesNoNone.Yes;
								if (m_maskable != YesNoNone.None)
									image.maskable = m_maskable == YesNoNone.Yes;
								if (m_perfectRatio == PerfectRatio.Height)
									RUtil.PerfectRatioImageByHeight(image);
								else if (m_perfectRatio == PerfectRatio.Width)
									RUtil.PerfectRatioImagesByWidth(image);
								dirty = true;
							}
						}
						if (dirty)
							EditorUtility.SetDirty(target);
					}
					AssetDatabase.SaveAssets();
				}
			}
			else
				EditorGUILayout.HelpBox("Missing Sprites or Textures to the box,\n Missing searching sources", MessageType.Warning);
		}
	}
}