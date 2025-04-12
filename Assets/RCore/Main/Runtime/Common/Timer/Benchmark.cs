using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace RCore
{
	public class Benchmark : MonoBehaviour
	{
		private static Benchmark m_Instance;
		public static Benchmark Instance => m_Instance;

		// Configuration
		public int targetFrameRate = 60; // Set this to your game's target frame rate.  0 = system default
		public float updateInterval = 1.0f; // How often should the FPS counter update? in seconds
		public string logFilePath = "BenchmarkLog.txt"; // Path to the log file.  Will be in Application.persistentDataPath
		public bool logToFile; // Enable/disable logging to file
		public bool trackMemory; // Enable/disable memory usage tracking.  Note: Can impact performance.

		// Timing and FPS
		private float m_timeElapsed;
		private int m_frameCount;
		private float m_currentFps;
		private float m_minFps = float.MaxValue;
		private float m_maxFps;
		private float m_averageFps;
		private List<float> m_fpsList = new();

		// Memory Tracking
		private long m_lastUsedMemory;
		private long m_peakUsedMemory;

		// Application State
		private bool m_isPaused;
		private bool m_isFocused = true;

		// Logging
		private StreamWriter m_logWriter;

		public float CurrentFPS => m_currentFps;
		public float MinFPS => m_minFps;
		public float MaxFPS => m_maxFps;
		public float AverageFPS => m_averageFps;
		public long CurrentMemoryUsage => m_lastUsedMemory;
		public long PeakMemoryUsage => m_peakUsedMemory;

		private void Awake()
		{
			if (m_Instance == null)
			{
				m_Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			Application.targetFrameRate = targetFrameRate;
			InitializeLogging();
		}

		private void Start()
		{
			m_timeElapsed = 0f;
			m_frameCount = 0;
			m_lastUsedMemory = 0; // Initialize memory
			m_peakUsedMemory = 0;
		}

		private void Update()
		{
			if (m_isPaused || !m_isFocused) return; // Don't benchmark when paused or unfocused.

			m_timeElapsed += Time.deltaTime;
			m_frameCount++;

			if (m_timeElapsed >= updateInterval)
			{
				m_currentFps = m_frameCount / m_timeElapsed;
				UpdateMinMaxFps(m_currentFps);
				UpdateAverageFps(m_currentFps);
				m_timeElapsed = 0f;
				m_frameCount = 0;
				LogData(); // Log every update interval
			}
			if (trackMemory)
			{
				SampleMemory();
			}
		}

		private void OnApplicationPause(bool p_pause)
		{
			m_isPaused = p_pause;
			if (p_pause)
			{
				LogToFile("Application Paused");
			}
			else
			{
				LogToFile("Application Resumed");
			}
		}

		private void OnApplicationFocus(bool p_hasFocus)
		{
			m_isFocused = p_hasFocus;
			if (!p_hasFocus)
			{
				LogToFile("Application Lost Focus");
			}
			else
			{
				LogToFile("Application Regained Focus");
			}
		}

		private void OnApplicationQuit()
		{
			LogToFile("Application Quit");
			CloseLogFile();
		}

		// --- FPS Calculation ---
		private void UpdateMinMaxFps(float p_fps)
		{
			m_minFps = Mathf.Min(m_minFps, p_fps);
			m_maxFps = Mathf.Max(m_maxFps, p_fps);
		}

		private void UpdateAverageFps(float p_currentFps)
		{
			m_fpsList.Add(p_currentFps);
			float sum = 0;
			foreach (float fpsValue in m_fpsList)
			{
				sum += fpsValue;
			}
			m_averageFps = sum / m_fpsList.Count;
			if (m_fpsList.Count > 300) // Limit the size of the list to avoid memory issues
				m_fpsList.RemoveAt(0);
		}

		// --- Memory Usage ---
		private void SampleMemory()
		{
#if UNITY_EDITOR
			long currentUsedMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
#else
			long currentUsedMemory = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64; // Alternative
#endif
			if (currentUsedMemory > m_peakUsedMemory)
			{
				m_peakUsedMemory = currentUsedMemory;
			}
			m_lastUsedMemory = currentUsedMemory;
		}

		// --- Logging ---
		private void InitializeLogging()
		{
			if (logToFile)
			{
				try
				{
					string filePath = Path.Combine(Application.persistentDataPath, logFilePath);
					m_logWriter = new StreamWriter(filePath, true); // 'true' for append
					m_logWriter.WriteLine("--- Benchmark Log Started: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ---");
					m_logWriter.Flush(); // Ensure the header is written immediately.
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to initialize logging: " + e.Message);
					logToFile = false; // Disable logging if initialization fails.
					m_logWriter = null;
				}
			}
		}

		private void LogData()
		{
			string logMessage = string.Format(
				"Time: {0:F2}, FPS: {1:F2}, Min: {2:F2}, Max: {3:F2}, Avg: {4:F2}",
				Time.time, m_currentFps, m_minFps, m_maxFps, m_averageFps
			);
			if (trackMemory)
			{
				logMessage += string.Format(", Mem: {0:N0} B, Peak: {1:N0} B", m_lastUsedMemory, m_peakUsedMemory);
			}
			Debug.Log(logMessage); // Always log to Unity console

			LogToFile(logMessage); // Log to file if enabled
		}

		private void LogToFile(string p_message)
		{
			if (logToFile && m_logWriter != null)
			{
				try
				{
					m_logWriter.WriteLine(p_message);
					m_logWriter.Flush(); // Important:  Flush the buffer to the file!
				}
				catch (Exception e)
				{
					Debug.LogError("Error writing to log file: " + e.Message);
					// Consider disabling logging here if you get repeated errors.
					logToFile = false;
					CloseLogFile();
				}
			}
		}

		private void CloseLogFile()
		{
			if (m_logWriter != null)
			{
				try
				{
					m_logWriter.Close();
				}
				catch (Exception e)
				{
					Debug.LogError("Error closing log file: " + e.Message);
				}
				finally
				{
					m_logWriter = null;
				}
			}
		}

		//Call this function to reset all the values.
		public void ResetBenchmark()
		{
			m_timeElapsed = 0f;
			m_frameCount = 0;
			m_currentFps = 0f;
			m_minFps = float.MaxValue;
			m_maxFps = 0f;
			m_averageFps = 0f;
			m_fpsList.Clear();
			m_lastUsedMemory = 0;
			m_peakUsedMemory = 0;
		}
	}
}