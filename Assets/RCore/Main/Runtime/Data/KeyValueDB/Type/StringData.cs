/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

namespace RCore.Data.KeyValue
{
    public class StringData : FunData
    {
        private string m_value;
        private string m_defaultValue;
        private bool m_changed;

        public string Value
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

        public StringData(int pId, string pDefaultValue = "") : base(pId)
        {
            m_defaultValue = pDefaultValue;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);

            m_value = GetSavedValue();
            if (m_value == null)
                m_value = m_defaultValue;
        }

        public override bool Stage()
        {
            if (m_changed)
            {
                SetStringValue(m_value);
                m_changed = false;
                return true;
            }
            return false;
        }

        private string GetSavedValue()
        {
            return GetStringValue();
        }

        public override void Reload()
        {
            base.Reload();
            Value = GetSavedValue();
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