using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	[Serializable]
	public class TMPFontReplacer
	{
		[Serializable]
		public class Input
		{
			public List<TMP_FontAsset> targets = new List<TMP_FontAsset>();
			public TMP_FontAsset replace;
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

			if (EditorHelper.HeaderFoldout("Find And Replace TMP Font", null, false, btn))
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
									target.replace = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(target.replace, "");
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
										target.targets[t] = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(target.targets[t], "");
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
						var objs = Object.FindObjectsOfType<GameObject>();
						int count = 0;
						var objectsFound = new List<GameObject>();
						foreach (var t in objs)
						{
							bool valid = false;
							var txtsUI = t.GetComponentsInChildren<TextMeshProUGUI>(true);
							foreach (var com in txtsUI)
							{
								foreach (var target in m_inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
										EditorUtility.SetDirty(t);
									}
							}

							var txts = t.GetComponentsInChildren<TextMeshPro>(true);
							foreach (var com in txts)
							{
								foreach (var target in m_inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
										EditorUtility.SetDirty(t);
									}
							}

							if (valid && !objectsFound.Contains(t))
								objectsFound.Add(t);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}
				}

				EditorGUILayout.EndVertical();
			}
		}
	}
}