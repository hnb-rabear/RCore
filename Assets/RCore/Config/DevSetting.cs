using System;
using System.Collections.Generic;
using UnityEngine;
using RCore.Inspector;
using Debug = UnityEngine.Debug;
using RCore.Common;

[CreateAssetMenu(fileName = "DevSetting", menuName = "RUtilities/Dev Setting")]
public class DevSetting : ScriptableObject
{
    #region Internal Class

    [System.Serializable]
    public class Profile
    {
        [ReadOnly]
        public string name;

        [Separator("-- COMMON --")]
        public bool enableLog;
        public bool enableDraw;
        public bool showFPS;

        [Separator("-- BUILD --"), ReadOnly]
        public List<Directive> defines;
    }

    [System.Serializable]
    public class Directive
    {
        public Directive()
        {
            color = Color.white;
        }
        public Directive(string pName, bool pEnable)
        {
            name = pName;
            enabled = pEnable;
            color = Color.white;
        }
        public string name;
        public Color color;
        public bool enabled = true;
    }

    #endregion

    //==================================

    private static DevSetting mInstance;
    public static DevSetting Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load<DevSetting>("DevSetting");
                mInstance.enableLogSystem = new PlayerPrefBool("EnableLogSystem");
#if DEVELOPMENT
                mInstance.enableLogSystem.Value = true;
#endif
            }
            return mInstance;
        }
    }

    public Action onSettingsChanged;
    public Profile profile = new Profile();
    public PlayerPrefBool enableLogSystem;

    public bool EnableLog
    {
        get { return profile.enableLog || enableLogSystem.Value; }
        set
        {
            profile.enableLog = value;
            onSettingsChanged?.Invoke();
        }
    }
    public bool EnableDraw
    {
        get { return profile.enableDraw; }
        set
        {
            profile.enableDraw = value;
            onSettingsChanged?.Invoke();
        }
    }
    public bool ShowFPS
    {
        get { return profile.showFPS; }
        set
        {
            profile.showFPS = value;
            onSettingsChanged?.Invoke();
        }
    }
}