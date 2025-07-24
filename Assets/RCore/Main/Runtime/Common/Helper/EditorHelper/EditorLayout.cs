#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	public static class EditorLayout
	{
		/// <summary>
		/// Draws a foldable group of GUI elements. The foldout state is persisted in EditorPrefs.
		/// </summary>
		/// <param name="label">The text to display for the foldout.</param>
		/// <param name="onFoldoutContent">An action containing the GUI code to be drawn inside the foldout when it is open.</param>
		/// <returns>True if the foldout is currently open (expanded).</returns>
		public static bool Foldout(string label, Action onFoldoutContent)
		{
			var foldout = new GuiFoldout { label = label, onFoldout = onFoldoutContent };
			foldout.Draw();
			return foldout.IsFoldout;
		}

		/// <summary>
		/// Draws a stylized header that can be folded out to reveal content.
		/// Optionally, additional GUI elements can be drawn horizontally next to the header.
		/// </summary>
		/// <param name="label">The text to display on the header.</param>
		/// <param name="key">A unique key to save the foldout's state in EditorPrefs.</param>
		/// <param name="minimalistic">If true, a more compact style is used for the header.</param>
		/// <param name="pHorizontalDraws">An array of IDraw elements to be drawn to the right of the header when expanded.</param>
		/// <returns>True if the header is currently open (expanded).</returns>
		public static bool HeaderFoldout(string label, string key, bool minimalistic = false, params IDraw[] pHorizontalDraws)
		{
			var headerFoldout = new GuiHeaderFoldout()
			{
				key = string.IsNullOrEmpty(key) ? label : key,
				label = label,
				minimalistic = minimalistic,
			};
			if (pHorizontalDraws != null)
				GUILayout.BeginHorizontal();

			headerFoldout.Draw();

			if (pHorizontalDraws != null && headerFoldout.IsFoldout)
				foreach (var d in pHorizontalDraws)
					d.Draw();

			if (pHorizontalDraws != null)
				GUILayout.EndHorizontal();

			return headerFoldout.IsFoldout;
		}

		/// <summary>
		/// Draws a stylized header foldout that contains content within a vertical box group.
		/// </summary>
		/// <param name="label">The text to display on the header.</param>
		/// <param name="key">A unique key to save the foldout's state in EditorPrefs.</param>
		/// <param name="pOnFoldOut">An action containing the GUI code to be drawn inside the box when the header is open.</param>
		/// <param name="pHorizontalDraws">An array of IDraw elements to be drawn to the right of the header.</param>
		public static void HeaderFoldout(string label, string key, Action pOnFoldOut, params IDraw[] pHorizontalDraws)
		{
			var headerFoldout = new GuiHeaderFoldout()
			{
				key = string.IsNullOrEmpty(key) ? label : key,
				label = label,
			};
			if (pHorizontalDraws != null)
				GUILayout.BeginHorizontal();

			headerFoldout.Draw();

			if (pHorizontalDraws != null && headerFoldout.IsFoldout)
				foreach (var d in pHorizontalDraws)
					d.Draw();

			if (pHorizontalDraws != null)
				GUILayout.EndHorizontal();

			if (headerFoldout.IsFoldout)
			{
				var style = new GUIStyle("box")
				{
					margin = new RectOffset(10, 0, 0, 0),
					padding = new RectOffset()
				};
				GUILayout.BeginVertical(style);
				pOnFoldOut();
				GUILayout.EndVertical();
			}
		}

		/// <summary>
		/// Draws a group of selectable tabs. The selected tab state is persisted in EditorPrefs.
		/// </summary>
		/// <param name="key">A unique key to save the current tab's state.</param>
		/// <param name="tabs">An array of strings representing the names of the tabs.</param>
		/// <returns>The name of the currently selected tab.</returns>
		public static string Tabs(string key, params string[] tabs)
		{
			var tabControl = new GuiTabs { key = key, tabsName = tabs };
			tabControl.Draw();
			return tabControl.CurrentTab;
		}

		/// <summary>
		/// Draws a GUI box that acts as a drop area for assets of a specific type.
		/// It handles both files and folders dropped from the Project window.
		/// </summary>
		/// <typeparam name="T">The type of UnityEngine.Object to accept.</typeparam>
		/// <param name="pName">A descriptive name for the type of object to be dropped (e.g., "Prefabs").</param>
		/// <param name="pOnDrop">An action that is invoked with an array of the dropped objects of type T.</param>
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
						// Process all dragged objects.
						foreach (var obj in DragAndDrop.objectReferences)
						{
							if (obj == null)
								continue;

							// Handle component types by checking the dragged GameObject.
							if (obj is GameObject gameObject && typeof(T).IsSubclassOf(typeof(Component)))
							{
								var component = gameObject.GetComponent<T>();
								if (component != null)
									objs.Add(component);
							}
							else
							{
								var path = AssetDatabase.GetAssetPath(obj);
								// If a folder is dropped, find all assets of type T within it.
								if (EditorAssetUtil.IsDirectory(path))
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
								else // It's a single file.
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

		/// <summary>
		/// Draws a read-only list of Unity Objects within a foldout group.
		/// </summary>
		public static void ListReadonlyObjects<T>(string pName, List<T> pList, List<string> pLabels = null, bool pShowObjectBox = true) where T : Object
		{
			ListObjects(pName, ref pList, pLabels, pShowObjectBox, true);
		}

		/// <summary>
		/// Draws a fully featured, editable list of Unity Objects within a foldout group.
		/// Includes pagination, drag-and-drop support, reordering, adding, and removing items.
		/// </summary>
		/// <typeparam name="T">The type of UnityEngine.Object in the list.</typeparam>
		/// <param name="pName">The header name for the list, used as a key for persisting state.</param>
		/// <param name="pObjects">A reference to the list of objects to be displayed and edited.</param>
		/// <param name="pLabels">An optional list of string labels to display next to each object field.</param>
		/// <param name="pShowObjectBox">If true, shows a small preview box for texture-like objects.</param>
		/// <param name="pReadOnly">If true, disables all editing controls.</param>
		/// <param name="pAdditionalDraws">An optional array of IDraw elements to draw at the bottom of the list controls.</param>
		/// <returns>True if the list's foldout is currently open (expanded).</returns>
		public static bool ListObjects<T>(string pName, ref List<T> pObjects, List<string> pLabels, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null) where T : Object
		{
			GUILayout.Space(3);

			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			var list = pObjects;
			var show = HeaderFoldout($"{pName} ({pObjects.Count})", pName);
			if (show)
			{
				// --- Pagination Logic ---
				int page = EditorPrefs.GetInt($"{pName}_page", 0);
				int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
				if (totalPages == 0) totalPages = 1;
				if (page < 0) page = 0;
				if (page >= totalPages) page = totalPages - 1;
				int from = page * 20;
				int to = page * 20 + 20 - 1;
				if (to > list.Count - 1) to = list.Count - 1;

				EditorGUILayout.BeginVertical("box");
				{
					// --- Controls ---
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
						DragDropBox<T>(pName, (objs) =>
						{
							list1.AddRange(objs);
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

					// --- List Item Drawing Loop ---
					for (int i = from; i <= to; i++)
					{
						if (i >= list.Count) continue;
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
							if (pLabels != null && i < pLabels.Count)
								EditorGui.Label(pLabels[i], (int)Mathf.Max(pLabels[i].Length * 8f, 100), false);
							list[i] = (T)EditorGui.ObjectField<T>(list[i], "");
							if (pShowObjectBox)
								list[i] = (T)EditorGui.ObjectField<T>(list[i], "", 0, boxSize, true);

							if (!pReadOnly)
							{
								// --- Reordering and Modification Buttons ---
								if (EditorGui.Button("▲", 23) && i > 0)
									(list[i], list[i - 1]) = (list[i - 1], list[i]); // Swap with previous
								if (EditorGui.Button("▼", 23) && i < list.Count - 1)
									(list[i], list[i + 1]) = (list[i + 1], list[i]); // Swap with next
								if (EditorGui.Button("-", Color.red, 23))
								{
									list.RemoveAt(i);
									i--;
								}
								if (EditorGui.Button("+", Color.green, 23))
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
						// --- Global List Modification Buttons ---
						EditorGUILayout.BeginHorizontal();
						{
							if (EditorGui.Button("+1", Color.green, 30))
							{
								list.Add(null);
								page = totalPages - 1;
								EditorPrefs.SetInt($"{pName}_page", page);
							}
							if (GUILayout.Button("Sort By Name"))
								list = list.OrderBy(m => m.name).ToList();
							if (GUILayout.Button("Remove Duplicate"))
								list.RemoveDuplicated();
							if (EditorGui.Button("Clear", Color.red, 50))
								if (EditorGui.ConfirmPopup())
									list = new List<T>();
						}
						EditorGUILayout.EndHorizontal();
					}

					if (pAdditionalDraws != null)
						foreach (var draw in pAdditionalDraws)
							draw.Draw();
				}
				EditorGUILayout.EndVertical();
			}

			pObjects = list;

			if (GUI.changed)
				EditorPrefs.SetBool(pName, show);

			GUI.backgroundColor = prevColor;

			return show;
		}

		/// <summary>
		/// Draws a paginated view for a large list, invoking a callback to draw each visible item.
		/// This is a generic UI helper for displaying any kind of list data, not just Unity Objects.
		/// </summary>
		/// <param name="pCount">The total number of items in the list.</param>
		/// <param name="pName">A unique name for this paginated view, used as a key for persisting state.</param>
		/// <param name="pOnDraw">An action that is called for each visible item, passing the item's index.</param>
		/// <param name="p_drawAtFirst">Optional array of IDraw elements to draw inside the top pagination bar.</param>
		/// <param name="p_drawAtLast">Optional array of IDraw elements to draw inside the bottom pagination bar.</param>
		public static void PagesForList(int pCount, string pName, Action<int> pOnDraw, IDraw[] p_drawAtFirst = null, IDraw[] p_drawAtLast = null)
		{
			GUILayout.Space(3);

			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			// --- Pagination Logic ---
			int page = EditorPrefs.GetInt($"{pName}_page", 0);
			int totalPages = Mathf.CeilToInt(pCount * 1f / 20f);
			if (totalPages == 0) totalPages = 1;
			if (page < 0) page = 0;
			if (page >= totalPages) page = totalPages - 1;
			int from = page * 20;
			int to = page * 20 + 20 - 1;
			if (to > pCount - 1) to = pCount - 1;

			EditorGUILayout.BeginVertical("box");
			{
				if (totalPages > 1)
				{
					// --- Top Pagination Bar ---
					EditorGUILayout.BeginHorizontal();
					if (EditorGui.Button("\u25c4", 23)) // Left arrow
					{
						if (page > 0) page--;
						EditorPrefs.SetInt($"{pName}_page", page);
					}
					EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");
					if (p_drawAtFirst != null)
					{
						foreach (var draw in p_drawAtFirst)
							draw.Draw();
					}
					if (EditorGui.Button("\u25ba", 23)) // Right arrow
					{
						if (page < totalPages - 1) page++;
						EditorPrefs.SetInt($"{pName}_page", page);
					}
					EditorGUILayout.EndHorizontal();
				}

				// --- Draw Items for Current Page ---
				for (int i = from; i <= to; i++)
				{
					pOnDraw?.Invoke(i);
				}

				if (totalPages > 1)
				{
					// --- Bottom Pagination Bar ---
					EditorGUILayout.BeginHorizontal();
					if (EditorGui.Button("\u25c4", 23))
					{
						if (page > 0) page--;
						EditorPrefs.SetInt($"{pName}_page", page);
					}
					EditorGUILayout.LabelField($"{from + 1}-{to + 1} ({pCount})");
					if (p_drawAtLast != null)
					{
						foreach (var draw in p_drawAtLast)
							draw.Draw();
					}
					if (EditorGui.Button("\u25ba", 23))
					{
						if (page < totalPages - 1) page++;
						EditorPrefs.SetInt($"{pName}_page", page);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndVertical();

			GUI.backgroundColor = prevColor;
		}

				/// <summary>
		/// Draws a fully featured, editable list of Unity Objects with an integrated search field.
		/// Includes pagination, drag-and-drop support, reordering, adding, and removing items, filtered by the search query.
		/// </summary>
		/// <typeparam name="T">The type of UnityEngine.Object in the list.</typeparam>
		/// <param name="pList">A reference to the list of objects to be displayed and edited.</param>
		/// <param name="pName">The header name for the list, used as a key for persisting state.</param>
		/// <param name="pShowObjectBox">If true, shows a small preview box for texture-like objects.</param>
		public static void ListObjectsWithSearch<T>(ref List<T> pList, string pName, bool pShowObjectBox = true) where T : Object
		{
			var prevColor = GUI.color;
			GUI.backgroundColor = new Color(1, 1, 0.5f);

			//bool show = EditorPrefs.GetBool(pName, false);
			//GUIContent content = new GUIContent(pName);
			//GUIStyle style = new GUIStyle(EditorStyles.foldout);
			//style.margin = new RectOffset(pInBox ? 13 : 0, 0, 0, 0);
			//show = EditorGUILayout.Foldout(show, content, style);

			// --- Header and State Management ---
			var list = pList;
			string search = EditorPrefs.GetString($"{pName}_search");
			var show = HeaderFoldout($"{pName} ({pList.Count})", pName);
			if (show)
			{
				// --- Pagination Logic ---
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
					// --- Controls ---
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

					// --- Search Field ---
					search = GUILayout.TextField(search);

					// --- List Item Drawing Loop ---
					bool searching = !string.IsNullOrEmpty(search);
					for (int i = from; i <= to; i++)
					{
						if (i >= list.Count)
							continue;
						
						// Apply search filter (case-insensitive).
						if (searching && !list[i].name.ToLower().Contains(search.ToLower()))
							continue;

						// Apply pagination after filtering (this is a simplified approach; for large lists, filtering before pagination is better).
						if(i < from || i > to)
							continue;

						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
							list[i] = (T)EditorGui.ObjectField<T>(list[i], "");
							if (pShowObjectBox)
								list[i] = (T)EditorGui.ObjectField<T>(list[i], "", 0, boxSize, true);
							
							// --- Reordering and Modification Buttons ---
							if (EditorGui.Button("▲", 23) && i > 0)
								(list[i], list[i - 1]) = (list[i - 1], list[i]);
							if (EditorGui.Button("▼", 23) && i < list.Count - 1)
								(list[i], list[i + 1]) = (list[i + 1], list[i]);
							if (EditorGui.Button("-", Color.red, 23))
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

					// --- Global List Modification Buttons ---
					EditorGUILayout.BeginHorizontal();
					{
						if (EditorGui.Button("+1", Color.green, 30))
						{
							list.Add(null);
							page = totalPages - 1;
							EditorPrefs.SetInt($"{pName}_page", page);
						}

						if (GUILayout.Button("Sort By Name"))
							list = list.OrderBy(m => m.name).ToList();
						if (GUILayout.Button("Remove Duplicate"))
							list.RemoveDuplicated();

						if (EditorGui.Button("Clear", Color.red, 50))
							if (EditorGui.ConfirmPopup())
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

		/// <summary>
		/// Draws an editable list of key-value pairs where the value is a Unity Object.
		/// Supports different key types (Enum, int, string) and provides controls for managing the list.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TValue">The type of the value, which must be a UnityEngine.Object.</typeparam>
		/// <param name="pName">The header name for the list, used as a key for persisting state.</param>
		/// <param name="pList">A reference to the list of key-value pairs to be displayed and edited.</param>
		/// <returns>True if the list's foldout is currently open (expanded).</returns>
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
				// --- Pagination Logic ---
				int page = EditorPrefs.GetInt($"{pName}_page", 0);
				int totalPages = Mathf.CeilToInt(list.Count * 1f / 20f);
				if (totalPages == 0) totalPages = 1;
				if (page < 0) page = 0;
				if (page >= totalPages) page = totalPages - 1;
				int from = page * 20;
				int to = page * 20 + 20 - 1;
				if (to > list.Count - 1) to = list.Count - 1;

				EditorGUILayout.BeginVertical("box");
				{
					// --- Controls ---
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
						// When objects are dropped, create new key-value pairs with default keys.
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

					// --- List Item Drawing Loop ---
					for (int i = from; i <= to; i++)
					{
						if (i >= list.Count || list[i] == null)
							continue;
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25));
							// --- Key Field Drawing (handles Enum, int, string) ---
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
							// --- Value Field Drawing ---
							list[i].v = (TValue)EditorGui.ObjectField<TValue>(list[i].v, "");
							if (pShowObjectBox)
								list[i].v = (TValue)EditorGui.ObjectField<TValue>(list[i].v, "", 0, boxSize, true);

							if (!pReadOnly)
							{
								if (EditorGui.Button("▲", 23) && i > 0)
									(list[i], list[i - 1]) = (list[i - 1], list[i]);

								if (EditorGui.Button("▼", 23) && i < list.Count - 1)
									(list[i], list[i + 1]) = (list[i + 1], list[i]);

								if (EditorGui.Button("-", Color.red, 23))
								{
									list.RemoveAt(i);
									i--;
								}

								if (EditorGui.Button("+", Color.green, 23))
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
						// --- Global List Modification Buttons ---
						EditorGUILayout.BeginHorizontal();
						{
							if (EditorGui.Button("+1", Color.green, 30))
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
							if (EditorGui.Button("Clear", Color.red, 50))
								if (EditorGui.ConfirmPopup())
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

		/// <summary>
		/// Draws a scrollable view area.
		/// </summary>
		/// <param name="scrollPos">A reference to the Vector2 storing the scroll position.</param>
		/// <param name="height">The fixed height of the scroll view area.</param>
		/// <param name="action">An action containing the GUI code to be drawn inside the scroll view.</param>
		/// <returns>The updated scroll position vector.</returns>
		public static Vector2 ScrollView(ref Vector2 scrollPos, float height, Action action)
		{
			EditorGUILayout.BeginVertical("box");
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
			action?.Invoke();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
			return scrollPos;
		}

		/// <summary>
		/// Creates a Unity `ReorderableList` for an array of Unity Objects.
		/// </summary>
		/// <typeparam name="T">The type of the Object in the array.</typeparam>
		/// <param name="pObjects">The array to be displayed in the list.</param>
		/// <param name="pName">The header name for the list.</param>
		/// <returns>A configured ReorderableList instance.</returns>
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

		/// <summary>
		/// Creates a Unity `ReorderableList` for a List of Unity Objects.
		/// </summary>
		/// <typeparam name="T">The type of the Object in the list.</typeparam>
		/// <param name="pObjects">The List to be displayed.</param>
		/// <param name="pName">The header name for the list.</param>
		/// <returns>A configured ReorderableList instance.</returns>
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

		/// <summary>
		/// A specialized drawing function for an `AssetsList<T>` type, integrating the default asset field
		/// with the main list editor.
		/// </summary>
		/// <typeparam name="T">The type of asset in the list.</typeparam>
		/// <param name="assets">The AssetsList instance to draw.</param>
		/// <param name="pDisplayName">The header name for the list.</param>
		/// <param name="readonly">If true, the list is not editable.</param>
		/// <param name="labels">Optional labels for each item in the list.</param>
		public static void DrawAssetsList<T>(AssetsList<T> assets, string pDisplayName, bool @readonly = false, List<string> labels = null) where T : Object
		{
			bool showBox = assets.defaultAsset is Sprite;
			var draw = new GuiObject<T>()
			{
				value = assets.defaultAsset,
				label = "Default",
			};
			// Draw the main list, passing the "Default Asset" field as an additional control to draw at the bottom.
			if (ListObjects(pDisplayName, ref assets.source, labels, showBox, @readonly, new IDraw[] { draw }))
				assets.defaultAsset = (T)draw.OutputValue;
		}

		/// <summary>
		/// Draws a list of IDraw elements in a grid layout.
		/// </summary>
		/// <param name="pCell">The number of cells (columns) in the grid.</param>
		/// <param name="pDraws">The list of IDraw elements to draw.</param>
		/// <param name="color">An optional background color for the grid container.</param>
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

		/// <summary>
		/// Draws a scrollable view area. An alias for `ScrollView`.
		/// </summary>
		/// <param name="scrollPos">A reference to the Vector2 storing the scroll position.</param>
		/// <param name="height">The fixed height of the scroll view area.</param>
		/// <param name="action">An action containing the GUI code to be drawn inside the scroll view.</param>
		/// <returns>The updated scroll position vector.</returns>
		public static Vector2 ScrollBar(ref Vector2 scrollPos, float height, Action action)
		{
			EditorGUILayout.BeginVertical("box");
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height));
			action();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			return scrollPos;
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
	}
}
#endif