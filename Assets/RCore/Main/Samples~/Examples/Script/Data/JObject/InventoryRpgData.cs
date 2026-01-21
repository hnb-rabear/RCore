using System;
using System.Collections.Generic;

namespace RCore.Example.Data.JObject
{
	[Serializable]
	public class InvRPGItemData : InvItemData
	{
		public int quantity;
		public int level;
		public int rarity; 
	}

	[Serializable]
	public class InventoryRpgData<T> : InventoryData<InvRPGItemData>
	{
		public void SortByRarity(bool des = false)
		{
			for (int i = 0; i < items.Count - 1; ++i)
			{
				for (int j = i + 1; j < items.Count; ++j)
				{
					if (items[i].rarity > items[j].rarity && !des
					    || items[i].rarity < items[j].rarity && des)
					{
						(items[i], items[j]) = (items[j], items[i]);
					}
				}
			}
		}

		public void SortByLevel(bool des = false)
		{
			for (int i = 0; i < items.Count - 1; ++i)
			{
				for (int j = i + 1; j < items.Count; ++j)
				{
					if (items[i].level > items[j].level && !des
					    || items[i].level < items[j].level && des)
					{
						(items[i], items[j]) = (items[j], items[i]);
					}
				}
			}
		}

		public void SortByQuantity(bool des = false)
		{
			for (int i = 0; i < items.Count - 1; ++i)
			{
				for (int j = i + 1; j < items.Count; ++j)
				{
					if (items[i].quantity > items[j].quantity && !des
					    || items[i].quantity < items[j].quantity && des)
					{
						(items[i], items[j]) = (items[j], items[i]);
					}
				}
			}
		}
		
		public List<int> GetItemsByRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].rarity == pRarity)
					list.Add(items[i].id);
			}
			return list;
		}

		public List<int> GetItemsByMinRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].rarity >= pRarity)
					list.Add(items[i].id);
			}
			return list;
		}
	}
}