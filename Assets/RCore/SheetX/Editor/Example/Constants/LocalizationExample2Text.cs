/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 ***/

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class LocalizationExample2Text : MonoBehaviour
{
    public enum CaseType
    {
        None,
        LowerCase,
        UpperCase,
        SentenceCase,
        CapitalizeEachWord,
    }

    [SerializeField] private TextMeshProUGUI m_TxtPro;
    [SerializeField] private Text m_Txt;
    [SerializeField] private string m_LocalizedIDString;
    [SerializeField] private CaseType m_CaseType;
	[SerializeField] private string[] m_Args;
	[SerializeField] private List<string> m_IgnoredLangs = new List<string>(); //Used for TMPPRo
    [SerializeField, HideInInspector] private LocalizationExample2.ID m_LocalizedEnumId = LocalizationExample2.ID.NONE; //Used to search for the usage of an ID within the project
    [NonSerialized] private int m_LocalizedID = -1;
	
	private int m_LanguageIndex = -1;
	public string LocalizedIDString => m_LocalizedIDString;
    public LocalizationExample2.ID LocalizedEnumId => m_LocalizedEnumId;

	private void Awake()
    {
		Init();
    }

    private void OnEnable()
    {
        if (LocalizationExample2.CurLangIndex > -1)
            Refresh();
		
		LocalizationExample2.OnLanguageChanged += OnLanguageChanged;
    }
	
    private void OnDisable()
    {
		LocalizationExample2.OnLanguageChanged -= OnLanguageChanged;
	}
	
    public void Set(int pId, params string[] pArgs)
    {
        if (m_LocalizedID != pId)
        {
            m_LocalizedID = pId;
            m_LocalizedEnumId = (LocalizationExample2.ID)pId;
            m_LocalizedIDString = LocalizationExample2.idString[m_LocalizedID];
        }
		m_Args = pArgs;
		Refresh();
    }

    public void Set(string pId, params string[] pArgs)
    {
        if (m_LocalizedIDString != pId)
        {
            m_LocalizedIDString = pId;
            m_LocalizedID = -1;
            m_LocalizedEnumId = LocalizationExample2.ID.NONE;
        }
		m_Args = pArgs;
		Refresh();
    }

    private void Init()
    {
        if (m_TxtPro == null && m_Txt == null)
        {
            m_TxtPro = gameObject.GetComponent<TextMeshProUGUI>();
            m_Txt = gameObject.GetComponent<Text>();
        }
    }

    public void Refresh()
    {
		int CurLangIndex = LocalizationExample2.CurLangIndex;
        if (Application.isPlaying && m_LanguageIndex == CurLangIndex)
			return;
		if (m_IgnoredLangs.Count > 0 && m_IgnoredLangs.Contains(LocalizationExample2.CurrentLanguage))
			return;
        string text = "";
        if (m_LocalizedID >= 0)
            text = LocalizationExample2.Get(m_LocalizedID).ToString();
        else if (!string.IsNullOrEmpty(m_LocalizedIDString))
            text = LocalizationExample2.Get(m_LocalizedIDString, ref m_LocalizedID).ToString();
        switch (m_CaseType)
        {
            case CaseType.LowerCase:
                text = text.ToLower();
                break;
            case CaseType.SentenceCase:
                text = ToSentenceCase(text);
                break;
            case CaseType.CapitalizeEachWord:
                text = ToCapitalizeEachWord(text);
                break;
            case CaseType.UpperCase:
                text = text.ToUpper();
                break;
        }
        if (m_Args != null && m_Args.Length > 0)
            text = string.Format(text, m_Args);
        if (m_Txt != null && !string.IsNullOrEmpty(text))
            m_Txt.text = text;
        if (m_TxtPro != null && !string.IsNullOrEmpty(text))
            m_TxtPro.text = text;
			
		m_LanguageIndex = CurLangIndex;
    }
	
	private void OnLanguageChanged()
	{
		Refresh();
	}

    private string ToCapitalizeEachWord(string pString)
    {
        return Regex.Replace(pString, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
    }

    private string ToSentenceCase(string pString)
    {
        var lowerCase = pString.ToLower();
        var r = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
        var result = r.Replace(lowerCase, s => s.Value.ToUpper());
        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        Init();

        if (string.IsNullOrEmpty(m_LocalizedIDString))
			return;
		
		int localizedID = -1;
        LocalizationExample2.InitInEditor();
		LocalizationExample2.Get(m_LocalizedIDString, ref localizedID);
		if (localizedID == -1)
		{
			string path = name;
			var parent = transform.parent;
			while (parent != null)
			{
				path = $"{parent.name}.{path}";
				parent = parent.parent;
			}
			Debug.LogError($"{path}: not found {m_LocalizedIDString} in {nameof(LocalizationExample2)}");
		}
    }

    [CustomEditor(typeof(LocalizationExample2Text))]
    public class LocalizationExample2TextEditor : Editor
    {
        private LocalizationExample2Text m_Target;
        private string m_Search;
        private bool m_ShowText;
        private int m_MaxResults;
		private int m_IgnoredLangIdx;

        private void OnEnable()
        {
            m_MaxResults = 20;
            m_Target = target as LocalizationExample2Text;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("Editor", GUI.skin.horizontalSlider);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search Key", GUILayout.Width(100));
            m_Search = EditorGUILayout.TextField(m_Search, EditorStyles.textField);
            m_ShowText = EditorGUILayout.Toggle(m_ShowText);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh"))
                m_Target.Refresh();
				
			if (LocalizationExample2.CurrentLanguage != LocalizationExample2.DefaultLanguage && GUILayout.Button("Reset Default Language"))
            {
				LocalizationExample2.CurrentLanguage = LocalizationExample2.DefaultLanguage;
				m_Target.Refresh();
			}
			
			if (GUILayout.Button("Add Ignored Lang"))
				m_Target.m_IgnoredLangs.Add("");
			if (m_Target.m_IgnoredLangs.Count > 0)
			{
				for (int i = 0; i < m_Target.m_IgnoredLangs.Count; i++)
				{
					GUILayout.BeginHorizontal();
					m_Target.m_IgnoredLangs[i] = EditorGUILayout.TextField(m_Target.m_IgnoredLangs[i], EditorStyles.textField);
					if (GUILayout.Button("<"))
					{
						m_IgnoredLangIdx--;
						m_IgnoredLangIdx = Mathf.Clamp(m_IgnoredLangIdx, 0, LocalizationExample2.LanguageFiles.Count - 1);
						m_Target.m_IgnoredLangs[i] = LocalizationExample2.GetLanguage(m_IgnoredLangIdx);
					}
					if (GUILayout.Button(">"))
					{
						m_IgnoredLangIdx++;
						m_IgnoredLangIdx = Mathf.Clamp(m_IgnoredLangIdx, 0, LocalizationExample2.LanguageFiles.Count - 1);
						m_Target.m_IgnoredLangs[i] = LocalizationExample2.GetLanguage(m_IgnoredLangIdx);
					}
					if (GUILayout.Button("Remove"))
					{
						m_Target.m_IgnoredLangs.RemoveAt(i);
						i--;
					}
					GUILayout.EndHorizontal();
				}
			}
			
			if (GUILayout.Button("Test Next Lang"))
            {
                int index = 0;
                foreach (var lang in LocalizationExample2.LanguageFiles)
                {
                    if (lang.Key == LocalizationExample2.CurrentLanguage)
                        break;
                    index++;
                }

                index = (index + 1) % LocalizationExample2.LanguageFiles.Count;

                int index2 = 0;
                foreach (var lang in LocalizationExample2.LanguageFiles)
                {
                    if (index2 == index)
                    {
                        LocalizationExample2.CurrentLanguage = lang.Key;
                        m_Target.Refresh();
                        break;
                    }
                    index2++;
                }
            }

            if (string.IsNullOrEmpty(m_Search))
                return;

            int count = 0;
            string[] searchs = m_Search.Split(' ');
            for (int i = 0; i < LocalizationExample2.idString.Length; i++)
            {
                if (count >= m_MaxResults)
                {
                    if (GUILayout.Button("..."))
	                    m_MaxResults += 5;
					break;
                }
                bool contain = false;
                string idString = LocalizationExample2.idString[i];
                for (int j = 0; j < searchs.Length; j++)
                {
                    if (idString.ToLower().Contains(searchs[j].ToLower()))
                    {
                        contain = true;
                        break;
                    }
                }
                if (contain)
                {
                    string additional = "";
                    if (m_ShowText)
                        additional = LocalizationExample2.Get(idString).ToString();
                    GUI.backgroundColor = Color.yellow;
                    var style = new GUIStyle("Button");
                    string buttonName = ($"{idString} / {additional}");
					if (buttonName.Length > 50)
						buttonName = buttonName.Substring(0, 50);
                    if (GUILayout.Button(buttonName, style))
                    {
                        m_Target.m_LocalizedIDString = idString;
                        m_Target.m_LocalizedID = -1;
                        m_Target.m_LocalizedEnumId = LocalizationExample2.ID.NONE;
                        m_Target.Init();
                        m_Target.Refresh();
                    }
                    GUI.backgroundColor = Color.white;
                    count++;
                }
            }
        }
    }
#endif
}