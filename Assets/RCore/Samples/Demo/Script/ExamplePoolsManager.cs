#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Demo
{
	public class ExamplePoolsManager : MonoBehaviour
	{
		#region Members

		private static ExamplePoolsManager mInstance;
		public static ExamplePoolsManager Instance
		{
			get
			{
				if (mInstance == null)
					mInstance = FindObjectOfType<ExamplePoolsManager>();
				return mInstance;
			}
		}

		[SerializeField] private CustomPool<Transform> mBuiltInPool; //This is a example single pool outside of Pool container

		/// <summary>
		/// Container which contain all pools of Transform Object
		/// </summary>
		private PoolsContainer<Transform> mPoolsContainerTransform;
		/// <summary>
		/// Container which contain all pools of Image Objects
		/// </summary>
		private PoolsContainer<Image> mImagePools;
		/// <summary>
		/// Simple class used to manage countdown action, called from update
		/// Use CountdownEventsManager.Register and CountdownEventsManager.Unregister to declare and release event action
		/// NOTE 1: View file CoroutineMediator.cs, this is a Global class used for Events Actions
		/// NOTE 2: View file WaitUtil, it is easy tool which work with CoroutineMediator
		/// </summary>
		private CountdownEventsManager mCountdownEventsManager = new CountdownEventsManager();
		/// <summary>
		/// Simple FPS Counter
		/// </summary>
		private FPSCounter mFPSCounter = new FPSCounter();

		public Transform redCubePrefab;
		public Transform blueCubePrefab;
		public Transform greenCubePrefab;
		public Transform spherePrefab;

		#endregion

		//============================================================

		#region MonoBehaviour

		private void Start()
		{
			// Simple Benmark, It basically is FPS Counter
			Benchmark.Instance.StartBenchmark(15, (fPS, minFPS, maxFPS) =>
			{
				Debug.Log($"Benmark Finished: FPS:{fPS} MinFPS:{minFPS} MaxFPS:{maxFPS}");
			});

			//Simple Wait For Work
			int a = 0;
			WaitUtil.Start(3f, (s) => { Debug.Log("Wait 3s"); });
			WaitUtil.Start(5f, (s) => { Debug.Log("Wait 5f"); });
			WaitUtil.Start(7f, (s) => { Debug.Log("Wait 7f"); });
			WaitUtil.Start(9f, (s) => { Debug.Log("Wait 9f"); a = 9; });
			WaitUtil.Start(() => a == 9, () => { Debug.Log("Wait till a = 9"); });
		}

		private void Update()
		{
			mFPSCounter.Update(Time.deltaTime);
			if (mFPSCounter.updated)
				Debug.Log("FPS: " + mFPSCounter.fps);
		}

		private void LateUpdate()
		{
			mCountdownEventsManager.LateUpdate();
		}

		public void Init()
		{
			//Init pools manager for cube prefabs
			mPoolsContainerTransform = new PoolsContainer<Transform>(3, transform);

			//Set parent for build in pool
			mBuiltInPool.SetParent(mPoolsContainerTransform.container);

			//Add buildin pool to dictionary of multi-pools
			mPoolsContainerTransform.Add(mBuiltInPool);
		}

		#endregion

		//===============================================================

		#region Public

		/// <summary>
		/// Release pooled object
		/// </summary>
		public void Spawn(Transform prefab, Vector3 position)
		{
			mPoolsContainerTransform.Spawn(prefab, position);
			var pool = mPoolsContainerTransform.Get(prefab);
			var activeList = pool.ActiveList();
			if (activeList.Count > 10)
				pool.Release(activeList[0]);
		}

		/// <summary>
		/// Spawn pooled object
		/// </summary>
		public void Spawn(Transform prefab, Transform position)
		{
			mPoolsContainerTransform.Spawn(prefab, position);
			var pool = mPoolsContainerTransform.Get(prefab);
			var activeList = pool.ActiveList();
			if (activeList.Count > 10)
				pool.Release(activeList[0]);
		}

		/// <summary>
		/// Release pooled object
		/// </summary>
		public void Release(Transform pObj)
		{
			mPoolsContainerTransform.Release(pObj);
		}

		/// <summary>
		/// Release pooled object
		/// </summary>
		public void Release(GameObject pObj)
		{
			mPoolsContainerTransform.Release(pObj);
		}

		/// <summary>
		/// Auto release pooled object after seconds
		/// </summary>
		public void Release(Transform pObj, float pCountdown)
		{
			if (pCountdown == 0)
			{
				UnRegistereWait(pObj.GetInstanceID());
				mPoolsContainerTransform.Release(pObj);
			}
			else
			{
				RegisterWait(new WaitUtil.CountdownEvent()
				{
					id = pObj.GetInstanceID(),
					waitTime = pCountdown,
					doSomething = (s) =>
					{
						mPoolsContainerTransform.Release(pObj);
					}
				});
			}
		}

		/// <summary>
		/// Auto release pooled object after seconds
		/// </summary>
		public void Release(GameObject pObj, float pCountdown)
		{
			if (pCountdown == 0)
			{
				UnRegistereWait(pObj.GetInstanceID());
				mPoolsContainerTransform.Release(pObj);
			}
			else
			{
				RegisterWait(new WaitUtil.CountdownEvent()
				{
					id = pObj.GetInstanceID(),
					waitTime = pCountdown,
					doSomething = (s) =>
					{
						mPoolsContainerTransform.Release(pObj);
					}
				});
			}
		}

		#endregion

		//=====================================================================

		#region Private

		private void RegisterWait(WaitUtil.CountdownEvent pEvent)
		{
			mCountdownEventsManager.Register(pEvent);
			enabled = true;
		}

		private void UnRegistereWait(int pId)
		{
			mCountdownEventsManager.UnRegister(pId);
		}

		#endregion

		//======================================================================

		#region Editor

#if UNITY_EDITOR
		[CustomEditor(typeof(ExamplePoolsManager))]
		public class ExamplePoolsManagerEditor : UnityEditor.Editor
		{
			private ExamplePoolsManager mTarget;

			private void OnEnable()
			{
				mTarget = target as ExamplePoolsManager;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (Application.isPlaying)
					mTarget.mPoolsContainerTransform.DrawOnEditor();
				else
					EditorGUILayout.HelpBox("Click play to see how it work", MessageType.Info);
			}
		}
#endif

		#endregion
	}
}