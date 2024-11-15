/***
 * Author HNB-RaBear - 2017
 **/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace RCore
{
	public static class Debug
	{
		public static Action OnEnabled;
		private static readonly StringBuilder m_StrBuilder = new StringBuilder();

		static Debug()
		{
			m_Enabled = PlayerPrefs.GetInt("EnabledLog", 0) == 1;
		}

		private static bool m_Enabled;
		public static bool Enabled
		{
			get => m_Enabled;
			set
			{
				if (m_Enabled == value)
					return;
				m_Enabled = value;
				PlayerPrefs.SetInt("EnabledLog", value ? 1 : 0);
				OnEnabled?.Invoke();
			}
		}

		public static void Log(object message)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append(message);
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		public static void Log(object message, bool pShowTime)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			if (pShowTime)
				m_StrBuilder.Append($"[{DateTime.Now}] ").Append(message);
			else
				m_StrBuilder.Append("[H] ").Append(message);
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		public static void Log(object message, Color pColor = default)
		{
			if (!Enabled)
				return;
			string c = pColor == default ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append($"<color=\"#{c}\">{message}</color>");
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		public static void Log(object title, object message, Color pColor = default)
		{
			if (!Enabled)
				return;
			string c = pColor == default ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append($"<b><color=\"#{c}\">{title}</color></b> {message}");
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		public static void LogError(object message, bool pShowTime = false)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			if (pShowTime)
				m_StrBuilder.Append($"[{DateTime.Now}] ").Append(message);
			else
				m_StrBuilder.Append("[H] ").Append(message);
			UnityEngine.Debug.LogError(m_StrBuilder);
		}

		public static void LogWarning(object message, bool pShowTime = false)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			if (pShowTime)
				m_StrBuilder.Append($"[{DateTime.Now}] ").Append(message);
			else
				m_StrBuilder.Append("[H] ").Append(message);
			UnityEngine.Debug.LogWarning(m_StrBuilder);
		}

		public static void LogException(Exception e, bool pShowTime = false)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			if (pShowTime)
				m_StrBuilder.Append($"[{DateTime.Now}] ").Append(e);
			else
				m_StrBuilder.Append("[H] ").Append(e);
			UnityEngine.Debug.LogError(m_StrBuilder);
		}

		public static void LogJson<T>(T pObj, Color pColor = default) where T : class
		{
			if (!Enabled || pObj == null)
				return;
			var jsonStr = JsonUtility.ToJson(pObj);
			if (jsonStr != "{}")
			{
				string c = pColor == default ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
				m_StrBuilder.Remove(0, m_StrBuilder.Length);
				m_StrBuilder.Append("[H] ").Append(string.Format("<color=\"#{2}\">{0}</color>{1}", pObj.GetType().FullName, jsonStr, c));
				UnityEngine.Debug.Log(m_StrBuilder);
			}
		}

		public static void LogNewtonJson(object pObj, Color pColor = default)
		{
			if (!Enabled)
				return;
			/*
			var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(pObj);
			if (jsonStr != "{}")
			{
			    string c = pColor == default(Color) ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
			    UnityEngine.Debug.Log(string.Format("<color=\"#{2}\">{0}</color>{1}", pObj.GetType().FullName, jsonStr, c));
			}
			*/
		}

		/// <summary>
		/// Print out an array.
		/// </summary>
		/// <typeparam name="T">Type of the array</typeparam>
		/// <param name="array">Array to print.</param>
		/// <param name="additionalText">Additional text to print.</param>
		public static void LogArray<T>(T[] array, string additionalText = "")
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append(additionalText).Append("{");
			for (int i = 0; i < array.Length; i++)
			{
				m_StrBuilder.Append((array[i] != null ? array[i].ToString() : "null") + (i < array.Length - 1 ? ", " : ""));
			}
			m_StrBuilder.Append("}");
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		/// <summary>
		/// Print a dictionary.
		/// </summary>
		/// <typeparam name="TKey">Key type.</typeparam>
		/// <typeparam name="TValue">Value type.</typeparam>
		/// <param name="dict">Dictionary to print</param>
		/// <param name="additionalText">Additional text to print before dict</param>
		public static void LogDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, string additionalText = "")
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append(additionalText).Append("{");
			int i = 0;
			foreach (var pair in dict)
			{
				i++;
				m_StrBuilder.Append("{" + pair.Key + ":" + pair.Value + "}");
				if (i < dict.Count - 1)
					m_StrBuilder.Append(",");
			}
			m_StrBuilder.Append("}");
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		/// <summary>
		/// Print a List.
		/// </summary>
		/// <param name="list">List.</param>
		/// <param name="additionalText">Massage before list.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static void LogList<T>(List<T> list, string additionalText = "")
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append(additionalText).Append("{");
			for (int i = 0; i < list.Count; i++)
			{
				m_StrBuilder.Append(list[i]);
				if (i < list.Count - 1)
					m_StrBuilder.Append(",");
			}
			m_StrBuilder.Append("}");
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		/// <summary>
		/// Print a log to check if objects are null or not.
		/// </summary>
		/// <param name="objs">Objects to check.</param>
		public static void LogNull(params object[] objs)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append("[H] ").Append("{");
			for (int i = 0; i < objs.Length; i++)
			{
				m_StrBuilder.Append(objs[i]);
				if (i < objs.Length - 1)
					m_StrBuilder.Append(",");
			}
			UnityEngine.Debug.Log(m_StrBuilder);
		}

		[Conditional("UNITY_EDITOR")]
		public static void LogToFile(object context, string fileName, bool deleteAfterQuit = true)
		{
			if (Enabled && Application.isEditor)
			{
				var sr = new StreamReader(fileName);
				var sw = new StreamWriter(fileName, true);
				sw.WriteLine($"{Time.time:0.00} \t {Time.frameCount} \t {context}");
				sw.Close();
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void WriteJsonObject(object pObj, string pFileName)
		{
			File.WriteAllText("D:\\" + pFileName + ".txt", JsonUtility.ToJson(pObj));
		}

		[Conditional("UNITY_EDITOR")]
		public static void WriteFile(string pContent, string pFileName)
		{
			File.WriteAllText("D:\\" + pFileName + ".txt", pContent);
		}

		public static void Assert(bool pCondition, string pLog)
		{
			if (!Enabled)
				return;
			m_StrBuilder.Remove(0, m_StrBuilder.Length);
			m_StrBuilder.Append(pLog);
			UnityEngine.Debug.Assert(pCondition, m_StrBuilder);
		}
	}
}