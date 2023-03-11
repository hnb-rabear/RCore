/**
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;

namespace RCore.Components
{
    public class OptimizedScrollItem : MonoBehaviour
    {
        protected int m_Index = -1;

        public virtual void UpdateContent(int pIndex)
        {
            m_Index = pIndex;
        }

        public void ResetIndex() => m_Index = -1;
    }
}
