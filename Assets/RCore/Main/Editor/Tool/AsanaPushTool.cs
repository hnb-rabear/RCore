/***
 * Author HNB-RaBear
 * Unity Editor tool to push Markdown tasklists to Asana as subtasks.
 * Supports Refresh (delete all + recreate) and Update (smart sync by name) modes.
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore.Editor.Tool
{
	public class AsanaPushTool : EditorWindow
	{
		private enum PushMode
		{
			ReplaceAll,
			Update,
		}

		private const string API_BASE = "https://app.asana.com/api/1.0";
		private const string PREF_PAT = "RCore_AsanaPAT";
		private const string PREF_TASK_ID = "RCore_AsanaTaskId";

		private string m_Pat;
		private string m_ParentTaskId;
		private string m_MarkdownPath;
		private TextAsset m_MarkdownAsset;
		private List<TaskNode> m_ParsedTasks = new List<TaskNode>();
		private Vector2 m_ScrollPreview;
		private Vector2 m_ScrollLog;
		private bool m_ShowPreview;
		private bool m_IsPushing;
		private bool m_ShowPat;
		private List<string> m_Logs = new List<string>();
		private int m_TotalSteps;
		private int m_CompletedSteps;
		private PushMode m_PushMode = PushMode.Update;

		[MenuItem("RCore/Tools/Push to Asana", priority = 110 + 7)]
		public static void ShowWindow()
		{
			var window = GetWindow<AsanaPushTool>("Asana Push Tool");
			window.minSize = new Vector2(450, 500);
		}

		private void OnEnable()
		{
			m_Pat = EditorPrefs.GetString(PREF_PAT, "");
			m_ParentTaskId = EditorPrefs.GetString(PREF_TASK_ID, "");
		}

		private void OnGUI()
		{
			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField("Asana Push Tool", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Push markdown tasklists to Asana as hierarchical subtasks.\nModes: Replace All (delete + recreate) | Update (smart sync)", MessageType.Info);
			EditorGUILayout.Space(4);

			// --- Settings ---
			EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

			// PAT field with show/hide toggle
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			if (m_ShowPat)
				m_Pat = EditorGUILayout.TextField("Personal Access Token", m_Pat);
			else
				m_Pat = EditorGUILayout.PasswordField("Personal Access Token", m_Pat);
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetString(PREF_PAT, m_Pat);
			if (GUILayout.Button(m_ShowPat ? "Hide" : "Show", GUILayout.Width(45)))
				m_ShowPat = !m_ShowPat;
			EditorGUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(m_Pat))
				EditorGUILayout.HelpBox("✅ Token saved (persists across sessions)", MessageType.None);

			EditorGUI.BeginChangeCheck();
			m_ParentTaskId = EditorGUILayout.TextField("Parent Task (ID or URL)", m_ParentTaskId);
			if (EditorGUI.EndChangeCheck())
			{
				m_ParentTaskId = ExtractTaskIdFromInput(m_ParentTaskId);
				EditorPrefs.SetString(PREF_TASK_ID, m_ParentTaskId);
			}

			EditorGUILayout.Space(8);

			// --- Markdown Source ---
			EditorGUILayout.LabelField("Markdown Source", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			m_MarkdownAsset = (TextAsset)EditorGUILayout.ObjectField("Markdown File", m_MarkdownAsset, typeof(TextAsset), false);
			if (EditorGUI.EndChangeCheck() && m_MarkdownAsset != null)
			{
				m_MarkdownPath = AssetDatabase.GetAssetPath(m_MarkdownAsset);
				m_ShowPreview = false;
			}

			EditorGUILayout.BeginHorizontal();
			m_MarkdownPath = EditorGUILayout.TextField("Or file path", m_MarkdownPath ?? "");
			if (GUILayout.Button("Browse", GUILayout.Width(60)))
			{
				string path = EditorUtility.OpenFilePanel("Select Markdown File", Application.dataPath, "md");
				if (!string.IsNullOrEmpty(path))
					m_MarkdownPath = path;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(8);

			// --- Actions ---
			EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_MarkdownPath));
			if (GUILayout.Button("Preview Tasks", GUILayout.Height(28)))
			{
				ParseMarkdown();
				m_ShowPreview = true;
			}
			EditorGUI.EndDisabledGroup();

			// --- Preview ---
			if (m_ShowPreview && m_ParsedTasks.Count > 0)
			{
				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField($"Preview ({CountAllTasks()} tasks)", EditorStyles.boldLabel);

				m_ScrollPreview = EditorGUILayout.BeginScrollView(m_ScrollPreview, GUILayout.MaxHeight(200));
				foreach (var task in m_ParsedTasks)
					DrawTaskPreview(task, 0);
				EditorGUILayout.EndScrollView();

				EditorGUILayout.Space(8);

				// --- Push Mode + Button ---
				EditorGUILayout.LabelField("Push Mode", EditorStyles.boldLabel);
				m_PushMode = (PushMode)EditorGUILayout.EnumPopup("Mode", m_PushMode);

				string modeDesc = m_PushMode == PushMode.ReplaceAll
					? "⚠️ Delete ALL existing subtasks and recreate from markdown (loses comments/assignees)"
					: "📝 Compare by name, only sync changes (preserves comments/assignees)";
				EditorGUILayout.HelpBox(modeDesc, m_PushMode == PushMode.ReplaceAll ? MessageType.Warning : MessageType.Info);

				EditorGUILayout.Space(4);

				EditorGUI.BeginDisabledGroup(m_IsPushing || string.IsNullOrEmpty(m_Pat) || string.IsNullOrEmpty(m_ParentTaskId));
				string btnLabel = m_IsPushing
					? $"Pushing... ({m_CompletedSteps}/{m_TotalSteps})"
					: (m_PushMode == PushMode.ReplaceAll ? "🔄 Replace All" : "📝 Update");

				if (GUILayout.Button(btnLabel, GUILayout.Height(32)))
				{
					string confirmMsg = m_PushMode == PushMode.ReplaceAll
						? $"REPLACE ALL: Will DELETE all existing subtasks and recreate {CountAllTasks()} tasks.\nAll comments, assignees, and history will be lost.\n\nContinue?"
						: $"UPDATE: Will sync {CountAllTasks()} tasks (add new, update changed, remove extra).\nComments and assignees will be preserved.\n\nContinue?";

					if (EditorUtility.DisplayDialog("Push to Asana", confirmMsg, "Push", "Cancel"))
					{
						m_Logs.Clear();
						StartPush();
					}
				}
				EditorGUI.EndDisabledGroup();
			}

			// --- Log ---
			if (m_Logs.Count > 0)
			{
				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
				m_ScrollLog = EditorGUILayout.BeginScrollView(m_ScrollLog, GUILayout.MaxHeight(150));
				foreach (string log in m_Logs)
					EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
				EditorGUILayout.EndScrollView();
			}
		}

		#region Markdown Parser

		private class TaskNode
		{
			public string Name;
			public bool Completed;
			public List<TaskNode> Children = new List<TaskNode>();
		}

		private void ParseMarkdown()
		{
			m_ParsedTasks.Clear();

			string content;
			if (!string.IsNullOrEmpty(m_MarkdownPath) && File.Exists(m_MarkdownPath))
				content = File.ReadAllText(m_MarkdownPath, Encoding.UTF8);
			else if (m_MarkdownAsset != null)
				content = m_MarkdownAsset.text;
			else
				return;

			var lines = content.Split('\n');
			TaskNode currentSection = null;

			foreach (string rawLine in lines)
			{
				string line = rawLine.TrimEnd('\r');

				var sectionMatch = Regex.Match(line, @"^##\s+(?:\d+\.\s*)?(.+)$");
				if (sectionMatch.Success)
				{
					string name = sectionMatch.Groups[1].Value.Trim();
					bool completed = name.Contains("✅");
					name = name.Replace("✅", "").Trim();
					currentSection = new TaskNode { Name = name, Completed = completed };
					m_ParsedTasks.Add(currentSection);
					continue;
				}

				var itemMatch = Regex.Match(line, @"^\s*- \[([ xX])\]\s+(.+)$");
				if (itemMatch.Success && currentSection != null)
				{
					bool completed = itemMatch.Groups[1].Value.ToLower() == "x";
					string name = itemMatch.Groups[2].Value.Trim();
					currentSection.Children.Add(new TaskNode { Name = name, Completed = completed });
				}
			}
		}

		private int CountAllTasks()
		{
			int count = 0;
			foreach (var t in m_ParsedTasks)
				count += 1 + t.Children.Count;
			return count;
		}

		#endregion

		#region Preview UI

		private void DrawTaskPreview(TaskNode node, int indent)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(indent * 20);
			string prefix = indent == 0 ? "📁" : "  •";
			string status = node.Completed ? " ✅" : "";
			EditorGUILayout.LabelField($"{prefix} {node.Name}{status}");
			EditorGUILayout.EndHorizontal();

			foreach (var child in node.Children)
				DrawTaskPreview(child, indent + 1);
		}

		#endregion

		#region Coroutine Runner

		private Stack<IEnumerator> m_CoroutineStack = new Stack<IEnumerator>();

		private void StartPush()
		{
			m_IsPushing = true;
			m_CompletedSteps = 0;
			m_TotalSteps = 0; // will be calculated during execution

			m_CoroutineStack.Clear();
			if (m_PushMode == PushMode.ReplaceAll)
				m_CoroutineStack.Push(ReplaceAllFlow());
			else
				m_CoroutineStack.Push(UpdateFlow());

			EditorApplication.update += PushUpdate;
		}

		private void PushUpdate()
		{
			if (m_CoroutineStack.Count == 0)
			{
				EditorApplication.update -= PushUpdate;
				m_IsPushing = false;
				Log("✅ Done!");
				Repaint();
				return;
			}

			var current = m_CoroutineStack.Peek();
			if (!current.MoveNext())
			{
				m_CoroutineStack.Pop();
			}
			else if (current.Current is IEnumerator nested)
			{
				m_CoroutineStack.Push(nested);
			}
			Repaint();
		}

		#endregion

		#region Replace All Flow

		private IEnumerator ReplaceAllFlow()
		{
			Log("🔄 REPLACE ALL: Fetching existing subtasks...");

			// Step 1: Fetch existing level 1 subtasks
			var fetchReq = GetSubtasks(m_ParentTaskId);
			yield return WaitForRequest(fetchReq);

			var existingTasks = ParseSubtasksResponse(fetchReq);
			if (existingTasks == null)
			{
				LogRequestError("Failed to fetch subtasks", fetchReq);
				yield break;
			}

			// Step 2: For each level 1, fetch and delete its children first
			m_TotalSteps = existingTasks.Count + CountAllTasks(); // delete + create
			Log($"📋 Found {existingTasks.Count} existing subtasks to delete");

			foreach (var task in existingTasks)
			{
				// Fetch level 2 children
				var fetchChildrenReq = GetSubtasks(task.Id);
				yield return WaitForRequest(fetchChildrenReq);

				var children = ParseSubtasksResponse(fetchChildrenReq);
				if (children != null)
				{
					m_TotalSteps += children.Count;
					foreach (var child in children)
					{
						var delChild = DeleteTask(child.Id);
						yield return WaitForRequest(delChild);
						m_CompletedSteps++;
					}
				}

				// Delete level 1
				var delReq = DeleteTask(task.Id);
				yield return WaitForRequest(delReq);
				m_CompletedSteps++;
				Log($"🗑️ [{m_CompletedSteps}/{m_TotalSteps}] Deleted: {task.Name}");
			}

			// Step 3: Create all from markdown
			Log("📝 Creating tasks from markdown...");
			yield return CreateAllTasks();
		}

		#endregion

		#region Update Flow

		private IEnumerator UpdateFlow()
		{
			Log("📝 UPDATE: Fetching existing subtasks...");

			// Step 1: Fetch existing level 1 subtasks
			var fetchReq = GetSubtasks(m_ParentTaskId);
			yield return WaitForRequest(fetchReq);

			var existingSections = ParseSubtasksResponse(fetchReq);
			if (existingSections == null)
			{
				LogRequestError("Failed to fetch subtasks", fetchReq);
				yield break;
			}

			Log($"📋 Found {existingSections.Count} existing sections");
			m_TotalSteps = CountAllTasks();

			// Step 2: Sync each section
			var processedSectionIds = new HashSet<string>();

			foreach (var mdSection in m_ParsedTasks)
			{
				var match = existingSections.FirstOrDefault(e => NormalizeName(e.Name) == NormalizeName(mdSection.Name));

				if (match != null)
				{
					processedSectionIds.Add(match.Id);

					// Update section completed status if changed
					if (match.Completed != mdSection.Completed)
					{
						var updateReq = UpdateTask(match.Id, mdSection.Name, mdSection.Completed);
						yield return WaitForRequest(updateReq);
						Log($"📝 [{m_CompletedSteps + 1}/{m_TotalSteps}] Updated: {mdSection.Name}");
					}
					else
					{
						Log($"✓ [{m_CompletedSteps + 1}/{m_TotalSteps}] Unchanged: {mdSection.Name}");
					}
					m_CompletedSteps++;

					// Sync children of this section
					yield return SyncChildren(match.Id, mdSection.Children);
				}
				else
				{
					// Create new section + children
					var createReq = CreateSubtask(m_ParentTaskId, mdSection.Name, mdSection.Completed);
					yield return WaitForRequest(createReq);

					string newId = ExtractGid(createReq);
					if (newId == null)
					{
						LogRequestError("Failed to create: " + mdSection.Name, createReq);
						yield break;
					}
					m_CompletedSteps++;
					Log($"🆕 [{m_CompletedSteps}/{m_TotalSteps}] Created: {mdSection.Name}");

					foreach (var child in mdSection.Children)
					{
						var childReq = CreateSubtask(newId, child.Name, child.Completed);
						yield return WaitForRequest(childReq);

						if (ExtractGid(childReq) == null)
						{
							LogRequestError("Failed to create: " + child.Name, childReq);
							yield break;
						}
						m_CompletedSteps++;
						Log($"  🆕 [{m_CompletedSteps}/{m_TotalSteps}] Created: {child.Name}");
					}
				}
			}

			// Step 3: Delete sections on Asana that are NOT in markdown
			foreach (var existing in existingSections)
			{
				if (!processedSectionIds.Contains(existing.Id))
				{
					// Delete children first
					var fetchChildrenReq = GetSubtasks(existing.Id);
					yield return WaitForRequest(fetchChildrenReq);
					var children = ParseSubtasksResponse(fetchChildrenReq);
					if (children != null)
					{
						foreach (var child in children)
						{
							var delChild = DeleteTask(child.Id);
							yield return WaitForRequest(delChild);
						}
					}

					var delReq = DeleteTask(existing.Id);
					yield return WaitForRequest(delReq);
					Log($"🗑️ Removed: {existing.Name} (not in markdown)");
				}
			}
		}

		private IEnumerator SyncChildren(string parentId, List<TaskNode> mdChildren)
		{
			// Fetch existing children
			var fetchReq = GetSubtasks(parentId);
			yield return WaitForRequest(fetchReq);

			var existingChildren = ParseSubtasksResponse(fetchReq) ?? new List<AsanaTask>();
			var processedIds = new HashSet<string>();

			foreach (var mdChild in mdChildren)
			{
				var match = existingChildren.FirstOrDefault(e => NormalizeName(e.Name) == NormalizeName(mdChild.Name));

				if (match != null)
				{
					processedIds.Add(match.Id);

					if (match.Completed != mdChild.Completed || NormalizeName(match.Name) != NormalizeName(mdChild.Name))
					{
						var updateReq = UpdateTask(match.Id, mdChild.Name, mdChild.Completed);
						yield return WaitForRequest(updateReq);
						Log($"  📝 [{m_CompletedSteps + 1}/{m_TotalSteps}] Updated: {mdChild.Name}");
					}
					else
					{
						Log($"  ✓ [{m_CompletedSteps + 1}/{m_TotalSteps}] Unchanged: {mdChild.Name}");
					}
				}
				else
				{
					var createReq = CreateSubtask(parentId, mdChild.Name, mdChild.Completed);
					yield return WaitForRequest(createReq);

					if (ExtractGid(createReq) == null)
					{
						LogRequestError("Failed to create: " + mdChild.Name, createReq);
						yield break;
					}
					Log($"  🆕 [{m_CompletedSteps + 1}/{m_TotalSteps}] Created: {mdChild.Name}");
				}
				m_CompletedSteps++;
			}

			// Delete children on Asana not in markdown
			foreach (var existing in existingChildren)
			{
				if (!processedIds.Contains(existing.Id))
				{
					var delReq = DeleteTask(existing.Id);
					yield return WaitForRequest(delReq);
					Log($"  🗑️ Removed: {existing.Name}");
				}
			}
		}

		private string NormalizeName(string name)
		{
			return name?.Trim().ToLowerInvariant() ?? "";
		}

		#endregion

		#region Create All Tasks (shared by Refresh)

		private IEnumerator CreateAllTasks()
		{
			foreach (var section in m_ParsedTasks)
			{
				var req1 = CreateSubtask(m_ParentTaskId, section.Name, section.Completed);
				yield return WaitForRequest(req1);

				string sectionId = ExtractGid(req1);
				if (sectionId == null)
				{
					LogRequestError("Failed to create: " + section.Name, req1);
					yield break;
				}
				m_CompletedSteps++;
				Log($"✅ [{m_CompletedSteps}/{m_TotalSteps}] {section.Name}");

				foreach (var item in section.Children)
				{
					var req2 = CreateSubtask(sectionId, item.Name, item.Completed);
					yield return WaitForRequest(req2);

					if (ExtractGid(req2) == null)
					{
						LogRequestError("Failed to create: " + item.Name, req2);
						yield break;
					}
					m_CompletedSteps++;
					Log($"  ✅ [{m_CompletedSteps}/{m_TotalSteps}] {item.Name}");
				}
			}
		}

		#endregion

		#region Asana API

		[Serializable]
		private class AsanaTaskData
		{
			public string gid;
			public string name;
			public bool completed;
		}

		[Serializable]
		private class AsanaResponse
		{
			public AsanaTaskData[] data;
		}

		private class AsanaTask
		{
			public string Id;
			public string Name;
			public bool Completed;
		}

		private UnityWebRequest CreateSubtask(string parentTaskId, string name, bool completed)
		{
			string url = $"{API_BASE}/tasks/{parentTaskId}/subtasks";
			string json = $"{{\"data\":{{\"name\":\"{EscapeJson(name)}\",\"completed\":{(completed ? "true" : "false")}}}}}";
			return SendRequest("POST", url, json);
		}

		private UnityWebRequest UpdateTask(string taskId, string name, bool completed)
		{
			string url = $"{API_BASE}/tasks/{taskId}";
			string json = $"{{\"data\":{{\"name\":\"{EscapeJson(name)}\",\"completed\":{(completed ? "true" : "false")}}}}}";
			return SendRequest("PUT", url, json);
		}

		private UnityWebRequest DeleteTask(string taskId)
		{
			string url = $"{API_BASE}/tasks/{taskId}";
			return SendRequest("DELETE", url, null);
		}

		private UnityWebRequest GetSubtasks(string taskId)
		{
			string url = $"{API_BASE}/tasks/{taskId}/subtasks?opt_fields=name,completed";
			return SendRequest("GET", url, null);
		}

		private UnityWebRequest SendRequest(string method, string url, string json)
		{
			var req = new UnityWebRequest(url, method);
			if (json != null)
				req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Authorization", "Bearer " + m_Pat);
			req.SetRequestHeader("Content-Type", "application/json");
			req.SendWebRequest();
			return req;
		}

		private IEnumerator WaitForRequest(UnityWebRequest req)
		{
			while (!req.isDone)
				yield return null;
		}

		private string ExtractGid(UnityWebRequest req)
		{
			if (req.result != UnityWebRequest.Result.Success)
				return null;
			try
			{
				var response = JsonUtility.FromJson<AsanaResponse>(req.downloadHandler.text);
				if (response?.data != null && response.data.Length > 0)
					return response.data[0].gid;
			}
			catch { }

			// Fallback: response for single task creation wraps in {"data":{"gid":"..."}}
			string body = req.downloadHandler.text;
			var match = Regex.Match(body, "\"gid\"\\s*:\\s*\"(\\d+)\"");
			return match.Success ? match.Groups[1].Value : null;
		}

		private List<AsanaTask> ParseSubtasksResponse(UnityWebRequest req)
		{
			if (req.result != UnityWebRequest.Result.Success)
				return null;

			var tasks = new List<AsanaTask>();
			string body = req.downloadHandler.text;

			try
			{
				var response = JsonUtility.FromJson<AsanaResponse>(body);
				if (response?.data != null)
				{
					foreach (var item in response.data)
					{
						if (!string.IsNullOrEmpty(item.gid))
						{
							tasks.Add(new AsanaTask
							{
								Id = item.gid,
								Name = item.name ?? "",
								Completed = item.completed
							});
						}
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[AsanaPush] JSON parse error: {e.Message}\nBody: {body}");
			}

			Log($"   (Parsed {tasks.Count} tasks from API)");
			return tasks;
		}

		private string ExtractTaskIdFromInput(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			input = input.Trim();
			var urlMatch = Regex.Match(input, @"/task/(\d+)");
			if (urlMatch.Success) return urlMatch.Groups[1].Value;

			var oldUrlMatch = Regex.Match(input, @"asana\.com/0/\d+/(\d+)");
			if (oldUrlMatch.Success) return oldUrlMatch.Groups[1].Value;

			return input;
		}

		private string EscapeJson(string s)
		{
			return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
		}

		private string UnescapeJson(string s)
		{
			return s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n");
		}

		private void LogRequestError(string context, UnityWebRequest req)
		{
			Log($"❌ {context}");
			Log($"   HTTP {req.responseCode} | {req.result}");
			if (!string.IsNullOrEmpty(req.error))
				Log($"   Error: {req.error}");
			if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
				Log($"   Response: {req.downloadHandler.text}");
		}

		private void Log(string msg)
		{
			m_Logs.Add(msg);
			m_ScrollLog = new Vector2(0, float.MaxValue); // Auto-scroll to bottom
			Debug.Log("[AsanaPush] " + msg);
		}

		#endregion
	}
}
