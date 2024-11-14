/***
 * Author HNB-RaBear - 2017
 **/


using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace RCore
{

    /* Usage:
     * YouObject[] objects = JsonHelper.GetJsonArray<YouObject> (jsonString);
     * string jsonString = JsonHelper.ArrayToJson<YouObject>(objects);
     */

    public static class JsonHelper
    {
        public static T[] ToArray<T>(string json)
        {
            try
            {
                var sb = new StringBuilder();
                string newJson = sb.Append("{").Append("\"array\":").Append(json).Append("}").ToString();
                var wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
                return wrapper.array;
            }
            catch
            {
                Debug.LogError(typeof(T).Name);
                return null;
            }
        }

        public static void ToArray<T>(string json, out T[] pOutPut) => pOutPut = ToArray<T>(json);

        public static List<T> ToList<T>(string json)
        {
            try
            {
                var sb = new StringBuilder();
                string newJson = sb.Append("{").Append("\"list\":").Append(json).Append("}").ToString();
                var wrapper = JsonUtility.FromJson<ListWrapper<T>>(newJson);
                return wrapper.list;
            }
            catch
            {
                Debug.LogError(typeof(T).Name);
                return null;
            }
        }

        public static void ToList<T>(string json, out List<T> pOutPut) => pOutPut = ToList<T>(json);

        public static string ToJson<T>(T[] array)
        {
            var wrapper = new Wrapper<T>();
            wrapper.array = array;
            string json = JsonUtility.ToJson(wrapper);
            json = json.Remove(0, 9);
            json = json.Remove(json.Length - 1);
            return json;
        }

        public static string ToJson<T>(List<T> list)
        {
            var wrapper = new ListWrapper<T>();
            wrapper.list = list;
            string json = JsonUtility.ToJson(wrapper);
            json = json.Remove(0, 8);
            json = json.Remove(json.Length - 1);
            return json;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }

        [Serializable]
        private class ListWrapper<T>
        {
            public List<T> list;
        }
    }
}