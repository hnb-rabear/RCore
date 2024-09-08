/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using UnityEngine;

namespace RCore.Framework.Data
{
    public class FloatData : FunData
    {
        //private ObscuredFloat mValue;
        private float mValue;
        private float mDefaultValue;
        private bool mChanged;

        public float Value
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

        public FloatData(int pId, float pDefaultValue = 0, string pAlias = null) : base(pId, pAlias)
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

        private float GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return mDefaultValue;

            float output = 0;
            if (float.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to float");
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
