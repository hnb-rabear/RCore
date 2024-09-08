/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Framework.Data
{
    public class DataGroup : FunData
    {
        #region Members

        private List<FunData> mChildren;
        private bool mIsStagging;
        private bool mChanged;

        public int ChildCount => mChildren.Count;
        public override List<FunData> Children => mChildren;

        #endregion

        //=====================================

        #region Public

        public DataGroup(int pId) : base(pId, null)
        {
            mChildren = new List<FunData>();
        }

        public T AddDataManually<T>(T pData) where T : FunData
        {
            mChanged = true;
            return AddData(pData);
        }

        public T AddData<T>(T pData) where T : FunData
        {
            int id = pData.Id;

            if (!CheckExistedID(id))
                mChildren.Add(pData);
            return pData;
        }

        public T GetData<T>(int pId) where T : FunData
        {
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Id == pId)
                    return mChildren[i] as T;
            }
            return default;
        }

        public T GetDataByIndex<T>(int pIndex) where T : FunData
        {
            return mChildren[pIndex] as T;
        }

        public T GetRandomData<T>() where T : FunData
        {
            return mChildren[UnityEngine.Random.Range(0, mChildren.Count)] as T;
        }

        public void RemoveData(int pId)
        {
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Id == pId)
                {
                    mChildren.RemoveAt(i);
                    mChanged = true;
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

            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].Load(m_Key, pSaverIdString);
        }

        public override void PostLoad()
        {
            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].PostLoad();
        }

        public override void OnApplicationPaused(bool pPaused)
        {
            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].OnApplicationPaused(pPaused);
        }

        public override void OnApplicationQuit()
        {
            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].OnApplicationQuit();
        }

        //------------------------------------------
        // Data Saver References
        //------------------------------------------

        /// <summary>
        /// Return all data in list of Data Saver in json
        /// </summary>
        public string GetJsonData()
        {
            return DataSaver.GetCurrentData();
        }

        /// <summary>
        /// Return all saved data of Data Saver in string
        /// </summary>
        public string GetSavedData()
        {
            return DataSaver.GetSavedData();
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

            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].Reload();
        }

        public override void Reset()
        {
            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].Reset();
        }

        public override bool Stage()
        {
            bool changed = false;
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Stage())
                    changed = true;
            }
            bool temp = mChanged;
            mChanged = false;
            return changed || temp;
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// Because call in sub group can cause multi unnecessary save
        /// </summary>
        public void Save(bool pForce = false)
        {
            if (Stage())
                DataSaver.Save(false);
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// </summary>
        public void SaveAsync(Action pOnCompleted)
        {
            StageAsync(() =>
            {
                DataSaver.Save(false);

                pOnCompleted?.Invoke();
            });
        }

        /// <summary>
        /// TODO: It is good practical if we call this method from first generation of DataGroup
        /// </summary>
        public void StageAsync(Action pOnCompleted)
        {
            if (mIsStagging)
                return;

            var listData = GetDescendant();
            CoroutineUtil.StartCoroutine(IEStageAsync(listData, pOnCompleted));
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
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Cleanable())
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
            foreach (var t in mChildren)
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
            foreach (var t in mChildren)
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
                DataSaver.RemoveKeys(cleanableIndexes);
                ClearIndex();
            }
        }

        #endregion

        //=====================================

        #region Private

        private IEnumerator IEStageAsync(List<FunData> pListData, Action pOnCompleted)
        {
            mIsStagging = true;

            for (int i = 0; i < pListData.Count; i++)
            {
                pListData[i].Stage();
                yield return null;
            }

            pOnCompleted?.Invoke();

            mIsStagging = false;
        }

        private bool CheckExistedID(int pId)
        {
            for (int i = 0; i < mChildren.Count; i++)
            {
                if (mChildren[i].Id == pId)
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

            for (int i = 0; i < mChildren.Count; i++)
                list.AddRange(GetDescendant(mChildren[i]));

            return list;
        }

        #endregion
    }
}