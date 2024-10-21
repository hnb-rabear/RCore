/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.RPGBase
{
	[Serializable]
	public class AttributesCollection<T> where T : Attribute
	{
		public List<T> Attributes;

		public T GetAtt(int pId)
		{
			for (int i = 0; i < Attributes.Count; i++)
				if (Attributes[i].id == pId)
					return Attributes[i];
			return null;
		}

		public float GetAttValue(int pId, float pDefault = 0)
		{
			var att = GetAtt(pId);
			return att?.value ?? pDefault;
		}
		public float GetAttValue(int pId, int pLevel, float pDefault = 0)
		{
			var att = GetAtt(pId);
			return att?.GetValue(pLevel) ?? pDefault;
		}
		public float[] GetAttValues(int pId, int pLevel)
		{
			var att = GetAtt(pId);
			var values = att.GetValues(pLevel);
			return values;
		}
		public int GetAttIntValue(int pId, int pDefault = 0)
		{
			var att = GetAtt(pId);
			return att == null ? pDefault : Mathf.RoundToInt(att.value);
		}
		public int GetAttIntValue(int pId, int pLevel, int pDefault = 0)
		{
			var att = GetAtt(pId);
			return att == null ? pDefault : Mathf.RoundToInt(att.GetValue(pLevel));
		}
		public bool GetAttBoolValue(int pId, bool pDefault = false)
		{
			var att = GetAtt(pId);
			return att == null ? pDefault : att.value >= 1;
		}
		public bool GetAttBoolValue(int pId, int pLevel, bool pDefault = false)
		{
			var att = GetAtt(pId);
			return att == null ? pDefault : att.GetValue(pLevel) >= 1;
		}
		public string LogValues()
		{
			string text = "";
			for (int i = 0; i < Attributes.Count; i++)
			{
				text += $"Id:{Attributes[i].id}, Value:{Attributes[i].value}";
				if (i + 1 <= Attributes.Count - 1)
					text += "/";
			}
			return text;
		}
		public Dictionary<int, float> AssembleAtts(int pLevel)
		{
			var dict = new Dictionary<int, float>();
			foreach (var item in Attributes)
			{
				if (!dict.ContainsKey(item.id))
					dict.Add(item.id, item.GetValue(pLevel));
				else
					dict[item.id] += item.GetValue(pLevel);
			}
			return dict;
		}
		public virtual List<Mod> GetMods()
		{
			var mods = new List<Mod>();
			for (int i = 0; i < Attributes.Count; i++)
			{
				var att = Attributes[i];
				if (att.values != null && att.values.Length > 0)
					mods.Add(new Mod(att.id, att.values));
				else
					mods.Add(new Mod(att.id, att.value));
			}
			return mods;
		}
		public virtual List<Mod> GetMods(int pLevel)
		{
			var mods = new List<Mod>();
			for (int i = 0; i < Attributes.Count; i++)
			{
				var att = Attributes[i];
				var values = att.GetValues(pLevel);
				mods.Add(new Mod(att.id, values));
			}
			return mods;
		}
		public virtual Dictionary<int, float[]> GetModsDict(int pLevel = 1)
		{
			var mods = new Dictionary<int, float[]>();
			for (int i = 0; i < Attributes.Count; i++)
			{
				var id = Attributes[i].id;
				var values = Attributes[i].GetValues(pLevel);
				if (!mods.ContainsKey(id))
					mods.Add(id, values);
				else
				{
					for (int j = 0; j < values.Length; j++)
						mods[id][j] += values[j];
				}
			}
			return mods;
		}
		public virtual List<Mod> GetModsFromTo(int pLevel, int pNextLevel)
		{
			if (pLevel == 0)
				return GetMods(pNextLevel);
			var mods = new List<Mod>();
			for (int i = 0; i < Attributes.Count; i++)
			{
				var id = Attributes[i].id;
				var values = Attributes[i].GetValues(pLevel);
				var nextValues = Attributes[i].GetValues(pNextLevel);
				for (int j = 0; j < nextValues.Length; j++)
					nextValues[j] = nextValues[j] - values[j];
				mods.Add(new Mod(id, nextValues));
			}
			return mods;
		}
		public virtual Dictionary<int, float[]> GetModsFromToDict(int pFromLevel, int pToLevel)
		{
			if (pFromLevel == 0)
				return GetModsDict(pToLevel);
			var mods = new Dictionary<int, float[]>();
			for (int i = 0; i < Attributes.Count; i++)
			{
				var id = Attributes[i].id;
				var values = Attributes[i].GetValues(pFromLevel);
				var nextValues = Attributes[i].GetValues(pToLevel);
				for (int j = 0; j < nextValues.Length; j++)
					nextValues[j] = nextValues[j] - values[j];

				if (!mods.ContainsKey(id))
					mods.Add(id, nextValues);
				else
				{
					for (int j = 0; j < nextValues.Length; j++)
						mods[id][j] += nextValues[j];
				}
			}
			return mods;
		}
		public bool IsEqual<M>(AttributesCollection<M> pOther) where M : Attribute
		{
			for (int i = 0; i < Attributes.Count; i++)
			{
				bool sameId = false;
				var att = Attributes[i];
				for (int j = 0; j < pOther.Attributes.Count; j++)
				{
					var otherAtt = pOther.Attributes[j];
					if (att.id == otherAtt.id)
					{
						sameId = true;
						if (att.value != otherAtt.value
							|| att.increase != otherAtt.increase)
							return false;

						if (att.values.Length != otherAtt.values.Length
							|| att.increases.Length != otherAtt.increases.Length)
							return false;

						for (int v = 0; v < att.values.Length; v++)
						{
							if (att.values[v] != otherAtt.values[v])
								return false;
						}

						if (att.increases != null)
							for (int v = 0; v < att.increases.Length; v++)
							{
								if (att.increases[v] != otherAtt.increases[v])
									return false;
							}
					}
				}
				if (!sameId)
					return false;
			}
			return true;
		}
	}

	[Serializable]
	public class Attribute
	{
		//=== MAIN
		public int id;
		public float value;
		public int unlock;
		public float increase;
		public float max;
		//=== Optional
		public float[] values; //Sometime we have more than one value eg. value-0 for Gold value-1 for Gem
		public float[] increases; //Sometime Increase is defined by multi values eg. increase-0 for level, increase-1 for rarity
		public float[] unlocks; //Sometime Unlock is defined for many purposes eg. level to unlock or rarity to unlock
		public float[] maxes; //Sometime max is defined for different value eg. max value when rarity == 1 or max value when rarity == 2

		public virtual float GetValue(int pLevel = 1, int pIndex = -1)
		{
			float value = this.value;
			float unlock = this.unlock;
			float max = this.max;
			float increase = this.increase;

			if (value == 0 && pIndex >= 0)
				value = values[Mathf.Clamp(pIndex, 0, values.Length - 1)];
			if (unlock == 0 && pIndex >= 0)
				unlock = unlocks[Mathf.Clamp(pIndex, 0, unlocks.Length - 1)];
			if (increase == 0 && pIndex >= 0)
				increase = increases[Mathf.Clamp(pIndex, 0, increases.Length - 1)];
			if (max == 0 && pIndex >= 0)
				max = maxes[Mathf.Clamp(pIndex, 0, maxes.Length - 1)];

			if (pLevel < unlock || pLevel == 0)
				return 0;

			float inc = 0;
			if (unlock > 0)
				inc = (pLevel - unlock) * increase;
			else
				inc = (pLevel - 1) * increase;
			if (inc > max && max > 0)
				inc = max;
			return value + inc;
		}
		/// <summary>
		/// Level is 0, every values are 0
		/// Level starts from 1
		/// </summary>
		public virtual float[] GetValues(int pLevel = 1)
		{
			float[] outputValues = new float[1] { 0 };

			if (value != 0) values = new float[1] { value };
			if (unlock != 0) unlocks = new float[1] { unlock };
			if (increase != 0) increases = new float[1] { increase };
			if (max != 0) maxes = new float[1] { max };

			if (values != null && values.Length > 0)
			{
				outputValues = new float[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					if (pLevel > 0)
					{
						float unlock = unlocks != null && unlocks.Length > i ? unlocks[i] : 0;
						float max = maxes != null && maxes.Length > i ? maxes[i] : 0;
						float increase = increases != null && increases.Length > i ? increases[i] : 0;

						float inc = 0;
						if (unlock > 0)
							inc = (pLevel - unlock) * increase;
						else
							inc = (pLevel - 1) * increase;
						if (inc > max && max > 0)
							inc = max;
						outputValues[i] = values[i] + inc;
					}
					else
					{
						outputValues[i] = 0;
					}
				}
			}
			return outputValues;
		}
		public virtual int GetIntValue(int pLevel = 1, int pValueIndex = -1)
		{
			return Mathf.RoundToInt(GetValue(pLevel, pValueIndex));
		}
		public virtual bool GetBoolValue(int pLevel = 1, int pValueIndex = -1)
		{
			return GetValue(pLevel, pValueIndex) >= 1;
		}
		public virtual string GetDescription(int pLevel = 1, int pNextLevel = 1, int pValueIndex = -1)
		{
			float curValue = GetValue(pLevel, pValueIndex);
			if (pNextLevel != pLevel)
			{
				string str = "";
				float nextValue = GetValue(pNextLevel);
				if (nextValue != curValue)
					str = $"{MathHelper.Round(curValue, 2)} (<color=#299110>+{MathHelper.Round(nextValue - curValue, 2)}</color>)";
				else
					str = curValue.ToString();
				return str;
			}
			return curValue.ToString();
		}
	}

	public static class AttributeParseExtenstion
	{
		public static string LogValues(this List<Attribute> attributes)
		{
			string text = "";
			for (int i = 0; i < attributes.Count; i++)
			{
				text +=
					$"Id:{attributes[i].id}, Value:{attributes[i].value}, Increase:{attributes[i].increase}, Max:{attributes[i].max}";
				if (i + 1 <= attributes.Count - 1)
					text += "\n";
			}
			return text;
		}
		public static float GetValueOf(this List<Attribute> attributes, int pId, int pLevel = 1)
		{
			for (int i = 0; i < attributes.Count; i++)
			{
				if (attributes[i].id == pId)
					return attributes[i].GetValue(pLevel);
			}
			return 0;
		}
		public static Attribute Find(this List<Attribute> attributes, int pId)
		{
			for (int i = 0; i < attributes.Count; i++)
			{
				if (attributes[i].id == pId)
					return attributes[i];
			}
			return null;
		}
	}
}