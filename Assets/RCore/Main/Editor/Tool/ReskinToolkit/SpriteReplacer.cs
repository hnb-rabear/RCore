using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if !TPS_BYTE_DECIMAL && !TPS_DECIMAL_BYTE
using tpsByteByte;

#elif TPS_BYTE_DECIMAL
using tpsByteDecimal;
#elif TPS_DECIMAL_BYTE
using tpsDecimalByte;
#endif

namespace RCore.Editor.Tool
{
	[Serializable]
	public class SpriteReplacer
	{
		public enum Tps
		{
			ByteByte,
			ByteDecimal,
			DecimalByte
		}

		[Serializable]
		public class Input
		{
			public Sprite spriteNew;
			public Sprite spriteOriginal;
			public Vector2 PivotNew => spriteNew != null ? MathHelper.Round(spriteNew.NormalizedPivot(), 2) : Vector2.one * 0.5f;
			public Vector2 PivotOriginal => spriteOriginal != null ? MathHelper.Round(spriteOriginal.NormalizedPivot(), 2) : Vector2.one * 0.5f;
			public Vector2 SizeNew => spriteNew != null ? spriteNew.NativeSize() : Vector2.zero;
			public Vector2 SizeOriginal => spriteOriginal != null ? spriteOriginal.NativeSize() : Vector2.zero;
			public float TrisNew => spriteNew != null ? spriteNew.triangles.Length / 3f : 0;
			public float TrisOriginal => spriteOriginal != null ? spriteOriginal.triangles.Length / 3f : 0;

			public void Swap()
			{
				(spriteNew, spriteOriginal) = (spriteOriginal, spriteNew);
			}
		}

		[SerializeField] private List<Object> m_sourceObjects = new List<Object>();
		[SerializeField] private List<Input> m_inputs = new List<Input>();
		[SerializeField] private bool m_displayIcon;
		[SerializeField] private Object m_tpsSaveFile;

		[SerializeField] private bool m_findSameName = true;
		[SerializeField] private bool m_findSpritesInPrefab;
		[SerializeField] private bool m_findSpritesInTexture = true;
		[SerializeField] private bool m_detectNamePattern;
		[SerializeField] private Tps m_tps;
		[SerializeField] private bool m_displayNullR;

		public float GetTotalTrisNew()
		{
			float total = 0;
			foreach (var input in m_inputs)
				total += input.TrisNew;
			return total;
		}

		public float GetTotalTrisOld()
		{
			float total = 0;
			foreach (var input in m_inputs)
				total += input.TrisOriginal;
			return total;
		}

		public void Clear()
		{
			m_sourceObjects.Clear();
			m_inputs.Clear();
			m_tpsSaveFile = null;
		}

		public void RemoveNull()
		{
			for (int i = m_sourceObjects.Count - 1; i >= 0; i--)
				if (m_sourceObjects[i] == null)
					m_sourceObjects.RemoveAt(i);
			for (int i = m_inputs.Count - 1; i >= 0; i--)
			{
				if (m_inputs[i] == null || m_inputs[i].spriteNew == null || m_inputs[i].spriteOriginal == null)
					m_inputs.RemoveAt(i);
			}
		}

		public string[] GetGuidsFromSourceObject(string filter)
		{
			string[] assetGUIDs;
			if (m_sourceObjects != null && m_sourceObjects.Count > 0)
			{
				var tempAssetGUIDs = new List<string>();
				for (int i = 0; i < m_sourceObjects.Count; i++)
				{
					string path = AssetDatabase.GetAssetPath(m_sourceObjects[i]);
					if (AssetDatabase.IsValidFolder(path))
					{
						var guids = AssetDatabase.FindAssets(filter, new[] { path });
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
				assetGUIDs = AssetDatabase.FindAssets(filter, new[] { "Assets" });
			return assetGUIDs;
		}

		public void Draw()
		{
			var btnSwap = new EditorButton
			{
				label = "Swap",
				onPressed = () =>
				{
					foreach (var input in m_inputs)
						input.Swap();
				}
			};
			var btnRemoveAll = new EditorButton
			{
				label = "Remove All",
				color = Color.red,
				onPressed = Clear
			};
			var btnCopyLeftToRight = new EditorButton
			{
				label = "Copy Pivot L To R",
				onPressed = () =>
				{
					foreach (var input in m_inputs)
						EditorHelper.CopyPivotAndBorder(input.spriteNew, input.spriteOriginal, true);
				}
			};
			var btnCopyRightToLeft = new EditorButton
			{
				label = "Copy Pivot R To L",
				onPressed = () =>
				{
					foreach (var input in m_inputs)
						EditorHelper.CopyPivotAndBorder(input.spriteOriginal, input.spriteNew, true);
				}
			};
			if (EditorHelper.HeaderFoldout("Find And Replace Sprites", null))
			{
				EditorGUILayout.BeginVertical("box");
				{
					EditorHelper.DragDropBox<Object>("Searching Sources", objs =>
					{
						foreach (var obj in objs)
						{
							if (obj is AnimationClip || obj is GameObject || obj is ScriptableObject || obj is SceneAsset)
								if (!m_sourceObjects.Contains(obj))
									m_sourceObjects.Add(obj);
						}
					});
					EditorHelper.PagesForList(m_sourceObjects.Count, $"m_spriteReplace.sources", i =>
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.ObjectField(m_sourceObjects[i], typeof(Object), false);
							if (EditorHelper.ButtonColor("-", Color.red, 23))
								m_sourceObjects.RemoveAt(i);
						}
						EditorGUILayout.EndHorizontal();
					});

					EditorGUILayout.BeginHorizontal();
					{
						EditorHelper.DragDropBox<Object>("Left Textures Or Sprites", objs =>
						{
							var spritesNew = m_inputs.Select(x => x.spriteNew).ToList();
							var spritesOriginal = m_inputs.Select(x => x.spriteOriginal).ToList();
							foreach (var obj in objs)
							{
								if (m_findSpritesInTexture && obj is Texture2D || obj is Sprite)
								{
									string path = AssetDatabase.GetAssetPath(obj);
									var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
									foreach (var sprite in sprites)
									{
										if (!spritesNew.Contains(sprite) && !spritesOriginal.Contains(sprite))
										{
											m_inputs.Add(new Input
											{
												spriteNew = sprite
											});
											spritesNew.Add(sprite);
										}
									}
								}
								else if (m_findSpritesInPrefab && obj is GameObject)
								{
									var images = (obj as GameObject).GetComponentsInChildren<Image>(true);
									foreach (var img in images)
									{
										if (img.sprite != null && !spritesNew.Contains(img.sprite) && !spritesOriginal.Contains(img.sprite))
										{
											m_inputs.Add(new Input
											{
												spriteNew = img.sprite
											});
											spritesNew.Add(img.sprite);
										}
									}
									var renderers = (obj as GameObject).GetComponentsInChildren<SpriteRenderer>(true);
									foreach (var renderer in renderers)
									{
										if (renderer.sprite != null && !spritesNew.Contains(renderer.sprite) && !spritesOriginal.Contains(renderer.sprite))
										{
											m_inputs.Add(new Input
											{
												spriteNew = renderer.sprite
											});
											spritesNew.Add(renderer.sprite);
										}
									}
								}
							}
						});

						if (m_inputs.Count > 0)
						{
							EditorHelper.DragDropBox<Object>("Right Textures or Sprites", objs =>
							{
								EditorHelper.ClearObjectFolderCaches();
								var spritesNew = m_inputs.Select(x => x.spriteNew).ToList();
								foreach (var obj in objs)
								{
									if (obj is Texture2D || obj is Sprite)
									{
										string path = AssetDatabase.GetAssetPath(obj);
										var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
										foreach (var sprite in sprites)
										{
											foreach (var input in m_inputs)
											{
												if (m_findSameName)
												{
													if (!spritesNew.Contains(sprite) && input.spriteNew.name == sprite.name)
													{
														input.spriteOriginal = sprite;
														break;
													}
													if (!spritesNew.Contains(sprite) && input.spriteNew.name.EndsWith($"{EditorHelper.GetObjectFolderName(sprite)}-{sprite.name}"))
													{
														input.spriteOriginal = sprite;
														break;
													}
													if (!spritesNew.Contains(sprite) && sprite.name.EndsWith($"{EditorHelper.GetObjectFolderName(input.spriteNew)}-{input.spriteNew.name}"))
													{
														input.spriteOriginal = sprite;
														break;
													}
												}
												else if (input.spriteNew != null && input.spriteOriginal == null)
												{
													input.spriteOriginal = sprite;
													break;
												}
											}
										}
									}
								}
							});
						}
					}
					EditorGUILayout.EndHorizontal();

					m_findSameName = EditorHelper.Toggle(m_findSameName, "Find same name", 200);
					m_findSpritesInPrefab = EditorHelper.Toggle(m_findSpritesInPrefab, "Find sprites in prefabs", 200);
					m_findSpritesInTexture = EditorHelper.Toggle(m_findSpritesInTexture, "Find sprites in textures", 200);

					EditorGUILayout.BeginHorizontal();
					{
						EditorHelper.GridDraws(4, new List<IDraw>
						{
							btnCopyLeftToRight,
							btnCopyRightToLeft,
							btnSwap,
							btnRemoveAll,
						});
						float totalTrisOld = GetTotalTrisOld();
						if (totalTrisOld > 0)
						{
							float trisRatio = GetTotalTrisNew() * 1f / totalTrisOld;
							if (trisRatio > 1)
								EditorHelper.LabelField(trisRatio.ToString("0.00"), 101, pTextColor: Color.red);
							else if (Math.Abs(trisRatio - 1f) < 0.01f)
								EditorHelper.LabelField(trisRatio.ToString("0.00"), 101, pTextColor: Color.yellow);
							else EditorHelper.LabelField(trisRatio.ToString("0.00"), 101, pTextColor: Color.green);
						}
					}
					EditorGUILayout.EndHorizontal();

					EditorHelper.BoxVertical(null, () =>
					{
						EditorGUILayout.BeginHorizontal();
						{
							m_tpsSaveFile = EditorHelper.ObjectField<Object>(m_tpsSaveFile, ".tps file", 50, 150);
							m_detectNamePattern = EditorHelper.Toggle(m_detectNamePattern, "Detect name pattern", 120, 30);
							if (EditorHelper.Button("Transfer Pivots", 100))
							{
								var originalSprites = m_inputs.Select(x => x.spriteOriginal).ToList();
								string projectPath = Application.dataPath.Replace("/Assets", "");
								var path = projectPath + "\\" + AssetDatabase.GetAssetPath(m_tpsSaveFile);
								var errorLogs = new List<string>();

								var obj = EditorHelper.LoadXMLFile<data>(path);
								var dataStructMap = obj.@struct.Items[91] as dataStructMap;
								for (var index = 0; index < dataStructMap.Items.Length - 1; index++)
								{
									int ii = index;
									var dataStructMapKey = dataStructMap.Items[ii] as dataStructMapKey;
									if (dataStructMapKey != null)
									{
										var names = dataStructMapKey.Value.Split('/');
										string fileName = Path.GetFileNameWithoutExtension(names[names.Length - 1]);
										var dataStructMapStruct = dataStructMap.Items[ii + 1] as dataStructMapStruct;

										if (dataStructMapStruct == null)
											for (int j = ii + 2; j < dataStructMap.Items.Length; j++)
											{
												dataStructMapStruct = dataStructMap.Items[j] as dataStructMapStruct;
												if (dataStructMapStruct == null)
													continue;

												var tempList = dataStructMap.Items.ToList();
												var dataStructMapStructTemp = new dataStructMapStruct(dataStructMapStruct);
												tempList.Insert(ii + 1, dataStructMapStructTemp);
												dataStructMap.Items = tempList.ToArray();
												dataStructMapStruct = dataStructMapStructTemp;
												break;
											}

										if (dataStructMapStruct != null)
										{
											//var pivots = dataStructMapStruct.Items[1].ToString().Split(',');
											//var folderName = names[names.Length - 2];
											//var pivot = new Vector2(float.Parse(pivots[0]), 1 - float.Parse(pivots[1]));
											index = ii;
											var defaultPivot = new Vector2(0.5f, 0.5f);
											foreach (var sprite in originalSprites)
											{
												var originalPivot = sprite.NormalizedPivot();
												if (sprite.name == fileName || (originalPivot != defaultPivot && m_detectNamePattern
														&& RemoveEndNumber(sprite.name) == RemoveEndNumber(fileName)))
												{
													dataStructMapStruct.Items[1] = $"{originalPivot.x},{1 - originalPivot.y}";
													Debug.Log($"overwrite pivot in sprite {fileName}, pivot:{originalPivot}");
													break;
												}
											}
										}
										else
										{
											foreach (var sprite in originalSprites)
											{
												if (sprite.name == fileName)
												{
													var originalPivot = sprite.NormalizedPivot();
													if (originalPivot != new Vector2(0.5f, 0.5f))
													{
														errorLogs.Add($"{fileName} x:{originalPivot.x},y:{originalPivot.y}");
														Debug.LogError($"Not found data of sprite {dataStructMapKey.Value} pivot x:{originalPivot.x} y:{originalPivot.y}");
													}
													break;
												}
											}
										}
									}
								}

								path = EditorHelper.SaveFilePanel("Save File", Path.GetDirectoryName(path), $"{Path.GetFileName(path)}", "tps");
								if (!string.IsNullOrEmpty(path))
								{
									EditorHelper.SaveXMLFile(path, obj);

									if (errorLogs.Count > 0)
										File.WriteAllLines($"{path}.error.txt", errorLogs);
								}
							}
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						{
							m_tps = EditorHelper.DropdownListEnum(m_tps, "Tps data type");
							if (EditorHelper.Button("Change"))
							{
								foreach (Tps val in Enum.GetValues(typeof(Tps)))
									if (val != m_tps)
										EditorHelper.RemoveDirective(GetDirective(val));
								EditorHelper.AddDirective(GetDirective(m_tps));
							}
						}
						EditorGUILayout.EndHorizontal();
					}, Color.yellow, true);

					if (m_inputs.Count > 0)
					{
						var togDisplayIcon = new EditorToggle
						{
							label = "Display icon",
							value = m_displayIcon,
						};
						var togDisplayNullR = new EditorToggle
						{
							label = "Display null R",
							value = m_displayNullR,
						};
						var btnQuickFillNullR = new EditorButton
						{
							label = "Quick fill null R",
							onPressed = () =>
							{
								var spriteGuids = GetGuidsFromSourceObject("t:Sprite");
								foreach (var guid in spriteGuids)
								{
									var path = AssetDatabase.GUIDToAssetPath(guid);
									var obj = AssetDatabase.LoadAssetAtPath<Sprite>(path);
									if (obj != null)
									{
										foreach (var input in m_inputs)
										{
											if (input.spriteOriginal == null
												&& input.spriteNew.name == obj.name
												&& input.spriteNew.GetInstanceID() != obj.GetInstanceID())
											{
												input.spriteOriginal = obj;
											}
										}
									}
								}
							},
							color = Color.yellow,
						};
						var btnRemoveNull = new EditorButton
						{
							label = "Remove Null",
							color = Color.red,
							onPressed = RemoveNull
						};
						EditorHelper.GridDraws(2, new List<IDraw> { togDisplayIcon, togDisplayNullR, btnQuickFillNullR, btnRemoveNull }, Color.yellow);
						m_displayNullR = togDisplayNullR.OutputValue;
						m_displayIcon = togDisplayIcon.OutputValue;
					}
					var tempInputs = m_inputs;
					if (m_displayNullR)
					{
						tempInputs = new List<Input>();
						foreach (var input in m_inputs)
						{
							if (input.spriteOriginal == null)
								tempInputs.Add(input);
						}
					}
					EditorHelper.PagesForList(tempInputs.Count, "m_spriteReplace.inputs", i =>
					{
						var input = tempInputs[i];
						EditorGUILayout.BeginHorizontal();
						{
							if (m_displayIcon)
								input.spriteNew = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteNew, "", 0, 60, showAsBox: true);
							EditorGUILayout.BeginVertical();
							{
								if (!m_displayIcon)
									input.spriteNew = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteNew, "");
								else
									EditorHelper.LabelField(input.spriteNew != null ? input.spriteNew.name : "", 101);
								EditorHelper.LabelField($"t:{input.TrisNew}, p:{input.PivotNew.x},{input.PivotNew.y}", 101);
								EditorHelper.LabelField($"{input.SizeNew.x},{input.SizeNew.y}", 101);
							}
							EditorGUILayout.EndVertical();

							if (m_displayIcon)
								input.spriteOriginal = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteOriginal, "", 0, 60, showAsBox: true);
							EditorGUILayout.BeginVertical();
							{
								if (!m_displayIcon)
									input.spriteOriginal = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteOriginal, "");
								else
									EditorHelper.LabelField(input.spriteOriginal != null ? input.spriteOriginal.name : "", 101);
								EditorHelper.LabelField($"t:{input.TrisOriginal}, p:{input.PivotOriginal.x},{input.PivotOriginal.y}", 101);
								EditorHelper.LabelField($"{input.SizeOriginal.x},{input.SizeOriginal.y}", 101);
							}
							EditorGUILayout.EndVertical();

							if (input.spriteOriginal)
							{
								EditorGUILayout.BeginVertical();
								{
									float ratio = input.TrisNew * 1f / input.TrisOriginal;
									if (ratio > 1)
										EditorHelper.LabelField(ratio.ToString("0.00"), 101, pTextColor: Color.red);
									else if (Math.Abs(ratio - 1f) < 0.01f)
										EditorHelper.LabelField(ratio.ToString("0.00"), 101, pTextColor: Color.yellow);
									else EditorHelper.LabelField(ratio.ToString("0.00"), 101, pTextColor: Color.green);
									if (input.PivotNew != input.PivotOriginal)
										EditorHelper.LabelField("!pivot", 101, pTextColor: Color.red);
								}
								EditorGUILayout.EndVertical();
							}

							EditorGUILayout.BeginVertical();
							{
								if (EditorHelper.ButtonColor("Remove", Color.red, 80))
									m_inputs.Remove(input);
								if (input.spriteOriginal && EditorHelper.ButtonColor("Pivot L->R", Color.white, 80))
									EditorHelper.CopyPivotAndBorder(input.spriteNew, input.spriteOriginal, true);
								if (input.spriteOriginal && EditorHelper.ButtonColor("Pivot R->L", Color.white, 80))
									EditorHelper.CopyPivotAndBorder(input.spriteOriginal, input.spriteNew, true);
							}
							EditorGUILayout.EndVertical();
						}
						EditorGUILayout.EndHorizontal();
					});

					if (m_inputs.Count > 0)
						if (EditorHelper.Button("Find And Replace R by L"))
						{
							for (int i = 0; i < m_inputs.Count; i++)
							{
								var target = m_inputs[i];
								if (target.spriteNew == null)
								{
									m_inputs.Remove(target);
									i--;
								}
							}

							if (m_inputs.Count == 0)
								return;

							AssetDatabase.StartAssetEditing();

							string[] assetGUIDs = GetGuidsFromSourceObject("t:GameObject t:ScriptableObject t:Scene t:AnimationClip");
							var cacheObjects = m_inputs.Select(x => x.spriteOriginal).ToList();
							EditorHelper.BuildReferenceMapCache(assetGUIDs, cacheObjects);
							foreach (var target in m_inputs)
							{
								var result = EditorHelper.SearchAndReplaceGuid(new List<Sprite> { target.spriteOriginal }, target.spriteNew, assetGUIDs);
								foreach (var item in result)
									Debug.Log($"{target.spriteNew.name} is replaced in {item.Value} Assets");
							}
							AssetDatabase.StopAssetEditing();
							AssetDatabase.SaveAssets();
							AssetDatabase.Refresh();
						}
				}

				EditorGUILayout.EndVertical();
			}
		}

		private static string RemoveEndNumber(string input)
		{
			const string pattern = @"\d+$";
			const string replacement = "";
			var rgx = new Regex(pattern);
			string result = rgx.Replace(input, replacement);
			return result;
		}

		public static string GetDirective(Tps type)
		{
			switch (type)
			{
				case Tps.ByteByte: return "";
				case Tps.ByteDecimal: return "TPS_BYTE_DECIMAL";
				case Tps.DecimalByte: return "TPS_DECIMAL_BYTE";
			}
			return "";
		}
	}
}