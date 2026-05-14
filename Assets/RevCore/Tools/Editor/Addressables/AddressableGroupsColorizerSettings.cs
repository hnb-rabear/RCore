#if ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class AddressableGroupsColorizerSettings : ScriptableObject
    {
        public bool enabled = true;
        public List<Rule> rules = new()
        {
            new Rule { prefix = "In", color = Color.green },
            new Rule { prefix = "Fa", color = Color.blue },
            new Rule { prefix = "On", color = Color.cyan },
            new Rule { prefix = "Ex", color = Color.red }
        };

        [Serializable]
        public class Rule
        {
            public string prefix;
            public Color color;
        }
    }
}
#endif
