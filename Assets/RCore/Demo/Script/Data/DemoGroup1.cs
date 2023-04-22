using System;
using RCore.Framework.Data;
using Random = UnityEngine.Random;

namespace RCore.Demo
{
    public class DemoGroup1 : DataGroup
    {
        public IntegerData intergerdata;
        public FloatData floatData;
        public LongData longData;
        public StringData stringData;
        public BoolData boolData;
        public DateTimeData dateTimeData;
        public TimeCounterData timeCouterData;
        public TimerTask timerTask;
        public DemoGroup2 subGroup;

        public DemoGroup1(int pId) : base(pId)
        {
            intergerdata = AddData(new IntegerData(0));
            floatData = AddData(new FloatData(1));
            longData = AddData(new LongData(2));
            stringData = AddData(new StringData(3));
            boolData = AddData(new BoolData(4));
            dateTimeData = AddData(new DateTimeData(5));
            timeCouterData = AddData(new TimeCounterData(6));
            subGroup = AddData(new DemoGroup2(7));
            timerTask = AddData(new TimerTask(8));
        }

        public void RandomizeData()
        {
            subGroup.intergerdata.Value = Random.Range(0, 100);
            subGroup.floatData.Value = Random.Range(0, 100) * 100;
            subGroup.longData.Value = Random.Range(0, 100) * 10000;
            subGroup.stringData.Value = Random.Range(0, 100).ToString() + "asd";
            subGroup.boolData.Value = Random.Range(0, 100) > 50;
            subGroup.dateTimeData.Value = DateTime.Now;
        }
    }
}