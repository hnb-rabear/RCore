﻿/***
 * Author HNB-RaBear - 2017
 **/

 using UnityEngine.UI;

namespace RCore.UI
{
    public class OptimizedScrollItemTest : OptimizedScrollItem
    {
        public Text mTxtIndex;

		protected override void OnUpdateContent()
		{
			name = m_index.ToString();
			mTxtIndex.text = m_index.ToString();
		}
	}
}