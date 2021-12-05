/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

#if UNITY_MIRROR
using Mirror;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace RCore.RCM
{
#if UNITY_MIRROR
    public struct DataMessage : NetworkMessage
    {
        public short k;
        public string v;
    }

    public delegate string ServerResponseDelegate(NetworkConnection conn, string data);
#endif
    public class RCM_Server : MonoBehaviour
    {
#if UNITY_MIRROR

        /// <summary>
        /// Invoked when server is initialized
        /// </summary>
        internal event Action StartedEvent;
        internal event Action<NetworkConnection> ClientJoinedEvent;
        internal event Action<NetworkConnection> ClientLeftEvent;

        private Dictionary<short, ServerResponseDelegate> m_ClientMessageHandlers = new Dictionary<short, ServerResponseDelegate>();

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnServerConnect(NetworkConnection conn)
        {
            ClientJoinedEvent?.Invoke(conn);
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnServerDisconnect(NetworkConnection conn)
        {
            ClientLeftEvent?.Invoke(conn);
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnStartServer()
        {
            StartedEvent?.Invoke();
            NetworkServer.RegisterHandler<DataMessage>(HandleMessageFromClient);
        }

        private void HandleMessageFromClient(NetworkConnection conn, DataMessage message)
        {
            if (m_ClientMessageHandlers.ContainsKey(message.k))
            {
                //Data must be string or json string
                var data = m_ClientMessageHandlers[message.k].Invoke(conn, message.v);
                //Send response back to client
                conn.Send(new DataMessage()
                {
                    k = message.k,
                    v = data
                });
            }
        }

        public void RegisterHandler(short opCode, ServerResponseDelegate handler)
        {
            if (!m_ClientMessageHandlers.ContainsKey(opCode))
                m_ClientMessageHandlers.Add(opCode, handler);
            else
                m_ClientMessageHandlers[opCode] = handler;
        }

        public void UnregisterHandler(short opCode)
        {
            if (m_ClientMessageHandlers.ContainsKey(opCode))
                m_ClientMessageHandlers.Remove(opCode);
        }
#endif
    }
}
