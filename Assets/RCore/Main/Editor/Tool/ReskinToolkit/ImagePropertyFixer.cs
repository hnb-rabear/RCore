using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>Sets Image properties (raycast target, sprite mesh, maskable, perfect ratio) on every Image that uses the listed sprites.</summary>
	[Serializable]
	public class ImagePropertyFixer
	{
		private const string SCAN_FILTER = "t:GameObject";
		private static readonly string[] m_PropertyOptions = { "Leave", "Off", "On" }; // YesNoNone order: None, No, Yes

		[SerializeField] private bool m_displayIcon;
		[SerializeField] private List<Sprite> m_sprites = new List<Sprite>();
		[SerializeField] private ReplaceScope m_scope = new ReplaceScope();
		[SerializeField] private YesNoNone m_useSpriteMesh;
		[SerializeField] private YesNoNone m_raycastTarget;
		[SerializeField] private YesNoNone m_maskable;
		[SerializeField] private PerfectRatio m_perfectRatio;
		private ReplaceFlow m_flow;

		private bool HasAnyProperty() => m_useSpriteMesh != YesNoNone.None || m_raycastTarget != YesNoNone.None
			|| m_maskable != YesNoNone.None || m_perfectRatio != PerfectRatio.None;

		private static YesNoNone PropertyField(string pLabel, YesNoNone pValue) => (YesNoNone)EditorGUILayout.Popup(pLabel, (int)pValue, m_PropertyOptions);

		public void Draw()
		{
			m_flow ??= new ReplaceFlow();
			m_scope ??= new ReplaceScope();
			m_sprites ??= new List<Sprite>();

			var dropped = ReplaceFlow.DropZone("Drop the sprites whose Images to update\n(sprites, textures, prefabs, folders)");
			if (dropped != null)
				foreach (var sprite in SpriteReplacer.CollectSprites(dropped))
					if (!m_sprites.Contains(sprite))
						m_sprites.Add(sprite);

			m_displayIcon = EditorGUILayout.Toggle("Icons", m_displayIcon);
			int remove = -1;
			EditorHelper.PagesForList(m_sprites.Count, $"{nameof(ImagePropertyFixer)}.sprites", i =>
			{
				EditorGUILayout.BeginHorizontal();
				if (m_displayIcon)
				{
					m_sprites[i] = (Sprite)EditorGUILayout.ObjectField(m_sprites[i], typeof(Sprite), false, GUILayout.Width(60), GUILayout.Height(60));
					GUILayout.Label(m_sprites[i] != null ? m_sprites[i].name : "(empty)");
				}
				else
					m_sprites[i] = (Sprite)EditorGUILayout.ObjectField(m_sprites[i], typeof(Sprite), false);
				if (EditorHelper.ButtonColor("x", Color.red, 23))
					remove = i;
				EditorGUILayout.EndHorizontal();
			});
			if (remove >= 0)
				m_sprites.RemoveAt(remove);
			if (m_sprites.Count > 0 && EditorHelper.ButtonColor("Clear sprites", Color.red))
				m_sprites.Clear();

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Set on matching Images", EditorStyles.boldLabel);
			m_raycastTarget = PropertyField("Raycast target", m_raycastTarget);
			m_useSpriteMesh = PropertyField("Use sprite mesh", m_useSpriteMesh);
			m_maskable = PropertyField("Maskable", m_maskable);
			m_perfectRatio = (PerfectRatio)EditorGUILayout.EnumPopup("Perfect ratio", m_perfectRatio);

			EditorGUILayout.Space(6);
			string reason = m_sprites.Count == 0 ? "Add sprites first." : !HasAnyProperty() ? "Set at least one property (not \"Leave\")." : null;
			string fingerprint = ReplaceFlow.Fingerprint(m_scope, m_sprites.Select(x => ((Object)x, (Object)null)).ToList())
				+ $"#{m_raycastTarget}{m_useSpriteMesh}{m_maskable}{m_perfectRatio}";
			m_flow.Draw(nameof(ImagePropertyFixer), m_scope, fingerprint, reason, Scan, Apply);
		}

		private List<ReplaceChange> Scan()
		{
			var sprites = new HashSet<Sprite>(m_sprites.Where(x => x != null));
			var changes = new List<ReplaceChange>();
			foreach (var guid in m_scope.GetGuids(SCAN_FILTER))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
					continue;
				int count = prefab.GetComponentsInChildren<Image>(true).Count(x => sprites.Contains(x.sprite));
				if (count > 0)
					changes.Add(new ReplaceChange { path = path, count = count });
			}
			return changes;
		}

		private string Apply(List<ReplaceChange> pChanges)
		{
			var sprites = new HashSet<Sprite>(m_sprites.Where(x => x != null));
			int images = 0;
			foreach (var change in pChanges)
			{
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(change.path);
				foreach (var image in prefab.GetComponentsInChildren<Image>(true))
				{
					if (!sprites.Contains(image.sprite))
						continue;
					if (m_raycastTarget != YesNoNone.None)
						image.raycastTarget = m_raycastTarget == YesNoNone.Yes;
					if (m_useSpriteMesh != YesNoNone.None)
						image.useSpriteMesh = m_useSpriteMesh == YesNoNone.Yes;
					if (m_maskable != YesNoNone.None)
						image.maskable = m_maskable == YesNoNone.Yes;
					if (m_perfectRatio == PerfectRatio.Height)
						RUtil.PerfectRatioImageByHeight(image);
					else if (m_perfectRatio == PerfectRatio.Width)
						RUtil.PerfectRatioImageByWidth(image);
					images++;
				}
				EditorUtility.SetDirty(prefab);
			}
			AssetDatabase.SaveAssets();
			return $"{images} Images updated in {pChanges.Count} prefabs";
		}
	}
}
