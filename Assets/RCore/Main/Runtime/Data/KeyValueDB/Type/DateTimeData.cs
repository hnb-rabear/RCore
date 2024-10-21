/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;

namespace RCore.Data.KeyValue
{
    public class DateTimeData : FunData
    {
        private int m_value;
        private int m_defaultValue;
        private bool m_changed;

        public DateTimeData(int pId, DateTime? pDefaultValue = null) : base(pId)
        {
            if (pDefaultValue != null)
                m_defaultValue = TimeHelper.DateTimeToUnixTimestampInt(pDefaultValue.Value);
        }

        public DateTime Get() => m_value > 0 ? TimeHelper.UnixTimestampToDateTime(m_value) : default;

        public void Set(DateTime time)
        {
            int timestamp = time.ToUnixTimestampInt();
            Set(timestamp);
        }
        
        public void Set(int timestamp)
        {
            if (timestamp == m_value)
                return;
            m_value = timestamp;
            m_changed = true;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);

            m_value = GetSavedValue();
        }

        public void AddYears(int pYears)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddYears(pYears).ToUnixTimestampInt();
            m_changed = true;
        }

        public void AddMonths(int pMonths)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddMonths(pMonths).ToUnixTimestampInt();
            m_changed = true;
        }

        public void AddDays(int pDays)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddDays(pDays).ToUnixTimestampInt();
            m_changed = true;
        }

        public void AddHours(int pHours)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddHours(pHours).ToUnixTimestampInt();
            m_changed = true;
        }

        public void AddMinutes(int pMinutes)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddMinutes(pMinutes).ToUnixTimestampInt();
            m_changed = true;
        }

        public void AddSeconds(int pSeconds)
        {
            m_value = TimeHelper.UnixTimestampToDateTime(m_value).AddSeconds(pSeconds).ToUnixTimestampInt();
            m_changed = true;
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
            if (int.TryParse(val, out int output))
                return output;
            Debug.LogError($"can not parse key {m_Key} with value {val}");
            return m_defaultValue;
        }

        public override void Reload()
        {
            base.Reload();
            m_value = GetSavedValue();
            m_changed = false;
        }

        public override void Reset()
        {
            m_value = m_defaultValue;
        }

        public override bool Cleanable()
        {
            return m_Index != -1 && m_value == m_defaultValue;
        }

        public bool IsNullOrEmpty()
        {
            return m_value == 0;
        }
    }
}