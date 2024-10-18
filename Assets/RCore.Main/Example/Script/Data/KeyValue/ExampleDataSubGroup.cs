using RCore.Data.KeyValue;

namespace RCore.Example.Data.KeyValue
{
    public class ExampleDataSubGroup : DataGroup
    {
        public IntegerData integerData;
        public FloatData floatData;
        public LongData longData;
        public StringData stringData;
        public BoolData boolData;
        public DateTimeData dateTimeData;

        public ExampleDataSubGroup(int pId) : base(pId)
        {
            integerData = AddData(new IntegerData(0));
            floatData = AddData(new FloatData(1));
            longData = AddData(new LongData(2));
            stringData = AddData(new StringData(3));
            boolData = AddData(new BoolData(4));
            dateTimeData = AddData(new DateTimeData(5));
        }
    }
}