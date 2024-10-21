/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

namespace RCore.Data.KeyValue
{
    public class BigNumberData : DataGroup
    {
        private FloatData m_readable;
        private IntegerData m_pow;

        public BigNumberF Value
        {
            get => new(m_readable.Value, m_pow.Value);
            set
            {
                m_readable.Value = value.readableValue;
                m_pow.Value = value.pow;
            }
        }

        public BigNumberData(int pId, BigNumberF pDefaultNumber = null) : base(pId)
        {
            m_readable = AddData(new FloatData(0, pDefaultNumber?.readableValue ?? 0));
            m_pow = AddData(new IntegerData(1, pDefaultNumber?.pow ?? 0));
        }
    }
}