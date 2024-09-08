﻿/***
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using System;
using System.Text;

namespace RCore.Common
{
    public class BigNumberBeta : IComparable<BigNumberBeta>
    {
        private static readonly int MAX_READ_LENGTH = 15; //Max length of float is 38 but we dont care any number after this position
        private static readonly decimal MAX_READ_VALUE = decimal.Parse("999999999999999"); //increase length of this nuber will increse the acurration

        //internal static BigNumberAlpha Zero { get { return new BigNumberAlpha(0); } }
        internal static BigNumberBeta One => new(1);
        internal static BigNumberBeta Two => new(2);
        internal static BigNumberBeta Ten => new(10);
        internal static BigNumberBeta OneHundred => new(100);

        public decimal readableValue;
        public int pow;
        public int valueLength;

        private StringBuilder mNumberBuilder = new StringBuilder();
        private StringBuilder mPowBuilder = new StringBuilder();

        public BigNumberBeta()
        {
            readableValue = 0;
            pow = 0;
            valueLength = 1;
        }

        public BigNumberBeta(BigNumberBeta pNumber)
        {
            readableValue = pNumber.readableValue;
            pow = pNumber.pow;
            valueLength = pNumber.valueLength;
        }

        public BigNumberBeta(BigNumberAlpha pNumber)
        {
            readableValue = (decimal)pNumber.readableValue;
            pow = pNumber.pow;
            valueLength = pNumber.valueLength;
        }

        //12345600000000000000000000000000000000000000000000000000000000.......
        public BigNumberBeta(string pValue)
        {
            if (string.IsNullOrEmpty(pValue))
            {
                readableValue = 0;
                pow = 0;
                valueLength = 1;
                return;
            }

            string[] parts = pValue.Split('.');
            int len = parts[0].Length;
            if (len <= MAX_READ_LENGTH)
            {
                readableValue = decimal.Parse(pValue);
                pow = 0;
            }
            else
            {
                string readablePart = parts[0].Substring(0, MAX_READ_LENGTH);
                int remainLen = len - readablePart.Length;
                if (decimal.TryParse(readablePart, out readableValue))
                {
                    pow = remainLen;
                }
                else
                {
                    Debug.LogError("Could not convert value " + parts[0]);
                }
            }

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(int pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(long pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(float pValue)
        {
            int len = GetLength(pValue);
            if (len <= MAX_READ_LENGTH)
            {
                readableValue = (decimal)pValue;
                pow = 0;
            }
            else
            {
                readableValue = (decimal)(pValue / Math.Pow(10, len - MAX_READ_LENGTH));
                pow = len - MAX_READ_LENGTH;
            }

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(double pValue)
        {
            int len = GetLength(pValue);
            if (len <= MAX_READ_LENGTH)
            {
                readableValue = (decimal)pValue;
                pow = 0;
            }
            else
            {
                readableValue = (decimal)(pValue / Math.Pow(10, len - MAX_READ_LENGTH));
                pow = len - MAX_READ_LENGTH;
            }

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(decimal pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberBeta(decimal pValue, int pPow)
        {
            readableValue = pValue;
            pow = pPow;
            ConvertMaximum();
        }

        public BigNumberBeta(decimal pValue, string pKKKUnit)
        {
            readableValue = pValue;
            pow = GetPowFromUnit(pKKKUnit.ToUpper());
            ConvertMaximum();
        }

        public BigNumberBeta Add(BigNumberBeta pBeta)
        {
            //Debug.Log(GetNotationString() + "+" + pBeta.GetNotationString());

            if (pBeta.readableValue == 0)
                return this;

            if (readableValue == 0)
            {
                readableValue = pBeta.readableValue;
                pow = pBeta.pow;
                ConvertMaximum();
                return this;
            }

            if (pow == pBeta.pow)
            {
                readableValue += pBeta.readableValue;
            }
            else if (pow > pBeta.pow)
            {
                int negPow = pow - pBeta.pow;
                readableValue += pBeta.readableValue * (decimal)Math.Pow(10, -negPow);
            }
            else if (pow < pBeta.pow)
            {
                int negPow = pBeta.pow - pow;
                readableValue = readableValue * (decimal)Math.Pow(10, -negPow) + pBeta.readableValue;
                pow = pBeta.pow;
            }

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberBeta Add(float pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Add(temp);
        }

        public BigNumberBeta Add(int pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Add(temp);
        }

        public BigNumberBeta Subtract(BigNumberBeta pBeta)
        {
            //Debug.Log(GetNotationString() + "-" + pBeta.GetNotationString());

            if (pBeta.readableValue == 0)
                return this;

            if (readableValue == 0)
            {
                readableValue = pBeta.readableValue * -1;
                pow = pBeta.pow;
                ConvertMaximum();
                return this;
            }

            if (pow == pBeta.pow)
            {
                readableValue -= pBeta.readableValue;
            }
            else if (pow > pBeta.pow)
            {
                int negPow = pow - pBeta.pow;
                readableValue -= pBeta.readableValue * (decimal)Math.Pow(10, -negPow);
            }
            else if (pow < pBeta.pow)
            {
                int negPow = pBeta.pow - pow;
                readableValue = readableValue * (decimal)Math.Pow(10, -negPow) - pBeta.readableValue;
                pow = pBeta.pow;
            }

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberBeta Subtract(float pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Subtract(temp);
        }

        public BigNumberBeta Subtract(int pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Subtract(temp);
        }

        public BigNumberBeta Divide(BigNumberBeta pBeta)
        {
            //Debug.Log(GetNotationString() + "/" + pBeta.GetNotationString());

            if (readableValue == 0)
                return this;

            if (pBeta.readableValue == 0)
            {
                Debug.LogError("Could not divide to Zero!");
                return this;
            }

            if (readableValue > MAX_READ_VALUE / (decimal)Math.Pow(10, 5))
            {
                readableValue /= (decimal)Math.Pow(10, 5);
                pow += 5;
            }

            if (pBeta.readableValue > MAX_READ_VALUE / (decimal)Math.Pow(10, 5))
            {
                pBeta.readableValue /= (decimal)Math.Pow(10, 5);
                pBeta.pow += 5;
            }

            readableValue /= pBeta.readableValue;
            pow -= pBeta.pow;

            ConvertMaximum();
            pBeta.ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberBeta Divide(float pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Divide(temp);
        }

        public BigNumberBeta Divide(int pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Divide(temp);
        }

        public BigNumberBeta Multiply(BigNumberBeta pBeta)
        {
            //Debug.Log(GetNotationString() + "*" + pBeta.GetNotationString());

            if (pBeta.readableValue == 0)
            {
                pow = 0;
                readableValue = 0;
                valueLength = 1;
                return this;
            }

            int addPow = 0;
            if (pow == pBeta.pow)
            {
                readableValue *= pBeta.readableValue;
            }
            else if (pow > pBeta.pow)
            {
                var betaValue = pBeta.readableValue;
                var betaValueLen = pBeta.valueLength;
                decimal stripValue = betaValue / (decimal)Math.Pow(10, betaValueLen - 1);
                addPow = betaValueLen - 1;

                readableValue = readableValue * stripValue;
            }
            else if (pow < pBeta.pow)
            {
                decimal stripValue = readableValue / (decimal)Math.Pow(10, valueLength - 1);
                pow += valueLength - 1;

                readableValue = pBeta.readableValue * stripValue;
            }

            pow = pow + addPow + pBeta.pow;

            ConvertMaximum();

            //Debug.Log(GetNotationString());

            return this;
        }

        public BigNumberBeta Multiply(float pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Multiply(temp);
        }

        public BigNumberBeta Multiply(int pValue)
        {
            var temp = new BigNumberBeta(pValue);
            return Multiply(temp);
        }

        public BigNumberBeta Pow(float pHat)
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
            var temp = readableValue / (decimal)Math.Pow(10, valueLength - offset);
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
                readableValue = (decimal)Math.Pow((double)readableValue, 4);
                hat /= 4f;
                while (readableValue > 1000)
                {
                    resPow += hat;
                    readableValue /= 10;
                }
            }
            pow += (int)Math.Truncate(resPow);
            float resPowDecimal = resPow - (float)Math.Truncate(resPow);

            if (hat > 0)
                readableValue = (decimal)Math.Pow((double)readableValue, hat);

            if (resPowDecimal > 0)
                readableValue = readableValue * (decimal)Math.Pow(10, resPowDecimal);

            ConvertMaximum();

            if (PowIsNegative)
            {
                var real = 1 / this;
                pow = real.pow;
                readableValue = real.readableValue;
            }

            if (ReadIsNegative)
            {
                readableValue *= (decimal)Math.Pow(-1, intHat);
            }

            ConvertMaximum();

            return this;
        }

        public BigNumberBeta Mod(BigNumberBeta pBeta)
        {
            //Debug.Log(GetString() + "%" + pBeta.GetString());

            if (CompareTo(pBeta) < 0)
                return new BigNumberBeta(this);

            var div = this / pBeta;
            decimal divDecimal = div.readableValue - Math.Truncate(div.readableValue);
            var mod = new BigNumberBeta(divDecimal) * pBeta;
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
                    return (int)readableValue;
                else
                {
                    Debug.LogError("Value is too big that can not be Integer!");
                    return int.MaxValue;
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
                    Debug.LogError("Value is too big that can not be Integer!");
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

                Debug.LogError("Value is invalid, can not be Integer! " + valueStr);
                return long.MaxValue;
            }
        }

        public string GetString(bool pStripDecimal = true)
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                if (pStripDecimal)
                    return Math.Round(readableValue, 0).ToString();
                else
                    return Math.Round(readableValue, 2).ToString();
            }

            if (pow == 0)
            {
                if (readableValue == 0)
                    return "0";

                if (pStripDecimal)
                    return readableValue.ToString("#");
                else
                    return readableValue.ToString();
            }
            else
            {
                string[] parts = readableValue.ToString().Split('.');
                if (parts.Length == 2)
                {
                    int p2Len = parts[1].Length;
                    if (p2Len > pow)
                    {
                        mPowBuilder.Clear().Append(parts[1].Substring(0, pow));
                    }
                    else if (p2Len == pow)
                    {
                        mPowBuilder.Clear().Append(parts[1]);
                    }
                    else if (p2Len < pow)
                    {
                        mPowBuilder.Clear().Append(parts[1]);
                        int a = pow - p2Len;
                        while (a > 0)
                        {
                            mPowBuilder.Append("0");
                            a--;
                        }
                    }
                    return mNumberBuilder.Clear().Append(parts[0]).Append(mPowBuilder).ToString();
                }
                else if (parts.Length == 1)
                {
                    mPowBuilder.Clear();
                    for (int i = 0; i < pow; i++)
                        mPowBuilder.Append("0");

                    return mNumberBuilder.Clear().Append(parts[0]).Append(mPowBuilder).ToString();
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

                decimal num = readableValue / (decimal)Math.Pow(10, len - 1);
                num = Math.Round(num, 2);
                mNumberBuilder.Clear()
                    .Append(num)
                    .Append("E+")
                    .Append(pow + len - 1);
            }
            else
            {
                int len = valueLength;
                if (len > 5)
                {
                    decimal num = readableValue / (decimal)Math.Pow(10, len - 3);
                    num = Math.Round(num, 2);
                    mNumberBuilder.Clear();
                    mNumberBuilder.Append(num);
                    if (pow + len - 3 >= 1)
                    {
                        mNumberBuilder.Append("E+").Append(pow + len - 3);
                    }
                }
                else
                {
                    if (readableValue % 1 == 0)
                        return Math.Round(readableValue, 0).ToString();
                    else
                        return Math.Round(readableValue, 2).ToString();
                }
            }

            return mNumberBuilder.ToString();
        }

        public string GetKKKString()
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                return Math.Round(readableValue, 0).ToString();
            }

            int integerPart = (len - 1) % 3 + 1;
            decimal displayPart = readableValue / (decimal)Math.Pow(10, valueLength - integerPart);

            decimal truncate = Math.Truncate(displayPart);
            if (displayPart - truncate > 0)
            {
                if (truncate >= 100)
                    displayPart = Math.Round(displayPart);
                else if (truncate >= 10)
                    displayPart = Math.Round(displayPart, 1);
                else
                    displayPart = Math.Round(displayPart, 2);
            }
            else
                displayPart = truncate;

            mNumberBuilder.Clear().Append(displayPart);

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    mNumberBuilder.Append(unitChar);
            }
            else if (len > 12)
                mNumberBuilder.Append("T");
            else if (len > 9)
                mNumberBuilder.Append("B");
            else if (len > 6)
                mNumberBuilder.Append("M");
            else if (len > 3)
                mNumberBuilder.Append("K");

            return mNumberBuilder.ToString();
        }

        public string GetKKKUnit()
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                return "";
            }

            mNumberBuilder.Clear();

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    mNumberBuilder.Append(unitChar);
            }
            else if (len > 12)
                mNumberBuilder.Append("T");
            else if (len > 9)
                mNumberBuilder.Append("B");
            else if (len > 6)
                mNumberBuilder.Append("M");
            else if (len > 3)
                mNumberBuilder.Append("K");

            return mNumberBuilder.ToString();
        }

        /// <summary>
        /// Unit must be aa,bb.cc or aaa,bbb,ccc, ....
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

        public bool GreaterThan(BigNumberBeta pBeta)
        {
            if (pow == pBeta.pow)
                return readableValue > pBeta.readableValue;
            else
                return pow > pBeta.pow;
        }

        public int CompareTo(BigNumberBeta pBeta)
        {
            if (pow == pBeta.pow)
            {
                if (readableValue > pBeta.readableValue)
                    return 1;
                else if (readableValue == pBeta.readableValue)
                    return 0;
                return -1;
            }
            else if (pow > pBeta.pow)
                return 1;
            else if (pow < pBeta.pow)
                return -1;
            return 0;
        }

        public int CompareTo(float pBeta)
        {
            if ((float)readableValue > pBeta)
                return 1;
            else
                return CompareTo(new BigNumberBeta(pBeta));
        }

        public void ConvertMaximum()
        {
            decimal tempReadableVal = readableValue;
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

            if (pow < 0)
            {
                readableValue *= (decimal)Math.Pow(10, pow);
                pow = 0;
            }

            if (readableValue == 0)
                pow = 0;

            UpdateValueLength();
        }

        private void ConvertMaximum(BigNumberBeta pAlpha)
        {
            decimal tempReadableVal = pAlpha.readableValue;
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
                int len = GetLength((double)tempReadableVal);
                int needLen = MAX_READ_LENGTH - len;
                if (needLen > pAlpha.pow)
                    needLen = pAlpha.pow;

                pAlpha.readableValue *= (decimal)Math.Pow(10, needLen);

                pAlpha.pow -= needLen;
            }
        }

        private void UpdateValueLength()
        {
            valueLength = GetLength((double)readableValue);
        }

        private int GetLength(double pNumber)
        {
            var temp = Math.Round(pNumber);
            if (temp < 0)
                temp *= -1;

            if (temp == 0)
                return 1;

            return (int)Math.Floor(Math.Log10(temp)) + 1;
        }

        public override string ToString()
        {
            return GetString();
        }

        public BigNumberBeta Sqrt()
        {
            readableValue = (decimal)Math.Sqrt((double)readableValue);
            pow /= 2;

            ConvertMaximum();
            return this;
        }

        public bool IsTooSmallTo(BigNumberBeta pOther)
        {
            if (pOther.pow + pOther.valueLength - pow - valueLength >= 10)
                return true;

            return false;
        }

        //==============================================

        #region Static

        public static BigNumberBeta Create(int pValue)
        {
            return new BigNumberBeta(pValue);
        }

        public static BigNumberBeta Create(float pValue)
        {
            return new BigNumberBeta(pValue);
        }

        public static BigNumberBeta Create(long pValue)
        {
            return new BigNumberBeta(pValue);
        }

        public static BigNumberBeta Create(string pValue)
        {
            return new BigNumberBeta(pValue);
        }

        public static BigNumberBeta operator +(BigNumberBeta pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Add(pRight);
        }

        public static BigNumberBeta operator +(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Add(pRight);
        }

        public static BigNumberBeta operator +(float pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pRight).Add(pLeft);
        }

        public static BigNumberBeta operator -(BigNumberBeta pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Subtract(pRight);
        }

        public static BigNumberBeta operator -(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Subtract(pRight);
        }

        public static BigNumberBeta operator -(float pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Subtract(pRight);
        }

        public static BigNumberBeta operator *(BigNumberBeta pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Multiply(pRight);
        }

        public static BigNumberBeta operator *(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Multiply(pRight);
        }

        public static BigNumberBeta operator *(float pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pRight).Multiply(pLeft);
        }

        public static BigNumberBeta operator /(BigNumberBeta pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Divide(pRight);
        }

        public static BigNumberBeta operator /(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Divide(pRight);
        }

        public static BigNumberBeta operator /(float pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Divide(pRight);
        }

        public static BigNumberBeta operator ^(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Pow(pRight);
        }

        public static BigNumberBeta operator %(BigNumberBeta pLeft, BigNumberBeta pRight)
        {
            return new BigNumberBeta(pLeft).Mod(pRight);
        }

        public static BigNumberBeta operator %(BigNumberBeta pLeft, float pRight)
        {
            return new BigNumberBeta(pLeft).Mod(new BigNumberBeta(pRight));
        }

        public static BigNumberBeta Lg(BigNumberBeta pLeft, float pRight)
        {
            if (pRight == 1)
            {
                Debug.LogError("Infinity");
                return new BigNumberBeta();
            }

            if (pLeft.pow > 0)
            {
                double a = Math.Log((double)pLeft.readableValue, pRight);
                double b = Math.Log(Math.Pow(10, pLeft.pow), pRight);
                return new BigNumberBeta(a + b);
            }
            else
            {
                double a = Math.Log((double)pLeft.readableValue, pRight);
                return new BigNumberBeta(a);
            }
        }

        public static BigNumberBeta Pow(BigNumberBeta pLeft, int pRight)
        {
            return new BigNumberBeta(pLeft).Pow(pRight);
        }

        public static BigNumberBeta Pow(float pLeft, int pRight)
        {
            return new BigNumberBeta(pLeft).Pow(pRight);
        }

        public static BigNumberBeta Max(BigNumberBeta p1, BigNumberBeta p2)
        {
            return p1.CompareTo(p2) > 0 ? p1 : p2;
        }

        public static BigNumberBeta Random(BigNumberBeta p1, BigNumberBeta p2)
        {
            if (p1.CompareTo(p2) > 0)
            {
                var div = p1 / p2;
                var radDiv = UnityEngine.Random.Range(0f, (float)div.readableValue);
                return p2 * radDiv;
            }
            else
            {
                var div = p2 / p1;
                var radDiv = UnityEngine.Random.Range(0f, (float)div.readableValue);
                return p1 * radDiv;
            }
        }

        public static BigNumberBeta Sqrt(BigNumberBeta pNumber)
        {
            return new BigNumberBeta(pNumber).Sqrt();
        }

        //====================================

        public static BigNumberBeta Pow(BigNumberAlpha pLeft, int pRight)
        {
            return new BigNumberBeta(pLeft).Pow(pRight);
        }

        public static BigNumberBeta operator +(BigNumberBeta pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberBeta(pLeft).Add(new BigNumberBeta(pRight));
        }

        public static BigNumberBeta operator -(BigNumberBeta pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberBeta(pLeft).Subtract(new BigNumberBeta(pRight));
        }

        public static BigNumberBeta operator *(BigNumberBeta pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberBeta(pLeft).Multiply(new BigNumberBeta(pRight));
        }

        public static BigNumberBeta operator /(BigNumberBeta pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberBeta(pLeft).Divide(new BigNumberBeta(pRight));
        }

        public static BigNumberBeta operator %(BigNumberBeta pLeft, BigNumberAlpha pRight)
        {
            return new BigNumberBeta(pLeft).Mod(new BigNumberBeta(pRight));
        }

        #endregion
    }
}