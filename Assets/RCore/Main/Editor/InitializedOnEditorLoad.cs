using System.Globalization;
using UnityEditor;

namespace RCore.Editor
{
    [InitializeOnLoad]
    public static class InitializedOnEditorLoad
    {
        // This static constructor runs automatically when the Editor loads
        static InitializedOnEditorLoad()
        {
            EditorApplication.update += RunOnEditorStart;
        }

        private static void RunOnEditorStart()
        {
            EditorApplication.update -= RunOnEditorStart;

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
        }
    }
}