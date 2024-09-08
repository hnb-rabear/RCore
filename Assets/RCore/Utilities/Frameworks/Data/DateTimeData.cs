/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using UnityEngine;

namespace RCore.Framework.Data
{
    public class DateTimeData : FunData
    {
        private DateTime? mValue;
        private DateTime? mDefaultValue;
        private DateTime? mCompareValue; //If value is changed inside it, like AddDays or AddSeconds
        private bool mChanged;

        public DateTime? Value
        {
            get => mValue;
            set
            {
                if (value != mValue)
                {
                    mValue = value;
                    mCompareValue = value;
                    mChanged = true;
                }
            }
        }

        public DateTimeData(int pId, DateTime? pDefaultValue = null, string pAlias = null) : base(pId, pAlias)
        {
            mDefaultValue = pDefaultValue;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);

            mValue = GetSavedValue();
            mCompareValue = mValue;
        }

        public void AddYears(int pYears)
        {
            if (mValue == null)
                return;
            mValue.Value.AddYears(pYears);
            mChanged = true;
        }

        public void AddMonths(int pMonths)
        {
            if (mValue == null)
                return;
            mValue.Value.AddMonths(pMonths);
            mChanged = true;
        }

        public void AddDays(int pDays)
        {
            if (mValue == null)
                return;
            mValue.Value.AddDays(pDays);
            mChanged = true;
        }

        public void AddHours(int pHours)
        {
            if (mValue == null)
                return;
            mValue.Value.AddHours(pHours);
            mChanged = true;
        }

        public void AddMinutes(int pMinutes)
        {
            if (mValue == null)
                return;
            mValue.Value.AddMinutes(pMinutes);
            mChanged = true;
        }

        public void AddSeconds(int pSeconds)
        {
            if (mValue == null)
                return;
            mValue.Value.AddSeconds(pSeconds);
            mChanged = true;
        }

        public override bool Stage()
        {
            if (mCompareValue != mValue || mChanged)
            {
                SetStringValue(mValue == null ? "" : mValue.Value.ToString());
                mCompareValue = mValue;
                mChanged = false;
                return true;
            }
            return false;
        }

        private DateTime? GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return mDefaultValue;

            var output = DateTime.MinValue;
            if (DateTime.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to int");
                return mDefaultValue;
            }
        }

        public override void Reload()
        {
            base.Reload();
            mValue = GetSavedValue();
            mCompareValue = mValue;
            mChanged = false;
        }

        public override void Reset()
        {
            Value = mDefaultValue;
        }

        public override bool Cleanable()
        {
            if (m_Index != -1 && Value == mDefaultValue)
            {
                return true;
            }
            return false;
        }

        public bool IsNullOrEmpty()
        {
            return Value == null || Value.ToString() == "";
        }
    }
}
