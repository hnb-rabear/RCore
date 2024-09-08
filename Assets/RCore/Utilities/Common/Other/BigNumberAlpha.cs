/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Text;
using UnityEngine;

namespace RCore.Common
{
    public class BigNumberAlpha : IComparable<BigNumberAlpha>
    {
        private static readonly int MAX_READ_LENGTH = 12; //Max length of float is 38 but we dont care any number after this position
        private static readonly float MAX_READ_VALUE = float.Parse("999999999999"); //increase length of this nuber will increse the acurration

        internal static BigNumberAlpha Zero => new(0);
        internal static BigNumberAlpha One => new(1);
        internal static BigNumberAlpha Two => new(2);
        internal static BigNumberAlpha Ten => new(10);
        internal static BigNumberAlpha OneHundred => new(100);

        public float readableValue;
        public int pow;
        public int valueLength;

        private StringBuilder m_NumberBuilder = new StringBuilder();
        private StringBuilder m_PowBuilder = new StringBuilder();

        public BigNumberAlpha()
        {
            readableValue = 0;
            pow = 0;
            valueLength = 1;
        }

        public void Clear()
        {
            readableValue = 0;
            pow = 0;
            valueLength = 1;
        }

        public BigNumberAlpha(BigNumberAlpha pNumber)
        {
            readableValue = pNumber.readableValue;
            pow = pNumber.pow;
            valueLength = pNumber.valueLength;
        }

        //12345600000000000000000000000000000000000000000000000000000000.......
        //123E+13
        public BigNumberAlpha(string pValue)
        {
            if (string.IsNullOrEmpty(pValue))
            {
                readableValue = 0;
                pow = 0;
                valueLength = 1;
                return;
            }

            string part1 = pValue;
            string part2 = "";
            string[] parts;
            if (pValue.Contains("E"))
            {
                parts = part1.Split('E');
                part1 = parts[0];
                part2 = parts[1];
                int.TryParse(part2, out pow);
            }

            parts = part1.Split('.');
            int len = parts[0].Length;
            if (len <= MAX_READ_LENGTH)
            {
                readableValue = float.Parse(part1);
                pow += 0;
            }
            else
            {
                string readablePart = parts[0].Substring(0, MAX_READ_LENGTH);
                int remainLen = len - readablePart.Length;
                if (float.TryParse(readablePart, out readableValue))
                {
                    pow += remainLen;
                }
                else
                {
                    Debug.LogError("Could not convert value " + parts[0]);
                }
            }

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(int pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberAlpha(long pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(float pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(double pValue)
        {
            int len = GetLength(pValue);
            if (len <= MAX_READ_LENGTH)
            {
                readableValue = (float)pValue;
                pow = 0;
            }
            else
            {
                readableValue = (float)(pValue / Mathf.Pow(10, len - MAX_READ_LENGTH));
                pow = len - MAX_READ_LENGTH;
            }

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(decimal pValue)
        {
            readableValue = (float)pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(float pValue, int pPow)
        {
            readableValue = pValue;
            pow = pPow;
            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberAlpha(float pValue, string pKKKUnit)
        {
            readableValue = pValue;
            pow = GetPowFromUnit(pKKKUnit.ToUpper());
            ConvertMaximum();
        }

        public BigNumberAlpha Add(BigNumberAlpha pNumber)
        {
            //Debug.Log(GetNotationString() + "+" + pNumber.GetNotationString());

            if (pNumber.readableValue == 0)
                return this;

            if (readableValue == 0)
            {
                readableValue = pNumber.readableValue;
                pow = pNumber.pow;
                ConvertMaximum();
                return this;
            }

            if (pow == pNumber.pow)
            {
                readableValue += pNumber.readableValue;
            }
            else if (pow > pNumber.pow)
            {
                int negPow = pow - pNumber.pow;
                readableValue += pNumber.readableValue * Mathf.Pow(10, -negPow);
            }
            else if (pow < pNumber.pow)
            {
                int negPow = pNumber.pow - pow;
                readableValue = readableValue * Mathf.Pow(10, -negPow) + pNumber.readableValue;
                pow = pNumber.pow;
            }

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberAlpha Add(float pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Add(temp);
        }

        public BigNumberAlpha Add(int pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Add(temp);
        }

        public BigNumberAlpha Subtract(BigNumberAlpha pNumber)
        {
            //Debug.Log(GetNotationString() + "-" + pNumber.GetNotationString());

            if (pNumber.readableValue == 0)
                return this;

            if (readableValue == 0)
            {
                readableValue = pNumber.readableValue * -1;
                pow = pNumber.pow;
                ConvertMaximum();
                return this;
            }

            if (pow == pNumber.pow)
            {
                readableValue -= pNumber.readableValue;
            }
            else if (pow > pNumber.pow)
            {
                int negPow = pow - pNumber.pow;
                readableValue -= pNumber.readableValue * Mathf.Pow(10, -negPow);
            }
            else if (pow < pNumber.pow)
            {
                int negPow = pNumber.pow - pow;
                readableValue = readableValue * Mathf.Pow(10, -negPow) - pNumber.readableValue;
                pow = pNumber.pow;
            }

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberAlpha Subtract(float pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Subtract(temp);
        }

        public BigNumberAlpha Subtract(int pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Subtract(temp);
        }

        public BigNumberAlpha Divide(BigNumberAlpha pNumber)
        {
            //Debug.Log(GetNotationString() + "/" + pNumber.GetNotationString());

            if (readableValue == 0)
                return this;

            if (pNumber.readableValue == 0)
            {
                Debug.LogError("Could not divide to Zero!");
                return this;
            }

            if (readableValue > MAX_READ_VALUE / Mathf.Pow(10, 5))
            {
                readableValue /= Mathf.Pow(10, 5);
                pow += 5;
            }

            if (pNumber.readableValue > MAX_READ_VALUE / Mathf.Pow(10, 5))
            {
                pNumber.readableValue /= Mathf.Pow(10, 5);
                pNumber.pow += 5;
            }

            readableValue /= pNumber.readableValue;
            pow -= pNumber.pow;

            ConvertMaximum();
            pNumber.ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberAlpha Divide(float pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Divide(temp);
        }

        public BigNumberAlpha Divide(int pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Divide(temp);
        }

        public BigNumberAlpha Multiply(BigNumberAlpha pNumber)
        {
            //Debug.Log(GetNotationString() + "*" + pNumber.GetNotationString());

            if (pNumber.readableValue == 0)
            {
                pow = 0;
                readableValue = 0;
                valueLength = 1;
                return this;
            }

            int addPow = 0;
            if (pow == pNumber.pow)
            {
                readableValue *= pNumber.readableValue;
            }
            else if (pow > pNumber.pow)
            {
                var betaValue = pNumber.readableValue;
                var betaValueLen = pNumber.valueLength;
                float stripValue = betaValue / Mathf.Pow(10, betaValueLen - 1);
                addPow = betaValueLen - 1;

                readableValue = readableValue * stripValue;
            }
            else if (pow < pNumber.pow)
            {
                float stripValue = readableValue / Mathf.Pow(10, valueLength - 1);
                pow += valueLength - 1;

                readableValue = pNumber.readableValue * stripValue;
            }

            pow = pow + addPow + pNumber.pow;

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberAlpha Multiply(float pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Multiply(temp);
        }

        public BigNumberAlpha Multiply(int pValue)
        {
            var temp = new BigNumberAlpha(pValue);
            return Multiply(temp);
        }

        public BigNumberAlpha Pow(float pHat)
        {
            //Debug.Log(GetString() + "^" + pHat);

            if (pHat == 1)
                return this;

            if (pHat == 0)
            {
                readableValue = 1;
                pow = 0;
                valueLength = 1;
                return this;
            }

            float hat = pHat;
            bool PowIsNegative = hat < 0;
            bool ReadIsNegative = readableValue < 0;
            if (hat < 0)
                hat *= -1;

            int offset = 1;
            var temp = readableValue / Mathf.Pow(10, valueLength - offset);
            if (temp == 1)
            {
                temp *= 10;
                offset = 2;
            }
            readableValue = temp;
            pow += valueLength - offset;
            int intHat = (int)Math.Truncate(hat);
            float resHat = hat - intHat;
            float resPow = resHat * pow;
            pow *= intHat;

            while (hat > 1f)
            {
                readableValue = Mathf.Pow(readableValue, 4);
                hat /= 4f;
                while (readableValue > 100000)
                {
                    resPow += hat;
                    readableValue /= 10;
                }
            }
            pow += (int)Math.Truncate(resPow);
            float resPowDecimal = resPow - (float)Math.Truncate(resPow);

            if (hat > 0)
                readableValue = Mathf.Pow(readableValue, hat);

            if (resPowDecimal > 0)
                readableValue = readableValue * Mathf.Pow(10, resPowDecimal);

            ConvertMaximum();

            if (PowIsNegative)
            {
                var real = 1 / this;
                pow = real.pow;
                readableValue = real.readableValue;
            }

            if (ReadIsNegative)
            {
                readableValue *= Mathf.Pow(-1, intHat);
            }

            ConvertMaximum();

            return this;
        }

        public BigNumberAlpha Mod(BigNumberAlpha pNumber)
        {
            //Debug.Log(GetString() + "%" + pNumber.GetString());

            if (CompareTo(pNumber) < 0)
                return new BigNumberAlpha(this);

            var div = this / pNumber;
            float divDecimal = div.readableValue - (float)Math.Truncate(div.readableValue);
            var mod = new BigNumberAlpha(divDecimal) * pNumber;
            return mod;
        }

        public bool HasValue()
        {
            return readableValue > 0;
        }

        public int Length()
        {
            return valueLength + pow;
        }

        public int ToInt()
        {
            if (pow <= 0)
            {
                if (readableValue <= int.MaxValue)
                    return Mathf.RoundToInt(readableValue);
                else
                {
                    Debug.LogError("Value is too big that can not be Integer!");
                    return 0;
                }
            }
            else
            {
                string valueStr = GetString();
                int outValue = 0;
                if (int.TryParse(valueStr, out outValue))
                {
                    return outValue;
                }

                Debug.LogError("Value is invalid, can not be Integer! " + valueStr);
                return int.MaxValue;
            }
        }

        public long ToLong()
        {
            if (pow <= 0)
            {
                if (readableValue <= long.MaxValue)
                    return (long)readableValue;
                else
                {
                    Debug.LogError("Value is too big that can not be Long!");
                    return 0;
                }
            }
            else
            {
                string valueStr = GetString();
                long outValue = 0;
                if (long.TryParse(valueStr, out outValue))
                {
                    return outValue;
                }

                Debug.LogError("Value is invalid, can not be Long! " + valueStr);
                return long.MaxValue;
            }
        }

        public string GetString(bool pStripDecimal = true)
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                if (pStripDecimal)
                {
                    string numberStr = Math.Round(readableValue, 0).ToString("#");
                    if (string.IsNullOrEmpty(numberStr))
                        numberStr = "0";
                    return numberStr;
                }
                else
                    return Math.Round(readableValue, 2).ToString();
            }

            if (pow == 0)
            {
                if (readableValue == 0)
                    return "0";

                if (pStripDecimal)
                {
                    string numberStr = readableValue.ToString("#");
                    if (string.IsNullOrEmpty(numberStr))
                        numberStr = "0";
                    return numberStr;
                }
                else
                {
                    string numberStr = "";
                    if (readableValue.ToString().Contains("E"))
                    {
                        numberStr = readableValue.ToString("#");
                        if (string.IsNullOrEmpty(numberStr))
                            numberStr = "0";
                    }
                    else
                        numberStr = readableValue.ToString();

                    return numberStr;
                }
            }
            else
            {
                string numberStr = "";
                if (readableValue.ToString().Contains("E"))
                {
                    numberStr = readableValue.ToString("#");
                    if (string.IsNullOrEmpty(numberStr))
                        numberStr = "0";
                }
                else
                    numberStr = readableValue.ToString();

                string[] parts = numberStr.Split('.');
                if (parts.Length == 2)
                {
                    int p2Len = parts[1].Length;
                    if (p2Len > pow)
                    {
                        m_PowBuilder.Clear().Append(parts[1].Substring(0, pow));
                    }
                    else if (p2Len == pow)
                    {
                        m_PowBuilder.Clear().Append(parts[1]);
                    }
                    else if (p2Len < pow)
                    {
                        m_PowBuilder.Clear().Append(parts[1]);
                        int a = pow - p2Len;
                        while (a > 0)
                        {
                            m_PowBuilder.Append("0");
                            a--;
                        }
                    }
                    return m_NumberBuilder.Clear().Append(parts[0]).Append(m_PowBuilder).ToString();
                }
                else if (parts.Length == 1)
                {
                    m_PowBuilder.Clear();
                    for (int i = 0; i < pow; i++)
                        m_PowBuilder.Append("0");

                    return m_NumberBuilder.Clear().Append(parts[0]).Append(m_PowBuilder).ToString();
                }
                else
                {
                    Debug.LogError("Number should never have more than one dot!");
                    return "0";
                }
            }
        }

        public string GetNotationString()
        {
            if (pow > 0)
            {
                int len = valueLength;

                float num = readableValue / Mathf.Pow(10, len - 1);
                num = (float)Math.Round(num, 2);
                m_NumberBuilder.Clear()
                    .Append(num)
                    .Append("E+")
                    .Append(pow + len - 1);
            }
            else
            {
                int len = valueLength;
                if (len > 3)
                {
                    float num = readableValue / Mathf.Pow(10, len - 3);
                    num = (float)Math.Round(num, 2);
                    m_NumberBuilder.Clear();
                    m_NumberBuilder.Append(num);
                    if (pow + len - 3 >= 1)
                    {
                        m_NumberBuilder.Append("E+").Append(pow + len - 3);
                    }
                }
                else
                {
                    return Math.Round(readableValue, 2).ToString();
                }
            }

            return m_NumberBuilder.ToString();
        }

        public string GetKKKString()
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                return MathHelper.Round(readableValue, 2).ToString();
            }

            int integerPart = (len - 1) % 3 + 1;
            float displayPart = readableValue / Mathf.Pow(10, valueLength - integerPart);

            var truncate = (float)Math.Truncate(displayPart);
            if (displayPart - truncate > 0)
            {
                if (truncate >= 100)
                    displayPart = (float)Math.Round(displayPart);
                else if (truncate >= 10)
                    displayPart = (float)Math.Round(displayPart, 1);
                else
                    displayPart = (float)Math.Round(displayPart, 2);
            }
            else
                displayPart = truncate;

            m_NumberBuilder.Clear().Append(displayPart);

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    m_NumberBuilder.Append(unitChar);
            }
            else if (len > 12)
                m_NumberBuilder.Append("T");
            else if (len > 9)
                m_NumberBuilder.Append("B");
            else if (len > 6)
                m_NumberBuilder.Append("M");
            else if (len > 3)
                m_NumberBuilder.Append("K");

            return m_NumberBuilder.ToString();
        }

        public string GetKKKUnit()
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                return "";
            }

            m_NumberBuilder.Clear();

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    m_NumberBuilder.Append(unitChar);
            }
            else if (len > 12)
                m_NumberBuilder.Append("T");
            else if (len > 9)
                m_NumberBuilder.Append("B");
            else if (len > 6)
                m_NumberBuilder.Append("M");
            else if (len > 3)
                m_NumberBuilder.Append("K");

            return m_NumberBuilder.ToString();
        }

        /// <summary>
        /// Unit must be aa,bb,cc or aaa,bbb,ccc, ....
        /// </summary>
        public static int GetPowFromUnit(string pUnit)
        {
            int pow = 0;
            if (pUnit.Length == 1)
            {
                switch (pUnit)
                {
                    case "K":
                        pow = 3;
                        break;
                    case "M":
                        pow = 6;
                        break;
                    case "B":
                        pow = 9;
                        break;
                    case "T":
                        pow = 12;
                        break;
                }
            }
            else if (pUnit.Length > 1)
            {
                int unitSize = pUnit.Length;
                int unitTypeInt = pUnit[0] - 65;
                int length = (unitSize - 2) * 3 * 26 + 15 + unitTypeInt * 3;
                pow = length - 1;
            }
            return pow;
        }

        public bool GreaterThan(BigNumberAlpha pNumber)
        {
            if (pow == pNumber.pow)
                return readableValue > pNumber.readableValue;
            else
                return pow > pNumber.pow;
        }

        public int CompareTo(BigNumberAlpha pNumber)
        {
            if (pow == pNumber.pow)
            {
                if (readableValue > pNumber.readableValue)
                    return 1;
                else if (readableValue == pNumber.readableValue)
                    return 0;
                return -1;
            }
            else if (pow > pNumber.pow)
                return 1;
            else if (pow < pNumber.pow)
                return -1;
            return 0;
        }

        public int CompareTo(float pNumber)
        {
            if (pNumber == 0)
                return readableValue.CompareTo(pNumber);

            if (readableValue > pNumber)
                return 1;
            else
                return CompareTo(new BigNumberAlpha(pNumber));
        }

        public void ConvertMaximum()
        {
            float tempReadableVal = readableValue;
            if (readableValue < 0)
                tempReadableVal *= -1;

            while (tempReadableVal > MAX_READ_VALUE)
            {
                readableValue /= 10;
                tempReadableVal /= 10;
                pow++;
            }

            while (tempReadableVal < MAX_READ_VALUE / 10 && pow > 0)
            {
                readableValue *= 10;
                tempReadableVal *= 10;
                pow--;
            }

            //if (tempReadableVal < MAX_READ_VALUE / 10)
            //{
            //    int len = GetLength((double)tempReadableVal);
            //    int needLen = MAX_READ_LENGTH - len;
            //    if (needLen > pow)
            //        needLen = pow;

            //    readableValue *= Mathf.Pow(10, needLen);

            //    pow -= needLen;
            //}

            if (pow < 0)
            {
                readableValue *= Mathf.Pow(10, pow);
                pow = 0;
            }

            if (readableValue == 0)
                pow = 0;

            UpdateValueLength();
        }

        private void ConvertMaximum(BigNumberAlpha pAlpha)
        {
            float tempReadableVal = pAlpha.readableValue;
            if (pAlpha.readableValue < 0)
                tempReadableVal *= -1;

            while (tempReadableVal > MAX_READ_VALUE)
            {
                pAlpha.readableValue /= 10;
                tempReadableVal /= 10;
                pAlpha.pow++;
            }

            if (tempReadableVal < MAX_READ_VALUE)
            {
                int len = GetLength(tempReadableVal);
                int needLen = MAX_READ_LENGTH - len;
                if (needLen > pAlpha.pow)
                    needLen = pAlpha.pow;

                pAlpha.readableValue *= Mathf.Pow(10, needLen);

                pAlpha.pow -= needLen;
            }
        }

        private void UpdateValueLength()
        {
            valueLength = GetLength(readableValue);
        }

        private int GetLength(double pNumber)
        {
            if (pNumber < 0)
                pNumber *= -1;
            if (pNumber == 0)
                return 1;
            return (int)Math.Floor(Math.Log10(pNumber)) + 1;
        }

        public override string ToString()
        {
            return GetString();
        }

        public BigNumberAlpha Sqrt()
        {
            readableValue = Mathf.Sqrt(readableValue);
            pow /= 2;

            ConvertMaximum();
            return this;
        }

        //==============================================

        #region Static

        public static BigNumberAlpha Create(int pValue)
        {
            return new BigNumberAlpha(pValue);
        }

        public static BigNumberAlpha Create(float pValue)
        {
            return new BigNumberAlpha(pValue);
        }

        public static BigNumberAlpha Create(long pValue)
        {
            return new BigNumberAlpha(pValue);
        }

        public static BigNumberAlpha Create(string pValue)
        {
            return new BigNumberAlpha(pValue);
        }

        public static BigNumberAlpha operator +(BigNumberAlpha pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Add(pRight);
        }

        public static BigNumberAlpha operator +(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Add(pRight);
        }

        public static BigNumberAlpha operator +(float pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pRight).Add(pLeft);
        }

        public static BigNumberAlpha operator -(BigNumberAlpha pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Subtract(pRight);
        }

        public static BigNumberAlpha operator -(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Subtract(pRight);
        }

        public static BigNumberAlpha operator -(float pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Subtract(pRight);
        }

        public static BigNumberAlpha operator *(BigNumberAlpha pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Multiply(pRight);
        }

        public static BigNumberAlpha operator *(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Multiply(pRight);
        }

        public static BigNumberAlpha operator *(float pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pRight).Multiply(pLeft);
        }

        public static BigNumberAlpha operator /(BigNumberAlpha pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Divide(pRight);
        }

        public static BigNumberAlpha operator /(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Divide(pRight);
        }

        public static BigNumberAlpha operator /(float pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Divide(pRight);
        }

        public static BigNumberAlpha operator %(BigNumberAlpha pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberAlpha(pLeft).Mod(pRight);
        }

        public static BigNumberAlpha operator %(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Mod(new BigNumberAlpha(pRight));
        }

        public static BigNumberAlpha Lg(BigNumberAlpha pLeft, float pRight)
        {
            if (pRight == 1)
            {
                Debug.LogError("Infinity");
                return new BigNumberAlpha();
            }

            if (pLeft.pow > 0)
            {
                double a = Math.Log(pLeft.readableValue, pRight);
                double b = Math.Log(Math.Pow(10, pLeft.pow), pRight);
                return new BigNumberAlpha(a + b);
            }
            else
            {
                double a = Math.Log(pLeft.readableValue, pRight);
                return new BigNumberAlpha(a);
            }
        }

        public static BigNumberAlpha Pow(BigNumberAlpha pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Pow(pRight);
        }

        public static BigNumberAlpha Pow(float pLeft, float pRight)
        {
            return new BigNumberAlpha(pLeft).Pow(pRight);
        }

        public static BigNumberAlpha Max(BigNumberAlpha p1, BigNumberAlpha p2)
        {
            return p1.CompareTo(p2) > 0 ? p1 : p2;
        }

        public static BigNumberAlpha Random(BigNumberAlpha p1, BigNumberAlpha p2)
        {
            if (p1.CompareTo(p2) > 0)
            {
                var div = p1 / p2;
                var radDiv = UnityEngine.Random.Range(0f, div.readableValue);
                return p2 * radDiv;
            }
            else
            {
                var div = p2 / p1;
                var radDiv = UnityEngine.Random.Range(0f, div.readableValue);
                return p1 * radDiv;
            }
        }

        public static BigNumberAlpha Sqrt(BigNumberAlpha pNumber)
        {
            return new BigNumberAlpha(pNumber).Sqrt();
        }

        public static BigNumberAlpha AA(int pBase)
        {
            return Ten.Pow(10).Multiply(pBase);
        }

        /// <summary>
        /// Convert 13.4AA to 1340000000000000...
        /// </summary>
        public static BigNumberAlpha ToBigNumber(string pKKKNumber)
        {
            int dotIndex = pKKKNumber.IndexOf(".");
            var numberPart = new StringBuilder();
            string unitPart = "";
            int count = pKKKNumber.Length;
            for (int i = 0; i < count; i++)
            {
                string s = pKKKNumber[i].ToString();
                int value = 0;
                if (int.TryParse(s, out value))
                {
                    numberPart.Append(s);
                }
                else if (i != dotIndex)
                {
                    unitPart = pKKKNumber.Substring(i);
                    break;
                }
            }
            int unitSize = unitPart.Length;
            if (unitSize > 0)
            {
                int len = 0;
                if (unitSize == 1)
                {
                    // k, m, b, t
                    if (unitPart == "k" || unitPart == "K")
                    {
                        len = 3;
                    }
                    else if (unitPart == "m" || unitPart == "M")
                    {
                        len = 6;
                    }
                    else if (unitPart == "b" || unitPart == "B")
                    {
                        len = 9;
                    }
                    else if (unitPart == "t" || unitPart == "T")
                    {
                        len = 12;
                    }
                }
                else
                {
                    // aa, bb, cc,...
                    int unitTypeInt = unitPart[0].ToString().ToUpper()[0] - 65;
                    len = (unitSize - 2) * 3 * 26 + 15 + unitTypeInt * 3;
                }
                len = dotIndex > 0 ? len - numberPart.ToString().Substring(dotIndex).Length : len;
                for (int i = 0; i < len; i++)
                {
                    numberPart.Append(0);
                }
            }
            return new BigNumberAlpha(numberPart.ToString());
        }

        #endregion
    }

    public static class BigNumberAlphaExtension
    {
        public static BigNumberAlpha ToBigNumber(this float pNumber)
        {
            return new BigNumberAlpha(pNumber);
        }
    }
}
