/***
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace RCore.RCM
{
    public interface IMirrorServerListener
    {
        void OnStartServer();
        void OnStopServer();
    }
    public struct ServerMessage : NetworkMessage
    {
        public byte k;
        public string v;
    }
    public delegate string ServerResponseDelegate(NetworkConnection conn, string data);
    public delegate UniTask<string> ServerResponseDelegateAsync(NetworkConnection conn, string data);
    
    public class RCM_Server : MonoBehaviour
    {
        private static RCM_Server m_Instance;
        private Dictionary<byte, ServerResponseDelegate> m_ClientMessageHandlers;
        private Dictionary<byte, ServerResponseDelegateAsync> m_ClientMessageHandlersAsync;
        private List<IMirrorServerListener> m_Listeners;
       
        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        public void Init()
        {
            m_ClientMessageHandlers = new Dictionary<byte, ServerResponseDelegate>();
            m_ClientMessageHandlersAsync = new Dictionary<byte, ServerResponseDelegateAsync>();
            m_Listeners = new List<IMirrorServerListener>();
        }

        public void OnStartServer()
        {
            NetworkServer.RegisterHandler<WrapMessage>(HandleClientMessage);

#if UNITY_EDITOR
            //TEST
            RegisterHandler(101, HandleClientTestMessage101);
            RegisterHandlerAsync(102, HandleClientTestMessage102);
#endif

            foreach (var listener in m_Listeners)
                listener.OnStartServer();
        }

        public void OnStopServer()
        {
            foreach (var listener in m_Listeners)
                listener.OnStopServer();
        }

        public void SendToAll<T>(byte opCode, T pMessage) where T : struct
        {
            NetworkServer.SendToAll(new ServerMessage()
            {
                k = opCode,
                v = JsonConvert.SerializeObject(pMessage)
            });
        }

        public void SendToAll(byte opCode, string data)
        {
            NetworkServer.SendToAll(new ServerMessage()
            {
                k = opCode,
                v = data
            });
        }

        public void Send<T>(NetworkConnection conn, byte opCode, T pMessage) where T : struct
        {
            conn.Send(new ServerMessage()
            {
                k = opCode,
                v = JsonConvert.SerializeObject(pMessage)
            });
        }

        public void Send(NetworkConnection conn, byte opCode, string data)
        {
            conn.Send(new ServerMessage()
            {
                k = opCode,
                v = data
            });
        }

        private async void HandleClientMessage(NetworkConnection conn, WrapMessage message)
        {
            if (m_ClientMessageHandlers.ContainsKey(message.k))
            {
                //Data must be string or json string
                var data = m_ClientMessageHandlers[message.k].Invoke(conn, message.v);
                //Send response back to client
                Send(conn, message.k, data);
            }
            if (m_ClientMessageHandlersAsync.ContainsKey(message.k))
            {
                //Data must be string or json string
                var data = await m_ClientMessageHandlersAsync[message.k].Invoke(conn, message.v);
                //Send response back to client
                Send(conn, message.k, data);
            }
        }

        public void RegisterHandler(byte opCode, ServerResponseDelegate handler)
        {
            if (!m_ClientMessageHandlers.ContainsKey(opCode))
                m_ClientMessageHandlers.Add(opCode, handler);
            else
                m_ClientMessageHandlers[opCode] = handler;
        }

        public void RegisterHandlerAsync(byte opCode, ServerResponseDelegateAsync handlerAsync)
        {
            if (!m_ClientMessageHandlersAsync.ContainsKey(opCode))
                m_ClientMessageHandlersAsync.Add(opCode, handlerAsync);
            else
                m_ClientMessageHandlersAsync[opCode] = handlerAsync;
        }

        public void UnregisterHandler(byte opCode)
        {
            if (m_ClientMessageHandlers.ContainsKey(opCode))
                m_ClientMessageHandlers.Remove(opCode);
            if (m_ClientMessageHandlersAsync.ContainsKey(opCode))
                m_ClientMessageHandlersAsync.Remove(opCode);
        }

        public void Register<T>(T pListener) where T : IMirrorServerListener
        {
            if (!m_Listeners.Contains(pListener))
            {
                m_Listeners.Add(pListener);
                if (NetworkServer.active)
                    pListener.OnStartServer();
            }
        }

        public void UnRegister<T>(T pListener) where T : IMirrorServerListener
        {
            m_Listeners.Remove(pListener);
        }

#if UNITY_EDITOR
        /// <summary>
        /// TEST
        /// </summary>
        private async UniTask<string> HandleClientTestMessage102(NetworkConnection conn, string data)
        {
            Debug.Log($"[SERVER] Client to server: {data}");
            await UniTask.DelayFrame(200);
            return "I'll kill you Client asynchronously!";
        }

        /// <summary>
        /// TEST
        /// </summary>
        private string HandleClientTestMessage101(NetworkConnection conn, string data)
        {
            Debug.Log($"[SERVER] Client to server: {data}");
            return "I'll kill you Client!";
        }

        [CustomEditor(typeof(RCM_Server))]
        public class RCM_ServerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (GUILayout.Button("Hello to all client!"))
                {
                    RCM.Server.SendToAll(103, "Hello to all clients");
                }
            }
        }
#endif
    }
}
#endif