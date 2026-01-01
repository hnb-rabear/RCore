using System.Globalization;
using UnityEditor;
using System;

namespace RCore.Editor
{
	/// <summary>
	/// Performs initialization tasks when the editor loads, such as setting culture and validating defines.
	/// </summary>
	[InitializeOnLoad]
	public static class InitializedOnEditorLoad
	{
		static InitializedOnEditorLoad()
		{
			EditorApplication.update += RunOnEditorStart;
		}

		private static void RunOnEditorStart()
		{
			var culture = CultureInfo.CreateSpecificCulture("en-US");
			var dateTimeFormat = new DateTimeFormatInfo
			{
				ShortDatePattern = "dd-MM-yyyy",
				LongTimePattern = "HH:mm:ss",
				ShortTimePattern = "HH:mm"
			};
			culture.DateTimeFormat = dateTimeFormat;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;

			Validate_DOTWEEN();
			Validate_ADDRESSABLES();
		}

		private static void Validate_DOTWEEN()
		{
			var dotweenType = Type.GetType("DG.Tweening.DOTween, DOTween");
			if (dotweenType != null)
				EditorHelper.AddDirective("DOTWEEN");
			else
				EditorHelper.RemoveDirective("DOTWEEN");
		}

		private static void Validate_ADDRESSABLES()
		{
			var addressablesType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
			if (addressablesType != null)
				EditorHelper.AddDirective("ADDRESSABLES");
			else
				EditorHelper.RemoveDirective("ADDRESSABLES");
		}
	}
}