/**
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using UnityEngine;
using System.Collections;

namespace RCore.Inspector
{
    public class SeparatorAttribute : PropertyAttribute
    {
        public readonly string title;

        public SeparatorAttribute()
        {
            this.title = "";
        }

        public SeparatorAttribute(string _title)
        {
            this.title = _title;
        }
    }
}