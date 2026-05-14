using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    public class JObjectModelCollection : ScriptableObject
    {
        [CreateScriptableObject, AutoFill] public SessionModel session;

        protected List<IJObjectModel> m_models = new();
        private Dictionary<Type, object> m_resolveCache = new();
        private static readonly object s_notFound = new();
        private static readonly Dictionary<Type, FieldInfo[]> s_injectFieldCache = new();

        public virtual void Load()
        {
            m_models = new List<IJObjectModel>();
            m_resolveCache.Clear();
            CreateModel(session, "SessionData");
        }

        public virtual void Save()
        {
            if (m_models == null) return;
            int ts = TimeHelper.GetNowTimestamp(true);
            foreach (var m in m_models) m.OnPreSave(ts);
            foreach (var m in m_models) m.Save();
            PlayerPrefContainer.SaveChanges();
            PlayerPrefs.Save();
        }

        public virtual void Import(string jsonData)
        {
            if (m_models == null) return;
            var pairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            if (pairs == null) return;
            foreach (var m in m_models)
                if (pairs.TryGetValue(m.Data.key, out string val))
                    m.Data.Load(val);
            PostLoad();
        }

        public virtual void Import(Dictionary<string, object> data)
        {
            if (m_models == null) return;
            foreach (var m in m_models)
            {
                if (data.TryGetValue(m.Data.key, out var obj))
                {
                    try { m.Data.Load(JsonConvert.SerializeObject(obj)); }
                    catch (Exception ex) { Debug.LogError($"Import error for '{m.Data.key}': {ex.Message}"); }
                }
            }
            PostLoad();
        }

        public Dictionary<string, object> GetData()
        {
            var dict = new Dictionary<string, object>();
            if (m_models != null)
                foreach (var m in m_models)
                    dict[m.Key] = m.Data;
            return dict;
        }

        public virtual void OnUpdate(float deltaTime)
        {
            if (m_models != null)
                foreach (var m in m_models) m.OnUpdate(deltaTime);
        }

        public virtual void OnPause(bool pause)
        {
            int ts = TimeHelper.GetNowTimestamp(true);
            int offline = pause ? 0 : session.GetOfflineSeconds();
            foreach (var m in m_models) m.OnPause(pause, ts, offline);
        }

        public virtual void PostLoad()
        {
            int offline = session.GetOfflineSeconds();
            int ts = TimeHelper.GetNowTimestamp(true);
            foreach (var m in m_models) m.OnPostLoad(ts, offline);
        }

        public void OnRemoteConfigFetched()
        {
            if (m_models != null)
                foreach (var m in m_models) m.OnRemoteConfigFetched();
        }

        public T Get<T>() where T : class
        {
            if (m_models == null) return null;
            return Resolve(typeof(T)) as T;
        }

        public void InjectDependencies()
        {
            if (m_models == null) return;
            foreach (var model in m_models)
            {
                foreach (var field in GetInjectFields(model.GetType()))
                {
                    var resolved = Resolve(field.FieldType);
                    if (resolved != null)
                        field.SetValue(model, resolved);
                    else
                        throw new InvalidOperationException(
                            $"[Inject] Cannot resolve '{field.FieldType.Name}' for '{model.GetType().Name}.{field.Name}'. " +
                            $"Register it via CreateModel() before calling InjectDependencies(). " +
                            $"Registered: {string.Join(", ", m_models.Select(m => m.GetType().Name))}");
                }
            }
        }

        protected void CreateModel<TData>(JObjectModel<TData> model, string key, TData defaultVal = null)
            where TData : JObjectData, new()
        {
            if (string.IsNullOrEmpty(key)) key = typeof(TData).Name;
            model.data = JObjectDB.CreateCollection(key, defaultVal);
            model.key = key;
            model.Init();
            model.data.key = key;
            m_models.Add(model);
        }

        protected void CreateModel<TData>(JObjectModel<TData> model, TData defaultVal = null)
            where TData : JObjectData, new()
        {
            if (string.IsNullOrEmpty(model.key)) model.key = typeof(TData).Name;
            model.data = JObjectDB.CreateCollection(model.key, defaultVal);
            model.Init();
            model.data.key = model.key;
            m_models.Add(model);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FieldInfo[] GetInjectFields(Type type)
        {
            if (!s_injectFieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<InjectAttribute>() != null)
                    .ToArray();
                s_injectFieldCache[type] = fields;
            }
            return fields;
        }

        private object Resolve(Type type)
        {
            if (m_resolveCache.TryGetValue(type, out var cached))
                return cached == s_notFound ? null : cached;

            foreach (var m in m_models)
                if (type.IsInstanceOfType(m)) { m_resolveCache[type] = m; return m; }

            foreach (var m in m_models)
                if (type.IsInstanceOfType(m.Data)) { m_resolveCache[type] = m.Data; return m.Data; }

            m_resolveCache[type] = s_notFound;
            return null;
        }
    }
}
