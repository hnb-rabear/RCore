using System;
using System.Collections.Generic;

namespace RCore.Framework.Data
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
	/// Basically, Cicle mods are buffs with lifetime
	/// </summary>
	[Serializable]
	public class CircleMod : Mod
	{
		public int sourceId;
		public float duration;
		public float cooldown;
		public bool added;
		private float m_TempDuration;
		private float m_TempCooldown;
		public CircleMod(int pSourceId, int pId, float pValue, float pDuration, float pCooldown = 0) : base(pId, pValue)
		{
			sourceId = pSourceId;
			duration = pDuration;
			cooldown = pCooldown;
			m_TempDuration = duration;
			m_TempCooldown = cooldown;
		}
		public CircleMod(int pSourceId, int pId, float[] pValues, float pDuration, float pCooldown = 0) : base(pId, pValues)
		{
			sourceId = pSourceId;
			duration = pDuration;
			cooldown = pCooldown;
			m_TempDuration = duration;
			m_TempCooldown = cooldown;
		}
		public CircleMod(int pId, float[] pValues, float pDuration, float pCooldown = 0) : base(pId, pValues)
		{
			duration = pDuration;
			cooldown = pCooldown;
			m_TempDuration = duration;
			m_TempCooldown = cooldown;
		}
		public override bool IsActive()
		{
			return m_TempDuration >= 0;
		}
		public void Reset()
		{
			m_TempDuration = duration;
			m_TempCooldown = cooldown;
		}
		public override void Update(float pDeltaTime)
		{
			if (m_TempDuration > 0)
			{
				m_TempDuration -= pDeltaTime;
				if (m_TempDuration <= 0)
					m_TempCooldown = cooldown;
			}
			else if (m_TempCooldown > 0)
			{
				m_TempCooldown -= pDeltaTime;
				if (m_TempCooldown <= 0)
					m_TempDuration = duration;
			}
		}
	}

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

	[Serializable]
	public class ModsContainer
	{
		public Action<Mod> onModChanged;
		public Action<List<Mod>> onModsChanged;

		public Dictionary<int, float[]> mods = new Dictionary<int, float[]>();
		public List<CircleMod> circleMods = new List<CircleMod>();
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
		public void AddCircleMod(int pSourceId, int pModId, float pValue, float pDuration, float pCooldown = 0)
		{
			AddCircleMod(pSourceId, pModId, new float[1] { pValue }, pDuration, pCooldown);
		}
		public void AddCircleMod(int pSourceId, int pModId, float[] pValues, float pDuration, float pCooldown = 0)
		{
			for (int i = 0; i < circleMods.Count; i++)
			{
				if (circleMods[i].sourceId == pSourceId && circleMods[i].id == pModId)
				{
					if (circleMods[i].added)
					{
						RemoveMod(circleMods[i].id, circleMods[i].values);
						circleMods[i].added = false;
					}
					circleMods[i].cooldown = pCooldown;
					circleMods[i].duration = pDuration;
					circleMods[i].values = pValues;
					circleMods[i].Reset();
					return;
				}
			}
			circleMods.Add(new CircleMod(pSourceId, pModId, pValues, pDuration, pCooldown));
			Update(0);
		}
		public bool RemoveCicleMod(int pSourceId, int pModId)
		{
			for (int i = 0; i < circleMods.Count; i++)
			{
				if (circleMods[i].sourceId == pSourceId && circleMods[i].id == pModId)
				{
					if (circleMods[i].added)
					{
						RemoveMod(circleMods[i].id, circleMods[i].values);
						circleMods[i].added = false;
					}
					circleMods.RemoveAt(i);
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
			for (int i = circleMods.Count - 1; i >= 0; i--)
			{
				circleMods[i].Update(pDeltaTime);

				if (circleMods[i].IsActive())
				{
					if (!circleMods[i].added)
					{
						AddMod(circleMods[i].id, circleMods[i].values);
						circleMods[i].added = true;
					}
				}
				else if (circleMods[i].added)
				{
					RemoveMod(circleMods[i].id, circleMods[i].values);
					circleMods[i].added = false;

					if (circleMods[i].cooldown == 0) //It mean this timer mod is once time mod
						circleMods.RemoveAt(i);
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