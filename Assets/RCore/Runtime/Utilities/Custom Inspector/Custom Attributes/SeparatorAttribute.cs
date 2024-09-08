﻿/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using UnityEngine;

namespace RCore.Inspector
{
    public class SeparatorAttribute : PropertyAttribute
    {
        public readonly string title;

        public SeparatorAttribute()
        {
            title = "";
        }

        public SeparatorAttribute(string _title)
        {
            title = _title;
        }
    }
}