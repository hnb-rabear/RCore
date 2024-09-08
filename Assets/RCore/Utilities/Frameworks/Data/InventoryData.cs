using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Framework.Data
{
	public interface IInventoryItem : IComparable<IInventoryItem>
	{
		int Id { get; set; }
		int BaseId { get; set; }
		int Rarity { get; set; }
		int Level { get; set; }
		int Quantity { get; set; }
	}

	public enum InvSort
	{
		Level,
		Rarity,
		Slot
	}

	[Serializable]
	public abstract class InventoryItem : IInventoryItem
	{
		[JsonProperty] [SerializeField] protected int b;
		[JsonProperty] [SerializeField] protected int id;
		[JsonProperty] [SerializeField] protected int r;
		[JsonProperty] [SerializeField] protected int l;
		[JsonProperty] [SerializeField] protected int q;

		[JsonIgnore] public int Id { get => id;
			set => id = value;
		}
		[JsonIgnore] public int BaseId { get => b;
			set => b = value;
		}
		[JsonIgnore] public int Rarity { get => r;
			set => r = value;
		}
		[JsonIgnore] public int Level { get => l;
			set => l = value;
		}
		[JsonIgnore] public int Quantity { get => q;
			set => q = value;
		}

		/// <summary>
		/// Example:
		/// Slot: Armor (Light, Medium, Heavy)
		/// Slot: Weapon (Handgun, Assault Rifle, Shotgun, ...)
		/// </summa>
		public abstract int ItemType();
		public abstract string SkillId();
		public abstract List<Mod> GetMods(int pLevel = 0, int pRarity = 0);
		public abstract int EquipSlot();
		public abstract bool IsEquipped();
		public abstract bool RankUp();
		public bool CanRankUp() => CanRankUp(out string reason);
		public abstract bool CanRankUp(out string pReason);
		public abstract bool LevelUp();
		public bool CanLevelUp() => CanLevelUp(out string reason);
		public abstract bool CanLevelUp(out string pReason);
		public int CompareTo(IInventoryItem pOther)
		{
			if (Id == pOther.Id)
			{
				if (Rarity == pOther.Rarity)
					return Level.CompareTo(pOther.Level);
				else
					return Rarity.CompareTo(pOther.Rarity);
			}
			else
				return Id.CompareTo(pOther.Id);
		}
		public abstract int MaxLevel();
		public bool Maxed() => Level >= MaxLevel();
	}

	public class InventoryData<T> : DataGroup where T : InventoryItem
	{
		protected ListData<T> m_Items;
		protected IntegerData m_LastItemId;
		protected ListData<int> m_DeletedIds;
		protected ListData<int> m_BuzzIds;

		public int Count => m_Items.Count;
		public T this[int index]
		{
			get => m_Items[index];
			set => m_Items[index] = value;
		}

		public InventoryData(int pId) : base(pId)
		{
			m_Items = AddData(new ListData<T>(0));
			m_LastItemId = AddData(new IntegerData(1));
			m_DeletedIds = AddData(new ListData<int>(2));
			m_BuzzIds = AddData(new ListData<int>(3));
		}

		public bool Insert(T pInvItem)
		{
			if (pInvItem.Id > 0)
			{
				for (int i = 0; i < m_Items.Count; i++)
					if (m_Items[i].Id == pInvItem.Id)
					{
						Debug.LogError("Id of inventory item must be unique!");
						return false;
					}
			}
			else
			{
				int newId = m_LastItemId.Value += 1;
				if (m_DeletedIds.Count > 0)
				{
					newId = m_DeletedIds[m_DeletedIds.Count - 1];
					m_DeletedIds.RemoveAt(m_DeletedIds.Count - 1);
				}

				pInvItem.Id = newId;
				m_BuzzIds.Add(newId);
			}

			m_Items.Add(pInvItem);

			if (pInvItem.Id > m_LastItemId.Value)
				m_LastItemId.Value = pInvItem.Id;

			return true;
		}

		public bool Insert(List<T> pInvItems)
		{
			return Insert(pInvItems.ToArray());
		}

		public bool Insert(params T[] pInvItems)
		{
			for (int j = 0; j < pInvItems.Length; j++)
			{
				if (pInvItems[j].Id > 0)
				{
					for (int i = 0; i < m_Items.Count; i++)
						if (m_Items[i].Id == pInvItems[j].Id)
						{
							Debug.LogError("Id of inventory item must be unique!");
							return false;
						}
				}
				else
				{
					int newId = m_LastItemId.Value += 1;
					if (m_DeletedIds.Count > 0)
					{
						newId = m_DeletedIds[m_DeletedIds.Count - 1];
						m_DeletedIds.RemoveAt(m_DeletedIds.Count - 1);
					}
					pInvItems[j].Id = newId;
					m_BuzzIds.Add(newId);
				}
			}

			for (int j = 0; j < pInvItems.Length; j++)
			{
				m_Items.Add(pInvItems[j]);
			}
			return true;
		}

		public bool Update(T pInvItem)
		{
			for (int i = 0; i < m_Items.Count; i++)
				if (m_Items[i].Id == pInvItem.Id)
				{
					m_Items[i] = pInvItem;
					return true;
				}
			Debug.LogError("Could not update item, because Id is not found!");
			return false;
		}

		public bool Delete(T pInvItem)
		{
			for (int i = 0; i < m_Items.Count; i++)
				if (m_Items[i].Id == pInvItem.Id)
				{
					m_DeletedIds.Add(m_Items[i].Id);
					m_Items.Remove(m_Items[i]);
					return true;
				}
			Debug.LogError("Could not delete item, because Id is not found!");
			return false;
		}

		public bool Delete(int id)
		{
			for (int i = 0; i < m_Items.Count; i++)
				if (m_Items[i].Id == id)
				{
					m_DeletedIds.Add(m_Items[i].Id);
					m_Items.Remove(m_Items[i]);
					return true;
				}
			Debug.LogError("Could not delete item, because Id is not found!");
			return false;
		}

		public T GetItemByIndex(int pIndex)
		{
			return m_Items[pIndex];
		}

		public T GetItemById(int pId)
		{
			if (pId > 0)
			{
				for (int i = 0; i < m_Items.Count; i++)
					if (m_Items[i].Id == pId)
						return m_Items[i];
			}
			return default;
		}

		public void RemoveBuzzId(int pId)
		{
			m_BuzzIds.Remove(pId);
		}

		public List<T> GetBuzzedItems()
		{
			var list = new List<T>();
			for (int i = 0; i < m_Items.Count; i++)
			{
				if (m_BuzzIds.Contain(m_Items[i].Id))
					list.Add(m_Items[i]);
			}
			return list;
		}

		public void SortyById(bool des = false)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					if (m_Items[i].Id > m_Items[j].Id && !des
						|| m_Items[i].Id < m_Items[j].Id && des)
					{
						var temp = m_Items[i];
						m_Items[i] = m_Items[j];
						m_Items[j] = temp;
					}
				}
			}
		}

		public void SortyByRarity(bool des = false)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					if (m_Items[i].Rarity > m_Items[j].Rarity && !des
						|| m_Items[i].Rarity < m_Items[j].Rarity && des)
					{
						var temp = m_Items[i];
						m_Items[i] = m_Items[j];
						m_Items[j] = temp;
					}
				}
			}
		}

		public void SortByLevel(bool des = false)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					if (m_Items[i].Level > m_Items[j].Level && !des
						|| m_Items[i].Level < m_Items[j].Level && des)
					{
						var temp = m_Items[i];
						m_Items[i] = m_Items[j];
						m_Items[j] = temp;
					}
				}
			}
		}

		public void SortByQuantity(bool des = false)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					if (m_Items[i].Quantity > m_Items[j].Quantity && !des
						|| m_Items[i].Quantity < m_Items[j].Quantity && des)
					{
						var temp = m_Items[i];
						m_Items[i] = m_Items[j];
						m_Items[j] = temp;
					}
				}
			}
		}

		public void SortByBaseId(bool des = false)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					if (m_Items[i].BaseId > m_Items[j].BaseId && !des
						|| m_Items[i].BaseId < m_Items[j].BaseId && des)
					{
						var temp = m_Items[i];
						m_Items[i] = m_Items[j];
						m_Items[j] = temp;
					}
				}
			}
		}

		public void InternalChange()
		{
			m_Items.MarkChange();
		}

		public bool Exist(int pBaseId, out int pId)
		{
			pId = 0;
			for (int i = 0; i < m_Items.Count; i++)
				if (m_Items[i].BaseId == pBaseId)
				{
					pId = m_Items[i].Id;
					return true;
				}
			return false;
		}

		public List<int> GetItemsByRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < m_Items.Count; i++)
			{
				if (m_Items[i].Rarity == pRarity)
					list.Add(m_Items[i].Id);
			}
			return list;
		}

		public List<int> GetItemsByMinRarity(int pRarity)
		{
			var list = new List<int>();
			for (int i = 0; i < m_Items.Count; i++)
			{
				if (m_Items[i].Rarity >= pRarity)
					list.Add(m_Items[i].Id);
			}
			return list;
		}

		public void Sort(InvSort pSort)
		{
			for (int i = 0; i < m_Items.Count - 1; ++i)
			{
				var iSlot = m_Items[i].EquipSlot();
				int iLevel = m_Items[i].Level; //Levels sort descending
				int iRarity = m_Items[i].Rarity; //Rarities sort descending
				bool iEquipped = m_Items[i].IsEquipped();
				for (int j = i + 1; j < m_Items.Count; ++j)
				{
					var jSlot = m_Items[j].EquipSlot();
					int jLevel = m_Items[j].Level;
					int jRarity = m_Items[j].Rarity;
					bool jEquipped = m_Items[j].IsEquipped();
					switch (pSort)
					{
						case InvSort.Level:
							if (!iEquipped && jEquipped
								|| iEquipped == jEquipped && iLevel < jLevel
								|| iEquipped == jEquipped && iLevel == jLevel && iRarity < jRarity
								|| iEquipped == jEquipped && iLevel == jLevel && iRarity == jRarity && iSlot > jSlot)
							{
								var temp = m_Items[i];
								m_Items[i] = m_Items[j];
								m_Items[j] = temp;
							}
							break;

						case InvSort.Rarity:
							if (!iEquipped && jEquipped
								|| iEquipped == jEquipped && iRarity < jRarity
								|| iEquipped == jEquipped && iRarity == jRarity && iLevel < jLevel
								|| iEquipped == jEquipped && iRarity == jRarity && iLevel == jLevel && iSlot > jSlot)
							{
								var temp = m_Items[i];
								m_Items[i] = m_Items[j];
								m_Items[j] = temp;
							}
							break;

						case InvSort.Slot:
							if (!iEquipped && jEquipped
								|| iEquipped == jEquipped && iSlot > jSlot
								|| iEquipped == jEquipped && iSlot == jSlot && iRarity < jRarity
								|| iEquipped == jEquipped && iSlot == jSlot && iRarity == jRarity && iLevel < jLevel)
							{
								var temp = m_Items[i];
								m_Items[i] = m_Items[j];
								m_Items[j] = temp;
							}
							break;
					}
				}
			}
		}
	}
}