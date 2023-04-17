/**
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace RCore.Common
{
    public static class Debug
    {
        private static StringBuilder mStrBuilder = new StringBuilder();
        public static void Log(object message)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append(message);
            UnityEngine.Debug.Log(mStrBuilder);
        }

        public static void Log(object message, bool pShowTime)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            if (pShowTime)
                mStrBuilder.Append($"[{DateTime.Now}] ").Append(message);
            else
                mStrBuilder.Append("[H] ").Append(message);
            UnityEngine.Debug.Log(mStrBuilder);
        }

        public static void Log(object message, Color pColor = default(Color))
        {
            if (!DevSetting.Instance.EnableLog) return;
            string c = pColor == default(Color) ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append($"<color=\"#{c}\">{message}</color>");
            UnityEngine.Debug.Log(mStrBuilder);
        }

        public static void Log(object title, object message, Color pColor = default(Color))
        {
            if (!DevSetting.Instance.EnableLog) return;
            string c = pColor == default(Color) ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append($"<b><color=\"#{c}\">{title}</color></b> {message}");
            UnityEngine.Debug.Log(mStrBuilder);
        }

        public static void LogError(object message, bool pShowTime = false)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            if (pShowTime)
                mStrBuilder.Append($"[{DateTime.Now}] ").Append(message);
            else
                mStrBuilder.Append("[H] ").Append(message);
            UnityEngine.Debug.LogError(mStrBuilder);
        }

        public static void LogWarning(object message, bool pShowTime = false)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            if (pShowTime)
                mStrBuilder.Append($"[{DateTime.Now}] ").Append(message);
            else
                mStrBuilder.Append("[H] ").Append(message);
            UnityEngine.Debug.LogWarning(mStrBuilder);
        }

        public static void LogException(Exception e, bool pShowTime = false)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            if (pShowTime)
                mStrBuilder.Append($"[{DateTime.Now}] ").Append(e);
            else
                mStrBuilder.Append("[H] ").Append(e);
            UnityEngine.Debug.LogError(mStrBuilder);
        }

        public static void LogJson<T>(T pObj, Color pColor = default(Color)) where T : class
        {
            if (!DevSetting.Instance.EnableLog || pObj == null) return;
            var jsonStr = JsonUtility.ToJson(pObj);
            if (jsonStr != "{}")
            {
                string c = pColor == default(Color) ? ColorUtility.ToHtmlStringRGBA(Color.yellow) : ColorUtility.ToHtmlStringRGBA(pColor);
                mStrBuilder.Remove(0, mStrBuilder.Length);
                mStrBuilder.Append("[H] ").Append(string.Format("<color=\"#{2}\">{0}</color>{1}", pObj.GetType().FullName, jsonStr, c));
                UnityEngine.Debug.Log(mStrBuilder);
            }
        }

        public static void LogNewtonJson(object pObj, Color pColor = default(Color))
        {
            if (!DevSetting.Instance.EnableLog)
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
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append(additionalText).Append("{");
            for (int i = 0; i < array.Length; i++)
            {
                mStrBuilder.Append((array[i] != null ? array[i].ToString() : "null") + (i < array.Length - 1 ? ", " : ""));
            }
            mStrBuilder.Append("}");
            UnityEngine.Debug.Log(mStrBuilder);
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
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append(additionalText).Append("{");
            int i = 0;
            foreach (KeyValuePair<TKey, TValue> pair in dict)
            {
                i++;
                mStrBuilder.Append("{" + pair.Key + ":" + pair.Value + "}");
                if (i < dict.Count - 1)
                    mStrBuilder.Append(",");
            }
            mStrBuilder.Append("}");
            UnityEngine.Debug.Log(mStrBuilder);
        }

        /// <summary>
        /// Print a List.
        /// </summary>
        /// <param name="list">List.</param>
        /// <param name="additionalText">Massage before list.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static void LogList<T>(List<T> list, string additionalText = "")
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append(additionalText).Append("{");
            for (int i = 0; i < list.Count; i++)
            {
                mStrBuilder.Append(list[i]);
                if (i < list.Count - 1)
                    mStrBuilder.Append(",");
            }
            mStrBuilder.Append("}");
            UnityEngine.Debug.Log(mStrBuilder);
        }

        /// <summary>
        /// Print a log to check if objects are null or not.
        /// </summary>
        /// <param name="objs">Objects to check.</param>
        public static void LogNull(params object[] objs)
        {
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append("[H] ").Append("{");
            for (int i = 0; i < objs.Length; i++)
            {
                mStrBuilder.Append(objs[i]);
                if (i < objs.Length - 1)
                    mStrBuilder.Append(",");
            }
            UnityEngine.Debug.Log(mStrBuilder);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogToFile(object context, string fileName, bool deleteAfterQuit = true)
        {
            if (DevSetting.Instance.EnableLog && Application.isEditor)
            {
                StreamReader sr = new StreamReader(fileName);
                StreamWriter sw = new StreamWriter(fileName, true);
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
            if (!DevSetting.Instance.EnableLog) return;
            mStrBuilder.Remove(0, mStrBuilder.Length);
            mStrBuilder.Append(pLog);
            UnityEngine.Debug.Assert(pCondition, mStrBuilder);
        }
    }
}