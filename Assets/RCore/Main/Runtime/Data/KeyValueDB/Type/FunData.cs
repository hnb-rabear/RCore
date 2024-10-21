/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Collections.Generic;

namespace RCore.Data.KeyValue
{
	public abstract class FunData
	{
		/// <summary>
		/// Unique of id of data in it's group
		/// </summary>
		protected int m_Id;
		/// <summary>
		/// Key of data, used to load data from data list
		/// </summary>
		protected string m_Key;
		/// <summary>
		/// Cached index of saved data, used to quick get/set
		/// </summary>
		protected int m_Index = -1;
		/// <summary>
		/// Id String of data saver
		/// </summary>
		protected string m_SaverIdString;
		public string Key => m_Key;
		public int Id => m_Id;
		public int Index => m_Index;
		public KeyValueCollection KeyValueCollection => KeyValueDB.GetCollection(m_SaverIdString);
		public virtual List<FunData> Children => null;
		public FunData(int pId)
		{
			m_Id = pId;
		}
		/// <summary>
		/// Build Key used in Data Saver
		/// </summary>
		/// <param name="pBaseKey">Key of Data Group (Its parent)</param>
		/// <param name="pSaverIdString"></param>
		public virtual void Load(string pBaseKey, string pSaverIdString)
		{
			Debug.Assert(pSaverIdString != null, "Data saver cannot be null!");

			if (!string.IsNullOrEmpty(pBaseKey))
				m_Key = $"{pBaseKey}.{m_Id}";
			else
				m_Key = m_Id.ToString();
			m_SaverIdString = pSaverIdString;
		}
		public virtual void PostLoad() { }
		public void SetStringValue(string pValue)
		{
			try
			{
				if (m_Index == -1)
					m_Index = KeyValueCollection.Set(m_Key, pValue);
				else
					KeyValueCollection.Set(m_Index, pValue);
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}
		public string GetStringValue()
		{
			if (m_Index == -1)
				return KeyValueCollection.Get(m_Key, out m_Index);
			return KeyValueCollection.Get(m_Index);
		}
		public virtual void ClearIndex()
		{
			m_Index = -1;
		}
		public virtual void OnApplicationPaused(bool pPaused) { }
		public virtual void OnApplicationQuit() { }
		/// <summary>
		/// Reload data back to last saved
		/// </summary>
		/// <param name="pClearIndex">Clear cached index of data in data list</param>
		public virtual void Reload()
		{
			m_Index = -1;
		}
		public abstract void Reset();
		public abstract bool Stage();
		public abstract bool Cleanable();
	}
}