using System;

namespace RCore
{
	public enum YesNoNone
	{
		None = 0,
		No = 1,
		Yes = 2,
	}
	
	public enum PerfectRatio
	{
		None,
		Width,
		Height,
	}
	
	public enum UIPivot
	{
		Bot,
		Top,
		TopLeft,
		BotLeft,
		TopRight,
		BotRight,
		Center,
	}

	public enum TapFeedback
	{
		None,
		Sound,
		Haptic,
		SoundAndHaptic,
	}
}