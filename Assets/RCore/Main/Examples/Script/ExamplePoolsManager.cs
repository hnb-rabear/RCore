#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore.Example
{
    public class ExamplePoolsManager : MonoBehaviour
    {
        private static ExamplePoolsManager m_instance;
        public static ExamplePoolsManager Instance => m_instance;

        [SerializeField] private Transform spherePrefab;
        [SerializeField] private CustomPool<Transform> m_serializedPool;
        
        private CustomPool<Transform> m_nonSerializedPool;
        /// <summary>
        /// Container which contain all pools of Transform
        /// </summary>
        private PoolsContainer<Transform> m_poolsContainer;

        //============================================================

        private void Awake()
        {
            if (m_instance == null)
                m_instance = this;
            else if (m_instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            // Simple Benchmark, It basically is FPS Counter
            TimerEventsInScene.Instance.StartBenchmark(300, (fPS, minFPS, maxFPS) => Debug.Log($"Benchmark Finished: FPS:{fPS} MinFPS:{minFPS} MaxFPS:{maxFPS}"));

            //Simple Wait For Work
            int a = 0;
            TimerEventsInScene.Instance.WaitForSeconds(3f, s => Debug.Log("Wait 3s"));
            TimerEventsInScene.Instance.WaitForSeconds(5f, s => Debug.Log("Wait 5f"));
            TimerEventsInScene.Instance.WaitForSeconds(7f, s => Debug.Log("Wait 7f"));
            TimerEventsInScene.Instance.WaitForSeconds(9f, s =>
            {
                Debug.Log("Wait 9f");
                a = 9;
            });
            TimerEventsInScene.Instance.WaitForCondition(() => a == 9, () => Debug.Log("Wait till a == 9"));
        }

        public void Init()
        {
            //NOTE: m_spherePool1 doesnt need to be Create by new() because it is serialized
            m_nonSerializedPool = new CustomPool<Transform>(spherePrefab, 1, transform);
            m_poolsContainer = new PoolsContainer<Transform>("pool", 3, transform);
        }

        //===============================================================

        /// <summary>
        /// Release pooled object
        /// </summary>
        public void SpawnByPoolContainer(Transform prefab, Vector3 position)
        {
            m_poolsContainer.Spawn(prefab, position);
            var pool = m_poolsContainer.Get(prefab);
            var activeList = pool.ActiveList();
            if (activeList.Count > 3)
                pool.Release(activeList[0]);
        }

        /// <summary>
        /// Spawn pooled object
        /// </summary>
        public void SpawnByPoolContainer(Transform prefab, Transform position)
        {
            m_poolsContainer.Spawn(prefab, position);
            var pool = m_poolsContainer.Get(prefab);
            var activeList = pool.ActiveList();
            if (activeList.Count > 3)
                pool.Release(activeList[0]);
        }

        /// <summary>
        /// Release pooled object
        /// </summary>
        public void Release(Transform pObj)
        {
            m_poolsContainer.Release(pObj);
        }

        /// <summary>
        /// Release pooled object
        /// </summary>
        public void Release(GameObject pObj)
        {
            m_poolsContainer.Release(pObj);
        }

        /// <summary>
        /// Auto release pooled object after seconds
        /// </summary>
        public void Release(Transform pObj, float pCountdown)
        {
            if (pCountdown == 0)
            {
                TimerEventsInScene.Instance.RemoveCountdownEvent(pObj.GetInstanceID());
                m_poolsContainer.Release(pObj);
            }
            else
            {
                TimerEventsInScene.Instance.WaitForSeconds(new CountdownEvent()
                {
                    id = pObj.GetInstanceID(),
                    waitTime = pCountdown,
                    onTimeOut = s => m_poolsContainer.Release(pObj)
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
                TimerEventsInScene.Instance.RemoveCountdownEvent(pObj.GetInstanceID());
                m_poolsContainer.Release(pObj);
            }
            else
            {
                TimerEventsInScene.Instance.WaitForSeconds(new CountdownEvent()
                {
                    id = pObj.GetInstanceID(),
                    waitTime = pCountdown,
                    onTimeOut = s => m_poolsContainer.Release(pObj)
                });
            }
        }

        public void SpawnByPool(Vector3 position)
        {
            if (Random.value > 0.5f)
            {
                m_serializedPool.Spawn(position, true);
                var activeList = m_serializedPool.ActiveList();
                if (activeList.Count > 3)
                    m_serializedPool.Release(activeList[0]);
            }
            else
            {
                m_nonSerializedPool.Spawn(position, true);
                var activeList = m_nonSerializedPool.ActiveList();
                if (activeList.Count > 3)
                    m_nonSerializedPool.Release(activeList[0]);
            }
        }
        
        //======================================================================

#if UNITY_EDITOR
        [CustomEditor(typeof(ExamplePoolsManager))]
        public class ExamplePoolsManagerEditor : UnityEditor.Editor
        {
            private ExamplePoolsManager m_target;

            private void OnEnable()
            {
                m_target = target as ExamplePoolsManager;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (Application.isPlaying)
                {
                    m_target.m_poolsContainer.DrawOnEditor();
                    m_target.m_serializedPool.DrawOnEditor();
                    m_target.m_nonSerializedPool.DrawOnEditor();
                }
                else
                    EditorGUILayout.HelpBox("Click play to see how it work", MessageType.Info);
            }
        }
#endif
    }
}