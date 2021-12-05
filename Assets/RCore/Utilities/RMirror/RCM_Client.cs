/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if MIRROR
using Mirror;
#endif
using System;
using Newtonsoft.Json;

namespace RCore.RCM
{
    public class RCM_Client : MonoBehaviour
    {
#if MIRROR
        internal event Action ConnectedEvent;
        internal event Action DisconnectedEvent;

        internal NetworkConnection Connection { get; private set; }

        private Dictionary<short, Action<string>> m_ServerMessageHandlers = new Dictionary<short, Action<string>>();

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnClientConnect(NetworkConnection conn)
        {
            Connection = conn;
            ConnectedEvent?.Invoke();
            NetworkClient.RegisterHandler<DataMessage>(HandleMessageFromServer, false);
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnClientDisconnect(NetworkConnection conn)
        {
            Connection = null;
            DisconnectedEvent?.Invoke();
        }

        public void Send<T>(short opCode, T message, Action<string> svResponseCallback) where T : struct
        {
            RegisterMessageHandler(opCode, svResponseCallback);
            var dataMessage = new DataMessage()
            {
                k = opCode,
                v = JsonConvert.SerializeObject(message)
            };
            Connection.Send(dataMessage);
        }

        public void Send(short opCode, string message, Action<string> svResponseCallback)
        {
            RegisterMessageHandler(opCode, svResponseCallback);
            var dataMessage = new DataMessage()
            {
                k = opCode,
                v = message
            };
            Connection.Send(dataMessage);
        }

        private void RegisterMessageHandler(short opCode, Action<string> svResponseCallback)
        {
            if (svResponseCallback == null)
                return;
            if (!m_ServerMessageHandlers.ContainsKey(opCode))
                m_ServerMessageHandlers.Add(opCode, svResponseCallback);
            else
                m_ServerMessageHandlers[opCode] = svResponseCallback;
        }

        private void HandleMessageFromServer(DataMessage message)
        {
            if (m_ServerMessageHandlers.ContainsKey(message.k))
            {
                m_ServerMessageHandlers[message.k].Invoke(message.v);
                m_ServerMessageHandlers.Remove(message.k);
            }
        }
#endif
    }
}
