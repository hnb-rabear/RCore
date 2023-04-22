using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using RCore.Common;
using RCore.Framework.Data;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace RCore.Demo
{
    public class ExampleGameData : DataManager
    {
        private const bool ENCRYPT_FILE = false;
        private const bool ENCRYPT_SAVER = false;
        private static readonly Encryption FILE_ENRYPTION = new Encryption();

        public static ExampleGameData mInstance;
        public static ExampleGameData Instance => mInstance;

        public DemoGroup1 exampleGroup;
        public DemoGroup3 demoGroup;

        private DataSaver mDataSaver;
        private bool mInitialized;

        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }

        public override void Init()
        {
            if (mInitialized)
                return;

            mInitialized = true;

            mDataSaver = DataSaverContainer.CreateSaver("example", ENCRYPT_SAVER ? FILE_ENRYPTION : null);

            exampleGroup = AddMainDataGroup(new DemoGroup1(0), mDataSaver);
            demoGroup = AddMainDataGroup(new DemoGroup3(1), mDataSaver);

            base.Init();
        }

        private void RandomizeData()
        {
            exampleGroup.intergerdata.Value = Random.Range(0, 100);
            exampleGroup.floatData.Value = Random.Range(0, 100) * 100;
            exampleGroup.longData.Value = Random.Range(0, 100) * 10000;
            exampleGroup.stringData.Value = Random.Range(0, 100).ToString() + "asd";
            exampleGroup.boolData.Value = Random.Range(0, 100) > 50;
            exampleGroup.dateTimeData.Value = DateTime.Now;
            exampleGroup.RandomizeData();
        }

        private void Log()
        {
            Debug.Log("intergerdata: " + exampleGroup.intergerdata.Value);
            Debug.Log("floatData: " + exampleGroup.floatData.Value);
            Debug.Log("longData: " + exampleGroup.longData.Value);
            Debug.Log("stringData: " + exampleGroup.stringData.Value);
            Debug.Log("boolData: " + exampleGroup.boolData.Value);
            Debug.Log("dateTimeData: " + exampleGroup.dateTimeData.Value);
            Debug.Log("timeCouterData: " + exampleGroup.timeCouterData.GetRemainSeconds());
            Debug.Log("timerTask: " + exampleGroup.timerTask.RemainSeconds);
        }

        private void LogAll()
        {
            var savedData = mDataSaver.GetSavedData();
            var currentData = mDataSaver.GetCurrentData();
            Debug.Log("Saved Data: " + savedData);
            Debug.Log("Running Data: " + currentData);
        }

        public static string LoadFile(string pPath, bool pEncrypt = ENCRYPT_FILE)
        {
            if (pEncrypt)
                return DataManager.LoadFile(pPath, FILE_ENRYPTION);
            else
                return DataManager.LoadFile(pPath, null);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ExampleGameData))]
        private class ExampleGameDataEditor : DataManagerEditor
        {
            private ExampleGameData mScript;

            private void OnEnable()
            {
                mScript = target as ExampleGameData;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (Application.isPlaying)
                {
                    if (GUILayout.Button("RandomizeData"))
                        mScript.RandomizeData();
                    if (GUILayout.Button("Log"))
                        mScript.Log();
                    if (GUILayout.Button("LogAll"))
                        mScript.LogAll();
                }
                else
                    EditorGUILayout.HelpBox("Click play to see how it work", MessageType.Info);
            }
        }
#endif
    }
}