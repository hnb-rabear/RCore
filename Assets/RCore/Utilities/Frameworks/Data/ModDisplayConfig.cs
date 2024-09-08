using RCore.Common;
using System;
using UnityEngine;

namespace RCore.Framework.Data
{
	[Serializable]
	public abstract class ModDisplayConfig
	{
		public const string VAL_COLOR = "#D5FF00";
		public const string VAL_PLUS_COLOR = "#01FF00";
		public const string VAL_MINUS_COLOR = "red";

		public int mod;
		public int group;
		public string positiveDescription;
		public int positiveColor;
		public bool addSign;

		protected int m_LocalizedId = -1;

		public string GetDescription(float[] pVals)
		{
			var valsStr = BuildDescriptionParams(pVals);
			return string.Format(GetDescription(), valsStr);
		}

		/// <summary>
		/// Get full description of mod with current value and additional value
		/// </summary>
		public string GetDescription(float[] pFromVals, float[] pToVals)
		{
			var valsStr = BuildDescriptionParams(pFromVals, pToVals);
			return string.Format(GetDescription(), valsStr);
		}

		public virtual string GetDescription()
		{
			return positiveDescription;
		}

		protected string[] BuildDescriptionParams(float[] pVals)
		{
			var valsStr = new string[pVals.Length];
			for (int j = 0; j < pVals.Length; j++)
			{
				float val = MathHelper.Round(pVals[j], 2);
				var curColor = positiveColor == Mathf.Sign(val) ? VAL_COLOR : VAL_MINUS_COLOR;
				string sign = addSign && val > 0 ? "+" : "";
				valsStr[j] = $"<color={curColor}>{sign}{val}</color>";
			}
			return valsStr;
		}

		protected string[] BuildDescriptionParams(float[] pFromVals, float[] pToVals)
		{
			var valsStr = new string[pFromVals.Length];
			for (int j = 0; j < pFromVals.Length; j++)
			{
				float toVal = MathHelper.Round(pToVals[j], 2);
				float fromVal = MathHelper.Round(pFromVals[j], 2);
				if (toVal != fromVal)
				{
					var curColor = positiveColor == Mathf.Sign(fromVal) ? VAL_COLOR : VAL_MINUS_COLOR;
					var bonusColor = positiveColor == Mathf.Sign(toVal - fromVal) ? VAL_PLUS_COLOR : VAL_MINUS_COLOR;
					if (fromVal != 0)
					{
						string signFrom = addSign && fromVal > 0 ? "+" : "";
						string signTo = addSign && toVal - fromVal > 0 ? "+" : "";
						valsStr[j] = $"<color={curColor}>{signFrom}{fromVal}</color>(<color={bonusColor}>{signTo}{toVal - fromVal}</color>)";
					}
					else
					{
						string sign = addSign && toVal > 0 ? "+" : "";
						valsStr[j] = $"<color={bonusColor}>{sign}{toVal}</color>";
					}
				}
				else
				{
					var bonusColor = positiveColor == Mathf.Sign(fromVal) ? VAL_PLUS_COLOR : VAL_MINUS_COLOR;
					string sign = addSign && fromVal > 0 ? "+" : "";
					valsStr[j] = $"<color={bonusColor}>{sign}{fromVal}</color>";
				}
			}
			return valsStr;
		}

		protected string[] BuildDescriptionParamsNext(float[] pFromVals, float[] pToVals)
		{
			var valsStr = new string[pFromVals.Length];
			for (int j = 0; j < pFromVals.Length; j++)
			{
				float toVal = MathHelper.Round(pToVals[j], 2);
				float fromVal = MathHelper.Round(pFromVals[j], 2);
				if (toVal != fromVal)
				{
					var bonusColor = positiveColor == Mathf.Sign(toVal - fromVal) ? VAL_PLUS_COLOR : VAL_MINUS_COLOR;
					string sign = addSign && toVal - fromVal > 0 ? "+" : "";
					valsStr[j] = $"<color={bonusColor}>{sign}{toVal - fromVal}</color>";
				}
				else
				{
					var bonusColor = positiveColor == Mathf.Sign(fromVal) ? VAL_PLUS_COLOR : VAL_MINUS_COLOR;
					string sign = addSign && fromVal > 0 ? "+" : "";
					valsStr[j] = $"<color={bonusColor}>{sign}{fromVal}</color>";
				}
			}
			return valsStr;
		}
	
		protected string[] BuildDescriptionParamsNext(float[] pVals)
		{
			var valsStr = new string[pVals.Length];
			for (int j = 0; j < pVals.Length; j++)
			{
				float val = MathHelper.Round(pVals[j], 2);
				var bonusColor = positiveColor == Mathf.Sign(val) ? VAL_PLUS_COLOR : VAL_MINUS_COLOR;
				string sign = addSign && val > 0 ? "+" : "";
				valsStr[j] = $"<color={bonusColor}>{sign}{val}</color>";
			}
			return valsStr;
		}

		public string GetDescriptionNext(float[] pVals)
		{
			var valsStr = BuildDescriptionParamsNext(pVals);
			return string.Format(GetDescription(), valsStr);
		}

		/// <summary>
		/// Get only next values to display
		/// </summary>
		public string GetDescriptionNext(float[] pFromVals, float[] pToVals)
		{
			var valsStr = BuildDescriptionParamsNext(pFromVals, pToVals);
			return string.Format(GetDescription(), valsStr);
		}
	}
}