/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using RCore.Common;
#if ACTIVE_FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace RCore.Service.RFirebase.Analytics
{
    public class RFirebaseAnalytics
    {
        private static RFirebaseAnalytics mInstance;
        public static RFirebaseAnalytics Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new RFirebaseAnalytics();
                return mInstance;
            }
        }

#if ACTIVE_FIREBASE_ANALYTICS
        public bool initialized { get; private set; }

        public void Initialize()
        {
            if (initialized)
                return;

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            initialized = true;

            LogEvent(FirebaseAnalytics.EventLogin);
        }

        public void SetUserProperty(string name, string property)
        {
            if (!initialized) return;
            Debug.Log(string.Format("SetUserProperty_{0}_{1}", name, property));
            FirebaseAnalytics.SetUserProperty(name, property);
        }

        public void SetUserId(string id)
        {
            if (!initialized) return;
            FirebaseAnalytics.SetUserId(id);
        }

        public void LogEvent(string name)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name);
        }

        public void LogEvent(string name, string parameterName, string parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string parameterName, double parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string parameterName, float parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string parameterName, long parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string parameterName, int parameterValue)
        {
            if (!initialized) return;
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, parameterName, parameterValue);
        }

        public void LogEvent(string name, string[] paramNames, string[] paramValues)
        {
            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }

        public void LogEvent(string name, string[] paramNames, double[] paramValues)
        {
            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }

        public void LogEvent(string name, string[] paramNames, float[] paramValues)
        {
            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }

        public void LogEvent(string name, string[] paramNames, int[] paramValues)
        {
            var array = new Parameter[paramNames.Length];
            for (int i = 0; i < paramNames.Length; i++)
            {
                array[i] = new Parameter(paramNames[i], paramValues[i]);
            }
            Debug.Log(string.Format("LogEvent_{0}", name));
            FirebaseAnalytics.LogEvent(name, array);
        }
#else
        public bool initialized { get; private set; }
        public void Initialize() { }
        public void SetUserProperty(string name, string property) { }
        public void SetUserId(string id) { }
        public void LogEvent(string name) { }
        public void LogEvent(string name, string parameterName, string parameterValue) { }
        public void LogEvent(string name, string parameterName, double parameterValue) { }
        public void LogEvent(string name, string parameterName, float parameterValue) { }
        public void LogEvent(string name, string parameterName, long parameterValue) { }
        public void LogEvent(string name, string parameterName, int parameterValue) { }
        public void LogEvent(string name, string[] paramNames, string[] paramValues) { }
        public void LogEvent(string name, string[] paramNames, double[] paramValues) { }
        public void LogEvent(string name, string[] paramNames, float[] paramValues) { }
        public void LogEvent(string name, string[] paramNames, int[] paramValues) { }
#endif
    }
}