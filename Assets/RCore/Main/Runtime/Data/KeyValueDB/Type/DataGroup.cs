/***
 * Author RaBear - HNB - 2018
 **/

using System;
using System.Collections;
using System.Collections.Generic;

namespace RCore.Data.KeyValue
{
    public class DataGroup : FunData
    {
        #region Members

        private List<FunData> m_children;
        private bool m_staging;
        private bool m_changed;

        public int ChildCount => m_children.Count;
        public override List<FunData> Children => m_children;

        #endregion

        //=====================================

        #region Public

        public DataGroup(int pId) : base(pId)
        {
            m_children = new List<FunData>();
        }

        public T AddDataManually<T>(T pData) where T : FunData
        {
            m_changed = true;
            return AddData(pData);
        }

        public T AddData<T>(T pData) where T : FunData
        {
            int id = pData.Id;

            if (!CheckExistedID(id))
                m_children.Add(pData);
            return pData;
        }

        public T GetData<T>(int pId) where T : FunData
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Id == pId)
                    return m_children[i] as T;
            }
            return default;
        }

        public T GetDataByIndex<T>(int pIndex) where T : FunData
        {
            return m_children[pIndex] as T;
        }

        public T GetRandomData<T>() where T : FunData
        {
            return m_children[UnityEngine.Random.Range(0, m_children.Count)] as T;
        }

        public void RemoveData(int pId)
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Id == pId)
                {
                    m_children.RemoveAt(i);
                    m_changed = true;
                    break;
                }
            }
        }

        //------------------------------------------
        // Override
        //------------------------------------------

        /// <summary>
        /// Build Key used in Data Saver
        /// </summary>
        /// <param name="pBaseKey">Key of Data Group (Its parent)</param>
        /// <param name="pSaverIdString"></param>
        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);

            for (int i = 0; i < m_children.Count; i++)
                m_children[i].Load(m_Key, pSaverIdString);
        }

        public override void PostLoad()
        {
            for (int i = 0; i < m_children.Count; i++)
                m_children[i].PostLoad();
        }

        public override void OnApplicationPaused(bool pPaused)
        {
            for (int i = 0; i < m_children.Count; i++)
                m_children[i].OnApplicationPaused(pPaused);
        }

        public override void OnApplicationQuit()
        {
            for (int i = 0; i < m_children.Count; i++)
                m_children[i].OnApplicationQuit();
        }

        //------------------------------------------
        // Data Saver References
        //------------------------------------------

        /// <summary>
        /// Return all data in list of Data Saver in json
        /// </summary>
        public string GetJsonData()
        {
            return KeyValueCollection.GetCurrentData();
        }

        /// <summary>
        /// Return all saved data of Data Saver in string
        /// </summary>
        public string GetSavedData()
        {
            return KeyValueCollection.GetSavedData();
        }

        //------------------------------------------
        // Save / Load Data
        //------------------------------------------

        /// <summary>
        /// Reload data back to last saved
        /// </summary>
        /// <param name="pClearIndex">Clear cached index of data in data list</param>
        public override void Reload()
        {
            base.Reload();

            for (int i = 0; i < m_children.Count; i++)
                m_children[i].Reload();
        }

        public override void Reset()
        {
            for (int i = 0; i < m_children.Count; i++)
                m_children[i].Reset();
        }

        public override bool Stage()
        {
            bool changed = false;
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Stage())
                    changed = true;
            }
            bool temp = m_changed;
            m_changed = false;
            return changed || temp;
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// Because call in sub group can cause multi unnecessary save
        /// </summary>
        public void Save(bool pForce = false)
        {
            if (Stage())
                KeyValueCollection.Save(false);
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// </summary>
        public void SaveAsync(Action pOnCompleted)
        {
            StageAsync(() =>
            {
                KeyValueCollection.Save(false);

                pOnCompleted?.Invoke();
            });
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// </summary>
        public void StageAsync(Action pOnCompleted)
        {
            if (m_staging)
                return;

            var listData = GetDescendant();
            TimerEventsGlobal.Instance.StartCoroutine(IEStageAsync(listData, pOnCompleted));
        }

        //------------------------------------------
        // Clean Data
        //------------------------------------------

        /// <summary>
        /// All data which is equal to default it's value should not be pushed in list
        /// </summary>
        public override bool Cleanable()
        {
            bool cleanable = false;
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Cleanable())
                {
                    cleanable = true;
                    break;
                }
            }
            return cleanable;
        }

        /// <summary>
        /// Find all children which changed back to default value
        /// In order to remove it from saver list keys and values, to minimize saved data
        /// </summary>
        /// <returns>List of indexes in list Keys Values</returns>
        private List<int> GetCleanableIndexes()
        {
            var cleanableKeys = new List<int>();
            foreach (var t in m_children)
            {
                if (t.Cleanable())
                    cleanableKeys.Add(t.Index);
            }
            return cleanableKeys;
        }

        /// <summary>
        /// Must call after cleaning index of any child in data saver list keys and values
        /// Because all list keys and values have to be reorder after cleaning
        /// </summary>
        public override void ClearIndex()
        {
            foreach (var t in m_children)
                t.ClearIndex();
        }

        /// <summary>
        /// Call it in case we want to reduce as many as possible amount of list keys and values
        /// </summary>
        public void CleanData()
        {
            var cleanableIndexes = GetCleanableIndexes();
            if (cleanableIndexes.Count > 0)
            {
                KeyValueCollection.RemoveKeys(cleanableIndexes);
                ClearIndex();
            }
        }

        #endregion

        //=====================================

        #region Private

        private IEnumerator IEStageAsync(List<FunData> pListData, Action pOnCompleted)
        {
            m_staging = true;

            for (int i = 0; i < pListData.Count; i++)
            {
                pListData[i].Stage();
                yield return null;
            }

            pOnCompleted?.Invoke();

            m_staging = false;
        }

        private bool CheckExistedID(int pId)
        {
            for (int i = 0; i < m_children.Count; i++)
            {
                if (m_children[i].Id == pId)
                {
                    Debug.LogError($"ID {pId} is aldready exited!");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get all children and grant children of this group
        /// </summary>
        private List<FunData> GetDescendant()
        {
            return GetDescendant(this);
        }

        /// <summary>
        /// Get all children and grant children of root
        /// </summary>
        private List<FunData> GetDescendant(FunData pRoot)
        {
            var list = new List<FunData>();

            //If it is not data group, add this to list
            if (pRoot.Children == null || pRoot.Children.Count == 0)
            {
                list.Add(pRoot);
                return list;
            }

            for (int i = 0; i < m_children.Count; i++)
                list.AddRange(GetDescendant(m_children[i]));

            return list;
        }

        #endregion
    }
}