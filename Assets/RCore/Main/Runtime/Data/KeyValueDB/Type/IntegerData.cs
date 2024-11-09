/***
 * Author RaBear - HNB - 2018
 **/

namespace RCore.Data.KeyValue
{
    public class IntegerData : FunData
    {
        private int m_value;
        private int m_defaultValue;
        private bool m_changed;

        public int Value
        {
            get => m_value;
            set
            {
                if (value != m_value)
                {
                    m_value = value;
                    m_changed = true;
                }
            }
        }

        public IntegerData(int pId, int pDefaultValue = 0) : base(pId)
        {
            m_defaultValue = pDefaultValue;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);
            m_value = GetSavedValue();
        }

        public override bool Stage()
        {
            if (m_changed)
            {
                SetStringValue(m_value.ToString());
                m_changed = false;
                return true;
            }
            return false;
        }

        private int GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return m_defaultValue;

            int output = 0;
            if (int.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to int");
                return m_defaultValue;
            }
        }

        public override void Reload()
        {
            base.Reload();
            m_value = GetSavedValue();
            m_changed = false;
        }

        public override void Reset()
        {
            Value = m_defaultValue;
        }

        public override bool Cleanable()
        {
            if (m_Index != -1 && Value == m_defaultValue)
            {
                return true;
            }
            return false;
        }
    }
}