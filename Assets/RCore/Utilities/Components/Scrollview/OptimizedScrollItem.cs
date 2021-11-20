/**
 * Author NBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;

namespace RCore.Components
{
    public class OptimizedScrollItem : MonoBehaviour
    {
        protected int mIndex = -1;

        public virtual void UpdateContent(int pIndex)
        {
            mIndex = pIndex;
        }
    }
}
