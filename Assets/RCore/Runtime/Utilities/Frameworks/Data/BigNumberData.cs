/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

namespace RCore.Framework.Data
{
    public class BigNumberData : DataGroup
    {
        private FloatData mReadable;
        private IntegerData mPow;

        public Common.BigNumberAlpha Value
        {
            get => new(mReadable.Value, mPow.Value);
            set
            {
                mReadable.Value = value.readableValue;
                mPow.Value = value.pow;
            }
        }

        public BigNumberData(int pId, Common.BigNumberAlpha pDefaultNumber = null) : base(pId)
        {
            mReadable = AddData(new FloatData(0, pDefaultNumber == null ? 0 : pDefaultNumber.readableValue));
            mPow = AddData(new IntegerData(1, pDefaultNumber == null ? 0 : pDefaultNumber.pow));
        }
    }
}