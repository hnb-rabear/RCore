using System;
using System.Collections.Generic;

/***
 * This is an example demonstrating how to integrate the Inventory Data in a RPG game
 */
namespace RCore.Data.KeyValue
{
	[Serializable]
	public class InvRPGItemData : InvItemData
	{
		public int quantity;
		public int level;
		public int rarity;
	}
	
	public class InventoryRPGData : InventoryData<InvRPGItemData>
	{
		public InventoryRPGData(int pId) : base(pId) { }
		
		public void SortByRarity(bool des = false)
		{
			for (int i = 0; i < m_items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_items.Count; ++j)
				{
					if (m_items[i].rarity > m_items[j].rarity && !des
					    || m_items[i].rarity < m_items[j].rarity && des)
					{
						(m_items[i], m_items[j]) = (m_items[j], m_items[i]);
					}
				}
			}
		}

		public void SortByLevel(bool des = false)
		{
			for (int i = 0; i < m_items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_items.Count; ++j)
				{
					if (m_items[i].level > m_items[j].level && !des
					    || m_items[i].level < m_items[j].level && des)
					{
						(m_items[i], m_items[j]) = (m_items[j], m_items[i]);
					}
				}
			}
		}

		public void SortByQuantity(bool des = false)
		{
			for (int i = 0; i < m_items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_items.Count; ++j)
				{
					if (m_items[i].quantity > m_items[j].quantity && !des
					    || m_items[i].quantity < m_items[j].quantity && des)
					{
						(m_items[i], m_items[j]) = (m_items[j], m_items[i]);
					}
				}
			}
		}
		
		public List<int> GetItemsByRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < m_items.Count; i++)
			{
				if (m_items[i].rarity == pRarity)
					list.Add(m_items[i].id);
			}
			return list;
		}

		public List<int> GetItemsByMinRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < m_items.Count; i++)
			{
				if (m_items[i].rarity >= pRarity)
					list.Add(m_items[i].id);
			}
			return list;
		}
	}
}