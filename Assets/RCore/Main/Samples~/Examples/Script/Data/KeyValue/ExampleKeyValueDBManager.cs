using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using RCore.Data.KeyValue;
using Random = UnityEngine.Random;

namespace RCore.Example.Data.KeyValue
{
    public class ExampleKeyValueDBManager : KeyValueDBManager
    {
        private const bool ENCRYPT_FILE = false;
        private const bool ENCRYPT_SAVER = false;
        private static readonly Encryption FILE_ENCRYPTION = new Encryption();

        public static ExampleKeyValueDBManager mInstance;
        public static ExampleKeyValueDBManager Instance => mInstance;

        public ExampleDataMainGroup1 mainGroup1;
        public ExampleDataMainGroup2 mainGroup2;

        private KeyValueCollection m_collection;
        private bool m_initialized;

        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }

        public override void Init()
        {
            if (m_initialized)
                return;

            m_initialized = true;

            m_collection = KeyValueDB.CreateCollection("example", ENCRYPT_SAVER ? FILE_ENCRYPTION : null);

            mainGroup1 = AddMainDataGroup(new ExampleDataMainGroup1(0), m_collection);
            mainGroup2 = AddMainDataGroup(new ExampleDataMainGroup2(1), m_collection);

            base.Init();
        }

        private void RandomizeData()
        {
            mainGroup1.integerData.Value = Random.Range(0, 100);
            mainGroup1.floatData.Value = Random.Range(0, 100) * 100;
            mainGroup1.longData.Value = Random.Range(0, 100) * 10000;
            mainGroup1.stringData.Value = Random.Range(0, 100) + "asd";
            mainGroup1.boolData.Value = Random.Range(0, 100) > 50;
            mainGroup1.dateTimeData.Set(DateTime.Now);
            mainGroup1.timedTask.Start(100);
            mainGroup1.RandomizeData();
        }

        private void Log()
        {
            Debug.Log("integerData: " + mainGroup1.integerData.Value);
            Debug.Log("floatData: " + mainGroup1.floatData.Value);
            Debug.Log("longData: " + mainGroup1.longData.Value);
            Debug.Log("stringData: " + mainGroup1.stringData.Value);
            Debug.Log("boolData: " + mainGroup1.boolData.Value);
            Debug.Log("dateTimeData: " + mainGroup1.dateTimeData.Get());
            Debug.Log("timerTask: " + mainGroup1.timedTask.RemainSeconds);
        }

        private void LogAll()
        {
            var savedData = m_collection.GetSavedData();
            var currentData = m_collection.GetCurrentData();
            Debug.Log("Saved Data: " + savedData);
            Debug.Log("Running Data: " + currentData);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ExampleKeyValueDBManager))]
        private class ExampleGameKeyValueDBEditor : KeyValueDBManagerEditor
        {
            private ExampleKeyValueDBManager m_script;

            private void OnEnable()
            {
                m_script = target as ExampleKeyValueDBManager;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (Application.isPlaying)
                {
                    if (GUILayout.Button("RandomizeData"))
                        m_script.RandomizeData();
                    if (GUILayout.Button("Log"))
                        m_script.Log();
                    if (GUILayout.Button("LogAll"))
                        m_script.LogAll();
                }
                else
                    EditorGUILayout.HelpBox("Click play to see how it work", MessageType.Info);
            }
        }
#endif
    }
}