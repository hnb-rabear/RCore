using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using EditorPrefs = UnityEditor.EditorPrefs;

namespace RCore.Editor.Tool
{
	[System.Obsolete]
	public class SwapSpriteWindow : EditorWindow
	{
		private Vector2 m_ScrollPositionAtlasTab;
		private Vector2 m_ScrollPositionCompareTab;
		private Vector2 m_ScrollPositionReplaceTab;
		private bool m_ShowResultsAsBoxes;
		private List<Image> m_Images;
		private List<SpriteRenderer> m_SpriteRenderers;
		private List<bool> m_SelectedImages;
		private List<bool> m_SelectedSpriteRenderers;
		private bool m_SelectAll;
		private List<Sprite> m_LeftOutputSprites;
		private List<Sprite> m_RightOutputSprites;
		private SwapSpriteTool m_Data;

		private void OnEnable()
		{
			m_Data = SwapSpriteTool.LoadOrCreateSettings();
		}

		private void OnGUI()
		{
			var tab = EditorHelper.Tabs("TextureReplacer", "Left", "Right", "Compare", "Replace");
			switch (tab)
			{
				case "Left":
					EditorHelper.ListObjects("Sprites", ref m_Data.leftInputSprites, null);
					if (EditorHelper.Button("Clear"))
					{
						//mLeftOutputSprites = new List<Sprite>();
						m_Data.leftInputSprites = new List<Sprite>();
						//mSave.leftAtlasTextures = new List<AtlasTexture>();
					}
					DrawDragDropAreas(m_Data.leftInputSprites);
					DrawScanSpriteButton(m_Data.leftInputSprites);
					DrawAtlasTab(m_Data.leftAtlasTextures);
					break;

				case "Right":
					EditorHelper.ListObjects("Sprites", ref m_Data.rightInputSprites, null);
					if (EditorHelper.Button("Clear"))
					{
						//mRightOutputSprites = new List<Sprite>();
						m_Data.rightInputSprites = new List<Sprite>();
						//mSave.rightAtlasTextures = new List<AtlasTexture>();
					}
					DrawDragDropAreas(m_Data.rightInputSprites);
					DrawScanSpriteButton(m_Data.rightInputSprites);
					DrawAtlasTab(m_Data.rightAtlasTextures);
					break;

				case "Compare":
					DrawCompareTab();
					break;

				case "Replace":
					DrawReplaceTab();
					break;
			}
		}

		private void DrawAtlasTab(List<AtlasTexture> pAtlasTextures)
		{
			using var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPositionAtlasTab);
			m_ScrollPositionAtlasTab = scrollView.scrollPosition;

			if (EditorHelper.Button("Add AtlasTexture"))
				pAtlasTextures.Add(new AtlasTexture());

			for (int i = 0; i < pAtlasTextures.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				int index = i;
				pAtlasTextures[index] = DisplaySpritesOfAtlas(pAtlasTextures[index]);
				if (EditorHelper.ButtonColor("X", Color.red, 23))
				{
					pAtlasTextures.RemoveAt(i);
					i--;
				}
				EditorGUILayout.EndHorizontal();
			}

		}

		private static void DrawScanSpriteButton(List<Sprite> pOutput)
		{
			var scanImageButton = new EditorButton()
			{
				label = "Find Sprites In Images",
				onPressed = () =>
				{
					if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
					{
						Debug.Log("Select at least one GameObject to see how it work");
						return;
					}

					foreach (var obj in Selection.gameObjects)
					{
						var images = obj.FindComponentsInChildren<Image>();
						foreach (var img in images)
						{
							var spt = img.sprite;
							if (spt != null && !pOutput.Contains(spt))
								pOutput.Add(spt);
						}

						var renderers = obj.FindComponentsInChildren<SpriteRenderer>();
						foreach (var img in renderers)
						{
							var spt = img.sprite;
							if (spt != null && !pOutput.Contains(spt))
								pOutput.Add(spt);
						}
					}
				}
			};
			scanImageButton.Draw();
		}

		private static void DrawDragDropAreas(List<Sprite> pOutput)
		{
			EditorHelper.DragDropBox<GameObject>("Prefabs", objs =>
			{
				foreach (var obj in objs)
				{
					var images = obj.FindComponentsInChildren<Image>();
					foreach (var img in images)
						if (img.sprite != null && !pOutput.Contains(img.sprite))
							pOutput.Add(img.sprite);

					var renderers = obj.FindComponentsInChildren<SpriteRenderer>();
					foreach (var renderer in renderers)
						if (renderer.sprite != null && !pOutput.Contains(renderer.sprite))
							pOutput.Add(renderer.sprite);
				}
			});
			EditorHelper.DragDropBox<Object>("Sprites", (objs) =>
			{
				foreach (var obj in objs)
				{
					if (obj is Texture2D)
					{
						string path = AssetDatabase.GetAssetPath(obj);
						var ss = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
						foreach (var s in ss)
							if (!pOutput.Contains(s))
								pOutput.Add(s);
					}
				}
			});
		}

		private void DrawCompareTab()
		{
			var matchButton = new EditorButton()
			{
				label = "Match Sprites",
				onPressed = () =>
				{
					m_LeftOutputSprites = new List<Sprite>();
					m_RightOutputSprites = new List<Sprite>();
					//Gather sprites in left atlases
					foreach (var atlas in m_Data.leftAtlasTextures)
					{
						if (atlas.Sprites != null)
							foreach (var sprite in atlas.Sprites)
								if (!m_LeftOutputSprites.Contains(sprite))
									m_LeftOutputSprites.Add(sprite);
					}
					foreach (var sprite in m_Data.leftInputSprites)
					{
						if (!m_LeftOutputSprites.Contains(sprite))
							m_LeftOutputSprites.Add(sprite);
					}
					//Gather sprites in right atlases
					foreach (var atlas in m_Data.rightAtlasTextures)
					{
						if (atlas.Sprites != null)
							foreach (var sprite in atlas.Sprites)
								if (!m_RightOutputSprites.Contains(sprite))
									m_RightOutputSprites.Add(sprite);
					}
					foreach (var sprite in m_Data.rightInputSprites)
					{
						if (!m_RightOutputSprites.Contains(sprite))
							m_RightOutputSprites.Add(sprite);
					}
					//Compare left list to right list
					m_Data.spritesToSprites = new List<SpriteToSprite>();
					foreach (var leftSpr in m_LeftOutputSprites)
					{
						var spriteToSprite = new SpriteToSprite();
						if (leftSpr != null)
						{
							spriteToSprite.left = leftSpr;
							foreach (var rightSpr in m_RightOutputSprites)
							{
								if (rightSpr != null && leftSpr.name == rightSpr.name)
									spriteToSprite.right = rightSpr;
							}
						}
						m_Data.spritesToSprites.Add(spriteToSprite);
					}
				}
			};
			EditorHelper.BoxHorizontal(() =>
			{
				matchButton.Draw();
				m_ShowResultsAsBoxes = EditorHelper.Toggle(m_ShowResultsAsBoxes, "Show Box");
			});

			using var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPositionCompareTab);
			m_ScrollPositionCompareTab = scrollView.scrollPosition;
			if (m_Data.spritesToSprites != null)
			{
				if (m_ShowResultsAsBoxes)
				{
					int spritesPerPage = 30;
					int page = EditorPrefs.GetInt("TexturesReplacer_page", 0);
					int totalPages = Mathf.CeilToInt(m_Data.spritesToSprites.Count * 1f / spritesPerPage);
					if (totalPages == 0)
						totalPages = 1;
					if (page < 0)
						page = 0;
					if (page >= totalPages)
						page = totalPages - 1;
					int from = page * spritesPerPage;
					int to = page * spritesPerPage + spritesPerPage - 1;
					if (to > m_Data.spritesToSprites.Count - 1)
						to = m_Data.spritesToSprites.Count - 1;

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (EditorHelper.Button("<Prev<", 80))
						{
							if (page > 0) page--;
							EditorPrefs.SetInt("TexturesReplacer_page", page);
						}
						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({m_Data.spritesToSprites.Count})", GUILayout.Width(100));
						if (EditorHelper.Button(">Next>", 80))
						{
							if (page < totalPages - 1) page++;
							EditorPrefs.SetInt("TexturesReplacer_page", page);
						}
						EditorGUILayout.EndHorizontal();
					}

					var style = new GUIStyle(EditorStyles.boldLabel);
					style.alignment = TextAnchor.MiddleCenter;
					for (int i = from; i <= to; i++)
					{
						var spriteToSprite = m_Data.spritesToSprites[i];
						int i1 = i;
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.ObjectField<Sprite>(spriteToSprite.left, $"{i1 + 1}, {spriteToSprite.left.name}", 150, 40, true);
							string leftName = spriteToSprite.left == null ? "" : spriteToSprite.left.name;
							string rightName = spriteToSprite.right == null ? "" : spriteToSprite.right.name;
							int leftId = spriteToSprite.left == null ? 0 : spriteToSprite.left.GetInstanceID();
							int rightId = spriteToSprite.right == null ? 0 : spriteToSprite.right.GetInstanceID();
							if (leftName != rightName || leftId != rightId)
							{
								style.normal.textColor = Color.red;
								EditorGUILayout.LabelField("!=", style, GUILayout.Width(23));
							}
							else
							{
								style.normal.textColor = Color.green;
								EditorGUILayout.LabelField("==", style, GUILayout.Width(23));
							}
							if (spriteToSprite.right != null)
								spriteToSprite.right = (Sprite)EditorHelper.ObjectField<Sprite>(spriteToSprite.right, $"{spriteToSprite.right.name}", 130, 40, true);
							else
								spriteToSprite.right = (Sprite)EditorHelper.ObjectField<Sprite>(spriteToSprite.right, "NULL", 130, 40, true);
						});
					}

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (EditorHelper.Button("<Prev<", 80))
						{
							if (page > 0) page--;
							EditorPrefs.SetInt("TexturesReplacer_page", page);
						}
						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({m_Data.spritesToSprites.Count})", GUILayout.Width(100));
						if (EditorHelper.Button(">Next>", 80))
						{
							if (page < totalPages - 1) page++;
							EditorPrefs.SetInt("TexturesReplacer_page", page);
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				else
				{
					var style = new GUIStyle(EditorStyles.boldLabel);
					style.alignment = TextAnchor.MiddleCenter;
					for (int i = 0; i < m_Data.spritesToSprites.Count; i++)
					{
						var spriteToSprite = m_Data.spritesToSprites[i];
						int i1 = i;
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.ObjectField<Sprite>(spriteToSprite.left, $"{i1 + 1}", 20, 200);
							string leftName = spriteToSprite.left == null ? "" : spriteToSprite.left.name;
							string rightName = spriteToSprite.right == null ? "" : spriteToSprite.right.name;
							int leftId = spriteToSprite.left == null ? 0 : spriteToSprite.left.GetInstanceID();
							int rightId = spriteToSprite.right == null ? 0 : spriteToSprite.right.GetInstanceID();
							if (leftName != rightName || leftId != rightId)
							{
								style.normal.textColor = Color.red;
								EditorGUILayout.LabelField("!=", style, GUILayout.Width(23));
							}
							else
							{
								style.normal.textColor = Color.green;
								EditorGUILayout.LabelField("==", style, GUILayout.Width(23));
							}
							spriteToSprite.right = (Sprite)EditorHelper.ObjectField<Sprite>(spriteToSprite.right, $"", 200);
						});
					}
				}
			}

		}

		private void DrawReplaceTab()
		{
			var scanButton = new EditorButton()
			{
				label = "Find Images/SpriteRenderers",
				color = Color.cyan,
				onPressed = () =>
				{
					m_Images = new List<Image>();
					m_SelectedImages = new List<bool>();

					m_SpriteRenderers = new List<SpriteRenderer>();
					m_SelectedSpriteRenderers = new List<bool>();

					foreach (var obj in Selection.gameObjects)
					{
						var images = obj.FindComponentsInChildren<Image>();
						foreach (var img in images)
						{
							if (!m_Images.Contains(img) /*&& IsExisted(img.sprite)*/)
							{
								m_Images.Add(img);
								m_SelectedImages.Add(false);
							}
						}
						var renderers = obj.FindComponentsInChildren<SpriteRenderer>();
						foreach (var renderer in renderers)
						{
							if (!m_SpriteRenderers.Contains(renderer) /*&& IsExisted(renderer.sprite)*/)
							{
								m_SpriteRenderers.Add(renderer);
								m_SelectedSpriteRenderers.Add(false);
							}
						}
						m_Images = m_Images.OrderBy(x => x.sprite.name).ToList();
						m_SpriteRenderers = m_SpriteRenderers.OrderBy(x => x.sprite.name).ToList();
					}
				}
			};
			scanButton.Draw();
			using var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPositionReplaceTab);
			m_ScrollPositionReplaceTab = scrollView.scrollPosition;
			EditorHelper.BoxVertical(() =>
			{
				if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
				{
					EditorGUILayout.HelpBox("Select at least one GameObject to see how it work", MessageType.Info);
					return;
				}

				//Draw images
				if (m_Images != null && m_Images.Count > 0)
				{
					EditorHelper.BoxHorizontal(() =>
					{
						EditorHelper.LabelField("#", 40);
						EditorHelper.LabelField("Sprite", 150);
						m_SelectAll = EditorHelper.Toggle(m_SelectAll, "", 0, 30, m_SelectAll ? Color.cyan : ColorHelper.DarkCyan);
						if (EditorHelper.ButtonColor("1st", m_SelectedImages.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
						{
							for (int i = 0; i < m_SelectedImages.Count; i++)
								if (m_SelectAll || (!m_SelectAll && m_SelectedImages[i]))
									ReplaceByLeft(m_Images[i]);
						}
						if (EditorHelper.ButtonColor("2nd", m_SelectedImages.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
						{
							for (int i = 0; i < m_SelectedImages.Count; i++)
								if (m_SelectAll || (!m_SelectAll && m_SelectedImages[i]))
									ReplaceByRight(m_Images[i]);
						}
					});

					for (int i = 0; i < m_Images.Count; i++)
					{
						GUILayout.BeginHorizontal();
						var img = m_Images[i];
						if (img == null)
							continue;
						EditorHelper.LabelField(i.ToString(), 40);
						EditorHelper.ObjectField<Sprite>(img.sprite, "", 0, 150);
						if (!m_SelectAll)
							m_SelectedImages[i] = EditorHelper.Toggle(m_SelectedImages[i], "", 0, 30, m_SelectedImages[i] ? Color.cyan : ColorHelper.DarkCyan);
						else
							EditorHelper.Toggle(true, "", 0, 30, Color.cyan);
						if (EditorHelper.Button($"{img.name}"))
							Selection.activeObject = img;

						bool hasLeftSpt = IsLeft(img.sprite, out int leftIndex);
						bool hasRightSpt = IsRight(img.sprite, out int rightIndex);

						if (hasLeftSpt != hasRightSpt)
						{
							if (EditorHelper.ButtonColor("1st", hasLeftSpt ? ColorHelper.DarkCyan : Color.cyan, 33))
							{
								if (hasLeftSpt) return;
								ReplaceByLeft(img, rightIndex);
							}

							if (EditorHelper.ButtonColor("2nd", hasRightSpt ? ColorHelper.DarkCyan : Color.cyan, 33))
							{
								if (hasRightSpt) return;
								ReplaceByRight(img, leftIndex);
							}
						}
						GUILayout.EndHorizontal();
					}
				}

				//Draw Sprite Renderers
				if (m_SpriteRenderers != null && m_SpriteRenderers.Count > 0)
				{
					EditorHelper.BoxHorizontal(() =>
					{
						EditorHelper.LabelField("#", 40);
						EditorHelper.LabelField("Sprite", 150);
						m_SelectAll = EditorHelper.Toggle(m_SelectAll, "", 0, 30, m_SelectAll ? Color.cyan : ColorHelper.DarkCyan);
						if (EditorHelper.ButtonColor("1st", m_SelectedSpriteRenderers.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
						{
							for (int i = 0; i < m_SelectedSpriteRenderers.Count; i++)
								if (m_SelectAll || (!m_SelectAll && m_SelectedSpriteRenderers[i]))
									ReplaceByLeft(m_SpriteRenderers[i]);
						}
						if (EditorHelper.ButtonColor("2nd", m_SelectedSpriteRenderers.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
						{
							for (int i = 0; i < m_SelectedSpriteRenderers.Count; i++)
								if (m_SelectAll || (!m_SelectAll && m_SelectedSpriteRenderers[i]))
									ReplaceByRight(m_SpriteRenderers[i]);
						}
					});

					for (int i = 0; i < m_SpriteRenderers.Count; i++)
					{
						GUILayout.BeginHorizontal();
						var img = m_SpriteRenderers[i];
						if (img == null)
							continue;
						EditorHelper.LabelField(i.ToString(), 40);
						EditorHelper.ObjectField<Sprite>(img.sprite, "", 0, 150);
						if (!m_SelectAll)
							m_SelectedSpriteRenderers[i] = EditorHelper.Toggle(m_SelectedSpriteRenderers[i], "", 0, 30, m_SelectedSpriteRenderers[i] ? Color.cyan : ColorHelper.DarkCyan);
						else
							EditorHelper.Toggle(true, "", 0, 30, Color.cyan);
						if (EditorHelper.Button($"{img.name}"))
							Selection.activeObject = img;

						bool hasLeftSpt = IsLeft(img.sprite, out int leftIndex);
						bool hasRightSpt = IsRight(img.sprite, out int rightIndex);

						if (hasLeftSpt != hasRightSpt)
						{
							if (EditorHelper.ButtonColor("1st", hasLeftSpt ? ColorHelper.DarkCyan : Color.cyan, 33))
							{
								if (hasLeftSpt) return;
								ReplaceByLeft(img, rightIndex);
							}

							if (EditorHelper.ButtonColor("2nd", hasRightSpt ? ColorHelper.DarkCyan : Color.cyan, 33))
							{
								if (hasRightSpt) return;
								ReplaceByRight(img, leftIndex);
							}
						}
						GUILayout.EndHorizontal();
					}

					if ((m_SelectedImages == null || m_SelectedImages.Count == 0) && (m_SelectedSpriteRenderers == null || m_SelectedSpriteRenderers.Count == 0))
						EditorGUILayout.HelpBox("Not found any image have sprite from left or right list!", MessageType.Info);
				}

			}, Color.yellow, true);

		}

		private Sprite FindLeft(string pName)
		{
			foreach (var spr in m_Data.spritesToSprites)
			{
				if (spr.left != null && spr.left.name == pName)
					return spr.left;
				if (spr.right != null && spr.right.name == pName)
					return spr.left;
			}
			return null;
		}

		private void ReplaceByLeft(SpriteRenderer img, int pIndex = -1)
		{
			var spr = pIndex == -1 ? FindLeft(img.sprite.name) : m_Data.spritesToSprites[pIndex].left;
			if (spr != null)
			{
				img.sprite = spr;
				Debug.Log($"ReplaceByLeft: {img.name}");
			}
			else
				Debug.LogWarning("Not found left for " + img.sprite.name);

		}

		private void ReplaceByLeft(Image img, int pIndex = -1)
		{
			var spr = pIndex == -1 ? FindLeft(img.sprite.name) : m_Data.spritesToSprites[pIndex].left;
			if (spr != null)
			{
				img.sprite = spr;
				Debug.Log($"ReplaceByLeft: {img.name}");
			}
			else
				Debug.LogWarning("Not found left for " + img.sprite.name);

		}

		private Sprite FindRight(string pName)
		{
			foreach (var spr in m_Data.spritesToSprites)
			{
				if (spr.right != null && spr.right.name == pName)
					return spr.right;
				else if (spr.left != null && spr.left.name == pName)
					return spr.right;
			}
			return null;
		}

		private void ReplaceByRight(SpriteRenderer img, int pIndex = -1)
		{
			var spr = pIndex == -1 ? FindRight(img.sprite.name) : m_Data.spritesToSprites[pIndex].right;
			if (spr != null)
			{
				img.sprite = spr;
				Debug.Log($"ReplaceByRight: {img.name}");
			}
			else
				Debug.LogWarning("Not found right for " + img.sprite.name);
		}

		private void ReplaceByRight(Image img, int pIndex = -1)
		{
			var spr = pIndex == -1 ? FindRight(img.sprite.name) : m_Data.spritesToSprites[pIndex].right;
			if (spr != null)
			{
				img.sprite = spr;
				Debug.Log($"ReplaceByRight: {img.name}");
			}
			else
				Debug.LogWarning("Not found right for " + img.sprite.name);
		}

		private bool IsLeft(Sprite pSpr, out int pIndex)
		{
			pIndex = -1;
			for (int i = 0; i < m_Data.spritesToSprites.Count; i++)
			{
				var spr = m_Data.spritesToSprites[i];
				if (spr.left == pSpr)
				{
					pIndex = i;
					return true;
				}
			}
			return false;
		}

		private bool IsRight(Sprite pSpr, out int pIndex)
		{
			pIndex = -1;
			for (int i = 0; i < m_Data.spritesToSprites.Count; i++)
			{
				var spr = m_Data.spritesToSprites[i];
				if (spr.right == pSpr)
				{
					pIndex = i;
					return true;
				}
			}
			return false;
		}

		private bool IsExisted(Sprite pSpr)
		{
			if (pSpr == null)
				return false;
			foreach (var spr in m_Data.spritesToSprites)
				if (spr.left == pSpr || spr.right == pSpr)
					return true;
			return false;
		}

		private static AtlasTexture DisplaySpritesOfAtlas(AtlasTexture pSource)
		{
			pSource ??= new AtlasTexture();

			var atlas = (Texture)EditorHelper.ObjectField<Texture>(pSource.Atlas, "", 0, 60, true);
			if (atlas != pSource.Atlas)
				pSource.Atlas = atlas;
			if (atlas != null)
			{
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField($"Name: {atlas.name}");
				EditorGUILayout.LabelField($"Total sprites: {pSource.Length}");
				EditorGUILayout.LabelField($"Instance Id: {pSource.Atlas.GetInstanceID()}");
				EditorGUILayout.EndVertical();
			}

			return pSource;
		}
		
		public static void ShowWindow()
		{
			var window = GetWindow<SwapSpriteWindow>("Swap Sprite", true);
			window.minSize = new Vector2(600, 400);
			window.Show();
		}
	}
}