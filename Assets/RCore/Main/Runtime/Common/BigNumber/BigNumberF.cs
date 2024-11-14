/***
 * Author HNB-RaBear - 2018
 **/

using System;
using System.Text;
using UnityEngine;

namespace RCore
{
    public class BigNumberF : IComparable<BigNumberF>
    {
        //Values of float range from approximately 1.5 × 10⁻⁴⁵ to 3.4 × 10³⁸, but only the first 6-9 digits are considered reliable.
        //A float can store a number like 123456.789, which has 9 digits total (6 before and 3 after the decimal).
        //If the number has more than 9 significant digits (e.g., 123456789.123), precision will be lost.
        private static readonly int MAX_READ_LENGTH = 12; 
        private static readonly float MAX_READ_VALUE = float.Parse("999999999999"); //increase length of this number will increase the precision

        public static BigNumberF Zero => new(0);
        public static BigNumberF One => new(1);
        public static BigNumberF Two => new(2);
        public static BigNumberF Ten => new(10);
        public static BigNumberF OneHundred => new(100);

        public float readableValue;
        public int pow;
        public int valueLength;

        private StringBuilder m_numberBuilder = new StringBuilder();
        private StringBuilder m_powBuilder = new StringBuilder();

        public BigNumberF()
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

        public BigNumberF(BigNumberF pNumber)
        {
            readableValue = pNumber.readableValue;
            pow = pNumber.pow;
            valueLength = pNumber.valueLength;
        }

        //12345600000000000000000000000000000000000000000000000000000000.......
        //123E+13
        public BigNumberF(string pValue)
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

        public BigNumberF(int pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false));
        }

        public BigNumberF(long pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberF(float pValue)
        {
            readableValue = pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberF(double pValue)
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

        public BigNumberF(decimal pValue)
        {
            readableValue = (float)pValue;
            pow = 0;

            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberF(float pValue, int pPow)
        {
            readableValue = pValue;
            pow = pPow;
            ConvertMaximum();

            //Debug.Log(pValue + "\n" + GetString(false) + "\n" + GetNotationString());
        }

        public BigNumberF(float pValue, string pKKKUnit)
        {
            readableValue = pValue;
            pow = GetPowFromUnit(pKKKUnit.ToUpper());
            ConvertMaximum();
        }

        public BigNumberF Add(BigNumberF pNumber)
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

        public BigNumberF Add(float pValue)
        {
            var temp = new BigNumberF(pValue);
            return Add(temp);
        }

        public BigNumberF Add(int pValue)
        {
            var temp = new BigNumberF(pValue);
            return Add(temp);
        }

        public BigNumberF Subtract(BigNumberF pNumber)
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

        public BigNumberF Subtract(float pValue)
        {
            var temp = new BigNumberF(pValue);
            return Subtract(temp);
        }

        public BigNumberF Subtract(int pValue)
        {
            var temp = new BigNumberF(pValue);
            return Subtract(temp);
        }

        public BigNumberF Divide(BigNumberF pNumber)
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

        public BigNumberF Divide(float pValue)
        {
            var temp = new BigNumberF(pValue);
            return Divide(temp);
        }

        public BigNumberF Divide(int pValue)
        {
            var temp = new BigNumberF(pValue);
            return Divide(temp);
        }

        public BigNumberF Multiply(BigNumberF pNumber)
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

        public BigNumberF Multiply(float pValue)
        {
            var temp = new BigNumberF(pValue);
            return Multiply(temp);
        }

        public BigNumberF Multiply(int pValue)
        {
            var temp = new BigNumberF(pValue);
            return Multiply(temp);
        }

        public BigNumberF Pow(float pHat)
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

        public BigNumberF Mod(BigNumberF pNumber)
        {
            //Debug.Log(GetString() + "%" + pNumber.GetString());

            if (CompareTo(pNumber) < 0)
                return new BigNumberF(this);

            var div = this / pNumber;
            float divDecimal = div.readableValue - (float)Math.Truncate(div.readableValue);
            var mod = new BigNumberF(divDecimal) * pNumber;
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
                        m_powBuilder.Clear().Append(parts[1].Substring(0, pow));
                    }
                    else if (p2Len == pow)
                    {
                        m_powBuilder.Clear().Append(parts[1]);
                    }
                    else if (p2Len < pow)
                    {
                        m_powBuilder.Clear().Append(parts[1]);
                        int a = pow - p2Len;
                        while (a > 0)
                        {
                            m_powBuilder.Append("0");
                            a--;
                        }
                    }
                    return m_numberBuilder.Clear().Append(parts[0]).Append(m_powBuilder).ToString();
                }
                else if (parts.Length == 1)
                {
                    m_powBuilder.Clear();
                    for (int i = 0; i < pow; i++)
                        m_powBuilder.Append("0");

                    return m_numberBuilder.Clear().Append(parts[0]).Append(m_powBuilder).ToString();
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
                m_numberBuilder.Clear()
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
                    m_numberBuilder.Clear();
                    m_numberBuilder.Append(num);
                    if (pow + len - 3 >= 1)
                    {
                        m_numberBuilder.Append("E+").Append(pow + len - 3);
                    }
                }
                else
                {
                    return Math.Round(readableValue, 2).ToString();
                }
            }

            return m_numberBuilder.ToString();
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

            m_numberBuilder.Clear().Append(displayPart);

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    m_numberBuilder.Append(unitChar);
            }
            else if (len > 12)
                m_numberBuilder.Append("T");
            else if (len > 9)
                m_numberBuilder.Append("B");
            else if (len > 6)
                m_numberBuilder.Append("M");
            else if (len > 3)
                m_numberBuilder.Append("K");

            return m_numberBuilder.ToString();
        }

        public string GetKKKUnit()
        {
            int len = valueLength + pow;
            if (len <= 3)
            {
                return "";
            }

            m_numberBuilder.Clear();

            if (len > 15)
            {
                int unitSize = (len - 16) / (3 * 26) + 2;
                int unitTypeInt = (len - 16) / 3 % 26;
                char unitChar = (char)(65 + unitTypeInt);
                for (int i = 0; i < unitSize; i++)
                    m_numberBuilder.Append(unitChar);
            }
            else if (len > 12)
                m_numberBuilder.Append("T");
            else if (len > 9)
                m_numberBuilder.Append("B");
            else if (len > 6)
                m_numberBuilder.Append("M");
            else if (len > 3)
                m_numberBuilder.Append("K");

            return m_numberBuilder.ToString();
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

        public bool GreaterThan(BigNumberF pNumber)
        {
            if (pow == pNumber.pow)
                return readableValue > pNumber.readableValue;
            else
                return pow > pNumber.pow;
        }

        public int CompareTo(BigNumberF pNumber)
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
                return CompareTo(new BigNumberF(pNumber));
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

        private void ConvertMaximum(BigNumberF pAlpha)
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

        public BigNumberF Sqrt()
        {
            readableValue = Mathf.Sqrt(readableValue);
            pow /= 2;

            ConvertMaximum();
            return this;
        }

        //==============================================

        #region Static

        public static BigNumberF Create(int pValue)
        {
            return new BigNumberF(pValue);
        }

        public static BigNumberF Create(float pValue)
        {
            return new BigNumberF(pValue);
        }

        public static BigNumberF Create(long pValue)
        {
            return new BigNumberF(pValue);
        }

        public static BigNumberF Create(string pValue)
        {
            return new BigNumberF(pValue);
        }

        public static BigNumberF operator +(BigNumberF pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Add(pRight);
        }

        public static BigNumberF operator +(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Add(pRight);
        }

        public static BigNumberF operator +(float pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pRight).Add(pLeft);
        }

        public static BigNumberF operator -(BigNumberF pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Subtract(pRight);
        }

        public static BigNumberF operator -(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Subtract(pRight);
        }

        public static BigNumberF operator -(float pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Subtract(pRight);
        }

        public static BigNumberF operator *(BigNumberF pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Multiply(pRight);
        }

        public static BigNumberF operator *(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Multiply(pRight);
        }

        public static BigNumberF operator *(float pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pRight).Multiply(pLeft);
        }

        public static BigNumberF operator /(BigNumberF pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Divide(pRight);
        }

        public static BigNumberF operator /(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Divide(pRight);
        }

        public static BigNumberF operator /(float pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Divide(pRight);
        }

        public static BigNumberF operator %(BigNumberF pLeft, BigNumberF pRight)
        {
            return new BigNumberF(pLeft).Mod(pRight);
        }

        public static BigNumberF operator %(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Mod(new BigNumberF(pRight));
        }

        public static BigNumberF Lg(BigNumberF pLeft, float pRight)
        {
            if (pRight == 1)
            {
                Debug.LogError("Infinity");
                return new BigNumberF();
            }

            if (pLeft.pow > 0)
            {
                double a = Math.Log(pLeft.readableValue, pRight);
                double b = Math.Log(Math.Pow(10, pLeft.pow), pRight);
                return new BigNumberF(a + b);
            }
            else
            {
                double a = Math.Log(pLeft.readableValue, pRight);
                return new BigNumberF(a);
            }
        }

        public static BigNumberF Pow(BigNumberF pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Pow(pRight);
        }

        public static BigNumberF Pow(float pLeft, float pRight)
        {
            return new BigNumberF(pLeft).Pow(pRight);
        }

        public static BigNumberF Max(BigNumberF p1, BigNumberF p2)
        {
            return p1.CompareTo(p2) > 0 ? p1 : p2;
        }

        public static BigNumberF Random(BigNumberF p1, BigNumberF p2)
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

        public static BigNumberF Sqrt(BigNumberF pNumber)
        {
            return new BigNumberF(pNumber).Sqrt();
        }

        public static BigNumberF AA(int pBase)
        {
            return Ten.Pow(10).Multiply(pBase);
        }

        /// <summary>
        /// Convert 13.4AA to 1340000000000000...
        /// </summary>
        public static BigNumberF ToBigNumber(string pKKKNumber)
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
            return new BigNumberF(numberPart.ToString());
        }

        #endregion
    }

    public static class BigNumberAlphaExtension
    {
        public static BigNumberF ToBigNumber(this float pNumber)
        {
            return new BigNumberF(pNumber);
        }
    }
}