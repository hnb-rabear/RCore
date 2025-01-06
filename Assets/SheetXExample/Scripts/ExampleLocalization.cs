using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SheetXExample
{
	public class ExampleLocalization : MonoBehaviour
	{
		[SerializeField] private Button m_btnNextLang;
		[SerializeField] private Button m_btnPrevLang;
		[SerializeField] private TextMeshProUGUI m_txtCurLangId;
		[SerializeField] private TextMeshProUGUI m_txtCurLangName;

		[SerializeField] private TextMeshProUGUI m_dynamicText1;
		[SerializeField] private TextMeshProUGUI m_dynamicText2;
		[SerializeField] private TextMeshProUGUI m_dynamicText3;
		[SerializeField] private TextMeshProUGUI m_simpleText1;
		[SerializeField] private TextMeshProUGUI m_simpleText2;
		[SerializeField] private TextMeshProUGUI m_simpleText3;

		private void Start()
		{
			Init();

			m_btnNextLang.onClick.AddListener(() =>
			{
				int index = 0;
				for (int i = 0; i < LocalizationsManager.languages.Count; i++)
				{
					string lang = LocalizationsManager.languages[i];
					if (lang == LocalizationsManager.CurrentLanguage)
					{
						index = i;
						break;
					}
				}
				LocalizationsManager.CurrentLanguage = LocalizationsManager.languages[(index + 1) % LocalizationsManager.languages.Count];
			});
			m_btnPrevLang.onClick.AddListener(() =>
			{
				int index = 0;
				for (int i = 0; i < LocalizationsManager.languages.Count; i++)
				{
					string lang = LocalizationsManager.languages[i];
					if (lang == LocalizationsManager.CurrentLanguage)
					{
						index = i;
						break;
					}
				}
				if (index <= 0)
					index = LocalizationsManager.languages.Count;
				LocalizationsManager.CurrentLanguage = LocalizationsManager.languages[(index - 1) % LocalizationsManager.languages.Count];
			});

			m_txtCurLangId.text = LocalizationsManager.CurrentLanguage;
			m_txtCurLangName.text = LocalizationExample1.Get("language_" + LocalizationsManager.CurrentLanguage).ToString();

			LocalizationExample2.RegisterDynamicText(m_dynamicText1.gameObject, LocalizationExample2.TAP_TO_COLLECT);
			LocalizationExample2.RegisterDynamicText(m_dynamicText2.gameObject, LocalizationExample2.REQUIRED_LEVEL_X, "3");
			LocalizationExample2.RegisterDynamicText(m_dynamicText3.gameObject, "REQUIRED_LEVEL_X", "30");

			LocalizationsManager.OnLanguageChanged += OnLanguageChanged;
		}

		private void OnDestroy()
		{
			LocalizationsManager.OnLanguageChanged -= OnLanguageChanged;
		}

		private void OnLanguageChanged()
		{
			m_txtCurLangId.text = LocalizationsManager.CurrentLanguage;
			m_txtCurLangName.text = LocalizationExample1.Get("language_" + LocalizationsManager.CurrentLanguage).ToString();

			m_simpleText1.text = LocalizationExample2.Get(LocalizationExample2.GO_TO_SHOP).ToString();
			m_simpleText2.text = LocalizationExample2.Get(LocalizationExample2.REQUIRED_CITY_LEVEL_X, 10).ToString();
			m_simpleText3.text = LocalizationExample2.Get("REQUIRED_CITY_LEVEL_X", 25).ToString();
		}

		private void Init()
		{
			var defaultLanguage = LocalizationsManager.GetSystemLanguage();
			LocalizationsManager.Init(defaultLanguage);
		}

		private IEnumerator InitAsync()
		{
			var defaultLanguage = LocalizationsManager.GetSystemLanguage();
			yield return LocalizationsManager.InitAsync(defaultLanguage);
		}

		private void SetLanguage()
		{
			LocalizationsManager.CurrentLanguage = "en";
			switch (Application.systemLanguage)
			{
				case SystemLanguage.English:
					LocalizationsManager.CurrentLanguage = "en";
					break;
				case SystemLanguage.Spanish:
					LocalizationsManager.CurrentLanguage = "es";
					break;
				case SystemLanguage.Japanese:
					LocalizationsManager.CurrentLanguage = "jp";
					break;
				case SystemLanguage.ChineseSimplified:
					LocalizationsManager.CurrentLanguage = "cn";
					break;
				case SystemLanguage.Korean:
					LocalizationsManager.CurrentLanguage = "ko";
					break;
				case SystemLanguage.Thai:
					LocalizationsManager.CurrentLanguage = "th";
					break;
				default:
					LocalizationsManager.CurrentLanguage = "en";
					break;
			}
		}
	}
}