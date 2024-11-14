using System;
using System.Collections;
using System.Collections.Generic;
using SheetXExample;
using UnityEngine;

public class ExampleLocalization : MonoBehaviour
{
	private void Start()
	{
		Init();
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
}