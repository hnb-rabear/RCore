using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SheetXExample
{
	[CreateAssetMenu(fileName = "ExampleDataCollection", menuName = "SheetXExample/Create ExampleDataCollection")]
	public class ExampleDataCollection : ScriptableObject
	{
		public ExampleData1[] exampleData1s;
		public ExampleData2[] exampleData2s;
		public ExampleData3[] exampleData3s;

		[ContextMenu("Load")]
		public void Load()
		{
			
#if UNITY_EDITOR
			var txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData1.txt");
			exampleData1s = JsonConvert.DeserializeObject<ExampleData1[]>(txt.text);

			txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData2.txt");
			exampleData2s = JsonConvert.DeserializeObject<ExampleData2[]>(txt.text);

			txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData3.txt");
			exampleData3s = JsonConvert.DeserializeObject<ExampleData3[]>(txt.text);
#endif
		}
	}

	[Serializable]
	public class ExampleData1
	{
		public int numberExample1;
		public int numberExample2;
		public float numberExample3;
		public bool boolExample;
		public string stringExample;
	}

	[Serializable]
	public class ExampleData2
	{
		[Serializable]
		public class Example
		{
			public int id;
			public string name;
		}

		public string[] array1;
		public int[] array2;
		public int[] array3;
		public bool[] array4;
		public int[] array5;
		public string[] array6;
		public Example json1;
	}

	[Serializable]
	public class ExampleData3
	{
		public int id;
		public string name;
		public List<Attribute> Attributes;
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
					str = $"{curValue} (<color=#299110>+{nextValue - curValue}</color>)";
				else
					str = curValue.ToString();
				return str;
			}
			return curValue.ToString();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(ExampleDataCollection))]
	public class ExampleDataCollectionEditor : Editor
	{
		private ExampleDataCollection m_target;

		private void OnEnable()
		{
			m_target = target as ExampleDataCollection;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Load"))
				m_target.Load();
		}
	}
#endif
}