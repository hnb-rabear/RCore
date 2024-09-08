/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using UnityEngine;

namespace RCore.Framework.Data
{
    public class LongData : FunData
    {
        //private ObscuredLong mValue;
        private long mValue;
        private long mDefaultValue;
        private bool mChanged;

        public long Value
        {
            get => mValue;
            set
            {
                if (value != mValue)
                {
                    mValue = value;
                    mChanged = true;
                }
            }
        }

        public LongData(int pId, long pDefaultValue = 0, string pAlias = null) : base(pId, pAlias)
        {
            mDefaultValue = pDefaultValue;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);
            mValue = GetSavedValue();
        }

        public override bool Stage()
        {
            if (mChanged)
            {
                SetStringValue(mValue.ToString());
                mChanged = false;
                return true;
            }
            return false;
        }

        private long GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return mDefaultValue;

            long output = 0;
            if (long.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to long");
                return mDefaultValue;
            }
        }

        public override void Reload()
        {
            base.Reload();
            mValue = GetSavedValue();
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
    }
}