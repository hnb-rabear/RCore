using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	[Serializable]
	public class ReplaceObjectTool
	{
		[Serializable]
		private class Input
		{
			public List<Object> targets = new List<Object>();
			public Object replace;
		}

		[SerializeField] private List<Input> m_inputs = new List<Input>();

		public void Draw()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_inputs.Add(new Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Object", null, false, btn))
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
									target.replace = EditorHelper.ObjectField<Object>(target.replace, "");
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
										target.targets[t] = EditorHelper.ObjectField<Object>(target.targets[t], "");
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

					if (EditorHelper.Button("Search and replace"))
					{
						AssetDatabase.StartAssetEditing();

						var assetGUIDs = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });
						foreach (var target in m_inputs)
						{
							var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

							foreach (var item in result)
								Debug.Log($"{target.replace.name} is replaced in {item.Value}");
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}

				EditorGUILayout.EndVertical();
			}
		}
	}

	[Serializable]
	public class GenericObjectsReplacer<T> where T : Object
	{
		[Serializable]
		private class Input
		{
			public List<T> targets = new List<T>();
			public T replace;
		}

		[SerializeField] private List<Input> m_inputs = new List<Input>();
		
		public void Draw()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_inputs.Add(new Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Object", null, false, btn))
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
									target.replace = (T)EditorHelper.ObjectField<T>(target.replace, "");
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
										target.targets[t] = (T)EditorHelper.ObjectField<T>(target.targets[t], "");
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

					if (EditorHelper.Button("Search and replace"))
					{
						AssetDatabase.StartAssetEditing();

						var assetGUIDs = AssetDatabase.FindAssets("t:Object", new string[] { "Assets" });
						foreach (var target in m_inputs)
						{
							var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

							foreach (var item in result)
								Debug.Log($"{target.replace.name} is replaced in {item.Value}");
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}

				EditorGUILayout.EndVertical();
			}
		}
	}
}