/***
 * Author RadBear - nbhung71711@gmail.com - 2020
 **/

using System;
using System.Collections.Generic;

namespace RCore.RPGBase
{
	[Serializable]
	public class Mod : IComparable<Mod>
	{
		public int id;
		public float[] values;
		public float value
		{
			get => values[0];
			set => values[0] = value;
		}
		public Mod(Mod pOther)
		{
			id = pOther.id;
			values = pOther.values;
			value = pOther.value;
		}
		public Mod(int pId, float pValue)
		{
			id = pId;
			values = new float[1] { pValue };
		}
		public Mod(int pId, float[] pValues)
		{
			id = pId;
			values = pValues;
		}
		public virtual void Update(float pDeltaTime)
		{
		}
		public virtual bool IsActive() => true;
		public int CompareTo(Mod other)
		{
			return id.CompareTo(other.id);
		}
	}

	/// <summary>
	/// A timed mod like a buff in RPG game, which change character stats in a specific duration
	/// </summary>
	[Serializable]
	public class TimedMod : Mod
	{
		public int sourceId;
		public float duration;
		public float cooldown;
		public bool added;
		private float m_tempDuration;
		private float m_tempCooldown;
		public TimedMod(int pSourceId, int pId, float pValue, float pDuration, float pCooldown = 0) : base(pId, pValue)
		{
			sourceId = pSourceId;
			duration = pDuration;
			cooldown = pCooldown;
			m_tempDuration = duration;
			m_tempCooldown = cooldown;
		}
		public TimedMod(int pSourceId, int pId, float[] pValues, float pDuration, float pCooldown = 0) : base(pId, pValues)
		{
			sourceId = pSourceId;
			duration = pDuration;
			cooldown = pCooldown;
			m_tempDuration = duration;
			m_tempCooldown = cooldown;
		}
		public TimedMod(int pId, float[] pValues, float pDuration, float pCooldown = 0) : base(pId, pValues)
		{
			duration = pDuration;
			cooldown = pCooldown;
			m_tempDuration = duration;
			m_tempCooldown = cooldown;
		}
		public override bool IsActive()
		{
			return m_tempDuration >= 0;
		}
		public void Reset()
		{
			m_tempDuration = duration;
			m_tempCooldown = cooldown;
		}
		public override void Update(float pDeltaTime)
		{
			if (m_tempDuration > 0)
			{
				m_tempDuration -= pDeltaTime;
				if (m_tempDuration <= 0)
					m_tempCooldown = cooldown;
			}
			else if (m_tempCooldown > 0)
			{
				m_tempCooldown -= pDeltaTime;
				if (m_tempCooldown <= 0)
					m_tempDuration = duration;
			}
		}
	}

	/// <summary>
	/// A linked mod like a buff in RPG game, which change character stats when character wearing an item
	/// </summary>
	[Serializable]
	public class LinkedMod : Mod
	{
		public int sourceId;
		public LinkedMod(int pSourceId, int pId, float pValue) : base(pId, pValue)
		{
			sourceId = pSourceId;
		}
		public LinkedMod(int pSourceId, int pId, float[] pValues) : base(pId, pValues)
		{
			sourceId = pSourceId;
		}
	}

	/// <summary>
	/// A container contain all the mods of a character or a item
	/// </summary>
	[Serializable]
	public class ModsContainer
	{
		public Action<Mod> onModChanged;
		public Action<List<Mod>> onModsChanged;

		public Dictionary<int, float[]> mods = new Dictionary<int, float[]>();
		public List<TimedMod> timedMods = new List<TimedMod>();
		public List<LinkedMod> fixedMods = new List<LinkedMod>();

		//---- ADD
		protected virtual void AddModAndValues(int id, params float[] values)
		{
			if (mods.ContainsKey(id))
			{
				for (int i = 0; i < values.Length; i++)
					mods[id][i] += values[i];
			}
			else
				mods.Add(id, values);
		}
		public void AddMods(Dictionary<int, float[]> pMods)
		{
			var mods = new List<Mod>();
			foreach (var mod in pMods)
			{
				AddMod(mod.Key, mod.Value, false);
				mods.Add(new Mod(mod.Key, mod.Value));
			}
			onModsChanged?.Invoke(mods);
		}
		public void AddMods(List<Mod> pMods)
		{
			for (int i = 0; i < pMods.Count; i++)
			{
				AddMod(pMods[i].id, pMods[i].values, false);
			}
			onModsChanged?.Invoke(pMods);
		}
		public void AddMod(int pModId, params float[] pValues)
		{
			AddMod(pModId, pValues, true);
		}
		protected virtual void AddMod(int pModId, float[] pValues, bool pSendEvent)
		{
			AddModAndValues(pModId, pValues);
			if (pSendEvent)
				onModChanged?.Invoke(new Mod(pModId, pValues));
		}
		//---- REMOVE
		protected virtual void RemoveModAndValues(int id, params float[] values)
		{
			if (mods.ContainsKey(id))
			{
				for (int i = 0; i < mods[id].Length; i++)
					mods[id][i] -= values[i];
			}
		}
		public void RemoveMods(List<Mod> pMods)
		{
			for (int i = 0; i < pMods.Count; i++)
			{
				int id = pMods[i].id;
				var values = pMods[i].values;
				RemoveMod(id, values, false);
			}
			onModsChanged?.Invoke(pMods);
		}
		public void RemoveMod(int pModId, params float[] pValues)
		{
			RemoveMod(pModId, pValues, true);
		}
		protected void RemoveMod(int pId, float[] pValues, bool pSendEvent)
		{
			RemoveModAndValues(pId, pValues);
			if (pSendEvent)
				onModChanged?.Invoke(new Mod(pId, pValues));
		}
		//---- CIRCLE MOD
		public void AddTimedMod(int pSourceId, int pModId, float pValue, float pDuration, float pCooldown = 0)
		{
			AddTimedMod(pSourceId, pModId, new float[1] { pValue }, pDuration, pCooldown);
		}
		public void AddTimedMod(int pSourceId, int pModId, float[] pValues, float pDuration, float pCooldown = 0)
		{
			for (int i = 0; i < timedMods.Count; i++)
			{
				if (timedMods[i].sourceId == pSourceId && timedMods[i].id == pModId)
				{
					if (timedMods[i].added)
					{
						RemoveMod(timedMods[i].id, timedMods[i].values);
						timedMods[i].added = false;
					}
					timedMods[i].cooldown = pCooldown;
					timedMods[i].duration = pDuration;
					timedMods[i].values = pValues;
					timedMods[i].Reset();
					return;
				}
			}
			timedMods.Add(new TimedMod(pSourceId, pModId, pValues, pDuration, pCooldown));
			Update(0);
		}
		public bool RemoveTimedMod(int pSourceId, int pModId)
		{
			for (int i = 0; i < timedMods.Count; i++)
			{
				if (timedMods[i].sourceId == pSourceId && timedMods[i].id == pModId)
				{
					if (timedMods[i].added)
					{
						RemoveMod(timedMods[i].id, timedMods[i].values);
						timedMods[i].added = false;
					}
					timedMods.RemoveAt(i);
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Must called in update to Circle Mods Work
		/// </summary>
		/// <param name="pDeltaTime"></param>
		public void Update(float pDeltaTime)
		{
			for (int i = timedMods.Count - 1; i >= 0; i--)
			{
				timedMods[i].Update(pDeltaTime);

				if (timedMods[i].IsActive())
				{
					if (!timedMods[i].added)
					{
						AddMod(timedMods[i].id, timedMods[i].values);
						timedMods[i].added = true;
					}
				}
				else if (timedMods[i].added)
				{
					RemoveMod(timedMods[i].id, timedMods[i].values);
					timedMods[i].added = false;

					if (timedMods[i].cooldown == 0) //It mean this timer mod is once time mod
						timedMods.RemoveAt(i);
				}
			}
		}
		//---- LINKED MOD
		public void AddLinkedMod(int pSourceId, int pModId, float pValue)
		{
			AddLinkedMod(pSourceId, pModId, new float[1] { pValue });
		}
		public void AddLinkedMod(int pSourceId, int pModId, float[] pValues)
		{
			for (int i = 0; i < fixedMods.Count; i++)
			{
				if (fixedMods[i].sourceId == pSourceId && fixedMods[i].id == pModId)
				{
					RemoveMod(fixedMods[i].id, fixedMods[i].values);
					AddMod(pModId, pValues);
					return;
				}
			}
			AddMod(pModId, pValues);
			fixedMods.Add(new LinkedMod(pSourceId, pModId, pValues));
		}
		public bool RemoveLinkedMod(int pSourceId, int pModId)
		{
			for (int i = 0; i < fixedMods.Count; i++)
			{
				if (fixedMods[i].sourceId == pSourceId && fixedMods[i].id == pModId)
				{
					RemoveMod(fixedMods[i].id, fixedMods[i].values);
					fixedMods.RemoveAt(i);
					return true;
				}
			}
			return false;
		}
	}
}