/***
 * Author HNB-RaBear - 2021
 **/

#if MIRROR
using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using RCore;

namespace RCore.RCM
{
    public class RCM
    {
        private static GameObject m_RCMObject;
        private static RCM_Client m_Client;
        private static RCM_Server m_Server;

        public static RCM_Client Client => m_Client;
        public static RCM_Server Server => m_Server;

        private static List<IRCM_MsgHandler> m_MsgHandlers;

        static RCM()
        {
            m_RCMObject = new GameObject("RCM");
            m_Client = m_RCMObject.AddComponent<RCM_Client>();
            m_Client.Init();
            m_Server = m_RCMObject.AddComponent<RCM_Server>();
            m_Server.Init();
            m_MsgHandlers = new List<IRCM_MsgHandler>();
            GameObject.DontDestroyOnLoad(m_RCMObject);
        }

        /// <summary>
        /// Client send and server response
        /// </summary>
        /// <typeparam name="ClientMsgT">Message client send</typeparam>
        /// <typeparam name="ResponseMsgT">Message server response</typeparam>
        /// <returns></returns>
        public static ClientServerMsgHandler<ClientMsgT, ResponseMsgT> AddClientServerMsgHandler<ClientMsgT, ResponseMsgT>(bool pRequireAuthentication = true, bool pBlockInputWhileWait = true, bool pSingleMessage = true)
            where ClientMsgT : struct, NetworkMessage
            where ResponseMsgT : struct, NetworkMessage
        {
            var handler = new ClientServerMsgHandler<ClientMsgT, ResponseMsgT>(pRequireAuthentication, pBlockInputWhileWait, pSingleMessage);
            m_MsgHandlers.Add(handler);
            return handler;
        }

        /// <summary>
        /// Server send and client response
        /// </summary>
        /// <typeparam name="ServerMsgT">Message server send</typeparam>
        /// <typeparam name="ResponseMsgT">Message client response</typeparam>
        /// <returns></returns>
        public static ServerClientMsgHandler<ServerMsgT, ResponseMsgT> AddServerClientMsgHandler<ServerMsgT, ResponseMsgT>()
            where ServerMsgT : struct, NetworkMessage
            where ResponseMsgT : struct, NetworkMessage
        {
            var handler = new ServerClientMsgHandler<ServerMsgT, ResponseMsgT>();
            m_MsgHandlers.Add(handler);
            return handler;
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStartServer()
        {
            foreach (var handler in m_MsgHandlers)
                handler.OnStartServer();
            m_Server.OnStartServer();
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStopServer()
        {
            foreach (var handler in m_MsgHandlers)
                handler.OnStopServer();
            m_Server.OnStopServer();
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStartClient()
        {
            foreach (var handler in m_MsgHandlers)
                handler.OnStartClient();
            m_Client.OnStartClient();
        }

        public static void OnClientConnect()
        {
            m_Client.OnClientConnect();
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStopClient()
        {
            foreach (var handler in m_MsgHandlers)
                handler.OnStopClient();
            m_Client.OnStopClient();
        }

        public static void AddClientListener<T>(T pListener) where T : IMirrorClientListener
        {
            m_Client.Register(pListener);
        }
        public static void RemoveClientListener<T>(T pListener) where T : IMirrorClientListener
        {
            m_Client.UnRegister(pListener);
        }
        public static void AddServerListener<T>(T pListener) where T : IMirrorServerListener
        {
            m_Server.Register(pListener);
        }
        public static void RemoveServerListener<T>(T pListener) where T : IMirrorServerListener
        {
            m_Server.UnRegister(pListener);
        }
    }

    public struct ServerResponseHistory
    {
        public int clientMsgId;
        public NetworkMessage responseMsg;
    }
}
#endif