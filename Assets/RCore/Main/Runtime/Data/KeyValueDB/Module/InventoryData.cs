using System;
using System.Collections.Generic;

/***
 * This template for the Inventory Module boasts all the essential functions you need.
 */
namespace RCore.Data.KeyValue
{
    [Serializable]
    public class InvItemData
    {
        public int id; // Auto increment id
        public int fk; // Foreign key, Id of configured item
    }

    public class InventoryData<T> : DataGroup where T : InvItemData
    {
        protected ListData<T> m_items;
        protected IntegerData m_lastItemId;
        protected ListData<int> m_deletedIds;
        protected ListData<int> m_noticedIds;

        public int Count => m_items.Count;
        public T this[int index]
        {
            get => m_items[index];
            set => m_items[index] = value;
        }

        public InventoryData(int pId) : base(pId)
        {
            m_items = AddData(new ListData<T>(0));
            m_lastItemId = AddData(new IntegerData(1));
            m_deletedIds = AddData(new ListData<int>(2));
            m_noticedIds = AddData(new ListData<int>(3));
        }

        public virtual bool Insert(T pInvItem)
        {
            if (pInvItem.id > 0)
            {
                for (int i = 0; i < m_items.Count; i++)
                    if (m_items[i].id == pInvItem.id)
                    {
                        Debug.LogError("Id of inventory item must be unique!");
                        return false;
                    }
            }
            else
            {
                int newId = m_lastItemId.Value += 1;
                if (m_deletedIds.Count > 0)
                {
                    newId = m_deletedIds[m_deletedIds.Count - 1];
                    m_deletedIds.RemoveAt(m_deletedIds.Count - 1);
                }

                pInvItem.id = newId;
                m_noticedIds.Add(newId);
            }

            m_items.Add(pInvItem);

            if (pInvItem.id > m_lastItemId.Value)
                m_lastItemId.Value = pInvItem.id;

            return true;
        }

        public virtual bool Insert(List<T> pInvItems)
        {
            return Insert(pInvItems.ToArray());
        }

        public virtual bool Insert(params T[] pInvItems)
        {
            for (int j = 0; j < pInvItems.Length; j++)
            {
                if (pInvItems[j].id > 0)
                {
                    for (int i = 0; i < m_items.Count; i++)
                        if (m_items[i].id == pInvItems[j].id)
                        {
                            Debug.LogError("Id of inventory item must be unique!");
                            return false;
                        }
                }
                else
                {
                    int newId = m_lastItemId.Value += 1;
                    if (m_deletedIds.Count > 0)
                    {
                        newId = m_deletedIds[m_deletedIds.Count - 1];
                        m_deletedIds.RemoveAt(m_deletedIds.Count - 1);
                    }
                    pInvItems[j].id = newId;
                    m_noticedIds.Add(newId);
                }
            }

            for (int j = 0; j < pInvItems.Length; j++)
            {
                m_items.Add(pInvItems[j]);
            }
            return true;
        }

        public virtual bool Update(T pInvItem)
        {
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].id == pInvItem.id)
                {
                    m_items[i] = pInvItem;
                    return true;
                }
            Debug.LogError("Could not update item, because Id is not found!");
            return false;
        }

        public virtual bool Delete(T pInvItem)
        {
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].id == pInvItem.id)
                {
                    m_deletedIds.Add(m_items[i].id);
                    m_items.Remove(m_items[i]);
                    return true;
                }
            Debug.LogError("Could not delete item, because Id is not found!");
            return false;
        }

        public virtual bool Delete(int id)
        {
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].id == id)
                {
                    m_deletedIds.Add(m_items[i].id);
                    m_items.Remove(m_items[i]);
                    return true;
                }
            Debug.LogError("Could not delete item, because Id is not found!");
            return false;
        }

        public virtual T GetItemByIndex(int pIndex)
        {
            return m_items[pIndex];
        }

        public virtual T GetItemById(int pId)
        {
            if (pId > 0)
            {
                for (int i = 0; i < m_items.Count; i++)
                    if (m_items[i].id == pId)
                        return m_items[i];
            }
            return default;
        }

        public virtual void RemoveNoticedId(int pId)
        {
            m_noticedIds.Remove(pId);
        }

        public virtual List<T> GetNoticedItems()
        {
            var list = new List<T>();
            for (int i = 0; i < m_items.Count; i++)
            {
                if (m_noticedIds.Contain(m_items[i].id))
                    list.Add(m_items[i]);
            }
            return list;
        }

        public virtual void SortById(bool des = false)
        {
            for (int i = 0; i < m_items.Count - 1; ++i)
            {
                for (int j = i + 1; j < m_items.Count; ++j)
                {
                    if (m_items[i].id > m_items[j].id && !des
                        || m_items[i].id < m_items[j].id && des)
                    {
                        (m_items[i], m_items[j]) = (m_items[j], m_items[i]);
                    }
                }
            }
        }

        public virtual void SortByBaseId(bool des = false)
        {
            for (int i = 0; i < m_items.Count - 1; ++i)
            {
                for (int j = i + 1; j < m_items.Count; ++j)
                {
                    if (m_items[i].fk > m_items[j].fk && !des
                        || m_items[i].fk < m_items[j].fk && des)
                    {
                        (m_items[i], m_items[j]) = (m_items[j], m_items[i]);
                    }
                }
            }
        }

        public virtual bool Exist(int pBaseId, out int pId)
        {
            pId = 0;
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].fk == pBaseId)
                {
                    pId = m_items[i].id;
                    return true;
                }
            return false;
        }
    }
}