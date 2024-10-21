/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using System.Collections;
using UnityEngine;

namespace RCore
{
    /// <summary>
    /// Place gameObjects which never be destroyed as children of this gameObject
    /// </summary>
    public class DontDestroyedGroup : MonoBehaviour
    {
        private static DontDestroyedGroup m_Instance;

        private IEnumerator Start()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
            yield return null;
            if (transform.childCount > 0)
                DontDestroyOnLoad(gameObject);
            else
                Destroy(gameObject);
        }
    }
}