/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 ***/

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
#if ADDRESSABLES
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
#endif

public static class LocalizationExample
{
	public enum ID 
	{
		NONE = -1,
		message_1 = 0, message_2, message_3, message_4, message_5, content_1, content_2, content_3, content_4, content_5, title_1, title_2, title_3, title_4, title_5, title_6, whatever_msg, hero_name_HERO_1, hero_name_HERO_2, hero_name_HERO_3, hero_name_HERO_4, hero_name_HERO_5,
	}
	public const int
		message_1 = 0, message_2 = 1, message_3 = 2, message_4 = 3, message_5 = 4, content_1 = 5, content_2 = 6, content_3 = 7, content_4 = 8, content_5 = 9, title_1 = 10, title_2 = 11, title_3 = 12, title_4 = 13, title_5 = 14, title_6 = 15, whatever_msg = 16, hero_name_HERO_1 = 17, hero_name_HERO_2 = 18, hero_name_HERO_3 = 19, hero_name_HERO_4 = 20, hero_name_HERO_5 = 21;
	public static readonly string[] idString = new string[]
	{
		"message_1", "message_2", "message_3", "message_4", "message_5", "content_1", "content_2", "content_3", "content_4", "content_5", "title_1", "title_2", "title_3", "title_4", "title_5", "title_6", "whatever_msg", "hero_name_HERO_1", "hero_name_HERO_2", "hero_name_HERO_3", "hero_name_HERO_4", "hero_name_HERO_5",
	};
	public static readonly Dictionary<string, string> LanguageFiles = new Dictionary<string, string>() {  { "english", "LocalizationExample_english" }, { "spanish", "LocalizationExample_spanish" }, { "japan", "LocalizationExample_japan" }, { "chinese", "LocalizationExample_chinese" }, { "korean", "LocalizationExample_korean" }, { "thai", "LocalizationExample_thai" }, };
	public static readonly string DefaultLanguage = "english";

    public static bool Addressable;
	public static string Folder = "";
    private static StringBuilder m_StringBuilder = new StringBuilder();
    public static Action OnLanguageChanged;
    private static string[] m_Texts;
    private static string m_LanguageTemp;
    public static string CurrentLanguage
    {
        get => PlayerPrefs.GetString("CurrentLanguage", DefaultLanguage);
        set
        {
            if (CurrentLanguage != value && LanguageFiles.ContainsKey(value))
            {
                PlayerPrefs.SetString("CurrentLanguage", value);
                Init();
            }
        }
    }
	public static int CurLangIndex = -1;

    public static async void Init()
	{
        var lang = m_LanguageTemp;
		if (!LanguageFiles.ContainsKey(CurrentLanguage))
		{
			if (string.IsNullOrEmpty(m_LanguageTemp))
				lang = DefaultLanguage;
		}
		else lang = CurrentLanguage;
				
		if (m_LanguageTemp != lang)
		{
#if UNITY_EDITOR
			Debug.Log($"Init {nameof(LocalizationExample)}");
#endif
		    string file = LanguageFiles[lang];
            string address = $"{Folder}/{file}";
#if ADDRESSABLES
            if (Addressable)
            {
                var operation = Addressables.LoadAssetAsync<TextAsset>(address);
                await operation;
                var textAsset = operation.Result;
                if (textAsset != null)
                {
                    m_Texts = GetJsonList(textAsset.text);
                    Addressables.Release(operation);
                }
            }
            else
#endif
            {
                var asset = Resources.Load<TextAsset>(address);
                if (asset == null)
                    return;
                string json = asset.text;
                m_Texts = GetJsonList(json);
                Resources.UnloadAsset(asset);
            }
			m_LanguageTemp = lang;
			int index = 0;
            foreach (var item in LanguageFiles)
            {
                if (lang == item.Key)
                {
                    CurLangIndex = index;
                    break;
                }
                index++;
            }
            for (int i = 0; i < m_DynamicTexts.Count; i++)
            {
                if (m_DynamicTexts[i].obj == null)
                {
                    m_DynamicTexts.RemoveAt(i);
                    i--;
                    continue;
                }
                m_DynamicTexts[i].Refresh();
            }
		    OnLanguageChanged?.Invoke();
		}
	}
	
    public static IEnumerator InitAsync()
    {
        var lang = m_LanguageTemp;
        if (!LanguageFiles.ContainsKey(CurrentLanguage))
        {
            if (string.IsNullOrEmpty(m_LanguageTemp))
                lang = DefaultLanguage;
        }
        else lang = CurrentLanguage;

        if (m_LanguageTemp != lang)
        {
#if UNITY_EDITOR
            Debug.Log($"Init {nameof(LocalizationExample)}");
#endif
            string file = LanguageFiles[lang];
            string address = $"{Folder}/{file}";
#if ADDRESSABLES
            if (Addressable)
            {
                var operation = Addressables.LoadAssetAsync<TextAsset>(address);
                yield return operation;
                var textAsset = operation.Result;
                if (textAsset != null)
                {
                    m_Texts = GetJsonList(textAsset.text);
                    Addressables.Release(operation);
                }
            }
            else
#endif
            {
                var request = Resources.LoadAsync<TextAsset>(address);
                while (!request.isDone)
                    yield return null;
                if (request.asset == null)
                    yield break;
                m_Texts = GetJsonList((request.asset as TextAsset).text);
                Resources.UnloadAsset(request.asset);
            }
            m_LanguageTemp = lang;
            int index = 0;
            foreach (var item in LanguageFiles)
            {
                if (lang == item.Key)
                {
                    CurLangIndex = index;
                    break;
                }
                index++;
            }
            for (int i = 0; i < m_DynamicTexts.Count; i++)
            {
                if (m_DynamicTexts[i].obj == null)
                {
                    m_DynamicTexts.RemoveAt(i);
                    i--;
                    continue;
                }
                m_DynamicTexts[i].Refresh();
            }
            OnLanguageChanged?.Invoke();
        }
    }

    public static StringBuilder Get(ID pId)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        return m_StringBuilder.Append(m_Texts[(int)pId]);
    }

    public static StringBuilder Get(ID pId, params object[] pArgs)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        m_StringBuilder.AppendFormat(m_Texts[(int)pId], pArgs);
        return m_StringBuilder;
    }

    public static StringBuilder Get(int pId)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        if (pId >= 0 && pId < m_Texts.Length)
            m_StringBuilder.Append(m_Texts[pId]);
#if UNITY_EDITOR
        else
            Debug.LogError("Not found id " + pId);
#endif
        return m_StringBuilder;
    }

    public static StringBuilder Get(int pId, params object[] pArgs)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        m_StringBuilder.AppendFormat(m_Texts[pId], pArgs);
        return m_StringBuilder;
    }

    public static StringBuilder Get(string pIdString)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        int index = GetIndex(pIdString);
        if (index >= 0)
            m_StringBuilder.Append(m_Texts[index]);
#if UNITY_EDITOR
        else
            Debug.LogError("Not found idString " + pIdString);
#endif
        return m_StringBuilder;
    }

    public static int GetIndex(string pIdString)
    {
        for (int i = 0; i < idString.Length; i++)
            if (pIdString == idString[i])
                return i;
#if UNITY_EDITOR
		Debug.LogError("Not found " + pIdString);
#endif
        return -1;
    }

    public static StringBuilder Get(string pIdString, params object[] pArgs)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        int index = GetIndex(pIdString);
        if (index >= 0)
            m_StringBuilder.AppendFormat(m_Texts[index], pArgs);
#if UNITY_EDITOR
        else
            Debug.LogError("Not found idString " + pIdString);
#endif
        return m_StringBuilder;
    }

    public static StringBuilder Get(string pIdString, ref int pIndex)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        pIndex = GetIndex(pIdString);
        if (pIndex >= 0)
            m_StringBuilder.Append(m_Texts[pIndex]);
#if UNITY_EDITOR
        else
            Debug.LogError("Not found idString " + pIdString);
#endif
        return m_StringBuilder;
    }

    public static StringBuilder Get(string pIdString, ref int pIndex, params object[] pArgs)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        pIndex = GetIndex(pIdString);
        if (pIndex >= 0)
            m_StringBuilder.AppendFormat(m_Texts[pIndex], pArgs);
#if UNITY_EDITOR
        else
            Debug.LogError("Not found idString " + pIdString);
#endif
        return m_StringBuilder;
    }

    public static StringBuilder Get(string pIdString, ref ID pId)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        int index = GetIndex(pIdString);
        if (index >= 0)
        {
            pId = (ID)index;
            m_StringBuilder.Append(m_Texts[index]);
        }
        else
        {
            pId = ID.NONE;
#if UNITY_EDITOR
            Debug.LogError("Not found idString " + pIdString);
#endif
        }
        return m_StringBuilder;
    }

    public static StringBuilder Get(string pIdString, ref ID pId, params object[] pArgs)
    {
        m_StringBuilder.Remove(0, m_StringBuilder.Length);
        int index = GetIndex(pIdString);
        if (index >= 0)
        {
            pId = (ID)index;
            m_StringBuilder.AppendFormat(m_Texts[index], pArgs);
        }
        else
        {
            pId = ID.NONE;
#if UNITY_EDITOR
            Debug.LogError("Not found idString " + pIdString);
#endif
        }
        return m_StringBuilder;
    }

    private static string[] GetJsonList(string json)
    {
        var sb = new StringBuilder();
        string newJson = sb.Append("{").Append("\"array\":").Append(json).Append("}").ToString();
        var wrapper = JsonUtility.FromJson<StringArray>(newJson);
        return wrapper.array;
    }
	
	public static string GetLanguage(int index)
	{
		int i = 0;
		foreach (var lang in LanguageFiles)
		{
			if (i == index)
				return lang.Key;
			i++;
		}
		return "";
	}

    [System.Serializable]
    private class StringArray
    {
        public string[] array;
    }

    private static List<DynamicText> m_DynamicTexts = new List<DynamicText>();
    /// <summary>
    /// Used this if you are unable to add LocalizationExampleText component to the gameObject
    /// </summary>
    /// <param name="pObj">gameObject contain Text or TextMeshProUGUI component</param>
    /// <param name="pLocalizedKey">string Id of localized text</param>
    /// <param name="pArgs">Value put into localized text</param>
    public static void RegisterDynamicText(GameObject pObj, string pLocalizedKey, params string[] pArgs)
    {
        int key = GetIndex(pLocalizedKey);
        RegisterDynamicText(pObj, key, pArgs);
    }
    /// <summary>
    /// Used this if you are unable to add LocalizationExampleText component to the gameObject
    /// </summary>
    /// <param name="pObj">gameObject contain Text or TextMeshProUGUI component</param>
    /// <param name="pLocalizedKey">integer Id of localized text</param>
    /// <param name="pArgs">Value put into localized text</param>
    public static void RegisterDynamicText(GameObject pObj, int pLocalizedKey, params string[] pArgs)
    {
        if (pObj == null)
            return;
        for (int i = 0; i < m_DynamicTexts.Count; i++)
        	if (m_DynamicTexts[i].obj == pObj)
            {
                if (m_DynamicTexts[i].key != pLocalizedKey || m_DynamicTexts[i].args != pArgs)
                {
                    m_DynamicTexts[i].curLangIndex = -1;
                    m_DynamicTexts[i].key = pLocalizedKey;
                    m_DynamicTexts[i].args = pArgs;
                }
                m_DynamicTexts[i].Refresh();
                return;
            }
        var text = new DynamicText(pLocalizedKey, pObj, pArgs);
        text.Refresh();
        m_DynamicTexts.Add(text);
#if UNITY_EDITOR
        if (pObj.TryGetComponent(out LocalizationExampleText _))
            Debug.LogError($"{pObj.name} should not have LocalizationExampleText!");
#endif
    }

    public static void UnregisterDynamicText(GameObject pObj)
    {
        for (int i = 0; i < m_DynamicTexts.Count; i++)
            if (m_DynamicTexts[i].obj == pObj)
            {
                m_DynamicTexts.RemoveAt(i);
                return;
            }
    }

    public class DynamicText
    {
        public int key = -1;
        public GameObject obj;
        public string[] args;
        public int curLangIndex = -1;
        public DynamicText(int pIntKey, GameObject pObj, params string[] pArgs)
        {
            key = pIntKey;
            obj = pObj;
            args = pArgs;
        }
        public void Refresh()
        {
            if (curLangIndex != CurLangIndex)
            {
#if UNITY_EDITOR
                var text = Get(key);
                try
                {
#endif
					var value = "";
                    if (args != null)
                        value = Get(key, args).ToString();
                    else
                        value = Get(key).ToString();

                    if (obj.TryGetComponent(out TMPro.TextMeshProUGUI txtPro))
                        txtPro.text = value;
                    else if (obj.TryGetComponent(out Text txt))
                        txt.text = value;

					curLangIndex = CurLangIndex;
#if UNITY_EDITOR
                }
                catch (Exception ex)
                {
                    Debug.LogError(text + "\n" + ex);
                }
#endif
            }
        }
    }
	
#if UNITY_EDITOR
	private static void CreateClassContainsRefinedIds(string pPath)
	{
		var ids = FindUsedIDs(pPath);
		Debug.Log($"Found {ids.Count} ids used in {pPath}");
		CreateClassContainsRefinedIds(ids);
	}
	private static List<string> FindUsedIDs(string pPath)
	{
		var results = new List<string>();
		var assetIds = UnityEditor.AssetDatabase.FindAssets("t:prefab", new[] { pPath });
		foreach (var guid in assetIds)
		{
			var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
			var components = obj.GetComponentsInChildren<LocalizationExampleText>(true);
			foreach (var com in components)
			{
                if (com.LocalizedEnumId != ID.NONE)
                {
                    if (!results.Contains(com.LocalizedEnumId.ToString()))
                        results.Add(com.LocalizedEnumId.ToString());
                }
                else
                {
                    string idStr = com.LocalizedIDString;
                    idStr = idStr.Replace(" ", "_");
                    idStr = System.Text.RegularExpressions.Regex.Replace(idStr, "[^a-zA-Z0-9_.]+", "", System.Text.RegularExpressions.RegexOptions.Compiled);
                    if (!results.Contains(idStr))
                        results.Add(idStr);
                }
			}
		}
		return results;
	}
	private static void CreateClassContainsRefinedIds(List<string> pRefinedIds)
	{
		string enumIds = "";
		string intIds = "";
		for (int i = 0; i < pRefinedIds.Count; i++)
		{
			enumIds += $"{nameof(LocalizationExample)}.ID.{pRefinedIds[i]}";
			intIds += $"{nameof(LocalizationExample)}.{pRefinedIds[i]}";
			if (i < pRefinedIds.Count - 1)
			{
				enumIds += ", ";
				intIds += ", ";
			}
		}
		if (enumIds == "")
			return;
		string template = GetTemplateClassContainsRefinedIds();
		string result = template.Replace("ENUM_IDS", enumIds).Replace("INT_IDS", intIds);
		string directoryPath = $"{Application.dataPath}/LocalizationRefinedIds";
		string path = $"{directoryPath}/{nameof(LocalizationExample)}RefinedIds.cs";
		if (!System.IO.Directory.Exists(directoryPath))
			System.IO.Directory.CreateDirectory(directoryPath);
		System.IO.File.WriteAllText(path, result);
        Debug.Log($"Created {path}");
	}
	private static string GetTemplateClassContainsRefinedIds()
	{
		string content = $"namespace {typeof(LocalizationExample).Namespace}\n"
			+ "{\n"
			+ "\tusing System.Collections.Generic;\n"
			+ $"\tpublic class LocalizationExampleRefinedIds\n"
			+ "\t{\n"
			+ "\t\tpublic static readonly List<LocalizationExample.ID> enumIds = new List<LocalizationExample.ID> { ENUM_IDS };\n"
			+ "\t\tpublic static readonly List<int> intIds = new List<int> { INT_IDS };\n"
			+ "\t}\n"
			+ "}";
		return content;
	}
	[UnityEditor.MenuItem("Assets/Create/RCore/Localization/Refine LocalizationExample Ids")]
	public static void CreateLocalizationRefinedIds()
	{
		string currentPath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
		CreateClassContainsRefinedIds(currentPath);
	}
    public static void InitInEditor()
    {
        var lang = m_LanguageTemp;
        if (!LanguageFiles.ContainsKey(CurrentLanguage))
        {
            if (string.IsNullOrEmpty(m_LanguageTemp))
                lang = DefaultLanguage;
        }
        else lang = CurrentLanguage;

        if (m_LanguageTemp != lang)
        {
            string file = LanguageFiles[lang];
            string address = $"{Folder}/{file}";
#if ADDRESSABLES
            if (Addressable)
            {
                var operation = Addressables.LoadAssetAsync<TextAsset>(address);
                operation.WaitForCompletion(); // Blocks the main thread until complete (Editor and Development Builds Only)
                var textAsset = operation.Result;
                if (textAsset != null)
                {
                    m_Texts = GetJsonList(textAsset.text);
                    Addressables.Release(operation);
                }
            }
            else
#endif
            {
                var asset = Resources.Load<TextAsset>(address);
                if (asset == null)
                    return;
                string json = asset.text;
                m_Texts = GetJsonList(json);
                Resources.UnloadAsset(asset);
            }
            m_LanguageTemp = lang;
            int index = 0;
            foreach (var item in LanguageFiles)
            {
                if (lang == item.Key)
                {
                    CurLangIndex = index;
                    break;
                }
                index++;
            }
            OnLanguageChanged?.Invoke();
        }
    }
#endif
}
