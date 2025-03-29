/***
 * Author HNB-RaBear - 2017
 **/

using System;
using System.Text;
using UnityEngine;

namespace RCore
{
    public static class BigNumberHelper
    {
        private static StringBuilder numberBuilder = new StringBuilder();
        private static StringBuilder builderScience = new StringBuilder();

        public static string ToNotation(string pNumber, int pLenAfterDot = 2)
        {
            if (string.IsNullOrEmpty(pNumber))
                return "0";

            if (pNumber.Contains("E"))
            {
                string[] parts = pNumber.Split('E');
                string first = Math.Round(double.Parse(parts[0]), 2).ToString();
                string second = parts[1];
                return builderScience.Clear()
                    .Append(first).Append("E+").Append(second).ToString();
            }
            else
            {
                string[] parts = pNumber.Split('.');

                int len = parts[0].Length;
                if (len <= 4)
                    return Math.Round(double.Parse(pNumber), 2).ToString();

                string firstPart = parts[0].Substring(0, 4); //2
                int firstPartLen = firstPart.Length;

                if (len > firstPartLen)
                {
                    pLenAfterDot = len - 4 > pLenAfterDot ? pLenAfterDot : len - 4;
                    string secondPart = parts[0].Substring(firstPartLen, pLenAfterDot); //4

                    int secondPartParse = 0;
                    if (int.TryParse(secondPart, out secondPartParse))
                        secondPart = secondPartParse.ToString();

                    string thirdPart = (len - firstPartLen).ToString();
                    if (thirdPart.Length == 1)
                        thirdPart = "0" + thirdPart;
                    if (secondPart != "0")
                        return builderScience.Clear()
                            .Append(firstPart).Append(".").Append(secondPart).Append("E+").Append(thirdPart).ToString();
                    else
                        return builderScience.Clear()
                            .Append(firstPart).Append("E+").Append(thirdPart).ToString();
                }
                else
                {
                    string thirdPart = (len - firstPartLen).ToString();
                    if (thirdPart.Length == 1)
                        thirdPart = "0" + thirdPart;
                    return builderScience.Clear()
                        .Append(firstPart).Append("E+").Append(thirdPart).ToString();
                }

            }
        }

        public static string ToKKKNumber(string pNumber, int maxNum = 3)
        {
            int len = pNumber.Length;
            if (len <= 3)
                return pNumber;

            int intPartNumber = (len - 1) % 3 + 1;
            string intPart = pNumber.Substring(0, intPartNumber);
            if (intPartNumber < maxNum)
            {
                string decimalPart = pNumber.Substring(intPartNumber, maxNum - intPartNumber);
                numberBuilder.Clear().Append(intPart).Append(".").Append(decimalPart);
            }
            else
                numberBuilder.Clear().Append(intPart);

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    numberBuilder.Append(unitChar);
            }
            else if (len > 12)
                numberBuilder.Append("T");
            else if (len > 9)
                numberBuilder.Append("B");
            else if (len > 6)
                numberBuilder.Append("M");
            else if (len > 3)
                numberBuilder.Append("K");

            return numberBuilder.ToString();
        }

        public static string ToKKKNumber(float pValue, int maxNum = 3)
        {
	        if (Math.Abs(pValue) < 1)
		        return "0";

	        if (Math.Abs(pValue) < 1000)
		        return pValue.ToString("0.#"); // Uses "0.#" to avoid unnecessary decimal places

	        bool negative = pValue < 0;
	        float valueTemp = Mathf.Abs(pValue);
	        int len = (int)Math.Floor(Math.Log10(valueTemp)) + 1;

	        int intPartNumber = (len - 1) % 3 + 1;
	        string intPart = valueTemp.ToString("0.##########").Substring(0, intPartNumber);

	        string decimalPart = "";
	        if (intPartNumber < maxNum)
	        {
		        decimalPart = valueTemp.ToString("0.##########")
			        .Substring(intPartNumber, Math.Min(maxNum - intPartNumber, len - intPartNumber));
	        }

	        var numberBuilder = new StringBuilder();
	        if (negative) numberBuilder.Append("-");
	        numberBuilder.Append(intPart);

	        if (!string.IsNullOrEmpty(decimalPart))
		        numberBuilder.Append(".").Append(decimalPart.TrimEnd('0')); // Removes trailing zeros

	        if (len > 15)
	        {
		        int unitSize = (len - 16) / (3 * 26) + 2;
		        int unitTypeInt = (len - 16) / 3 % 26;
		        char unitChar = (char)(65 + unitTypeInt);
		        for (int i = 0; i < unitSize; i++)
			        numberBuilder.Append(unitChar);
	        }
	        else if (len > 12)
		        numberBuilder.Append("T");
	        else if (len > 9)
		        numberBuilder.Append("B");
	        else if (len > 6)
		        numberBuilder.Append("M");
	        else if (len > 3)
		        numberBuilder.Append("K");

	        return numberBuilder.ToString();
        }

        public static string RemoveDecimalPart(string pNumber)
        {
            string[] parts = pNumber.Split('.');
            return parts[0];
        }

        public static int GetLength(float pNumber)
        {
            if (pNumber < 0)
                pNumber *= -1;
            if (pNumber == 0)
                return 1;
            return (int)Mathf.Floor(Mathf.Log10(pNumber)) + 1;
        }
    }
}