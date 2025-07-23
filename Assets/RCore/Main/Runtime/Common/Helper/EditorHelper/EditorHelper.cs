/***
 * Author HNB-RaBear - 2017
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	public static class EditorHelper
	{
#region EditorFileUtil

		public static string SaveFilePanel(string mainDirectory, string defaultName, string content, string extension = "json,txt") => EditorFileUtil.SaveFilePanel(mainDirectory, defaultName, content, extension);

		public static void SaveFile(string path, string content) => EditorFileUtil.SaveFile(path, content);

		public static void SaveJsonFilePanel<T>(string pMainDirectory, string defaultName, T obj) => EditorFileUtil.SaveJsonFilePanel(pMainDirectory, defaultName, obj);

		public static void SaveJsonFile<T>(string pPath, T pObj) => EditorFileUtil.SaveJsonFile(pPath, pObj);

		public static bool LoadJsonFilePanel<T>(string pMainDirectory, ref T pOutput) => EditorFileUtil.LoadJsonFilePanel(pMainDirectory, ref pOutput);

		public static string LoadFilePanel(string pMainDirectory, string extensions = "json,txt") => EditorFileUtil.LoadFilePanel(pMainDirectory, extensions);

		public static KeyValuePair<string, string> LoadFilePanel2(string pMainDirectory, string extensions = "json,txt") => EditorFileUtil.LoadFilePanel2(pMainDirectory, extensions);

		public static bool LoadJsonFromFile<T>(string pPath, ref T pOutput) => EditorFileUtil.LoadJsonFromFile(pPath, ref pOutput);

		public static void SaveXMLFile<T>(string pPath, T pObj) => EditorFileUtil.SaveXMLFile(pPath, pObj);

		public static T LoadXMLFile<T>(string pPath) => EditorFileUtil.LoadXMLFile<T>(pPath);

#endregion

		public static List<T> FindAll<T>() where T : Component => EditorComponentUtil.FindAll<T>();
		
#region Layout

		private static readonly Dictionary<int, Color> BoxColours = new Dictionary<int, Color>();

		public static void BoxVerticalOpen(int id, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			var defaultColor = GUI.backgroundColor;
			if (!BoxColours.ContainsKey(id))
				BoxColours.Add(id, defaultColor);

			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				EditorGUILayout.BeginVertical(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				EditorGUILayout.BeginVertical(style);
			}
		}

		public static void BoxVerticalClose(int id)
		{
			GUI.backgroundColor = BoxColours[id];
			EditorGUILayout.EndVertical();
		}

		public static Rect BoxVertical(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}

			doSomething();

			EditorGUILayout.EndVertical();
			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}

		public static Rect BoxVertical(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}

			if (!string.IsNullOrEmpty(pTitle))
				DrawHeaderTitle(pTitle);

			doSomething();

			EditorGUILayout.EndVertical();
			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}

		public static Rect BoxHorizontal(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}

			doSomething();

			EditorGUILayout.EndHorizontal();

			if (color != default)
				GUI.backgroundColor = defaultColor;
			return rect;
		}

		public static Rect BoxHorizontal(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!string.IsNullOrEmpty(pTitle))
			{
				EditorGUILayout.BeginVertical();
				DrawHeaderTitle(pTitle);
			}

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}

			doSomething();

			EditorGUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(pTitle))
				EditorGUILayout.EndVertical();

			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}

		public static void GridDraws(int pCell, List<IDraw> pDraws, Color color = default)
		{
			int row = Mathf.CeilToInt(pDraws.Count * 1f / pCell);
			var bgColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;
			EditorGUILayout.BeginVertical("box");
			for (int i = 0; i < row; i++)
			{
				EditorGUILayout.BeginHorizontal();

				for (int j = 0; j < pCell; j++)
				{
					int index = i * pCell + j;
					if (index < pDraws.Count)
						pDraws[index].Draw();
				}

				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
			if (color != default)
				GUI.backgroundColor = bgColor;
		}

		public static void DrawLine(float padding = 0)
		{
			if (padding > 0)
				EditorGUILayout.Space(padding);
			var lineColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.6f, 0.6f, 0.6f);
			var originalColor = GUI.color;
			GUI.color = lineColor;
			float lineThickness = 1;
			var lineRect = EditorGUILayout.GetControlRect(false, lineThickness);
			EditorGUI.DrawRect(lineRect, lineColor);
			GUI.color = originalColor;
			if (padding > 0)
				EditorGUILayout.Space(padding);
		}

		public static void Separator(string label = null, Color labelColor = default)
		{
			if (string.IsNullOrEmpty(label))
			{
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
			else
			{
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();

				// Left separator line
				GUILayout.Label("", GUI.skin.horizontalSlider);

				// Set bold and colored style for the label
				var boldStyle = new GUIStyle(GUI.skin.label);
				boldStyle.fontStyle = FontStyle.Bold;
				boldStyle.alignment = TextAnchor.MiddleCenter; // Center the label vertically
				if (labelColor != default)
					GUI.contentColor = labelColor; // Set the desired color (red in this example)

				// Label with "Editor" in bold and color
				GUILayout.Label(label, boldStyle, GUILayout.Width(50), GUILayout.ExpandWidth(false));

				// Reset content color to default (to avoid affecting other GUI elements)
				GUI.contentColor = Color.white;

				// Right separator line
				GUILayout.Label("", GUI.skin.horizontalSlider);

				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}
		}

		/// <summary>
		/// Draw a visible separator in addition to adding some padding.
		/// </summary>
		public static void SeparatorBox()
		{
			GUILayout.Space(10);

			if (Event.current.type == EventType.Repaint)
			{
				var tex = EditorGUIUtility.whiteTexture;
				var rect = GUILayoutUtility.GetLastRect();
				GUI.color = new Color(0f, 0f, 0f, 0.25f);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
				GUI.color = Color.white;
			}
		}

		public static Vector2 ScrollBar(ref Vector2 scrollPos, float width, float height, string label, Action action)
		{
			EditorGUILayout.BeginVertical("box");
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
			action();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			return scrollPos;
		}

#endregion

		//========================================

#region Tools

		public static bool Button(string label, int width = 0, int height = 0) => EditorGui.Button(label, width, height);

		public static bool ButtonColor(string label, Color color = default, int width = 0, int height = 0)
		{
			return EditorGui.Button(label, color, width, height);
		}

		public static bool Button(string label, Texture2D icon, Color color = default, int width = 0, int height = 0)
		{
			return EditorGui.Button(label, icon, color, width, height);
		}

		public static string FolderField(string defaultPath, string label, int labelWidth = 0, bool pFormatToUnityPath = true) => EditorGui.FolderField(defaultPath, label, labelWidth, pFormatToUnityPath);

		public static string FileField(string defaultPath, string label, string extension, int labelWidth = 0, bool pFormatToUnityPath = true) => EditorGui.FileField(defaultPath, label, extension, labelWidth, pFormatToUnityPath);

		public static bool Foldout(string label) => EditorLayout.Foldout(label, null);

		/// <summary>
		/// Draw a distinctly different looking header label
		/// </summary>
		public static bool HeaderFoldout(string label, string key = "", bool minimalistic = false, params IDraw[] pHorizontalDraws) => EditorLayout.HeaderFoldout(label, key, minimalistic, pHorizontalDraws);

		public static void HeaderFoldout(string label, string key, Action pOnFoldOut, params IDraw[] pHorizontalDraws) => EditorLayout.HeaderFoldout(label, key, pOnFoldOut, pHorizontalDraws);

		public static bool ConfirmPopup(string pMessage = null, string pYes = null, string pNo = null) => EditorGui.ConfirmPopup(pMessage, pYes, pNo);

		public static void ListReadonlyObjects<T>(string pName, List<T> pList, List<string> pLabels = null, bool pShowObjectBox = true) where T : Object
		{
			ListObjects(pName, ref pList, pLabels, pShowObjectBox, true);
		}

		//public static List<T> OrderedListObject<T>(List<T> list, bool pShowObjectBox) where T : UnityEngine.Object
		//{
		//    var reorderableList = new ReorderableList(list, typeof(T), true, false, true, true);
		//    reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
		//    {
		//        int lineHeight = pShowObjectBox ? 34 : 17;
		//        Rect rtIndex = new Rect(rect.x, rect.y, 17, lineHeight);
		//        Rect rtBox = new Rect();
		//        if (pShowObjectBox)
		//            rtBox = new Rect(rect.x + 17, rect.y, 34, lineHeight);
		//        Rect rtMain = new Rect(rect.x + rtIndex.width + rtBox.width, rect.y, rect.width - rtIndex.width - rtBox.width, 17);

		//        EditorGUI.LabelField(rtIndex, (index + 1).ToString());
		//        list[index] = (T)EditorGUI.ObjectField(rtBox, list[index], typeof(T), false);
		//        list[index] = (T)EditorGUI.ObjectField(rtMain, list[index], typeof(T), false);
		//    };
		//    reorderableList.onAddCallback = (lli) =>
		//    {
		//        list.Add(default(T));
		//    };
		//    reorderableList.elementHeight = pShowObjectBox ? 34 : 17;
		//    reorderableList.DoLayoutList();
		//    return list;
		//}

		public static bool ListObjects<T>(string pName, ref List<T> pObjects, List<string> pLabels, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null) where T : Object =>
			EditorLayout.ListObjects(pName, ref pObjects, pLabels, pShowObjectBox, pReadOnly, pAdditionalDraws);


		public static void PagesForList(int pCount, string pName, Action<int> pOnDraw, IDraw[] p_drawAtFirst = null, IDraw[] p_drawAtLast = null)
		{
			GUILayout.Space(3);

			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			int page = EditorPrefs.GetInt($"{pName}_page", 0);
			int totalPages = Mathf.CeilToInt(pCount * 1f / 20f);
			if (totalPages == 0)
				totalPages = 1;
			if (page < 0)
				page = 0;
			if (page >= totalPages)
				page = totalPages - 1;
			int from = page * 20;
			int to = page * 20 + 20 - 1;
			if (to > pCount - 1)
				to = pCount - 1;

			EditorGUILayout.BeginVertical("box");
			{
				if (totalPages > 1)
				{
					EditorGUILayout.BeginHorizontal();
					if (Button("\u25c4", 23))
					{
						if (page > 0)
							page--;
						EditorPrefs.SetInt($"{pName}_page", page);
					}

					EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");

					if (p_drawAtFirst != null)
					{
						foreach (var draw in p_drawAtFirst)
							draw.Draw();
					}

					if (Button("\u25ba", 23))
					{
						if (page < totalPages - 1)
							page++;
						EditorPrefs.SetInt($"{pName}_page", page);
					}

					EditorGUILayout.EndHorizontal();
				}

				for (int i = from; i <= to; i++)
				{
					pOnDraw?.Invoke(i);
				}

				if (totalPages > 1)
				{
					EditorGUILayout.BeginHorizontal();
					if (Button("\u25c4", 23))
					{
						if (page > 0)
							page--;
						EditorPrefs.SetInt($"{pName}_page", page);
					}

					EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");

					if (p_drawAtLast != null)
					{
						foreach (var draw in p_drawAtLast)
							draw.Draw();
					}

					if (Button("\u25ba", 23))
					{
						if (page < totalPages - 1)
							page++;
						EditorPrefs.SetInt($"{pName}_page", page);
					}

					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndVertical();

			GUI.backgroundColor = prevColor;
		}

		public static void ListObjectsWithSearch<T>(ref List<T> pList, string pName, bool pShowObjectBox = true) where T : Object
		{
			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			//bool show = EditorPrefs.GetBool(pName, false);
			//GUIContent content = new GUIContent(pName);
			//GUIStyle style = new GUIStyle(EditorStyles.foldout);
			//style.margin = new RectOffset(pInBox ? 13 : 0, 0, 0, 0);
			//show = EditorGUILayout.Foldout(show, content, style);

			var list = pList;
			string search = EditorPrefs.GetString($"{pName}_search");
			var show = HeaderFoldout($"{pName} ({pList.Count})", pName);
			if (show)
			{
				int page = EditorPrefs.GetInt($"{pName}_page", 0);
				int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
				if (totalPages == 0)
					totalPages = 1;
				if (page < 0)
					page = 0;
				if (page >= totalPages)
					page = totalPages - 1;
				int from = page * 20;
				int to = page * 20 + 20 - 1;
				if (to > list.Count - 1)
					to = list.Count - 1;

				EditorGUILayout.BeginVertical("true");
				{
					int boxSize = 34;
					if (pShowObjectBox)
					{
						boxSize = EditorPrefs.GetInt($"{pName}_Slider", 34);
						int boxSizeNew = (int)EditorGUILayout.Slider(boxSize, 34, 68);
						if (boxSize != boxSizeNew)
						{
							EditorPrefs.SetInt($"{pName}_Slider", boxSizeNew);
							boxSize = boxSizeNew;
						}
					}

					var list1 = list;
					DragDropBox<T>(pName, (objs) =>
					{
						list1.AddRange(objs);
					});

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("<Prev<"))
						{
							if (page > 0)
								page--;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (GUILayout.Button("<Next<"))
						{
							if (page < totalPages - 1)
								page++;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.EndHorizontal();
					}

					search = GUILayout.TextField(search);

					bool searching = !string.IsNullOrEmpty(search);
					for (int i = from; i <= to; i++)
					{
						if (i >= list.Count)
							continue;

						if (searching && !list[i].name.Contains(search))
							continue;

						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
							list[i] = (T)ObjectField<T>(list[i], "");
							if (pShowObjectBox)
								list[i] = (T)ObjectField<T>(list[i], "", 0, boxSize, true);

							if (Button("▲", 23) && i > 0)
								(list[i], list[i - 1]) = (list[i - 1], list[i]);

							if (Button("▼", 23) && i < list.Count - 1)
								(list[i], list[i + 1]) = (list[i + 1], list[i]);

							if (ButtonColor("-", Color.red, 23))
							{
								list.RemoveAt(i);
								i--;
							}
						}
						EditorGUILayout.EndHorizontal();
					}

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("<Prev<"))
						{
							if (page > 0)
								page--;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (GUILayout.Button(">Next>"))
						{
							if (page < totalPages - 1)
								page++;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.EndHorizontal();
					}


					EditorGUILayout.BeginHorizontal();
					{
						if (ButtonColor("+1", Color.green, 30))
						{
							list.Add(null);
							page = totalPages - 1;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						if (GUILayout.Button("Sort By Name"))
							list = list.OrderBy(m => m.name).ToList();
						if (GUILayout.Button("Remove Duplicate"))
							list.RemoveDuplicated();

						if (ButtonColor("Clear", Color.red, 50))
							if (ConfirmPopup())
								list = new List<T>();
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
			}

			pList = list;

			if (GUI.changed)
			{
				EditorPrefs.SetBool(pName, show);
				EditorPrefs.SetString($"{pName}_search", search);
			}

			GUI.backgroundColor = prevColor;
		}

		public static bool ListKeyObjects<TKey, TValue>(string pName, ref List<SerializableKeyValue<TKey, TValue>> pList, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null)
			where TValue : Object
		{
			GUILayout.Space(3);

			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			var list = pList;
			var show = HeaderFoldout($"{pName} ({pList.Count})", pName);
			if (show)
			{
				int page = EditorPrefs.GetInt($"{pName}_page", 0);
				int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
				if (totalPages == 0)
					totalPages = 1;
				if (page < 0)
					page = 0;
				if (page >= totalPages)
					page = totalPages - 1;
				int from = page * 20;
				int to = page * 20 + 20 - 1;
				if (to > list.Count - 1)
					to = list.Count - 1;

				EditorGUILayout.BeginVertical("box");
				{
					int boxSize = 34;
					if (pShowObjectBox)
					{
						boxSize = EditorPrefs.GetInt($"{pName}_Slider", 34);
						int boxSizeNew = (int)EditorGUILayout.Slider(boxSize, 34, 68);
						if (boxSize != boxSizeNew)
						{
							EditorPrefs.SetInt($"{pName}_Slider", boxSizeNew);
							boxSize = boxSizeNew;
						}
					}

					if (!pReadOnly)
					{
						var list1 = list;
						DragDropBox<TValue>(pName, (objs) =>
						{
							foreach (var value in objs)
								list1.Add(new SerializableKeyValue<TKey, TValue>(default, value));
						});
					}

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("<Prev<"))
						{
							if (page > 0)
								page--;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (GUILayout.Button(">Next>"))
						{
							if (page < totalPages - 1)
								page++;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.EndHorizontal();
					}

					for (int i = from; i <= to; i++)
					{
						if (i >= list.Count || list[i] == null)
							continue;
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
							if (typeof(TKey).IsEnum)
							{
								var key = list[i].k;
								var newKey = (TKey)Enum.Parse(typeof(TKey), EditorGUILayout.EnumPopup((Enum)Enum.ToObject(typeof(TKey), key)).ToString());
								list[i].k = newKey;
							}
							else if (typeof(TKey) == typeof(int))
							{
								int key = Convert.ToInt32(list[i].k);
								int newKey = EditorGUILayout.IntField(key, EditorStyles.textField, GUILayout.MinWidth(40));
								list[i].k = (TKey)(object)newKey;
							}
							else if (typeof(TKey) == typeof(string))
							{
								string key = list[i].k.ToString();
								string newKey = EditorGUILayout.TextField(key, EditorStyles.textField, GUILayout.MinWidth(40));
								list[i].k = (TKey)(object)newKey;
							}
							list[i].v = (TValue)ObjectField<TValue>(list[i].v, "");
							if (pShowObjectBox)
								list[i].v = (TValue)ObjectField<TValue>(list[i].v, "", 0, boxSize, true);

							if (!pReadOnly)
							{
								if (Button("▲", 23) && i > 0)
									(list[i], list[i - 1]) = (list[i - 1], list[i]);

								if (Button("▼", 23) && i < list.Count - 1)
									(list[i], list[i + 1]) = (list[i + 1], list[i]);

								if (ButtonColor("-", Color.red, 23))
								{
									list.RemoveAt(i);
									i--;
								}

								if (ButtonColor("+", Color.green, 23))
									list.Insert(i + 1, null);
							}
						}
						EditorGUILayout.EndHorizontal();
					}

					if (totalPages > 1)
					{
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("<Prev<"))
						{
							if (page > 0)
								page--;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({list.Count})");
						if (GUILayout.Button(">Next>"))
						{
							if (page < totalPages - 1)
								page++;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						EditorGUILayout.EndHorizontal();
					}

					if (!pReadOnly)
					{
						EditorGUILayout.BeginHorizontal();
						{
							if (ButtonColor("+1", Color.green, 30))
							{
								list.Add(null);
								page = totalPages - 1;
								EditorPrefs.SetInt($"{pName}_page", page);
							}
							if (GUILayout.Button("Sort By Name"))
								list = list.OrderBy(m => m.v.name).ToList();
							if (GUILayout.Button("Remove Duplicated Key"))
								list.RemoveDuplicatedKey();
							if (GUILayout.Button("Remove Duplicated Value"))
								list.RemoveDuplicatedValue();
							if (ButtonColor("Clear", Color.red, 50))
								if (ConfirmPopup())
									list = new List<SerializableKeyValue<TKey, TValue>>();
						}
						EditorGUILayout.EndHorizontal();
					}

					if (pAdditionalDraws != null)
						foreach (var draw in pAdditionalDraws)
							draw.Draw();
				}
				EditorGUILayout.EndVertical();
			}

			pList = list;

			if (GUI.changed)
				EditorPrefs.SetBool(pName, show);

			GUI.backgroundColor = prevColor;

			return show;
		}

		public static string Tabs(string pKey, params string[] pTabsName)
		{
			var tabs = new EditorTabs()
			{
				key = pKey,
				tabsName = pTabsName,
			};
			tabs.Draw();
			return tabs.CurrentTab;
		}

		private static void DrawHeaderTitle(string pHeader)
		{
			var prevColor = GUI.color;

			var boxStyle = new GUIStyle(EditorStyles.toolbar)
			{
				fixedHeight = 0,
				padding = new RectOffset(5, 5, 5, 5)
			};

			var titleStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				fontStyle = FontStyle.Bold,
				normal =
				{
					textColor = Color.white
				},
				alignment = TextAnchor.MiddleCenter
			};

			GUI.color = new Color(0.5f, 0.5f, 0.5f);
			EditorGUILayout.BeginHorizontal(boxStyle, GUILayout.Height(20));
			{
				GUI.color = prevColor;
				EditorGUILayout.LabelField(pHeader, titleStyle, GUILayout.Height(20));
			}
			EditorGUILayout.EndHorizontal();
		}

		public static void DragDropBox<T>(string pName, Action<T[]> pOnDrop) where T : Object
		{
			var evt = Event.current;
			var style = new GUIStyle("Toolbar");
			var dropArea = GUILayoutUtility.GetRect(0.0f, 30, style, GUILayout.ExpandWidth(true));
			GUI.Box(dropArea, $"Drag drop {pName}");

			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!dropArea.Contains(evt.mousePosition))
						return;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						var objs = new List<T>();
						foreach (var obj in DragAndDrop.objectReferences)
						{
							if (obj == null)
								continue;

							if (obj is GameObject gameObject && typeof(T).IsSubclassOf(typeof(Component)))
							{
								var component = gameObject.GetComponent<T>();
								if (component != null)
									objs.Add(component);
							}
							else
							{
								var path = AssetDatabase.GetAssetPath(obj);
								if (IsDirectory(path))
								{
									var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
									foreach (var guid in guids)
									{
										var subObjPath = AssetDatabase.GUIDToAssetPath(guid);
										var subObj = AssetDatabase.LoadAssetAtPath<T>(subObjPath);
										if (subObj != null)
											objs.Add(subObj);
									}
								}
								else
								{
									var s = obj as T;
									if (s != null)
										objs.Add(s);
								}
							}
						}

						pOnDrop(objs.ToArray());
					}

					break;
			}
		}

		public static void DrawTextureIcon(Texture pTexture, Vector2 pSize)
		{
			var rect = EditorGUILayout.GetControlRect(GUILayout.Width(pSize.x), GUILayout.Height(pSize.y));
			GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
			if (pTexture != null)
				GUI.DrawTexture(rect, pTexture, ScaleMode.ScaleToFit);
			else
				GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("console.warnicon"), ScaleMode.ScaleToFit);
		}

		public static ReorderableList CreateReorderableList<T>(T[] pObjects, string pName) where T : Object
		{
			var reorderableList = new ReorderableList(pObjects, typeof(T), true, false, true, true);
			reorderableList.drawElementCallback += (rect, index, a, b) =>
			{
				pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
			};
			reorderableList.elementHeight = 17f;
			reorderableList.headerHeight = 17f;
			reorderableList.drawHeaderCallback += (rect) =>
			{
				EditorGUI.LabelField(rect, pName);
			};
			return reorderableList;
		}

		public static ReorderableList CreateReorderableList<T>(List<T> pObjects, string pName) where T : Object
		{
			var reorderableList = new ReorderableList(pObjects, typeof(T), true, false, true, true);
			reorderableList.drawElementCallback += (rect, index, a, b) =>
			{
				pObjects[index] = (T)EditorGUI.ObjectField(rect, pObjects[index], typeof(T), true);
			};
			reorderableList.elementHeight = 17f;
			reorderableList.headerHeight = 17f;
			reorderableList.drawHeaderCallback += (rect) =>
			{
				EditorGUI.LabelField(rect, pName);
			};
			return reorderableList;
		}

		public static void ReplaceGameObjectsInScene(ref List<GameObject> selections, List<GameObject> prefabs)
		{
			for (var i = selections.Count - 1; i >= 0; --i)
			{
				GameObject newObject;
				var selected = selections[i];
				var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
				if (prefab.IsPrefab())
				{
					newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				}
				else
				{
					newObject = Object.Instantiate(prefab);
					newObject.name = prefab.name;
				}

				if (newObject == null)
				{
					UnityEngine.Debug.LogError("Error instantiating prefab");
					break;
				}

				Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
				newObject.transform.parent = selected.transform.parent;
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
				Undo.DestroyObjectImmediate(selected);
				selections[i] = newObject;
			}
		}

#endregion

		//========================================

#region EditorGui

		public static string TextField(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default) => EditorGui.TextField(value, label, labelWidth, valueWidth, readOnly, color);

		public static string TextArea(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false) => EditorGui.TextArea(value, label, labelWidth, valueWidth, readOnly);

		public static string DropdownList(string value, string label, string[] selections, int labelWidth = 80, int valueWidth = 0) => EditorGui.DropdownList(value, label, selections, labelWidth, valueWidth);

		public static int DropdownList(int value, string label, int[] selections, int labelWidth = 80, int valueWidth = 0) => EditorGui.DropdownList(value, label, selections, labelWidth, valueWidth);

		public static T DropdownListEnum<T>(T value, string label, int labelWidth = 80, int valueWidth = 0) where T : struct, IConvertible => EditorGui.DropdownList(value, label, labelWidth, valueWidth);

		public static T DropdownList<T>(T selectedObj, string label, List<T> pOptions) where T : Object => EditorGui.DropdownList(selectedObj, label, pOptions);

		public static bool Toggle(bool value, string label, int labelWidth = 80, int valueWidth = 0, Color color = default) => EditorGui.Toggle(value, label, labelWidth, valueWidth, color);

		public static int IntField(int value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, int pMin = 0, int pMax = 0) => EditorGui.IntField(value, label, labelWidth, valueWidth, readOnly, pMin, pMax);

		public static float FloatField(float value, string label, int labelWidth = 80, int valueWidth = 0, float pMin = 0, float pMax = 0) => EditorGui.FloatField(value, label, labelWidth, valueWidth, pMin, pMax);

		public static Object ObjectField<T>(Object value, string label, int labelWidth = 80, int valueWidth = 0, bool showAsBox = false) => EditorGui.ObjectField<T>(value, label, labelWidth, valueWidth, showAsBox);

		public static void LabelField(string label, int width = 0, bool isBold = true, TextAnchor pTextAnchor = TextAnchor.MiddleLeft, Color pTextColor = default) => EditorGui.Label(label, width, isBold, pTextAnchor, pTextColor);

		public static Color ColorField(Color value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.ColorField(value, label, labelWidth, valueWidth);

		public static Vector2 Vector2Field(Vector2 value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.Vector2Field(value, label, labelWidth, valueWidth);

		public static Vector3 Vector3Field(Vector3 value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.Vector3Field(value, label, labelWidth, valueWidth);

		public static float[] ArrayField(float[] values, string label, bool showHorizontal = true, int labelWidth = 80, int valueWidth = 0) => EditorGui.ArrayField(values, label, showHorizontal, labelWidth, valueWidth);

		private static object GetValue_Imp(object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}

		private static object GetValue_Imp(object source, string name, int index)
		{
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			//while (index-- >= 0)
			//    enm.MoveNext();
			//return enm.Current;

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}

			return enm.Current;
		}

#endregion

		//========================================

#region SerializedProperty SerializedObject

		public static void SerializeFields(this SerializedProperty pProperty, params string[] properties)
		{
			foreach (var p in properties)
			{
				var item = pProperty.FindPropertyRelative(p);
				EditorGUILayout.PropertyField(item, true);
			}
		}

		public static void SerializeFields(this SerializedObject pObj, params string[] properties)
		{
			foreach (var p in properties)
				pObj.SerializeField(p);
		}

		public static SerializedProperty SerializeField(this SerializedObject pObj, string pPropertyName, string pDisplayName = null, params GUILayoutOption[] options)
		{
			var property = pObj.FindProperty(pPropertyName);
			if (property == null)
			{
				UnityEngine.Debug.LogError($"Not found property {pPropertyName}");
				return null;
			}

			if (!property.isArray)
			{
				EditorGUILayout.PropertyField(property, new GUIContent(string.IsNullOrEmpty(pDisplayName) ? property.displayName : pDisplayName));
				return property;
			}

			if (property.isExpanded)
				EditorGUILayout.PropertyField(property, true, options);
			else
				EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), options);
			return property;
		}

		public static object GetTargetObjectOfProperty(SerializedProperty prop)
		{
			if (prop == null) return null;

			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
					var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal))
						.Replace("[", "")
						.Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}

			return obj;
		}

		public static bool IsFirstElementOfList(this SerializedProperty property)
		{
			string path = property.propertyPath;
			int index = path.LastIndexOf('[');
			if (index < 0)
				return false;

			int endIndex = path.IndexOf(']', index);
			if (endIndex < 0)
				return false;

			int elementIndex = int.Parse(path.Substring(index + 1, endIndex - index - 1));
			return elementIndex == 0;
		}

		public static bool IsInList(this SerializedProperty property)
		{
			return property.displayName.Contains("Element");
		}

#endregion

		//========================================

#region EditorBuildUtil

		public static void RemoveDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.RemoveDirective(pSymbol, pTarget);

		public static void RemoveDirective(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.RemoveDirective(pSymbols, pTarget);

		public static void AddDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.AddDirective(pSymbol, pTarget);

		public static void AddDirectives(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.AddDirectives(pSymbols, pTarget);

		public static string[] GetDirectives(BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.GetDirectives(pTarget);

		public static bool ContainDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.ContainDirective(pSymbol, pTarget);

		public static string[] GetScenePaths() => EditorBuildUtil.GetScenePaths();

		public static string GetBuildName() => EditorBuildUtil.GetBuildName();

#endregion

		//========================================

#region EditorFileUtil

		public static string OpenFolderPanel(string pFolderPath = null) => EditorFileUtil.OpenFolderPanel(pFolderPath);

		public static string FormatPathToUnityPath(string path) => EditorFileUtil.FormatPathToUnityPath(path);

		public static string[] GetDirectories(string path) => EditorFileUtil.GetDirectories(path);


		public static List<string> OpenFilePanelWithFilters(string title, string[] filter) => EditorFileUtil.OpenFilePanelWithFilters(title, filter);

		public static string OpenFilePanel(string title, string extension, string directory = null) => EditorFileUtil.OpenFilePanel(title, extension, directory);

#endregion

		//========================================

#region EditorAssetUtil

		public static void Save(Object pObj) => EditorAssetUtil.Save(pObj);
		
		public static string GetObjectFolderName(Object pObj) => EditorAssetUtil.GetObjectFolderName(pObj);

		public static Object LoadAsset(string path) => EditorAssetUtil.LoadAsset(path);

		public static T LoadAsset<T>(string path) where T : Object => EditorAssetUtil.LoadAsset<T>(path);

		public static string ObjectToGuid(Object obj) => EditorAssetUtil.ObjectToGuid(obj);
		
		public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject => EditorAssetUtil.CreateScriptableAsset<T>(path);
		
		public static List<T> GetObjects<T>(string pPath, string filter, bool getChild = true) where T : Object => EditorAssetUtil.GetObjects<T>(pPath, filter, getChild);

		public static List<AnimationClip> GetAnimClipsFromFBX() => EditorAssetUtil.GetAnimClipsFromFBX();

		public static ModelImporterClipAnimation[] GetAnimationsFromModel(string pPath) => EditorAssetUtil.GetAnimationsFromModel(pPath);

		public static AnimationClip GetAnimationFromModel(string pPath, string pName) => EditorAssetUtil.GetAnimationFromModel(pPath, pName);

		public static void ExportSelectedFoldersToUnityPackage() => EditorAssetUtil.ExportSelectedFoldersToUnityPackage();

		public static void RefreshAssetsInSelectedFolder(string filter) => EditorAssetUtil.RefreshAssetsInSelectedFolder(filter);

		public static void RefreshAssets(string filter, string folderPath = null) => EditorAssetUtil.RefreshAssets(filter, folderPath);

		public static void BuildReferenceMapCache<T>(string[] assetGUIDs, List<T> cachedObjects) where T : Object => EditorAssetUtil.BuildReferenceMapCache(assetGUIDs, cachedObjects);

		public static Dictionary<string, int> SearchAndReplaceGuid<T>(List<T> oldObjects, T newObject, string[] assetGUIDs) where T : Object => EditorAssetUtil.SearchAndReplaceGuid(oldObjects, newObject, assetGUIDs);

		public static string[] ReadMetaFile(Object pObject) => EditorAssetUtil.ReadMetaFile(pObject);

		public static string ReadContentMetaFile(Object pObject) => EditorAssetUtil.ReadContentMetaFile(pObject);

		public static void WriteMetaFile(Object pObject, string[] pLines, bool pRefreshDatabase) => EditorAssetUtil.WriteMetaFile(pObject, pLines, pRefreshDatabase);

		public static Dictionary<string, EditorAssetUtil.SpriteInfo> GetPivotsOfSprites(Sprite pSpriteFrom) => EditorAssetUtil.GetPivotsOfSprites(pSpriteFrom);

		public static void SetTextureReadable(Texture2D p_texture2D, bool p_readable) => EditorAssetUtil.SetTextureReadable(p_texture2D, p_readable);

		public static void CopyPivotAndBorder(Sprite pOriginal, Sprite pTarget, bool pRefreshDatabase) => EditorAssetUtil.CopyPivotAndBorder(pOriginal, pTarget, pRefreshDatabase);

		public static void ExportSpritesFromTexture(Object pObj, string pExportDirectory = null, string pNamePattern = null, bool pRenameOriginal = false) => EditorAssetUtil.ExportSpritesFromTexture(pObj, pExportDirectory, pNamePattern, pRenameOriginal);

		private static bool IsDirectory(string path) => EditorAssetUtil.IsDirectory(path);

		public static Dictionary<GameObject, List<T>> FindComponents<T>(GameObject[] objs, ConditionalDelegate<T> pValidCondition) where T : Component => EditorComponentUtil.FindComponents(objs, pValidCondition);

		public static void ReplaceTextsByTextTMP(params GameObject[] gos) => EditorComponentUtil.ReplaceTextsByTextTMP(gos);

		public static void DrawAssetsList<T>(AssetsList<T> assets, string pDisplayName, bool @readonly = false, List<string> labels = null) where T : Object => EditorLayout.DrawAssetsList(assets, pDisplayName, @readonly, labels);

#endregion
	}
}
#endif