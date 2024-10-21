/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

namespace RCore.Data.KeyValue
{
    public class BoolData : FunData
    {
        private bool m_value;
        private bool m_defaultValue;
        private bool m_changed;

        public bool Value
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

        public BoolData(int pId, bool pDefaultValue = false) : base(pId)
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

        private bool GetSavedValue()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return m_defaultValue;

            bool output = false;
            if (bool.TryParse(val, out output))
            {
                return output;
            }
            else
            {
                Debug.LogError("can not parse key " + m_Key + " with value " + val + " to bool");
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