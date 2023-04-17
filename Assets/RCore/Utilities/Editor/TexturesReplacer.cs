using RCore.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using EditorPrefs = UnityEditor.EditorPrefs;

namespace RCore.Editor
{
	public class TexturesReplacer : EditorWindow
	{
		private Vector2 mScrollPositionAtlasTab;
		private Vector2 mScrollPositionCompareTab;
		private Vector2 mScrollPositionReplaceTab;
		private bool mShowResultsAsBoxes;
		private List<Image> mImages;
		private List<SpriteRenderer> mSpriteRenderers;
		private List<bool> mSelectedImages;
		private List<bool> mSelectedSpriteRenderers;
		private bool mSelectAll;
		private List<Sprite> mLeftOutputSprites;
		private List<Sprite> mRightOutputSprites;
		private TexturesReplacerSave mSave;

		private void OnEnable()
		{
			mSave = TexturesReplacerSave.LoadOrCreateSettings();
		}

		private void OnGUI()
		{
			var tab = EditorHelper.Tabs("TextureReplacer", "Left", "Right", "Compare", "Replace");
			switch (tab)
			{
				case "Left":
					EditorHelper.ListObjects("Sprites", ref mSave.leftInputSprites, null);
					if (EditorHelper.Button("Clear"))
					{
						//mLeftOutputSprites = new List<Sprite>();
						mSave.leftInputSprites = new List<Sprite>();
						//mSave.leftAtlasTextures = new List<AtlasTexture>();
					}
					DrawDragDropAreas(mSave.leftInputSprites);
					DrawScanSpriteButton(mSave.leftInputSprites);
					DrawAtlasTab(mSave.leftAtlasTextures);
					break;

				case "Right":
					EditorHelper.ListObjects("Sprites", ref mSave.rightInputSprites, null);
					if (EditorHelper.Button("Clear"))
					{
						//mRightOutputSprites = new List<Sprite>();
						mSave.rightInputSprites = new List<Sprite>();
						//mSave.rightAtlasTextures = new List<AtlasTexture>();
					}
					DrawDragDropAreas(mSave.rightInputSprites);
					DrawScanSpriteButton(mSave.rightInputSprites);
					DrawAtlasTab(mSave.rightAtlasTextures);
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
			using (var scrollView = new EditorGUILayout.ScrollViewScope(mScrollPositionAtlasTab))
			{
				mScrollPositionAtlasTab = scrollView.scrollPosition;

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
		}

		private void DrawScanSpriteButton(List<Sprite> pOutput)
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

		private void DrawDragDropAreas(List<Sprite> pOutput)
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
						var tex = obj as Texture2D;
						string path = AssetDatabase.GetAssetPath(obj);
						Sprite[] ss = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
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
					mLeftOutputSprites = new List<Sprite>();
					mRightOutputSprites = new List<Sprite>();
					//Gather sprites in left atlases
					foreach (var atlas in mSave.leftAtlasTextures)
					{
						if (atlas.Sprites != null)
							foreach (var sprite in atlas.Sprites)
								if (!mLeftOutputSprites.Contains(sprite))
									mLeftOutputSprites.Add(sprite);
					}
					foreach (var sprite in mSave.leftInputSprites)
					{
						if (!mLeftOutputSprites.Contains(sprite))
							mLeftOutputSprites.Add(sprite);
					}
					//Gather sprites in right atlases
					foreach (var atlas in mSave.rightAtlasTextures)
					{
						if (atlas.Sprites != null)
							foreach (var sprite in atlas.Sprites)
								if (!mRightOutputSprites.Contains(sprite))
									mRightOutputSprites.Add(sprite);
					}
					foreach (var sprite in mSave.rightInputSprites)
					{
						if (!mRightOutputSprites.Contains(sprite))
							mRightOutputSprites.Add(sprite);
					}
					//Compare left list to right list
					mSave.spritesToSprites = new List<SpriteToSprite>();
					foreach (var leftSpr in mLeftOutputSprites)
					{
						var spriteToSprite = new SpriteToSprite();
						if (leftSpr != null)
						{
							spriteToSprite.left = leftSpr;
							foreach (var rightSpr in mRightOutputSprites)
							{
								if (rightSpr != null && leftSpr.name == rightSpr.name)
									spriteToSprite.right = rightSpr;
							}
						}
						mSave.spritesToSprites.Add(spriteToSprite);
					}
				}
			};
			EditorHelper.BoxHorizontal(() =>
			{
				matchButton.Draw();
				mShowResultsAsBoxes = EditorHelper.Toggle(mShowResultsAsBoxes, "Show Box");
			});
			using (var scrollView = new EditorGUILayout.ScrollViewScope(mScrollPositionCompareTab))
			{
				mScrollPositionCompareTab = scrollView.scrollPosition;
				if (mSave.spritesToSprites != null)
				{
					if (mShowResultsAsBoxes)
					{
						int spritesPerPage = 30;
						int page = EditorPrefs.GetInt("TexturesReplacer_page", 0);
						int totalPages = Mathf.CeilToInt(mSave.spritesToSprites.Count * 1f / spritesPerPage);
						if (totalPages == 0)
							totalPages = 1;
						if (page < 0)
							page = 0;
						if (page >= totalPages)
							page = totalPages - 1;
						int from = page * spritesPerPage;
						int to = page * spritesPerPage + spritesPerPage - 1;
						if (to > mSave.spritesToSprites.Count - 1)
							to = mSave.spritesToSprites.Count - 1;

						if (totalPages > 1)
						{
							EditorGUILayout.BeginHorizontal();
							if (EditorHelper.Button("<Prev<", 80))
							{
								if (page > 0) page--;
								EditorPrefs.SetInt("TexturesReplacer_page", page);
							}
							EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({mSave.spritesToSprites.Count})", GUILayout.Width(100));
							if (EditorHelper.Button(">Next>", 80))
							{
								if (page < totalPages - 1) page++;
								EditorPrefs.SetInt("TexturesReplacer_page", page);
							}
							EditorGUILayout.EndHorizontal();
						}

						GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
						style.alignment = TextAnchor.MiddleCenter;
						for (int i = from; i <= to; i++)
						{
							var spriteToSprite = mSave.spritesToSprites[i];
							EditorHelper.BoxHorizontal(() =>
							{
								EditorHelper.ObjectField<Sprite>(spriteToSprite.left, $"{i + 1}, {spriteToSprite.left.name}", 150, 40, true);
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
							EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({mSave.spritesToSprites.Count})", GUILayout.Width(100));
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
						GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
						style.alignment = TextAnchor.MiddleCenter;
						for (int i = 0; i < mSave.spritesToSprites.Count; i++)
						{
							var spriteToSprite = mSave.spritesToSprites[i];
							EditorHelper.BoxHorizontal(() =>
							{
								EditorHelper.ObjectField<Sprite>(spriteToSprite.left, $"{i + 1}", 20, 200);
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
		}

		private void DrawReplaceTab()
		{
			var scanButton = new EditorButton()
			{
				label = "Find Images/SpriteRenderers",
				color = Color.cyan,
				onPressed = () =>
				{
					mImages = new List<Image>();
					mSelectedImages = new List<bool>();

					mSpriteRenderers = new List<SpriteRenderer>();
					mSelectedSpriteRenderers = new List<bool>();

					foreach (var obj in Selection.gameObjects)
					{
						var images = obj.FindComponentsInChildren<Image>();
						foreach (var img in images)
						{
							if (!mImages.Contains(img) /*&& IsExisted(img.sprite)*/)
							{
								mImages.Add(img);
								mSelectedImages.Add(false);
							}
						}
						var renderers = obj.FindComponentsInChildren<SpriteRenderer>();
						foreach (var renderer in renderers)
						{
							if (!mSpriteRenderers.Contains(renderer) /*&& IsExisted(renderer.sprite)*/)
							{
								mSpriteRenderers.Add(renderer);
								mSelectedSpriteRenderers.Add(false);
							}
						}
						mImages = mImages.OrderBy(x => x.sprite.name).ToList();
						mSpriteRenderers = mSpriteRenderers.OrderBy(x => x.sprite.name).ToList();
					}
				}
			};
			scanButton.Draw();
			using (var scrollView = new EditorGUILayout.ScrollViewScope(mScrollPositionReplaceTab))
			{
				mScrollPositionReplaceTab = scrollView.scrollPosition;
				EditorHelper.BoxVertical(() =>
				{
					if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
					{
						EditorGUILayout.HelpBox("Select at least one GameObject to see how it work", MessageType.Info);
						return;
					}

					//Draw images
					if (mImages != null && mImages.Count > 0)
					{
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.LabelField("#", 40);
							EditorHelper.LabelField("Sprite", 150);
							mSelectAll = EditorHelper.Toggle(mSelectAll, "", 0, 30, mSelectAll ? Color.cyan : ColorHelper.DarkCyan);
							if (EditorHelper.ButtonColor("1st", mSelectedImages.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
							{
								for (int i = 0; i < mSelectedImages.Count; i++)
									if (mSelectAll || (!mSelectAll && mSelectedImages[i]))
										ReplaceByLeft(mImages[i]);
							}
							if (EditorHelper.ButtonColor("2nd", mSelectedImages.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
							{
								for (int i = 0; i < mSelectedImages.Count; i++)
									if (mSelectAll || (!mSelectAll && mSelectedImages[i]))
										ReplaceByRight(mImages[i]);
							}
						});

						for (int i = 0; i < mImages.Count; i++)
						{
							GUILayout.BeginHorizontal();
							var img = mImages[i];
							if (img == null)
								continue;
							EditorHelper.LabelField(i.ToString(), 40);
							EditorHelper.ObjectField<Sprite>(img.sprite, "", 0, 150);
							if (!mSelectAll)
								mSelectedImages[i] = EditorHelper.Toggle(mSelectedImages[i], "", 0, 30, mSelectedImages[i] ? Color.cyan : ColorHelper.DarkCyan);
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
					if (mSpriteRenderers != null && mSpriteRenderers.Count > 0)
					{
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.LabelField("#", 40);
							EditorHelper.LabelField("Sprite", 150);
							mSelectAll = EditorHelper.Toggle(mSelectAll, "", 0, 30, mSelectAll ? Color.cyan : ColorHelper.DarkCyan);
							if (EditorHelper.ButtonColor("1st", mSelectedSpriteRenderers.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
							{
								for (int i = 0; i < mSelectedSpriteRenderers.Count; i++)
									if (mSelectAll || (!mSelectAll && mSelectedSpriteRenderers[i]))
										ReplaceByLeft(mSpriteRenderers[i]);
							}
							if (EditorHelper.ButtonColor("2nd", mSelectedSpriteRenderers.Count > 0 ? Color.cyan : ColorHelper.DarkCyan, 33))
							{
								for (int i = 0; i < mSelectedSpriteRenderers.Count; i++)
									if (mSelectAll || (!mSelectAll && mSelectedSpriteRenderers[i]))
										ReplaceByRight(mSpriteRenderers[i]);
							}
						});

						for (int i = 0; i < mSpriteRenderers.Count; i++)
						{
							GUILayout.BeginHorizontal();
							var img = mSpriteRenderers[i];
							if (img == null)
								continue;
							EditorHelper.LabelField(i.ToString(), 40);
							EditorHelper.ObjectField<Sprite>(img.sprite, "", 0, 150);
							if (!mSelectAll)
								mSelectedSpriteRenderers[i] = EditorHelper.Toggle(mSelectedSpriteRenderers[i], "", 0, 30, mSelectedSpriteRenderers[i] ? Color.cyan : ColorHelper.DarkCyan);
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

						if ((mSelectedImages == null || mSelectedImages.Count == 0) && (mSelectedSpriteRenderers == null || mSelectedSpriteRenderers.Count == 0))
							EditorGUILayout.HelpBox("Not found any image have sprite from left or right list!", MessageType.Info);
					}

				}, Color.yellow, true);
			}
		}

		private Sprite FindLeft(string pName)
		{
			foreach (var spr in mSave.spritesToSprites)
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
			Sprite spr = null;
			if (pIndex == -1)
				spr = FindLeft(img.sprite.name);
			else
				spr = mSave.spritesToSprites[pIndex].left;
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
			Sprite spr = null;
			if (pIndex == -1)
				spr = FindLeft(img.sprite.name);
			else
				spr = mSave.spritesToSprites[pIndex].left;
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
			foreach (var spr in mSave.spritesToSprites)
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
			Sprite spr = null;
			if (pIndex == -1)
				spr = FindRight(img.sprite.name);
			else
				spr = mSave.spritesToSprites[pIndex].right;
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
			Sprite spr = null;
			if (pIndex == -1)
				spr = FindRight(img.sprite.name);
			else
				spr = mSave.spritesToSprites[pIndex].right;
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
			for (int i = 0; i < mSave.spritesToSprites.Count; i++)
			{
				var spr = mSave.spritesToSprites[i];
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
			for (int i = 0; i < mSave.spritesToSprites.Count; i++)
			{
				var spr = mSave.spritesToSprites[i];
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
			foreach (var spr in mSave.spritesToSprites)
				if (spr.left == pSpr || spr.right == pSpr)
					return true;
			return false;
		}

		private AtlasTexture DisplaySpritesOfAtlas(AtlasTexture pSource)
		{
			if (pSource == null)
				pSource = new AtlasTexture();

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

		[MenuItem("RUtilities/Tools/Textures Replacer")]
		private static void OpenEditorWindow()
		{
			var window = GetWindow<TexturesReplacer>("Textures Replacer", true);
			window.minSize = new Vector2(600, 400);
			window.Show();
		}
	}
}