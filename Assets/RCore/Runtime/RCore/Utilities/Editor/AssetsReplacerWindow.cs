using RCore.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace RCore.Editor
{
	public class AssetsReplacer : EditorWindow
	{
		private Vector2 m_scrollPosition;

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);

			SearchAndReplaceTextures();
			SearchAndReplaceFont();
			SearchAndReplaceTMPFont();
			m_objectReplace ??= new ObjectReplace();
			m_objectReplace.Draw();

			GUILayout.EndScrollView();
		}
		
		private ObjectReplace m_objectReplace;

		public class TextureReplace
		{
			public class Input
			{
				public readonly List<Sprite> targets = new List<Sprite>();
				public Sprite replace;
				public void Swap()
				{
					(replace, targets[0]) = (targets[0], replace);
				}
			}

			public readonly List<Object> sources = new List<Object>();
			public readonly List<Input> inputs = new List<Input>();
		}
		private TextureReplace m_textureReplace;
		private void SearchAndReplaceTextures()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_textureReplace.inputs.Add(new TextureReplace.Input()),
				label = "Add New Textures",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Textures", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_textureReplace ??= new TextureReplace();

					EditorHelper.DragDropBox<Object>("Sources", objs => m_textureReplace.sources.AddRange(objs));
					EditorHelper.PagesForList(m_textureReplace.sources.Count, $"m_textureReplace.sources", i =>
					{
						EditorGUILayout.ObjectField(m_textureReplace.sources[i], typeof(Object), false);
					});
					
					EditorGUILayout.BeginHorizontal();
					{
						EditorHelper.DragDropBox<Object>("New Textures", objs =>
						{
							foreach (var obj in objs)
							{
								if (obj is Texture2D)
								{
									string path = AssetDatabase.GetAssetPath(obj);
									var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
									foreach (var sprite in sprites)
										m_textureReplace.inputs.Add(new TextureReplace.Input
										{
											replace = sprite
										});
								}
							}
						});
						if (m_textureReplace.inputs.Count > 0)
							EditorHelper.DragDropBox<Object>("Old Textures", objs =>
							{
								foreach (var obj in objs)
								{
									if (obj is Texture2D)
									{
										string path = AssetDatabase.GetAssetPath(obj);
										var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
										foreach (var sprite in sprites)
											foreach (var input in m_textureReplace.inputs)
												if (input.replace.name == sprite.name && !input.targets.Contains(sprite))
													input.targets.Add(sprite);
									}
								}
							});
					}
					EditorGUILayout.EndHorizontal();

					if (EditorHelper.Button("Swap", 80))
					{
						foreach (var input in m_textureReplace.inputs)
							input.Swap();
					}
					EditorHelper.PagesForList(m_textureReplace.inputs.Count, "SearchAndReplaceTexture", i =>
					{
						var target = m_textureReplace.inputs[i];
						EditorGUILayout.BeginHorizontal();
						{
							target.replace = (Sprite)EditorHelper.ObjectField<Sprite>(target.replace, "", 0, 60, showAsBox: true);
							EditorGUILayout.BeginVertical();
							{
								if (EditorHelper.ButtonColor("Remove", Color.red, 60))
									m_textureReplace.inputs.Remove(target);

								EditorHelper.LabelField(target.replace != null ? target.replace.name : "", 60);
								EditorHelper.LabelField(target.replace != null ? $"{target.replace.NativeSize().x},{target.replace.NativeSize().y}" : "", 60);
							}

							EditorGUILayout.EndVertical();

							EditorGUILayout.BeginVertical();
							{
								if (EditorHelper.ButtonColor("Add target", Color.green, 80))
									target.targets.Add(null);
								if (target.targets.Count > 0)
									if (EditorHelper.Button("Swap", 80))
										target.Swap();
							}
							EditorGUILayout.EndVertical();

							for (int t = 0; t < target.targets.Count; t++)
							{
								target.targets[t] = (Sprite)EditorHelper.ObjectField<Sprite>(target.targets[t], "", 0, 60, showAsBox: true);
								EditorGUILayout.BeginVertical();
								{
									if (EditorHelper.ButtonColor("-", Color.red, 23))
									{
										target.targets.RemoveAt(t);
										t--;
									}

									EditorHelper.LabelField(target.targets[t] != null ? target.targets[t].name : "", 60);
									EditorHelper.LabelField(target.targets[t] != null ? $"{target.targets[t].NativeSize().x},{target.targets[t].NativeSize().y}" : "", 60);
								}

								EditorGUILayout.EndVertical();
							}
						}
						EditorGUILayout.EndHorizontal();
					});

					if (m_textureReplace.inputs.Count > 0)
						if (EditorHelper.Button("Search and replace"))
						{
							for (int i = 0; i < m_textureReplace.inputs.Count; i++)
							{
								var target = m_textureReplace.inputs[i];
								if (target.replace == null)
								{
									m_textureReplace.inputs.Remove(target);
									i--;
									continue;
								}

								for (int t = target.targets.Count - 1; t >= 0; t--)
									if (target.targets[t] == null)
										target.targets.RemoveAt(t);
							}

							if (m_textureReplace.inputs.Count == 0)
								return;

							AssetDatabase.StartAssetEditing();

							string[] assetGUIDs;
							if (m_textureReplace.sources != null && m_textureReplace.sources.Count > 0)
							{
								var tempAssetGUIDs = new List<string>();
								for (int i = 0; i < m_textureReplace.sources.Count; i++)
								{
									string path = AssetDatabase.GetAssetPath(m_textureReplace.sources[i]);
									if (AssetDatabase.IsValidFolder(path))
									{
										var guids = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", new[] { path });
										tempAssetGUIDs.AddRange(guids);
									}
									else
									{
										var guid = AssetDatabase.AssetPathToGUID(path);
										if (!tempAssetGUIDs.Contains(guid))
											tempAssetGUIDs.Add(guid);
									}
								}
								assetGUIDs = tempAssetGUIDs.ToArray();
							}
							else
								assetGUIDs = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", new[] { "Assets" });
							foreach (var target in m_textureReplace.inputs)
							{
								var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

								foreach (var item in result)
									Debug.Log($"{target.replace.name} is replaced in {item.Value} Assets");
							}
							AssetDatabase.StopAssetEditing();
							AssetDatabase.SaveAssets();
							AssetDatabase.Refresh();
						}
				}

				EditorGUILayout.EndVertical();
			}
		}
		
		public class FontReplace
		{
			public class Input
			{
				public readonly List<Font> targets = new List<Font>();
				public Font replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}
		private FontReplace m_FontReplace = new FontReplace();
		private void SearchAndReplaceFont()
		{
			
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_FontReplace.inputs.Add(new FontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_FontReplace ??= new FontReplace();

					foreach (var target in m_FontReplace.inputs)
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
										m_FontReplace.inputs.Remove(target);
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

					if (EditorHelper.Button("Search and replace in Projects"))
					{
						AssetDatabase.StartAssetEditing();

						var assetGUIDs = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", new string[] { "Assets" });
						foreach (var target in m_FontReplace.inputs)
						{
							var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

							foreach (var item in result)
								Debug.Log($"{target.replace.name} is replaced in {item.Value} Assets");
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}

					if (EditorHelper.Button("Search and replace in Scene"))
					{
						AssetDatabase.StartAssetEditing();

						var objs = FindObjectsOfType<GameObject>();
						int count = 0;
						for (int i = 0; i < objs.Length; i++)
						{
							var obj = objs[i];
							var images = obj.FindComponentsInChildren<Text>();
							foreach (var com in images)
							{
								foreach (var target in m_FontReplace.inputs)
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
		
		public class TextMeshProFontReplace
		{
			public class Input
			{
				public readonly List<TMP_FontAsset> targets = new List<TMP_FontAsset>();
				public TMP_FontAsset replace;
			}
			public readonly List<Input> inputs = new List<Input>();
		}

		private TextMeshProFontReplace m_TMPFontReplace = new TextMeshProFontReplace();
		private void SearchAndReplaceTMPFont()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_TMPFontReplace.inputs.Add(new TextMeshProFontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace TMP Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_TMPFontReplace ??= new TextMeshProFontReplace();

					foreach (var target in m_TMPFontReplace.inputs)
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
										m_TMPFontReplace.inputs.Remove(target);
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

					if (EditorHelper.Button("Search and replace in Projects"))
					{
						AssetDatabase.StartAssetEditing();

						var assetGUIDs = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", new string[] { "Assets" });
						foreach (var target in m_TMPFontReplace.inputs)
						{
							var result = EditorHelper.SearchAndReplaceGuid(target.targets, target.replace, assetGUIDs);

							foreach (var item in result)
								Debug.Log($"{target.replace.name} is replaced in {item.Value} Assets");
						}

						AssetDatabase.StopAssetEditing();
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}

					if (EditorHelper.Button("Search and replace in Scene"))
					{
						var objs = FindObjectsOfType<GameObject>();
						int count = 0;
						var objectsFound = new List<GameObject>();
						for (int i = 0; i < objs.Length; i++)
						{
							bool valid = false;
							var obj = objs[i];
							var txtsUI = obj.FindComponentsInChildren<TextMeshProUGUI>();
							foreach (var com in txtsUI)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
										EditorUtility.SetDirty(obj);
									}
							}

							var txts = obj.FindComponentsInChildren<TextMeshPro>();
							foreach (var com in txts)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
										EditorUtility.SetDirty(obj);
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
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
		
		private class ObjectReplace
		{
			private class Input
			{
				public readonly List<Object> targets = new List<Object>();
				public Object replace;
			}

			private readonly List<Input> m_inputs = new List<Input>();

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

		public class GenericObjectReplace<T> where T : Object
		{
			private class Input
			{
				public readonly List<T> targets = new List<T>();
				public T replace;
			}

			private readonly List<Input> m_inputs = new List<Input>();

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
		
		[MenuItem("RCore/Tools/Assets Replacer")]
		private static void OpenEditorWindow()
		{
			var window = GetWindow<AssetsReplacer>("Assets Replacer", true);
			window.Show();
		}
	}
}