/**
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

 using UnityEngine.UI;

namespace RCore.Components
{
    public class OptimizedScrollItemTest : OptimizedScrollItem
    {
        public Text mTxtIndex;

        public override void UpdateContent(int pIndex)
        {
            base.UpdateContent(pIndex);

            name = pIndex.ToString();
            mTxtIndex.text = pIndex.ToString();
        }
    }
}