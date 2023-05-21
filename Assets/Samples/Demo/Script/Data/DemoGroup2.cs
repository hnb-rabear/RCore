using RCore.Framework.Data;

namespace RCore.Demo
{
    public class DemoGroup2 : DataGroup
    {
        public IntegerData intergerdata;
        public FloatData floatData;
        public LongData longData;
        public StringData stringData;
        public BoolData boolData;
        public DateTimeData dateTimeData;

        public DemoGroup2(int pId) : base(pId)
        {
            intergerdata = AddData(new IntegerData(0));
            floatData = AddData(new FloatData(1));
            longData = AddData(new LongData(2));
            stringData = AddData(new StringData(3));
            boolData = AddData(new BoolData(4));
            dateTimeData = AddData(new DateTimeData(5));
        }
    }
}