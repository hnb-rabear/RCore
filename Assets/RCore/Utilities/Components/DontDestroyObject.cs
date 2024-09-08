/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using UnityEngine;

namespace RCore.Components
{
    public class DontDestroyObject : MonoBehaviour
    {
        private static DontDestroyObject m_Instance;

        private void Start()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (m_Instance != this)
                Destroy(gameObject);
        }
    }
}