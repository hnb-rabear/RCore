/***
 * Author RadBear - nbhung71711 @gmail.com - 2020
 **/

using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;

namespace RCore.Common
{
    public class TimeCounterJobSystem : IDisposable
    {
        public delegate void DelegateTimeCounterUpdated(int id, float timeTarget, bool loop, float elapsedTime);
        public event DelegateTimeCounterUpdated onTimeCounterUpdated;

        public delegate void DelegateTimeCounterFinished(int id, float timeTarget, bool loop);
        public event DelegateTimeCounterUpdated onTimeCounterFinished;

        public struct InputData
        {
            public int id; //All ids must greater than 0
            public float timeTarget;
            public bool loop;
        }

        public struct OutputData
        {
            public int id; //All ids must greater than 0
            public float elapsedTime;
            public bool finished;
        }

        [BurstCompile]
        public struct Job : IJobParallelFor
        {
            [ReadOnly] public NativeList<InputData> inputData;
            [ReadOnly] public float deltaTime;

            public NativeArray<OutputData> outputData;

            public void Execute(int index)
            {
                var input = inputData[index];
                var output = outputData[index];
                output.elapsedTime += deltaTime;
                if (output.elapsedTime > input.timeTarget)
                {
                    output.elapsedTime = input.timeTarget;
                    output.finished = true;
                }
                else
                {
                    output.finished = false;
                }
                outputData[index] = output;
            }
        }

        public static TimeCounterJobSystem Instance => m_Instance == null ? m_Instance = new TimeCounterJobSystem() : m_Instance;
        private static TimeCounterJobSystem m_Instance;

        private NativeList<InputData> m_InputData;
        private NativeList<OutputData> m_OutputData;
        private JobHandle m_JobHandle;
        private int m_AutoIncrementId;
        private List<int> m_RemovedIds;
        /// <summary>
        /// Key is next number
        /// </summary>
        private Dictionary<int, DelegateTimeCounterFinished> m_OnFinishedCallbacks;

        public TimeCounterJobSystem()
        {
            m_Instance = this;
            m_InputData = new NativeList<InputData>(1, Allocator.Persistent);
            m_OutputData = new NativeList<OutputData>(1, Allocator.Persistent);
            m_OnFinishedCallbacks = new Dictionary<int, DelegateTimeCounterFinished>();
            m_AutoIncrementId = 0;
            m_RemovedIds = new List<int>();
        }

        public void Dispose()
        {
            m_InputData.Dispose();
            m_OutputData.Dispose();
            m_OnFinishedCallbacks.Clear();
            m_AutoIncrementId = 0;
            m_RemovedIds.Clear();
        }

        public void Update(float pDeltaTime)
        {
            int length = m_InputData.Length;
            if (length == 0)
                return;

            var job = new Job()
            {
                inputData = m_InputData,
                outputData = m_OutputData.AsDeferredJobArray(),
                deltaTime = pDeltaTime,
            };

            m_JobHandle = job.Schedule(length, 100);

            JobHandle.ScheduleBatchedJobs();
        }

        public void LateUpdate()
        {
            m_JobHandle.Complete();

            for (int i = m_OutputData.Length - 1; i >= 0; i--)
            {
                bool loop = m_InputData[i].loop;
                float timeTarget = m_InputData[i].timeTarget;
                int id = m_OutputData[i].id;
                if (m_OutputData[i].finished)
                {
                    if (!loop)
                    {
                        m_OutputData.RemoveAtSwapBack(i);
                        m_InputData.RemoveAtSwapBack(i);
                        if (m_OnFinishedCallbacks.ContainsKey(id))
                        {
                            m_OnFinishedCallbacks[id](id, timeTarget, loop);
                            m_OnFinishedCallbacks.Remove(id);
                        }
                        m_RemovedIds.Add(id);
                    }
                    else
                    {
                        m_OutputData[i] = new OutputData()
                        {
                            id = id,
                            elapsedTime = 0,
                            finished = false,
                        };
                        if (m_OnFinishedCallbacks.ContainsKey(id))
                            m_OnFinishedCallbacks[id](id, timeTarget, loop);
                    }
                }
            }
        }

        public void Register(int id, float timeTarget, bool loop, DelegateTimeCounterFinished onFinished = null)
        {
            for (int i = 0; i < m_InputData.Length; i++)
            {
                if (m_InputData[i].id == id)
                {
                    m_InputData[i] = new InputData()
                    {
                        id = id,
                        loop = loop,
                        timeTarget = timeTarget
                    };
                    return;
                }
            }
            m_InputData.Add(new InputData()
            {
                id = id,
                timeTarget = timeTarget,
                loop = loop,
            });
            m_OutputData.Add(new OutputData()
            {
                id = id,
                elapsedTime = 0,
                finished = false,
            });
            if (onFinished != null)
                m_OnFinishedCallbacks.Add(id, onFinished);
        }

        public void Register(float timeTarget, bool loop, DelegateTimeCounterFinished onFinished = null)
        {
            int newId = 0;
            if (m_RemovedIds.Count > 0)
            {
                newId = m_RemovedIds[m_RemovedIds.Count - 1];
                m_RemovedIds.RemoveAtSwapBack(m_RemovedIds.Count - 1);
            }
            else
                newId = ++m_AutoIncrementId;
            m_InputData.Add(new InputData()
            {
                id = newId,
                timeTarget = timeTarget,
                loop = loop,
            });
            m_OutputData.Add(new OutputData()
            {
                id = newId,
                elapsedTime = 0,
                finished = false,
            });
            if (onFinished != null)
                m_OnFinishedCallbacks.Add(newId, onFinished);
        }

        public void Stop(int id)
        {
            for (int i = m_InputData.Length - 1; i >= 0; i--)
            {
                if (m_InputData[i].id == id)
                {
                    m_InputData.RemoveAtSwapBack(i);
                    m_OutputData.RemoveAtSwapBack(i);
                    if (m_OnFinishedCallbacks.ContainsKey(id))
                        m_OnFinishedCallbacks.Remove(id);
                    break;
                }
            }
        }

        public void Finish(int id)
        {
            for (int i = 0; i < m_InputData.Length; i++)
            {
                if (m_InputData[i].id == id)
                {
                    m_OutputData[i] = new OutputData()
                    {
                        id = m_InputData[i].id,
                        elapsedTime = m_InputData[i].timeTarget,
                        finished = true,
                    };
                }
            }
        }

        public void FinishAll()
        {
            for (int i = 0; i < m_InputData.Length; i++)
            {
                m_OutputData[i] = new OutputData()
                {
                    id = m_InputData[i].id,
                    elapsedTime = m_InputData[i].timeTarget,
                    finished = true,
                };
            }
        }
    }
}