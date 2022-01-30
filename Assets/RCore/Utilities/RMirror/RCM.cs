/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

#if MIRROR
using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using RCore.Common;

namespace RCore.RCM
{
    public class RCM
    {
        private static GameObject m_RCMObject;
        private static RCM_Client m_Client;
        private static RCM_Server m_Server;

        public static RCM_Client Client => m_Client;
        public static RCM_Server Server => m_Server;

        private static Dictionary<int, List<ServerResponseHistory>> m_ResponseHistories = new Dictionary<int, List<ServerResponseHistory>>();
        private static List<IClientServerMsgHandler> m_ClientServerMsgHandlers;

        static RCM()
        {
            m_RCMObject = new GameObject("RCM");
            m_Client = m_RCMObject.AddComponent<RCM_Client>();
            m_Client.Init();
            m_Server = m_RCMObject.AddComponent<RCM_Server>();
            m_Server.Init();
            m_ClientServerMsgHandlers = new List<IClientServerMsgHandler>();
            GameObject.DontDestroyOnLoad(m_RCMObject);
        }

        public static ClientServerMsgHandler<ClientMsgT, ResponseMsgT> AddClientServerMsgHandler<ClientMsgT, ResponseMsgT>(bool pRequireAuthentication = true, bool pBlockInputWhileWait = true, bool pSingleMessage = true)
            where ClientMsgT : struct, NetworkMessage
            where ResponseMsgT : struct, NetworkMessage
        {
            var handler = new ClientServerMsgHandler<ClientMsgT, ResponseMsgT>(pRequireAuthentication, pBlockInputWhileWait, pSingleMessage);
            m_ClientServerMsgHandlers.Add(handler);
            return handler;
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStartServer()
        {
            foreach (var handler in m_ClientServerMsgHandlers)
                handler.OnStartServer();
            m_Server.OnStartServer();
            m_ResponseHistories = new Dictionary<int, List<ServerResponseHistory>>();
            //NetworkServer.RegisterHandler<ConfirmMsgFromClient>(HandleConfirmMsgFromClient);
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStopServer()
        {
            foreach (var handler in m_ClientServerMsgHandlers)
                handler.OnStopServer();
            m_Server.OnStopServer();

            //NetworkServer.UnregisterHandler<ConfirmMsgFromClient>();
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        public static void OnStartClient()
        {
            foreach (var handler in m_ClientServerMsgHandlers)
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
            foreach (var handler in m_ClientServerMsgHandlers)
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


        /// <summary>
        /// Server received a Confirmation Message from Client about success message
        /// </summary>
        //private static void HandleConfirmMsgFromClient(NetworkConnection conn, ConfirmMsgFromClient msg)
        //{
        //    if (m_ResponseHistories.ContainsKey(conn.connectionId))
        //    {
        //        var histories = m_ResponseHistories[conn.connectionId];
        //        for (int i = histories.Count - 1; i >= 0; i--)
        //            if (histories[i].clientMsgId == msg.clientMsgId)
        //                histories.RemoveAt(i);
        //    }
        //}

        /// <summary>
        /// Server save the last response which just sent to client
        /// </summary>
        public static void SaveResponseHistory(int connectionId, ServerResponseHistory history)
        {
            if (m_ResponseHistories.ContainsKey(connectionId))
                m_ResponseHistories[connectionId].Add(history);
            else
                m_ResponseHistories.Add(connectionId, new List<ServerResponseHistory>() { history });
        }

        public static void ResendLostMessages(NetworkConnection conn, int prevConnectionId)
        {
            //if (m_ResponseHistories.ContainsKey(prevConnectionId))
            //{
            //    var histories = m_ResponseHistories[prevConnectionId];
            //    foreach (var history in histories)
            //        conn.Send(history.responseMsg);
            //}
        }
    }

    public struct ServerResponseHistory
    {
        public int clientMsgId;
        public NetworkMessage responseMsg;
    }
}
#endif