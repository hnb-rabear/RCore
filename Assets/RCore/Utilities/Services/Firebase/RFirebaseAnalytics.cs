/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using RCore.Common;
using System.Collections.Generic;
using UnityEngine;
using Debug = RCore.Common.Debug;
#if ACTIVE_FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace RCore.Service
{
    public static class RFirebaseAnalytics
    {
        private const string FAILED_EVENTS = "FailedEvents";
        private static List<AnalyticEvent> m_FailedEvents = new List<AnalyticEvent>();

#if ACTIVE_FIREBASE_ANALYTICS
        public static bool initialized { get; private set; }

        public static void Initialize()
        {
            if (initialized)
                return;

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            initialized = true;

            LogEvent(FirebaseAnalytics.EventLogin);

            ResendFailedEvents();
        }

        public static void SetUserProperty(string name, string property)
        {
            if (!initialized) return;
            Debug.Log(string.Format("SetUserProperty_{0}_{1}", name, property));
            FirebaseAnalytics.SetUserProperty(name, property);
        }

        public static void SetUserId(string id)
        {
            if (!initialized) return;
            FirebaseAnalytics.SetUserId(id);
        }

        public static void LogEvent(string name)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name));
                return;
            }

            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name);
        }

        public static void LogEvent(string name, string parameterName, string parameterValue)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name, parameterName, parameterValue));
                return;
            }

            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public static void LogEvent(string name, string parameterName, double parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public static void LogEvent(string name, string parameterName, float parameterValue)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name, parameterName, parameterValue));
                return;
            }

            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public static void LogEvent(string name, string parameterName, long parameterValue)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name, parameterName, parameterValue));
                return;
            }

            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public static void LogEvent(string name, string parameterName, int parameterValue)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name, parameterName, parameterValue));
                return;
            }

            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public static void LogEvent(string name, string[] paramNames, string[] paramValues)
        {
            if (initialized)
            {
                var array = new Parameter[paramNames.Length];
                for (int i = 0; i < paramNames.Length; i++)
                {
                    array[i] = new Parameter(paramNames[i], paramValues[i]);
                }
                Debug.Log(string.Format("LogEvent_{0}", name));
                FirebaseAnalytics.LogEvent(name, array);
            }
            else
            {
                m_FailedEvents.Add(new AnalyticEvent(name, paramNames, paramValues));
            }
        }

        public static void LogEvent(string name, params Parameter[] pParams)
        {
            FirebaseAnalytics.LogEvent(name, pParams);
        }

        public static void LogEvent(string name, string[] paramNames, double[] paramValues)
        {
            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }

        public static void LogEvent(string name, string[] paramNames, float[] paramValues)
        {
            if (initialized)
            {
                var array = new Parameter[paramNames.Length];
                for (int i = 0; i < paramNames.Length; i++)
                {
                    array[i] = new Parameter(paramNames[i], paramValues[i]);
                }
                Debug.Log(string.Format("LogEvent_{0}", name));
                FirebaseAnalytics.LogEvent(name, array);
            }
            else
            {
                m_FailedEvents.Add(new AnalyticEvent(name, paramNames, paramValues));
            }
        }

        public static void LogEvent(string name, string[] paramNames, int[] paramValues)
        {
            if (!initialized)
            {
                m_FailedEvents.Add(new AnalyticEvent(name, paramNames, paramValues));
                return;
            }

            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }

        public static void SaveFailedEvents()
        {
            string failedEvents = JsonHelper.ToJson(m_FailedEvents);
            PlayerPrefs.SetString(FAILED_EVENTS, failedEvents);
        }

        public static void ResendFailedEvents()
        {
            if (!initialized)
                return;

            var allEventsStr = PlayerPrefs.GetString(FAILED_EVENTS);
            m_FailedEvents = JsonHelper.ToList<AnalyticEvent>(allEventsStr);

            if (m_FailedEvents == null)
                m_FailedEvents = new List<AnalyticEvent>();

            if (m_FailedEvents.Count > 0)
            {
                for (int i = m_FailedEvents.Count - 1; i >= 0; i--)
                {
                    string name = m_FailedEvents[i].name;
                    string[] paramNames = m_FailedEvents[i].paramNames;
                    string[] paramValuesStr = m_FailedEvents[i].paramValuesStr;
                    float[] paramValuesNum = m_FailedEvents[i].paramValuesNum;

                    if (paramValuesStr != null && paramValuesStr.Length > 0)
                        LogEvent(name, paramNames, paramValuesStr);
                    else if (paramValuesNum != null && paramValuesNum.Length > 0)
                        LogEvent(name, paramNames, paramValuesNum);
                    else
                        LogEvent(name);

                    m_FailedEvents.RemoveAt(i);
                }
            }

            PlayerPrefs.DeleteKey(FAILED_EVENTS);
        }
#else
        public static bool initialized { get; private set; }
        public static void Initialize() { }
        public static void SetUserProperty(string name, string property) { }
        public static void SetUserId(string id) { }
        public static void LogEvent(string name) { }
        public static void LogEvent(string name, string parameterName, string parameterValue) { }
        public static void LogEvent(string name, string parameterName, double parameterValue) { }
        public static void LogEvent(string name, string parameterName, float parameterValue) { }
        public static void LogEvent(string name, string parameterName, long parameterValue) { }
        public static void LogEvent(string name, string parameterName, int parameterValue) { }
        public static void LogEvent(string name, string[] paramNames, string[] paramValues) { }
        public static void LogEvent(string name, string[] paramNames, double[] paramValues) { }
        public static void LogEvent(string name, string[] paramNames, float[] paramValues) { }
        public static void LogEvent(string name, string[] paramNames, int[] paramValues) { }
        public static void SaveFailedEvents() { }
#endif

        [System.Serializable]
        public class AnalyticEvent
        {
            public string name;
            public string[] paramNames;
            public string[] paramValuesStr;
            public float[] paramValuesNum;
            public AnalyticEvent(string pName) { name = pName; }
            public AnalyticEvent(string pName, string pParamNames, float pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesNum = new float[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string pParamNames, int pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesNum = new float[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string pParamNames, string pPramValues)
            {
                name = pName;
                paramNames = new string[1] { pParamNames };
                paramValuesStr = new string[1] { pPramValues };
            }
            public AnalyticEvent(string pName, string[] pParamNames, float[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesNum = pPramValues;
            }
            public AnalyticEvent(string pName, string[] pParamNames, int[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesNum = new float[pPramValues.Length];
                for (int i = 0; i < pPramValues.Length; i++)
                    paramValuesNum[i] = pPramValues[i];
            }
            public AnalyticEvent(string pName, string[] pParamNames, string[] pPramValues)
            {
                name = pName;
                paramNames = pParamNames;
                paramValuesStr = pPramValues;
            }
        }
    }
}