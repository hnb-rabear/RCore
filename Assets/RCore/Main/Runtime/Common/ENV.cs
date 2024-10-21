using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RCore
{
    public static class Env
    {
        public static readonly string EnvPath = Path.Combine(Application.dataPath, "../", $".env");
        public static readonly string ResourcesDirPath = Path.Combine(Application.dataPath, "Resources");
        public static readonly string BuiltInEnvPath = Path.Combine(ResourcesDirPath, $"env.txt");

        private static Dictionary<string, string> variables;
        private static Dictionary<string, string> builtInVariables;

        static Env()
        {
            Load();
        }

        public static void LogEnv()
        {
            if (variables != null)
                foreach (var variable in variables)
                    UnityEngine.Debug.Log($"[.env] {variable.Key} {variable.Value}");

            if (builtInVariables != null)
                foreach (var variable in builtInVariables)
                    UnityEngine.Debug.Log($"[builtIn env]{variable.Key} {variable.Value}");
        }

        public static Dictionary<string, string> ParseEnvironmentFile(string contents)
        {
            return contents.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#") && l.IndexOf("=", StringComparison.Ordinal) != -1)
                .ToDictionary(l => l.Substring(0, l.IndexOf("=", StringComparison.Ordinal)).Trim().ToUpper(), l => l.Substring(l.IndexOf("=", StringComparison.Ordinal) + 1).Trim().Trim('"', '\''));
        }

        public static bool TryParse(string key, out string value)
        {
            value = "";
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value) && variables != null && variables.TryGetValue(key, out string variable))
            {
                value = variable;
                return true;
            }
#endif
            if (string.IsNullOrEmpty(value) && builtInVariables != null && builtInVariables.TryGetValue(key, out string inVariable))
            {
                value = inVariable;
                return true;
            }
            return !string.IsNullOrEmpty(value);
        }

        public static bool TryParse(string key, out bool value)
        {
            value = false;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (bool.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && bool.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && bool.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out double value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (double.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && double.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && double.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out float value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (float.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && float.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && float.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out int value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (int.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && int.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && int.TryParse(builtInVariables[key], out value);
		}

        private static void SetSystemVariable(string key, object value)
        {
            var systemValue = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(systemValue))
                Environment.SetEnvironmentVariable(key, value.ToString());
        }

        public static void Load()
        {
            variables = new Dictionary<string, string>();
            var envPath = Path.Combine(Application.dataPath, "../", $".env");
            if (File.Exists(envPath))
            {
                var content = File.ReadAllText(envPath, Encoding.UTF8);
                variables = ParseEnvironmentFile(content);
            }

            var envTextAsset = Resources.Load<TextAsset>($"env");
            if (envTextAsset != null)
                builtInVariables = ParseEnvironmentFile(envTextAsset.text);
#if UNITY_EDITOR || DEVELOPMENT
            LogEnv();
#endif
        }
    }
}