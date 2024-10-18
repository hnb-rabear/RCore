using RCore.Data.KeyValue;
using System;
using Random = UnityEngine.Random;

namespace RCore.Example.Data.KeyValue
{
    public class ExampleDataMainGroup1 : DataGroup
    {
        public IntegerData integerData;
        public FloatData floatData;
        public LongData longData;
        public StringData stringData;
        public BoolData boolData;
        public DateTimeData dateTimeData;
        public TimedTaskData timedTask;
        public ExampleDataSubGroup subGroup;

        public ExampleDataMainGroup1(int pId) : base(pId)
        {
            integerData = AddData(new IntegerData(0));
            floatData = AddData(new FloatData(1));
            longData = AddData(new LongData(2));
            stringData = AddData(new StringData(3));
            boolData = AddData(new BoolData(4));
            dateTimeData = AddData(new DateTimeData(5));
            subGroup = AddData(new ExampleDataSubGroup(7));
            timedTask = AddData(new TimedTaskData(8));
        }

        public void RandomizeData()
        {
            subGroup.integerData.Value = Random.Range(0, 100);
            subGroup.floatData.Value = Random.Range(0, 100) * 100;
            subGroup.longData.Value = Random.Range(0, 100) * 10000;
            subGroup.stringData.Value = Random.Range(0, 100) + "asd";
            subGroup.boolData.Value = Random.Range(0, 100) > 50;
            subGroup.dateTimeData.Set(DateTime.Now);
        }
    }
}