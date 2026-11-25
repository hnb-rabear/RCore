#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Self-check for <see cref="SpineEditorPreview.StepTimeline"/> â€” the one piece of the
	/// preview that is pure math and can be verified without a scene. Throws on failure so a broken
	/// case cannot pass silently.
	/// </summary>
	public static class SkeletonGraphicPreviewTimelineTests
	{
		private const float EPSILON = 1e-4f;

		[MenuItem("RCore/Spine/Run SkeletonGraphic Preview Timeline Tests")]
		public static void RunTests()
		{
			var failures = new List<string>();
			int total = 0;

			// Advance: a normal delta moves time forward and keeps playing.
			total++;
			{
				float time = 0.5f;
				bool isPlaying = true;
				SpineEditorPreview.StepTimeline(ref time, 0.05f, 1f, 2.0f, true, ref isPlaying);
				Check(failures, "Advance", 0.55f, time);
				CheckBool(failures, "Advance keeps playing", true, isPlaying);
			}

			// Clamp before speed: a stalled editor frame is clamped first, then scaled â€” so 2x stays 2x.
			total++;
			{
				float time = 0f;
				bool isPlaying = true;
				SpineEditorPreview.StepTimeline(ref time, 0.5f, 2f, 2.0f, true, ref isPlaying);
				Check(failures, "Clamp before speed", 0.2f, time);
			}

			// Wrap: looping continues past the end instead of stopping.
			total++;
			{
				float time = 1.95f;
				bool isPlaying = true;
				SpineEditorPreview.StepTimeline(ref time, 0.1f, 1f, 2.0f, true, ref isPlaying);
				Check(failures, "Loop wrap", 0.05f, time);
				CheckBool(failures, "Loop wrap keeps playing", true, isPlaying);
			}

			// Non-loop endpoint: pins to duration and stops.
			total++;
			{
				float time = 1.95f;
				bool isPlaying = true;
				SpineEditorPreview.StepTimeline(ref time, 0.1f, 1f, 2.0f, false, ref isPlaying);
				Check(failures, "Non-loop endpoint", 2.0f, time);
				CheckBool(failures, "Non-loop endpoint stops", false, isPlaying);
			}

			// Zero duration: timeline locked at 0, never ticks.
			total++;
			{
				float time = 1.0f;
				bool isPlaying = true;
				SpineEditorPreview.StepTimeline(ref time, 0.1f, 1f, 0f, true, ref isPlaying);
				Check(failures, "Zero duration time", 0f, time);
				CheckBool(failures, "Zero duration stops", false, isPlaying);
			}

			if (failures.Count > 0)
				throw new Exception($"[SkeletonGraphicPreviewTimelineTests] {failures.Count} of {total} cases failed:\n"
					+ string.Join("\n", failures.ToArray()));

			Debug.Log($"[SkeletonGraphicPreviewTimelineTests] {total} cases passed.");
		}

		private static void Check(List<string> pFailures, string pLabel, float pExpected, float pActual)
		{
			if (Mathf.Abs(pExpected - pActual) > EPSILON)
				pFailures.Add($"  {pLabel}: expected {pExpected}, got {pActual}");
		}

		private static void CheckBool(List<string> pFailures, string pLabel, bool pExpected, bool pActual)
		{
			if (pExpected != pActual)
				pFailures.Add($"  {pLabel}: expected {pExpected}, got {pActual}");
		}
	}
}
#endif
