/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using UnityEngine;

namespace RCore.Framework.Data
{
    public class BoolData : FunData
    {
        private bool mValue;
        private bool mDefaultValue;
        private bool mChanged;

        public bool Value
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

        public BoolData(int pId, bool pDefaultValue = false, string pAlias = null) : base(pId, pAlias)
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

        private bool GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return mDefaultValue;

            bool output = false;
            if (bool.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to bool");
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