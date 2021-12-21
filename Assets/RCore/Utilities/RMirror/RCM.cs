/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

#if MIRROR
using Mirror;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace RCore.RCM
{
    public class RCM
    {
        private static GameObject m_RCMObject;
        private static RCM_Client m_Client;
        private static RCM_Server m_Server;

        public static RCM_Client Client => m_Client;
        public static RCM_Server Server => m_Server;

        static RCM()
        {
            m_RCMObject = new GameObject("RCM");
            m_Client = m_RCMObject.AddComponent<RCM_Client>();
            m_Server = m_RCMObject.AddComponent<RCM_Server>();
            GameObject.DontDestroyOnLoad(m_RCMObject);
        }
    }
}
