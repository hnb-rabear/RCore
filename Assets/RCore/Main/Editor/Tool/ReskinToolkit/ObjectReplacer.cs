using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>Replaces every reference to old objects with a new object in the scope.</summary>
	[Serializable]
	public class ObjectReplacer
	{
		[Serializable]
		private class Input
		{
			public List<Object> targets = new List<Object>();
			public Object replace;
		}

		private const string SCAN_FILTER = "t:Object";

		[SerializeField] private List<Input> m_inputs = new List<Input>();
		[SerializeField] private ReplaceScope m_scope = new ReplaceScope();
		private ReplaceFlow m_flow;

		private List<(Object oldObj, Object newObj)> GetMappings()
		{
			var mappings = new List<(Object oldObj, Object newObj)>();
			foreach (var input in m_inputs)
			{
				if (input.replace == null)
					continue;
				foreach (var target in input.targets)
					if (target != null)
						mappings.Add((target, input.replace));
			}
			return mappings;
		}

		public void Draw()
		{
			m_flow ??= new ReplaceFlow();
			m_scope ??= new ReplaceScope();

			Input removeInput = null;
			foreach (var input in m_inputs)
			{
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("New object", GUILayout.Width(80));
				input.replace = EditorGUILayout.ObjectField(input.replace, typeof(Object), false);
				if (EditorHelper.ButtonColor("x", Color.red, 23))
					removeInput = input;
				EditorGUILayout.EndHorizontal();

				// DropZone, not EditorHelper.DragDropBox: that one expands a dropped folder into every asset inside it.
				var dropped = ReplaceFlow.DropZone("Drop OLD objects to replace", 24);
				if (dropped != null)
					foreach (var obj in dropped)
						if (!input.targets.Contains(obj))
							input.targets.Add(obj);
				int removeTarget = -1;
				for (int t = 0; t < input.targets.Count; t++)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Old", GUILayout.Width(80));
					input.targets[t] = EditorGUILayout.ObjectField(input.targets[t], typeof(Object), false);
					if (EditorHelper.ButtonColor("x", Color.red, 23))
						removeTarget = t;
					EditorGUILayout.EndHorizontal();
					var oldObj = input.targets[t];
					if (oldObj != null && input.replace != null && oldObj.GetType() != input.replace.GetType())
						EditorGUILayout.HelpBox($"Type differs: {oldObj.GetType().Name} -> {input.replace.GetType().Name}. References may break.", MessageType.Warning);
				}
				if (removeTarget >= 0)
					input.targets.RemoveAt(removeTarget);
				if (EditorHelper.ButtonColor("+ Old object", Color.green))
					input.targets.Add(null);
				EditorGUILayout.EndVertical();
			}
			if (removeInput != null)
				m_inputs.Remove(removeInput);
			if (EditorHelper.ButtonColor("+ Add mapping", Color.yellow))
				m_inputs.Add(new Input());

			EditorGUILayout.Space(6);
			var mappings = GetMappings();
			m_flow.Draw(nameof(ObjectReplacer), m_scope, ReplaceFlow.Fingerprint(m_scope, mappings),
				mappings.Count == 0 ? "Add a mapping with at least one Old object and a New object." : null,
				() => ReplaceFlow.ScanGuidRefs(mappings, m_scope.GetGuids(SCAN_FILTER)),
				changes => ReplaceFlow.ApplyGuidRefs(mappings, changes));
		}
	}
}
