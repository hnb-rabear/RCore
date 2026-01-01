using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Helper class to replace specific fonts in scenes and project assets.
	/// </summary>
	[Serializable]
	public class FontReplacer
	{
		[Serializable]
		public class Input
		{
			public List<Font> targets = new List<Font>();
			public Font replace;
		}

		[SerializeField] private List<Input> m_inputs = new List<Input>();

		private void Draw()
		{
			var btn = new GuiButton()
			{
				color = Color.yellow,
				onPressed = () => m_inputs.Add(new Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Find And Replace Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					foreach (var target in m_inputs)
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.BeginVertical();
							{
								EditorGUILayout.BeginHorizontal();
								{
									target.replace = (Font)EditorHelper.ObjectField<Font>(target.replace, "");
									if (EditorHelper.ButtonColor("x", Color.red, 23))
									{
										m_inputs.Remove(target);
										break;
									}
								}

								EditorGUILayout.EndHorizontal();
							}

							EditorGUILayout.EndVertical();

							if (EditorHelper.ButtonColor("+", Color.green, 23))
								target.targets.Add(null);

							for (int t = 0; t < target.targets.Count; t++)
							{
								EditorGUILayout.BeginVertical();
								{
									EditorGUILayout.BeginHorizontal();
									{
										target.targets[t] = (Font)EditorHelper.ObjectField<Font>(target.targets[t], "");
										if (EditorHelper.ButtonColor("x", Color.red, 23))
										{
											target.targets.RemoveAt(t);
											t--;
										}
									}

									EditorGUILayout.EndHorizontal();
								}

								EditorGUILayout.EndVertical();
							}
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Separator();
					}

					if (EditorHelper.Button("Find And Replace in Projects"))
					{
						AssetDatabase.StartAssetEditing();

						var assetGUIDs = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", new string[] { "Assets" });
						foreach (var target in m_inputs)
						{
							var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

							foreach (var item in result)
								Debug.Log($"{target.replace.name} is replaced in {item.Value} Assets");
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}

					if (EditorHelper.Button("Find And Replace in Scene"))
					{
						AssetDatabase.StartAssetEditing();

						var objs = Object.FindObjectsOfType<GameObject>();
						int count = 0;
						foreach (var obj in objs)
						{
							var images = obj.GetComponentsInChildren<Text>(true);
							foreach (var com in images)
							{
								foreach (var target in m_inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										count++;
										EditorUtility.SetDirty(obj);
									}
							}
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();

						Debug.Log($"Replace {count} Objects");
					}
				}

				EditorGUILayout.EndVertical();
			}
		}
	}
}