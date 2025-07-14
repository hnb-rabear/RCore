using UnityEngine;
using UnityEditor;
using System;
using System.Globalization;

namespace RCore.Editor
{
	public class DateTimePickerWindow : EditorWindow
	{
		public DateTime selectedDateTime = DateTime.Now;
		private string[] m_monthNames;
		private string[] m_dayNames;
		private int m_selectedYear;
		private int m_selectedMonth;
		private int m_selectedDay;
		private int m_selectedHour;
		private int m_selectedMinute;
		private int m_selectedSecond;
		private Vector2 m_scrollPosition;
		private DateTime m_lastValidDateTime;
		private GUIStyle m_largeCenteredGreyMiniLabel;
		public Action<DateTime> onQuit;
		private bool m_keepFocus;

		public static DateTimePickerWindow ShowWindow(DateTime selectedDate, bool pKeepFocus = false)
		{
			var window = GetWindow<DateTimePickerWindow>("Date-Time Picker");
			window.minSize = new Vector2(310, 550); // Increased height for better layout
			window.maxSize = new Vector2(310, 550);
			window.m_keepFocus = pKeepFocus;
			window.selectedDateTime = selectedDate;
			window.UpdateSelectedFieldsFromDateTime();
			return window;
		}

		private void OnEnable()
		{
			// Initialize with current date and time
			m_lastValidDateTime = selectedDateTime;
			UpdateSelectedFieldsFromDateTime();

			// Get month and day names based on current culture
			m_monthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
			// Remove empty string if present (some cultures have 13 months with one empty)
			m_monthNames = Array.FindAll(m_monthNames, name => !string.IsNullOrEmpty(name));

			m_dayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
			m_largeCenteredGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
			{
				fontSize = 12, // Adjust the font size as needed
				fontStyle = FontStyle.Bold,
				normal = { textColor = Color.yellow } // Set text color to red
			};
		}

		private void OnDestroy()
		{
			onQuit?.Invoke(selectedDateTime);
		}

		private void UpdateSelectedFieldsFromDateTime()
		{
			m_selectedYear = selectedDateTime.Year;
			m_selectedMonth = selectedDateTime.Month;
			m_selectedDay = selectedDateTime.Day;
			m_selectedHour = selectedDateTime.Hour;
			m_selectedMinute = selectedDateTime.Minute;
			m_selectedSecond = selectedDateTime.Second;
		}

		private bool TryUpdateDateTimeFromSelectedFields()
		{
			try
			{
				var newDateTime = new DateTime(m_selectedYear, m_selectedMonth, m_selectedDay, m_selectedHour, m_selectedMinute, m_selectedSecond);
				selectedDateTime = newDateTime;
				m_lastValidDateTime = selectedDateTime; // Store this as the new last valid date
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{
				// Handle invalid date, e.g., February 30th
				EditorUtility.DisplayDialog("Invalid Date",
					$"The date {m_selectedYear}-{m_selectedMonth:D2}-{m_selectedDay:D2} is not valid. Reverting to the last valid date.",
					"OK");
				selectedDateTime = m_lastValidDateTime; // Revert to the last valid date
				UpdateSelectedFieldsFromDateTime(); // Update fields to reflect the reverted date
				return false;
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Select Date and Time", EditorStyles.boldLabel);

			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

			// --- Date Selection ---
			EditorGUILayout.LabelField("Date", m_largeCenteredGreyMiniLabel);
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			m_selectedYear = EditorGUILayout.IntField("Year", m_selectedYear);
			if (GUILayout.Button("-", GUILayout.Width(25))) m_selectedYear--;
			if (GUILayout.Button("+", GUILayout.Width(25))) m_selectedYear++;
			EditorGUILayout.EndHorizontal();

			// Clamp year to a reasonable range
			m_selectedYear = Mathf.Clamp(m_selectedYear, 1, 9999);

			EditorGUILayout.BeginHorizontal();
			m_selectedMonth = EditorGUILayout.Popup("Month", m_selectedMonth - 1, m_monthNames) + 1;
			EditorGUILayout.EndHorizontal();

			// Clamp month (though popup should handle this)
			m_selectedMonth = Mathf.Clamp(m_selectedMonth, 1, 12);

			int daysInMonth = DateTime.DaysInMonth(m_selectedYear, m_selectedMonth);
			EditorGUILayout.BeginHorizontal();
			m_selectedDay = EditorGUILayout.IntSlider("Day", m_selectedDay, 1, daysInMonth);
			EditorGUILayout.EndHorizontal();

			// Clamp day
			m_selectedDay = Mathf.Clamp(m_selectedDay, 1, daysInMonth);


			if (EditorGUI.EndChangeCheck())
			{
				TryUpdateDateTimeFromSelectedFields();
			}

			// --- Calendar View ---
			EditorGUILayout.LabelField("Calendar", m_largeCenteredGreyMiniLabel, GUILayout.ExpandWidth(true));
			DrawCalendar();

			// --- Time Selection ---
			EditorGUILayout.LabelField("Time", m_largeCenteredGreyMiniLabel);
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			m_selectedHour = EditorGUILayout.IntSlider("Hour", m_selectedHour, 0, 23, GUILayout.Width(300));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			m_selectedMinute = EditorGUILayout.IntSlider("Minute", m_selectedMinute, 0, 59, GUILayout.Width(300));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			m_selectedSecond = EditorGUILayout.IntSlider("Second", m_selectedSecond, 0, 59, GUILayout.Width(300));
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				TryUpdateDateTimeFromSelectedFields();
			}

			// --- Display Selected Date and Time ---
			EditorGUILayout.LabelField("Selected Date & Time", m_largeCenteredGreyMiniLabel);
			EditorGUILayout.SelectableLabel(selectedDateTime.ToString(CultureInfo.CurrentCulture), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
			EditorGUILayout.SelectableLabel(selectedDateTime.ToUnixTimestampInt().ToString(), EditorStyles.textField,
				GUILayout.Height(EditorGUIUtility.singleLineHeight));

			// --- Action Buttons ---
			if (GUILayout.Button("Set to Current Time"))
			{
				selectedDateTime = DateTime.Now;
				m_lastValidDateTime = selectedDateTime;
				UpdateSelectedFieldsFromDateTime();
				GUI.FocusControl(null); // Remove focus from any field
			}

			if (GUILayout.Button("Copy to Clipboard"))
			{
				EditorGUIUtility.systemCopyBuffer = selectedDateTime.ToString("F", CultureInfo.CurrentCulture);
				ShowNotification(new GUIContent("Copied to clipboard!"));
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawCalendar()
		{
			var firstDayOfMonth = new DateTime(m_selectedYear, m_selectedMonth, 1);
			int daysInMonth = DateTime.DaysInMonth(m_selectedYear, m_selectedMonth);
			int dayOfWeek = (int)firstDayOfMonth.DayOfWeek; // 0 (Sunday) to 6 (Saturday)

			// Adjust to match common calendar layouts where Monday is the first day (optional)
			// if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday) {
			// dayOfWeek = (dayOfWeek == 0) ? 6 : dayOfWeek - 1;
			// }

			EditorGUILayout.BeginVertical(EditorStyles.helpBox); // Use HelpBox for a bordered look

			// Month and Year Header for Calendar
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("◄", GUILayout.Width(30)))
			{
				selectedDateTime = selectedDateTime.AddMonths(-1);
				m_lastValidDateTime = selectedDateTime;
				UpdateSelectedFieldsFromDateTime();
			}
			EditorGUILayout.LabelField($"{m_monthNames[m_selectedMonth - 1]} {m_selectedYear}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
			if (GUILayout.Button("►", GUILayout.Width(30)))
			{
				selectedDateTime = selectedDateTime.AddMonths(1);
				m_lastValidDateTime = selectedDateTime;
				UpdateSelectedFieldsFromDateTime();
			}
			EditorGUILayout.EndHorizontal();

			// Day Names Header
			EditorGUILayout.BeginHorizontal();
			for (int i = 0; i < 7; i++)
			{
				// Adjust index based on first day of week for culture-aware display
				int dayIndex = ((int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + i) % 7;
				EditorGUILayout.LabelField(m_dayNames[dayIndex].Substring(0, Mathf.Min(m_dayNames[dayIndex].Length, 2)), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(35));
			}
			EditorGUILayout.EndHorizontal();

			// Calendar Grid
			GUILayout.BeginVertical();
			bool dayStarted = false;
			int currentDay = 1;
			for (int i = 0; i < 6; i++) // Max 6 rows for a month
			{
				if (currentDay > daysInMonth) break;

				GUILayout.BeginHorizontal();
				for (int j = 0; j < 7; j++)
				{
					if (!dayStarted && j == dayOfWeek)
					{
						dayStarted = true;
					}

					var dayStyle = new GUIStyle(EditorStyles.miniButton);
					dayStyle.fixedWidth = 36f;
					dayStyle.fixedHeight = 25; // Make buttons a bit taller

					if (dayStarted && currentDay <= daysInMonth)
					{
						if (currentDay == m_selectedDay)
						{
							dayStyle.normal.background = MakeTex(1, 1, new Color(0.5f, 0.7f, 1f, 0.7f)); // Highlight selected day
							dayStyle.fontStyle = FontStyle.Bold;
						}
						else if (IsToday(currentDay))
						{
							dayStyle.normal.textColor = Color.Lerp(GUI.skin.label.normal.textColor, Color.blue, 0.7f); // Highlight current day
							dayStyle.fontStyle = FontStyle.Italic;
						}


						if (GUILayout.Button(currentDay.ToString(), dayStyle))
						{
							m_selectedDay = currentDay;
							if (TryUpdateDateTimeFromSelectedFields())
							{
								// Successfully updated
							}
							GUI.FocusControl(null); // Defocus
						}
						currentDay++;
					}
					else
					{
						GUI.enabled = false;
						if (GUILayout.Button("", dayStyle)) { }
						GUI.enabled = true;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
			EditorGUILayout.EndVertical();
		}

		private bool IsToday(int day)
		{
			var today = DateTime.Now;
			return m_selectedYear == today.Year && m_selectedMonth == today.Month && day == today.Day;
		}

		private Texture2D MakeTex(int width, int height, Color col)
		{
			var pix = new Color[width * height];
			for (int i = 0; i < pix.Length; ++i)
			{
				pix[i] = col;
			}
			var result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}

		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			if (m_keepFocus)
				Focus();
		}
	}
}