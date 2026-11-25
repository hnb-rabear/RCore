#if UNITY_EDITOR
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Animation = Spine.Animation;

namespace RCore.Editor
{
	/// <summary>
	/// Plays a <see cref="SkeletonGraphic"/> or <see cref="SkeletonAnimation"/> animation on the SceneView while
	/// in Edit Mode, so Spine animations can be checked in Prefab Mode without entering Play Mode.
	///
	/// Poses are sampled directly rather than through the component's <c>Update(delta)</c>: preview speed
	/// stays independent of the component's own timeScale, and no Spine timeline events are dispatched.
	/// At most one session exists editor-wide (<see cref="s_activeOwner"/>), and the session restores the
	/// target before anything can persist the previewed pose.
	/// </summary>
	public class SpineEditorPreview
	{
		private const float MAX_DELTA = 0.1f;

		private static SpineEditorPreview s_activeOwner;

		// SkeletonGraphic or SkeletonAnimation; the inspectors only ever pass those two.
		private readonly MonoBehaviour m_target;
		private readonly System.Action m_repaintOwner;

		private bool m_isPlaying;
		private bool m_isLoop = true;
		private float m_speedScale = 1f;
		private float m_currentTime;
		private float m_duration;
		private double m_lastTime;
		private string m_selectedAnimationName;
		private SkeletonDataAsset m_ownedDataAsset;

		private bool m_globalHooksSubscribed;
		private bool m_playbackTickSubscribed;

		public SpineEditorPreview(SkeletonGraphic pTarget, System.Action pRepaintOwner) : this((MonoBehaviour)pTarget, pRepaintOwner) { }
		public SpineEditorPreview(SkeletonAnimation pTarget, System.Action pRepaintOwner) : this((MonoBehaviour)pTarget, pRepaintOwner) { }

		private SpineEditorPreview(MonoBehaviour pTarget, System.Action pRepaintOwner)
		{
			m_target = pTarget;
			m_repaintOwner = pRepaintOwner;
		}

		public MonoBehaviour Target => m_target;
		public bool IsOwner => s_activeOwner == this;

		private SkeletonDataAsset DataAsset => ((ISkeletonComponent)m_target).SkeletonDataAsset;
		private Skeleton Skeleton => ((ISkeletonComponent)m_target).Skeleton;

		/// <summary>
		/// The preview writes a pose into the live component and rebuilds its mesh — the same call the
		/// [ExecuteAlways] component already makes from its own LateUpdate in Edit Mode, so the preview adds no
		/// mutation of its own. SkeletonGraphic separation is still refused — separator parts reparent by slot group.
		/// </summary>
		public bool CheckSupport(out string pReason)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
			{
				pReason = "Preview is only available in Edit Mode.";
				return false;
			}
			if (m_target == null || !m_target.isActiveAndEnabled)
			{
				pReason = "Target is null, inactive, or disabled.";
				return false;
			}
			if (Selection.objects != null && Selection.objects.Length > 1)
			{
				pReason = "Multi-selection is not supported by preview.";
				return false;
			}
			if (EditorUtility.IsPersistent(m_target)
				&& UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(m_target.gameObject) == null)
			{
				pReason = "Open the prefab in Prefab Mode to preview it.";
				return false;
			}
			if (DataAsset == null)
			{
				pReason = "You need to assign a SkeletonDataAsset first.";
				return false;
			}

			var skeletonData = DataAsset.GetSkeletonData(true);
			if (skeletonData == null)
			{
				pReason = "SkeletonData could not be loaded.";
				return false;
			}
			if (skeletonData.Animations == null || skeletonData.Animations.Count == 0)
			{
				pReason = "No animations found in SkeletonData.";
				return false;
			}
			if (!string.IsNullOrEmpty(m_selectedAnimationName) && skeletonData.FindAnimation(m_selectedAnimationName) == null)
			{
				pReason = $"Selected animation '{m_selectedAnimationName}' no longer exists.";
				return false;
			}
			// A session that took ownership of one asset must not keep sampling after the asset is swapped.
			if (m_ownedDataAsset != null && m_ownedDataAsset != DataAsset)
			{
				pReason = "SkeletonDataAsset changed. Press Play to restart the preview.";
				return false;
			}
			if (m_target is SkeletonGraphic graphic && HasSeparation(graphic))
			{
				pReason = "Separator slots are configured. Separation is not supported by preview.";
				return false;
			}

			pReason = null;
			return true;
		}

		private static bool HasSeparation(SkeletonGraphic pTarget)
		{
			return pTarget.enableSeparatorSlots
				|| (pTarget.separatorSlots != null && pTarget.separatorSlots.Count > 0)
				|| (pTarget.SeparatorParts != null && pTarget.SeparatorParts.Count > 0)
				|| HasSeparatorSlotNames(pTarget);
		}

		/// <summary>separatorSlotNames is protected, so it is read through a throwaway SerializedObject. Read only.</summary>
		private static bool HasSeparatorSlotNames(SkeletonGraphic pTarget)
		{
			using (var so = new SerializedObject(pTarget))
			{
				var prop = so.FindProperty("separatorSlotNames");
				return prop != null && prop.arraySize > 0;
			}
		}

		private void TakeOwnership()
		{
			if (s_activeOwner != null && s_activeOwner != this)
				s_activeOwner.StopPreview();
			s_activeOwner = this;
			m_ownedDataAsset = m_target != null ? DataAsset : null;

			// Once, on taking the target: the component still runs Update(0f) in Edit Mode, which would
			// re-apply its starting animation over every sampled pose.
			var state = m_target != null ? ((IAnimationStateComponent)m_target).AnimationState : null;
			if (state != null) state.ClearTracks();

			SubscribeGlobalHooks();
		}

		public void Play()
		{
			if (!CheckSupport(out _)) return;
			if (!IsOwner) TakeOwnership();

			if (m_duration <= 0f)
			{
				m_currentTime = 0f;
				Pause();
				SampleCurrentPose();
				return;
			}

			if (m_currentTime >= m_duration) m_currentTime = 0f;
			m_lastTime = EditorApplication.timeSinceStartup;
			m_isPlaying = true;
			SubscribePlaybackTick();
			SampleCurrentPose();
		}

		/// <summary>Stops ticking but keeps ownership: a paused session still holds a modified pose.</summary>
		public void Pause()
		{
			m_isPlaying = false;
			UnsubscribePlaybackTick();
		}

		public void StopPreview(bool pRestoreTarget = true)
		{
			bool wasOwner = IsOwner;
			if (wasOwner) s_activeOwner = null;

			UnsubscribePlaybackTick();
			UnsubscribeGlobalHooks();

			m_isPlaying = false;
			m_currentTime = 0f;
			m_ownedDataAsset = null;

			if (wasOwner && pRestoreTarget) RestoreTarget();
		}

		private void RestoreTarget()
		{
			// Not gated on isPlayingOrWillChangePlaymode: that flag is already set during ExitingEditMode,
			// which is exactly when this restore has to run.
			if (Application.isPlaying || !IsTargetValidForRestore()) return;
			try
			{
				if (m_target is SkeletonGraphic graphic) graphic.Initialize(true);
				else ((SkeletonRenderer)m_target).Initialize(true);
				RebuildMesh();
				Repaint();
			}
			catch (System.Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private bool IsTargetValidForRestore()
		{
			if (m_target == null || m_target.gameObject == null) return false;
			if (DataAsset == null) return false;
			if (DataAsset.GetSkeletonData(true) == null) return false;
			if (m_target is SkeletonGraphic graphic)
			{
				if (graphic.enableSeparatorSlots) return false;
				if (graphic.separatorSlots != null && graphic.separatorSlots.Count > 0) return false;
				if (graphic.SeparatorParts != null && graphic.SeparatorParts.Count > 0) return false;
			}
			return true;
		}

		/// <summary>The same mesh rebuild each component runs from its own LateUpdate.</summary>
		private void RebuildMesh()
		{
			if (m_target is SkeletonGraphic graphic) graphic.UpdateMesh();
			else ((SkeletonRenderer)m_target).LateUpdate();
		}

		private void SampleCurrentPose()
		{
			// A guard can fail for a recoverable reason (multi-selection, entering play mode), so still ask
			// for the restore; RestoreTarget skips targets that were destroyed or fell out of scope.
			if (!CheckSupport(out _))
			{
				StopPreview();
				return;
			}

			var skeleton = Skeleton;
			var skeletonData = DataAsset.GetSkeletonData(true);
			var animation = skeletonData != null ? skeletonData.FindAnimation(m_selectedAnimationName) : null;
			if (animation == null || skeleton == null) return;

			skeleton.SetToSetupPose();
			// loop:false always — the helper wraps time itself, so scrubbing to the very end shows the last
			// frame instead of snapping back. A null event list keeps timeline events from firing.
			animation.Apply(skeleton, m_currentTime, m_currentTime, false, null, 1f, MixBlend.Replace, MixDirection.In);
			skeleton.UpdateWorldTransform();
			RebuildMesh();

			Repaint();
		}

		private void Repaint()
		{
			SceneView.RepaintAll();
			m_repaintOwner?.Invoke();
		}

		private void OnEditorUpdate()
		{
			if (!m_isPlaying) return;
			if (!IsOwner)
			{
				UnsubscribePlaybackTick();
				return;
			}
			if (!CheckSupport(out _))
			{
				StopPreview();
				return;
			}
			if (m_duration <= 0f)
			{
				Pause();
				return;
			}

			double now = EditorApplication.timeSinceStartup;
			float rawDelta = (float)(now - m_lastTime);
			m_lastTime = now;

			StepTimeline(ref m_currentTime, rawDelta, m_speedScale, m_duration, m_isLoop, ref m_isPlaying);
			if (!m_isPlaying) UnsubscribePlaybackTick();

			SampleCurrentPose();
		}

		/// <summary>
		/// Pure timeline math, kept static so <c>SkeletonGraphicPreviewTimelineTests</c> can exercise it.
		/// The raw delta is clamped before the speed scale, otherwise a 2x preview would silently lose its
		/// multiplier whenever the editor stalls past the clamp.
		/// </summary>
		public static void StepTimeline(ref float pCurrentTime, float pRawDelta, float pSpeedScale, float pDuration,
			bool pIsLoop, ref bool pIsPlaying)
		{
			if (pDuration <= 0f)
			{
				pCurrentTime = 0f;
				pIsPlaying = false;
				return;
			}

			float delta = Mathf.Clamp(pRawDelta, 0f, MAX_DELTA);
			pCurrentTime += delta * pSpeedScale;

			if (pCurrentTime < pDuration) return;

			if (pIsLoop)
			{
				pCurrentTime %= pDuration;
			}
			else
			{
				pCurrentTime = pDuration;
				pIsPlaying = false;
			}
		}

		private void SubscribeGlobalHooks()
		{
			if (m_globalHooksSubscribed) return;
			m_globalHooksSubscribed = true;

			Selection.selectionChanged += OnSelectionChanged;
			UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
			UnityEditor.SceneManagement.PrefabStage.prefabSaving += OnPrefabSaving;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			EditorApplication.hierarchyChanged += OnHierarchyOrProjectChanged;
			EditorApplication.projectChanged += OnHierarchyOrProjectChanged;
		}

		private void UnsubscribeGlobalHooks()
		{
			if (!m_globalHooksSubscribed) return;
			m_globalHooksSubscribed = false;

			Selection.selectionChanged -= OnSelectionChanged;
			UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
			UnityEditor.SceneManagement.PrefabStage.prefabSaving -= OnPrefabSaving;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			EditorApplication.hierarchyChanged -= OnHierarchyOrProjectChanged;
			EditorApplication.projectChanged -= OnHierarchyOrProjectChanged;
		}

		private void SubscribePlaybackTick()
		{
			if (m_playbackTickSubscribed) return;
			m_playbackTickSubscribed = true;
			EditorApplication.update += OnEditorUpdate;
		}

		private void UnsubscribePlaybackTick()
		{
			if (!m_playbackTickSubscribed) return;
			m_playbackTickSubscribed = false;
			EditorApplication.update -= OnEditorUpdate;
		}

		public void OnInspectorDisabled()
		{
			if (IsOwner) StopPreview();
		}

		private void OnSelectionChanged() => StopPreview();

		private void OnPrefabStageClosing(UnityEditor.SceneManagement.PrefabStage pStage)
		{
			if (pStage == null || m_target == null) return;
			if (pStage.scene == m_target.gameObject.scene
				|| (pStage.prefabContentsRoot != null && m_target.transform.IsChildOf(pStage.prefabContentsRoot.transform)))
				StopPreview();
		}

		private void OnPrefabSaving(GameObject pPrefabRoot)
		{
			if (pPrefabRoot == null || m_target == null) return;
			if (m_target.gameObject == pPrefabRoot
				|| m_target.transform.IsChildOf(pPrefabRoot.transform)
				|| m_target.gameObject.scene == pPrefabRoot.scene)
				StopPreview();
		}

		private void OnSceneSaving(UnityEngine.SceneManagement.Scene pScene, string pPath)
		{
			if (m_target != null && m_target.gameObject.scene == pScene)
				StopPreview();
		}

		private void OnPlayModeStateChanged(PlayModeStateChange pState)
		{
			if (pState == PlayModeStateChange.ExitingEditMode) StopPreview();
		}

		private void OnBeforeAssemblyReload() => StopPreview();

		private void OnHierarchyOrProjectChanged()
		{
			if (m_target == null || m_target.gameObject == null || !CheckSupport(out _))
				StopPreview();
		}

		/// <summary>
		/// The component's configured animation. Read before ownership clears the tracks: SkeletonAnimation's
		/// AnimationName getter returns the current track once the component is valid.
		/// </summary>
		private string StartingAnimationName => m_target is SkeletonGraphic graphic
			? graphic.startingAnimation
			: ((SkeletonAnimation)m_target).AnimationName;

		public void DrawGUI()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Spine Animation Preview", EditorStyles.boldLabel);

				if (!CheckSupport(out string guardReason))
				{
					if (IsOwner) StopPreview();
					EditorGUILayout.HelpBox(guardReason, MessageType.Info);
					return;
				}

				var skeletonData = DataAsset.GetSkeletonData(true);
				var animations = skeletonData.Animations;

				if (string.IsNullOrEmpty(m_selectedAnimationName) || skeletonData.FindAnimation(m_selectedAnimationName) == null)
				{
					string startingAnimation = StartingAnimationName;
					m_selectedAnimationName = !string.IsNullOrEmpty(startingAnimation)
						&& skeletonData.FindAnimation(startingAnimation) != null
							? startingAnimation
							: animations.Items[0].Name;
				}

				var currentAnim = skeletonData.FindAnimation(m_selectedAnimationName);
				m_duration = currentAnim != null ? currentAnim.Duration : 0f;

				DrawAnimationPopup(skeletonData, animations);
				DrawPlaybackButtons();
				DrawTimeline();

				m_isLoop = EditorGUILayout.Toggle("Loop", m_isLoop);
				m_speedScale = EditorGUILayout.Slider("Speed", m_speedScale, 0.1f, 2f);
			}
		}

		private void DrawAnimationPopup(SkeletonData pSkeletonData, ExposedList<Animation> pAnimations)
		{
			int selectedIndex = 0;
			var labels = new GUIContent[pAnimations.Count];
			var names = new string[pAnimations.Count];
			for (int i = 0; i < pAnimations.Count; i++)
			{
				string animName = pAnimations.Items[i].Name;
				names[i] = animName;
				labels[i] = new GUIContent(animName);
				if (animName == m_selectedAnimationName) selectedIndex = i;
			}

			EditorGUI.BeginChangeCheck();
			int newIndex = EditorGUILayout.Popup(new GUIContent("Animation"), selectedIndex, labels);
			if (!EditorGUI.EndChangeCheck()) return;
			if (newIndex < 0 || newIndex >= names.Length || names[newIndex] == m_selectedAnimationName) return;

			m_selectedAnimationName = names[newIndex];
			var anim = pSkeletonData.FindAnimation(m_selectedAnimationName);
			m_duration = anim != null ? anim.Duration : 0f;
			m_currentTime = 0f;

			if (!IsOwner) TakeOwnership();
			if (m_duration <= 0f) Pause();
			else if (m_isPlaying) m_lastTime = EditorApplication.timeSinceStartup;

			SampleCurrentPose();
		}

		private void DrawPlaybackButtons()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button(m_isPlaying ? "Pause" : "Play", GUILayout.Height(20)))
				{
					if (m_isPlaying) Pause();
					else Play();
				}

				using (new EditorGUI.DisabledGroupScope(!IsOwner))
				{
					if (GUILayout.Button("Stop", GUILayout.Height(20)))
						StopPreview();
				}
			}
		}

		private void DrawTimeline()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField($"{m_currentTime:F2}s / {m_duration:F2}s", GUILayout.Width(90f));

				using (new EditorGUI.DisabledGroupScope(m_duration <= 0f))
				{
					EditorGUI.BeginChangeCheck();
					float sliderTime = EditorGUILayout.Slider(m_currentTime, 0f, m_duration > 0f ? m_duration : 0.001f);
					if (!EditorGUI.EndChangeCheck()) return;

					if (!IsOwner) TakeOwnership();
					Pause(); // scrubbing takes over playback
					m_currentTime = Mathf.Clamp(sliderTime, 0f, m_duration);
					SampleCurrentPose();
				}
			}
		}
	}
}
#endif
