using RCore.Common;
using RCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if !TPS_BYTE_DECIMAL && !TPS_DECIMAL_BYTE
using tpsByteByte;

#elif TPS_BYTE_DECIMAL
using tpsByteDecimal;
#elif TPS_DECIMAL_BYTE
using tpsDecimalByte;
#endif

namespace RCore.Editor
{
	public enum YesNoNone
	{
		None = 0,
		No = 1,
		Yes = 2,
	}

	[Serializable]
	public class SpriteReplace
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

		public List<Object> sourceObjects = new List<Object>();
		public List<Input> inputs = new List<Input>();
		public bool displayIcon;
		public Object tps;
		public bool findSameName = true;
		public bool findSpritesInPrefab;
		public bool findSpritesInTexture = true;
		public bool detectNamePattern;
		public List<Texture2D> spriteSheets;

		public float GetTotalTrisNew()
		{
			float total = 0;
			foreach (var input in inputs)
				total += input.TrisNew;
			return total;
		}

		public float GetTotalTrisOld()
		{
			float total = 0;
			foreach (var input in inputs)
				total += input.TrisOriginal;
			return total;
		}

		public void Clear()
		{
			sourceObjects.Clear();
			inputs.Clear();
			tps = null;
			spriteSheets.Clear();
		}

		public void RemoveNull()
		{
			for (int i = sourceObjects.Count - 1; i >= 0; i--)
				if (sourceObjects[i] == null)
					sourceObjects.RemoveAt(i);
			for (int i = inputs.Count - 1; i >= 0; i--)
			{
				if (inputs[i] == null || inputs[i].spriteNew == null || inputs[i].spriteOriginal == null)
					inputs.RemoveAt(i);
			}
			for (int i = spriteSheets.Count - 1; i >= 0; i--)
				if (spriteSheets[i] == null)
					spriteSheets.RemoveAt(i);
		}

		public string[] GetGuidsFromSourceObject(string filter)
		{
			string[] assetGUIDs;
			if (sourceObjects != null && sourceObjects.Count > 0)
			{
				var tempAssetGUIDs = new List<string>();
				for (int i = 0; i < sourceObjects.Count; i++)
				{
					string path = AssetDatabase.GetAssetPath(sourceObjects[i]);
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
	}

	[Serializable]
	public class SpriteUtilities
	{
		public bool displayIcon;
		public List<Sprite> sprites = new List<Sprite>();
		public List<GameObject> targets = new List<GameObject>();
		public YesNoNone useSpriteMesh;
		public YesNoNone raycastTarget;
		public YesNoNone maskable;
		public JustButton.PerfectRatio perfectRatio;
		public bool CanSubmit()
		{
			return useSpriteMesh != YesNoNone.None || raycastTarget != YesNoNone.None || maskable != YesNoNone.None || perfectRatio != JustButton.PerfectRatio.None;
		}
	}

	public class AssetsReplacer : ScriptableObject
	{
		public static readonly string FilePath = $"Assets/Editor/{nameof(AssetsReplacer)}Cache.asset";
		public SpriteReplace spriteReplace;
		public SpriteUtilities spriteUtilities;

		public static AssetsReplacer Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FilePath, typeof(AssetsReplacer)) as AssetsReplacer;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<AssetsReplacer>(FilePath);
			return collection;
		}
	}

	public class AssetsReplacerWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private AssetsReplacer m_assetsReplacer;
		private SpriteReplace m_spriteReplace;
		private SpriteUtilities m_spriteUtilities;
		private string m_tab;
		private SpriteReplace.Tps m_tps;
		private bool m_displayNullR;
		// private ObjectReplace m_objectReplace;

		private void OnEnable() { }

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			m_assetsReplacer ??= AssetsReplacer.Load();
			m_spriteReplace = m_assetsReplacer.spriteReplace;
			m_spriteUtilities = m_assetsReplacer.spriteUtilities;

			m_tab = EditorHelper.Tabs("m_assetsReplacer.spriteReplace", "Sprites Replacer", "Export sprites from sheet", "Sprite Utilities");
			GUILayout.BeginVertical("box");
			switch (m_tab)
			{
				case "Sprites Replacer":
					DrawSpriteReplacer();
					break;
				case "Export sprites from sheet":
					DrawExportSpritesFromSheet();
					break;
				case "Sprite Utilities":
					DrawSpriteUtilities();
					break;
			}
			GUILayout.EndVertical();

			// SearchAndReplaceFont();
			// SearchAndReplaceTMPFont();
			// m_objectReplace ??= new ObjectReplace();
			// m_objectReplace.Draw();

			GUILayout.EndScrollView();
		}

		private string RemoveEndNumber(string input)
		{
			string pattern = @"\d+$";
			string replacement = "";
			var rgx = new Regex(pattern);
			string result = rgx.Replace(input, replacement);
			return result;
		}

#region SpriteReplace

		private void DrawSpriteReplacer()
		{
			var btnSwap = new EditorButton
			{
				label = "Swap",
				onPressed = () =>
				{
					foreach (var input in m_spriteReplace.inputs)
						input.Swap();
				}
			};
			var btnRemoveAll = new EditorButton
			{
				label = "Remove All",
				color = Color.red,
				onPressed = () => m_spriteReplace.Clear()
			};
			var btnCopyLeftToRight = new EditorButton
			{
				label = "Copy Pivot L To R",
				onPressed = () =>
				{
					foreach (var input in m_spriteReplace.inputs)
						EditorHelper.CopyPivotAndBorder(input.spriteNew, input.spriteOriginal, true);
				}
			};
			var btnCopyRightToLeft = new EditorButton
			{
				label = "Copy Pivot R To L",
				onPressed = () =>
				{
					foreach (var input in m_spriteReplace.inputs)
						EditorHelper.CopyPivotAndBorder(input.spriteOriginal, input.spriteNew, true);
				}
			};
			if (EditorHelper.HeaderFoldout("Search And Replace Sprites", null))
			{
				EditorGUILayout.BeginVertical("box");
				{
					EditorHelper.DragDropBox<Object>("Searching Sources", objs =>
					{
						foreach (var obj in objs)
						{
							if (obj is AnimationClip || obj is GameObject || obj is ScriptableObject || obj is SceneAsset)
								if (!m_spriteReplace.sourceObjects.Contains(obj))
									m_spriteReplace.sourceObjects.Add(obj);
						}
					});
					EditorHelper.PagesForList(m_spriteReplace.sourceObjects.Count, $"m_spriteReplace.sources", i =>
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.ObjectField(m_spriteReplace.sourceObjects[i], typeof(Object), false);
							if (EditorHelper.ButtonColor("-", Color.red, 23))
								m_spriteReplace.sourceObjects.RemoveAt(i);
						}
						EditorGUILayout.EndHorizontal();
					});

					EditorGUILayout.BeginHorizontal();
					{
						EditorHelper.DragDropBox<Object>("Left Textures Or Sprites", objs =>
						{
							var spritesNew = m_spriteReplace.inputs.Select(x => x.spriteNew).ToList();
							var spritesOriginal = m_spriteReplace.inputs.Select(x => x.spriteOriginal).ToList();
							foreach (var obj in objs)
							{
								if (m_spriteReplace.findSpritesInTexture && obj is Texture2D || obj is Sprite)
								{
									string path = AssetDatabase.GetAssetPath(obj);
									var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
									foreach (var sprite in sprites)
									{
										if (!spritesNew.Contains(sprite) && !spritesOriginal.Contains(sprite))
										{
											m_spriteReplace.inputs.Add(new SpriteReplace.Input
											{
												spriteNew = sprite
											});
											spritesNew.Add(sprite);
										}
									}
								}
								else if (m_spriteReplace.findSpritesInPrefab && obj is GameObject)
								{
									var images = (obj as GameObject).GetComponentsInChildren<Image>();
									foreach (var img in images)
									{
										if (img.sprite != null && !spritesNew.Contains(img.sprite) && !spritesOriginal.Contains(img.sprite))
										{
											m_spriteReplace.inputs.Add(new SpriteReplace.Input
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
											m_spriteReplace.inputs.Add(new SpriteReplace.Input
											{
												spriteNew = renderer.sprite
											});
											spritesNew.Add(renderer.sprite);
										}
									}
								}
							}
						});

						if (m_spriteReplace.inputs.Count > 0)
						{
							EditorHelper.DragDropBox<Object>("Right Textures or Sprites", objs =>
							{
								var spritesNew = m_spriteReplace.inputs.Select(x => x.spriteNew).ToList();
								foreach (var obj in objs)
								{
									if (obj is Texture2D || obj is Sprite)
									{
										string path = AssetDatabase.GetAssetPath(obj);
										var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
										foreach (var sprite in sprites)
										{
											foreach (var input in m_spriteReplace.inputs)
											{
												if (m_spriteReplace.findSameName)
												{
													if (!spritesNew.Contains(sprite) && input.spriteNew.name == sprite.name)
													{
														input.spriteOriginal = sprite;
														break;
													}
													if (!spritesNew.Contains(sprite) && input.spriteNew.name.Contains($"{EditorHelper.GetObjectFolderName(sprite)}-{sprite.name}"))
													{
														input.spriteOriginal = sprite;
														break;
													}
													if (!spritesNew.Contains(sprite) && sprite.name.Contains($"{EditorHelper.GetObjectFolderName(input.spriteNew)}-{input.spriteNew.name}"))
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

					m_spriteReplace.findSameName = EditorHelper.Toggle(m_spriteReplace.findSameName, "Find same name", 200);
					m_spriteReplace.findSpritesInPrefab = EditorHelper.Toggle(m_spriteReplace.findSpritesInPrefab, "Find sprites in prefabs", 200);
					m_spriteReplace.findSpritesInTexture = EditorHelper.Toggle(m_spriteReplace.findSpritesInTexture, "Find sprites in textures", 200);

					EditorGUILayout.BeginHorizontal();
					{
						EditorHelper.GridDraws(4, new List<IDraw>
						{
							btnCopyLeftToRight,
							btnCopyRightToLeft,
							btnSwap,
							btnRemoveAll,
						});
						float totalTrisOld = m_spriteReplace.GetTotalTrisOld();
						if (totalTrisOld > 0)
						{
							float trisRatio = m_spriteReplace.GetTotalTrisNew() * 1f / totalTrisOld;
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
							m_spriteReplace.tps = EditorHelper.ObjectField<Object>(m_spriteReplace.tps, "tps", 50, 150);
							m_spriteReplace.detectNamePattern = EditorHelper.Toggle(m_spriteReplace.detectNamePattern, "Detect name pattern", 120, 30);
							if (EditorHelper.Button("Transfer Pivots", 100))
							{
								var originalSprites = m_spriteReplace.inputs.Select(x => x.spriteOriginal).ToList();
								string projectPath = Application.dataPath.Replace("/Assets", "");
								var path = projectPath + "\\" + AssetDatabase.GetAssetPath(m_spriteReplace.tps);
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
												if (sprite.name == fileName || (originalPivot != defaultPivot && m_spriteReplace.detectNamePattern
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

								path = EditorUtility.SaveFilePanel("Save File", Path.GetDirectoryName(path), $"{Path.GetFileName(path)}", "tps");
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
								foreach (SpriteReplace.Tps val in Enum.GetValues(typeof(SpriteReplace.Tps)))
									if (val != m_tps)
										EditorHelper.RemoveDirective(GetDirective(val));
								EditorHelper.AddDirective(GetDirective(m_tps));
							}
						}
						EditorGUILayout.EndHorizontal();
					}, Color.yellow, true);

					if (m_spriteReplace.inputs.Count > 0)
					{
						var togDisplayIcon = new EditorToggle
						{
							label = "Display icon",
							value = m_spriteReplace.displayIcon,
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
								var spriteGuids = m_spriteReplace.GetGuidsFromSourceObject("t:Sprite");
								foreach (var guid in spriteGuids)
								{
									var path = AssetDatabase.GUIDToAssetPath(guid);
									var obj = AssetDatabase.LoadAssetAtPath<Sprite>(path);
									if (obj != null)
									{
										foreach (var input in m_spriteReplace.inputs)
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
							onPressed = () => m_spriteReplace.RemoveNull()
						};
						EditorHelper.GridDraws(2, new List<IDraw> { togDisplayIcon, togDisplayNullR, btnQuickFillNullR, btnRemoveNull }, Color.yellow);
						m_displayNullR = togDisplayNullR.OutputValue;
						m_spriteReplace.displayIcon = togDisplayIcon.OutputValue;
					}
					var inputs = m_spriteReplace.inputs;
					if (m_displayNullR)
					{
						inputs = new List<SpriteReplace.Input>();
						foreach (var input in m_spriteReplace.inputs)
						{
							if (input.spriteOriginal == null)
								inputs.Add(input);
						}
					}
					EditorHelper.PagesForList(inputs.Count, "m_spriteReplace.inputs", i =>
					{
						var input = inputs[i];
						EditorGUILayout.BeginHorizontal();
						{
							if (m_spriteReplace.displayIcon)
								input.spriteNew = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteNew, "", 0, 60, showAsBox: true);
							EditorGUILayout.BeginVertical();
							{
								if (!m_spriteReplace.displayIcon)
									input.spriteNew = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteNew, "");
								else
									EditorHelper.LabelField(input.spriteNew != null ? input.spriteNew.name : "", 101);
								EditorHelper.LabelField($"t:{input.TrisNew}, p:{input.PivotNew.x},{input.PivotNew.y}", 101);
								EditorHelper.LabelField($"{input.SizeNew.x},{input.SizeNew.y}", 101);
							}
							EditorGUILayout.EndVertical();

							if (m_spriteReplace.displayIcon)
								input.spriteOriginal = (Sprite)EditorHelper.ObjectField<Sprite>(input.spriteOriginal, "", 0, 60, showAsBox: true);
							EditorGUILayout.BeginVertical();
							{
								if (!m_spriteReplace.displayIcon)
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
									m_spriteReplace.inputs.Remove(input);
								if (input.spriteOriginal && EditorHelper.ButtonColor("Pivot L->R", Color.white, 80))
									EditorHelper.CopyPivotAndBorder(input.spriteNew, input.spriteOriginal, true);
								if (input.spriteOriginal && EditorHelper.ButtonColor("Pivot R->L", Color.white, 80))
									EditorHelper.CopyPivotAndBorder(input.spriteOriginal, input.spriteNew, true);
							}
							EditorGUILayout.EndVertical();
						}
						EditorGUILayout.EndHorizontal();
					});

					if (m_spriteReplace.inputs.Count > 0)
						if (EditorHelper.Button("Search and replace R by L"))
						{
							for (int i = 0; i < m_spriteReplace.inputs.Count; i++)
							{
								var target = m_spriteReplace.inputs[i];
								if (target.spriteNew == null)
								{
									m_spriteReplace.inputs.Remove(target);
									i--;
								}
							}

							if (m_spriteReplace.inputs.Count == 0)
								return;

							AssetDatabase.StartAssetEditing();

							string[] assetGUIDs = m_spriteReplace.GetGuidsFromSourceObject("t:GameObject t:ScriptableObject t:Scene t:AnimationClip");
							var cacheObjects = m_spriteReplace.inputs.Select(x => x.spriteOriginal).ToList();
							EditorHelper.BuildReferenceMapCache(assetGUIDs, cacheObjects);
							foreach (var target in m_spriteReplace.inputs)
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

		private void DrawExportSpritesFromSheet()
		{
			m_spriteReplace.spriteSheets ??= new List<Texture2D>();
			EditorHelper.DragDropBox<Texture2D>("Sprite sheet", objs =>
			{
				foreach (var obj in objs)
				{
					if (!m_spriteReplace.spriteSheets.Contains(obj))
						m_spriteReplace.spriteSheets.Add(obj);
				}
			});
			if (m_spriteReplace.spriteSheets.Count > 0)
			{
				for (int i = 0; i < m_spriteReplace.spriteSheets.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					var spriteSheet = m_spriteReplace.spriteSheets[i];
					spriteSheet = (Texture2D)EditorHelper.ObjectField<Texture2D>(spriteSheet, null);
					if (spriteSheet != null)
					{
						if (EditorHelper.Button("Export sprites", 100))
						{
							EditorHelper.ExportSpritesFromSpriteSheet(spriteSheet);
						}
						if (EditorHelper.ButtonColor("x", Color.red, 23))
						{
							m_spriteReplace.spriteSheets.RemoveAt(i);
							i--;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				if (EditorHelper.Button("Export sprites"))
				{
					foreach (var spriteSheet in m_spriteReplace.spriteSheets)
						EditorHelper.ExportSpritesFromSpriteSheet(spriteSheet);
				}
				if (EditorHelper.Button("Sort"))
					m_spriteReplace.spriteSheets = m_spriteReplace.spriteSheets.OrderBy(x => x.name).ToList();
				if (EditorHelper.Button("Remove sprite sheets"))
					m_spriteReplace.spriteSheets.Clear();
			}
		}

		private void DrawSpriteUtilities()
		{
			var btnClearSprites = new EditorButton
			{
				label = "Clear sprites",
				onPressed = () => m_spriteUtilities.sprites.Clear(),
				color = Color.red
			};
			var btnClearTargets = new EditorButton
			{
				label = "Clear targets",
				onPressed = () => m_spriteUtilities.targets.Clear(),
				color = Color.red
			};
			m_spriteUtilities.sprites ??= new List<Sprite>();
			EditorHelper.DragDropBox<Object>("Sprite or Texture", objs =>
			{
				foreach (var obj in objs)
				{
					if (obj is Texture2D || obj is Sprite)
					{
						string path = AssetDatabase.GetAssetPath(obj);
						var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
						foreach (var sprite in sprites)
							if (!m_spriteUtilities.sprites.Contains(sprite))
								m_spriteUtilities.sprites.Add(sprite);
					}
					else if (obj is GameObject)
					{
						var images = (obj as GameObject).GetComponentsInChildren<Image>();
						foreach (var image in images)
							if (!m_spriteUtilities.sprites.Contains(image.sprite))
								m_spriteUtilities.sprites.Add(image.sprite);
						// var spriteRenderers = (obj as GameObject).GetComponentsInChildren<SpriteRenderer>();
						// foreach (var image in spriteRenderers)
						//     if (!m_spriteUtilities.sprites.Contains(image.sprite))
						//         m_spriteUtilities.sprites.Add(image.sprite);
					}
				}
			});
			m_spriteUtilities.displayIcon = EditorHelper.Toggle(m_spriteUtilities.displayIcon, "Display icon");
			EditorHelper.PagesForList(m_spriteUtilities.sprites.Count, $"m_spriteUtilities.sprites", i =>
			{
				EditorGUILayout.BeginHorizontal();
				{
					if (m_spriteUtilities.displayIcon)
					{
						m_spriteUtilities.sprites[i] = (Sprite)EditorHelper.ObjectField<Sprite>(m_spriteUtilities.sprites[i], "", 0, 60, showAsBox: true);
						EditorHelper.TextField(m_spriteUtilities.sprites[i] != null ? m_spriteUtilities.sprites[i].name : "", "", 0, 200);
					}
					else
						m_spriteUtilities.sprites[i] = (Sprite)EditorHelper.ObjectField<Sprite>(m_spriteUtilities.sprites[i], "");
					if (EditorHelper.ButtonColor("-", Color.red, 23))
						m_spriteUtilities.sprites.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}, new IDraw[] { btnClearSprites }, new IDraw[] { btnClearSprites });
			EditorHelper.DragDropBox<Object>("Searching sources", objs =>
			{
				foreach (var obj in objs)
				{
					if (obj is GameObject gameObject)
						if (!m_spriteUtilities.targets.Contains(obj))
							m_spriteUtilities.targets.Add(gameObject);
				}
			});
			EditorGUILayout.HelpBox("If the searching sources is empty, the tool will scan all the objects in Assets folder", MessageType.Info);
			EditorHelper.PagesForList(m_spriteUtilities.targets.Count, $"m_spriteUtilities.targets", i =>
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.ObjectField(m_spriteUtilities.targets[i], typeof(Object), false);
					if (EditorHelper.ButtonColor("-", Color.red, 23))
						m_spriteUtilities.targets.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}, new IDraw[] { btnClearTargets }, new IDraw[] { btnClearTargets });

			if (m_spriteUtilities.sprites.Count > 0)
			{
				m_spriteUtilities.raycastTarget = EditorHelper.DropdownListEnum(m_spriteUtilities.raycastTarget, "Raycast target", 100);
				m_spriteUtilities.useSpriteMesh = EditorHelper.DropdownListEnum(m_spriteUtilities.useSpriteMesh, "Use sprite mesh", 100);
				m_spriteUtilities.maskable = EditorHelper.DropdownListEnum(m_spriteUtilities.maskable, "Maskable", 100);
				m_spriteUtilities.perfectRatio = EditorHelper.DropdownListEnum(m_spriteUtilities.perfectRatio, "Perfect ratio", 100);
				if (EditorHelper.Button("Find images and apply"))
				{
					var targets = m_spriteUtilities.targets;
					if (!m_spriteUtilities.CanSubmit())
					{
						EditorUtility.DisplayDialog("Error", "Please select an option", "OK");
						return;
					}
					targets ??= new List<GameObject>();
					if (targets.Count == 0)
					{
						bool ok = EditorUtility.DisplayDialog("Error", "If the searching sources is empty, the tool will scan all the objects in Assets folder", "Scan all", "Cancel");
						if (!ok)
							return;
						var assetGUIDs = AssetDatabase.FindAssets("t:GameObject", new string[] { "Assets" });
						foreach (var guiD in assetGUIDs)
						{
							var path = AssetDatabase.GUIDToAssetPath(guiD);
							var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
							targets.Add(obj);
						}
					}
					foreach (var target in targets)
					{
						bool dirty = false;
						var images = target.GetComponentsInChildren<Image>(true);
						foreach (var image in images)
						{
							if (m_spriteUtilities.sprites.Contains(image.sprite))
							{
								if (m_spriteUtilities.raycastTarget != YesNoNone.None)
									image.raycastTarget = m_spriteUtilities.raycastTarget == YesNoNone.Yes;
								if (m_spriteUtilities.useSpriteMesh != YesNoNone.None)
									image.useSpriteMesh = m_spriteUtilities.useSpriteMesh == YesNoNone.Yes;
								if (m_spriteUtilities.maskable != YesNoNone.None)
									image.maskable = m_spriteUtilities.maskable == YesNoNone.Yes;
								if (m_spriteUtilities.perfectRatio == JustButton.PerfectRatio.Height)
									RUtil.PerfectRatioImageByHeight(image);
								else if (m_spriteUtilities.perfectRatio == JustButton.PerfectRatio.Width)
									RUtil.PerfectRatioImagesByWidth(image);
								dirty = true;
							}
						}
						if (dirty)
							EditorUtility.SetDirty(target);
					}
				}
			}
			else
				EditorGUILayout.HelpBox("Missing Sprites or Textures to the box,\n Missing searching sources", MessageType.Warning);
		}

		public static string GetDirective(SpriteReplace.Tps type)
		{
			switch (type)
			{
				case SpriteReplace.Tps.ByteByte: return "";
				case SpriteReplace.Tps.ByteDecimal: return "TPS_BYTE_DECIMAL";
				case SpriteReplace.Tps.DecimalByte: return "TPS_DECIMAL_BYTE";
			}
			return "";
		}

#endregion

#region FontReplace

		public class FontReplace
		{
			public class Input
			{
				public readonly List<Font> targets = new List<Font>();
				public Font replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}

		private FontReplace m_fontReplace = new FontReplace();

		private void SearchAndReplaceFont()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_fontReplace.inputs.Add(new FontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_fontReplace ??= new FontReplace();

					foreach (var target in m_fontReplace.inputs)
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
										m_fontReplace.inputs.Remove(target);
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
						foreach (var target in m_fontReplace.inputs)
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
							var images = obj.GetComponentsInChildren<Text>(true);
							foreach (var com in images)
							{
								foreach (var target in m_fontReplace.inputs)
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

#endregion

#region TextMeshProFontReplace

		public class TextMeshProFontReplace
		{
			public class Input
			{
				public readonly List<TMP_FontAsset> targets = new List<TMP_FontAsset>();
				public TMP_FontAsset replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}

		private TextMeshProFontReplace m_tmpFontReplace = new TextMeshProFontReplace();

		private void SearchAndReplaceTMPFont()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_tmpFontReplace.inputs.Add(new TextMeshProFontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace TMP Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_tmpFontReplace ??= new TextMeshProFontReplace();

					foreach (var target in m_tmpFontReplace.inputs)
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
										m_tmpFontReplace.inputs.Remove(target);
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
						foreach (var target in m_tmpFontReplace.inputs)
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
							var txtsUI = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
							foreach (var com in txtsUI)
							{
								foreach (var target in m_tmpFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
										EditorUtility.SetDirty(obj);
									}
							}

							var txts = obj.GetComponentsInChildren<TextMeshPro>(true);
							foreach (var com in txts)
							{
								foreach (var target in m_tmpFontReplace.inputs)
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

#endregion

#region ObjectReplace

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
			var window = GetWindow<AssetsReplacerWindow>("Assets Replacer", true);
			window.Show();
		}

#endregion
	}
}