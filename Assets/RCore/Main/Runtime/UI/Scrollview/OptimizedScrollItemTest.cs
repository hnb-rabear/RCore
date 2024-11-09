/***
 * Author RaBear - HNB - 2017
 **/

 using UnityEngine.UI;

namespace RCore.UI
{
    public class OptimizedScrollItemTest : OptimizedScrollItem
    {
        public Text mTxtIndex;

		protected override void OnUpdateContent()
		{
			name = m_Index.ToString();
			mTxtIndex.text = m_Index.ToString();
		}
	}
}