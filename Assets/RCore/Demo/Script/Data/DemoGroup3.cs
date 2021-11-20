using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Pattern.Data;
using Random = UnityEngine.Random;

namespace RCore.Demo
{
    public class DemoGroup3 : DataGroup
    {
        public BoolData toggleIsOn;
        public StringData inputFieldText;
        public FloatData progressBarValue;
        public IntegerData tabIndex;

        public DemoGroup3(int pId) : base(pId)
        {
            toggleIsOn = AddData(new BoolData(0));
            inputFieldText = AddData(new StringData(1));
            progressBarValue = AddData(new FloatData(2));
            tabIndex = AddData(new IntegerData(4));
        }
    }
}