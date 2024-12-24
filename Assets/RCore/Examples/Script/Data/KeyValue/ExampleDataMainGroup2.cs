using RCore.Data.KeyValue;

namespace RCore.Example.Data.KeyValue
{
    public class ExampleDataMainGroup2 : DataGroup
    {
        public BoolData toggleIsOn;
        public StringData inputFieldText;
        public FloatData progressBarValue;
        public IntegerData tabIndex;

        public ExampleDataMainGroup2(int pId) : base(pId)
        {
            toggleIsOn = AddData(new BoolData(0));
            inputFieldText = AddData(new StringData(1));
            progressBarValue = AddData(new FloatData(2));
            tabIndex = AddData(new IntegerData(4));
        }
    }
}