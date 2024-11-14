﻿namespace SheetXExample
{
	/***
	 * Author RadBear - nbhung71711@gmail.com - 2018
	 ***/
	
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	
	public abstract class LocalizationsManager
	{
	    public static Action OnLanguageChanged;
	
		public static readonly List<string> languages = new List<string>() { "english", "spanish", "japan", "chinese", "korean", "thai", };
		public static readonly string DefaultLanguage = "english";
		
	    public static string CurrentLanguage
	    {
	        get => PlayerPrefs.GetString("CurrentLanguage", DefaultLanguage);
	        set
	        {
	            if (CurrentLanguage != value && languages.Contains(value))
	            {
	                PlayerPrefs.SetString("CurrentLanguage", value);
	                Init("");
	            }
	        }
	    }
	
	    public static void Init(string pLang)
	    {
			if (languages.Contains(pLang))
				PlayerPrefs.SetString("CurrentLanguage", pLang);
			LocalizationExample1.Init();
			LocalizationExample2.Init();
	        OnLanguageChanged?.Invoke();
	    }
	
	    public static IEnumerator InitAsync(string pLang)
	    {
			if (languages.Contains(pLang))
				PlayerPrefs.SetString("CurrentLanguage", pLang);
			yield return LocalizationExample1.InitAsync();
			yield return LocalizationExample2.InitAsync();
	        OnLanguageChanged?.Invoke();
	    }
		
		public static void SetFolder(string pFolder)
		{
			LocalizationExample1.Folder = pFolder;
			LocalizationExample2.Folder = pFolder;
		}
	
	    public static void UseAddressable(bool pValue)
	    {
			LocalizationExample1.Addressable = pValue;
			LocalizationExample2.Addressable = pValue;
	    }
	
	    public static string GetSystemLanguage()
		{
			if (PlayerPrefs.HasKey("CurrentLanguage"))
				return PlayerPrefs.GetString("CurrentLanguage");
	
			var curLang = Application.systemLanguage;
			return curLang switch
			{
				SystemLanguage.English => "english",
				SystemLanguage.Spanish => "spanish",
				SystemLanguage.Japanese => "japan",
				SystemLanguage.ChineseSimplified => "chinese",
				SystemLanguage.Korean => "korean",
				SystemLanguage.Thai => "thai",
				_ => "english",
	
			};
		}
	}
	
}
