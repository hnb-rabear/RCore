// Copyright (c) Scott Doxey. All Rights Reserved. Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CandyCoded.HapticFeedback
{
	public static class HapticFeedback
	{
		public static void LightFeedback()
		{
#if UNITY_IOS
            iOS.HapticFeedback.PerformHapticFeedback("light");
#elif UNITY_ANDROID
			Android.HapticFeedback.PerformHapticFeedback(Android.HapticFeedbackConstants.CLOCK_TICK);
#endif
		}

		public static void MediumFeedback()
		{
#if UNITY_IOS
            iOS.HapticFeedback.PerformHapticFeedback("medium");
#elif UNITY_ANDROID
			Android.HapticFeedback.PerformHapticFeedback(Android.HapticFeedbackConstants.VIRTUAL_KEY);
#endif

		}

		public static void HeavyFeedback()
		{

#if UNITY_IOS
            iOS.HapticFeedback.PerformHapticFeedback("heavy");
#elif UNITY_ANDROID
			Android.HapticFeedback.PerformHapticFeedback(Android.HapticFeedbackConstants.CALENDAR_DATE);
#endif

		}
	}
}