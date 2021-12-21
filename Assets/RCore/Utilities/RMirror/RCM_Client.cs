/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

using System.Collections.Generic;
using UnityEngine;
#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
#endif
using System;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace RCore.RCM
{
#if MIRROR
    public struct WrapMessage : NetworkMessage
    {
        public byte k;
        public string v;
    }
#endif

    public class RCM_Client : MonoBehaviour
    {
#if MIRROR
        internal event Action ConnectedEvent;
        internal event Action DisconnectedEvent;

        private Dictionary<byte, Action<string>> m_ServerMessageHandlers = new Dictionary<byte, Action<string>>();

        private ClientServerMsgHandler<ExampleSendMessage1, ExampleResponseMessage1> m_ExampleClientMessageHandler;
        private ClientServerMsgHandler<ExampleSendMessage2, ExampleResponseMessage2> m_ExampleClientMessageHandlerAsync;
        private List<IMsgSender> m_MsgSenders = new List<IMsgSender>();
        private List<byte> m_ProcessingOperations = new List<byte>();

        private void Start()
        {
#if UNITY_EDITOR
            //TEST
            RegisterMessageHandler(103, HandleServerTestMessage103);

            m_ExampleClientMessageHandler = new ClientServerMsgHandler<ExampleSendMessage1, ExampleResponseMessage1>();
            m_ExampleClientMessageHandler.ServerRegisterHandler(HandleExampleClientMessage);

            m_ExampleClientMessageHandlerAsync = new ClientServerMsgHandler<ExampleSendMessage2, ExampleResponseMessage2>(true, true);
            m_ExampleClientMessageHandlerAsync.ServerRegisterHandlerAsync(HandleExampleClientMessageAsync);
#endif
        }

#if DEVELOPMENT && !UNITY_SERVER
        public int GUIStartX = 100;
        public int GUIStartY = 100;
        private void OnGUI()
        {
            if (NetworkClient.isConnected)
            {
                GUILayout.BeginArea(new Rect(GUIStartX, GUIStartY, 300, 300));

                bool waitingForResponse = WaitingResponse();
                GUI.color = waitingForResponse ? Color.yellow : Color.green;
                GUILayout.Label($"Waiting for response: {waitingForResponse}");

                GUI.color = BlockingInput() ? Color.red : Color.green;
                GUILayout.Label($"Blocking input: {BlockingInput()}");

                GUILayout.EndArea();
            }
        }
#endif

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnClientConnect(NetworkConnection conn)
        {
            ConnectedEvent?.Invoke();
            m_MsgSenders = new List<IMsgSender>();
            m_ProcessingOperations = new List<byte>();
            NetworkClient.RegisterHandler<ServerMessage>(HandleServerMessage, false);
        }

        /// <summary>
        /// Should be called from <see cref="NetworkManager"/>
        /// </summary>
        internal void OnClientDisconnect(NetworkConnection conn)
        {
            m_MsgSenders = new List<IMsgSender>();
            m_ProcessingOperations = new List<byte>();
            DisconnectedEvent?.Invoke();
        }

        public void Send<T>(byte opCode, T message, Action<string> svResponseCallback) where T : struct
        {
            RegisterMessageHandler(opCode, svResponseCallback);
            var dataMessage = new WrapMessage()
            {
                k = opCode,
                v = JsonConvert.SerializeObject(message)
            };
            if (!m_ProcessingOperations.Contains(opCode))
                m_ProcessingOperations.Add(opCode);
            NetworkClient.Send(dataMessage);
        }

        public void Send(byte opCode, string message, Action<string> svResponseCallback)
        {
            RegisterMessageHandler(opCode, svResponseCallback);
            var dataMessage = new WrapMessage()
            {
                k = opCode,
                v = message
            };
            if (!m_ProcessingOperations.Contains(opCode))
                m_ProcessingOperations.Add(opCode);
            NetworkClient.Send(dataMessage);
        }

        private void RegisterMessageHandler(byte opCode, Action<string> svResponseCallback)
        {
            if (svResponseCallback == null)
                return;
            if (!m_ServerMessageHandlers.ContainsKey(opCode))
                m_ServerMessageHandlers.Add(opCode, svResponseCallback);
            else
                m_ServerMessageHandlers[opCode] = svResponseCallback;
        }

        private void HandleServerMessage(ServerMessage message)
        {
            if (m_ServerMessageHandlers.ContainsKey(message.k))
            {
                m_ServerMessageHandlers[message.k].Invoke(message.v);
                m_ServerMessageHandlers.Remove(message.k);
                m_ProcessingOperations.Remove(message.k);
            }
        }

        /// <summary>
        /// TEST
        /// </summary>
        private void HandleServerTestMessage103(string svMessage)
        {
            Debug.Log($"Server message: {svMessage}");
        }

        /// <summary>
        /// TEST
        /// </summary>
        private async UniTask<ExampleResponseMessage2> HandleExampleClientMessageAsync(NetworkConnection conn, ExampleSendMessage2 clientMsg)
        {
            await UniTask.DelayFrame(180);
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMessage2
            {
                value = $"Hello Client ({clientMsg.index})"
            };
            return result;
        }

        /// <summary>
        /// TEST
        /// </summary>
        private ExampleResponseMessage1 HandleExampleClientMessage(NetworkConnection conn, ExampleSendMessage1 clientMsg)
        {
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMessage1
            {
                value = $"Hello Client ({clientMsg.index})"
            };
            return result;
        }

        public void TrackMsgSender(IMsgSender pHandler)
        {
            if (!m_MsgSenders.Contains(pHandler))
                m_MsgSenders.Add(pHandler);
        }

        public bool BlockingInput()
        {
            for (int i = 0; i < m_MsgSenders.Count; i++)
                if (m_MsgSenders[i].WaitForResponse && m_MsgSenders[i].BlockInputWhileWait)
                    return true;
            return m_ProcessingOperations.Count > 0;
        }

        public bool WaitingResponse()
        {
            for (int i = 0; i < m_MsgSenders.Count; i++)
                if (m_MsgSenders[i].WaitForResponse)
                    return true;
            return m_ProcessingOperations.Count > 0;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RCM_Client))]
        public class RCM_ClientEditor : Editor
        {
            private RCM_Client m_Script;

            private void OnEnable()
            {
                m_Script = target as RCM_Client;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (GUILayout.Button("Send Hello"))
                    RCM.Client.Send(101, "Hello", svReponse =>
                    {
                        Debug.Log($"[CLIENT] Server response: {svReponse}");
                    });

                if (GUILayout.Button("Send Hello Async"))
                    RCM.Client.Send(102, "Hello Async", svReponse =>
                    {
                        Debug.Log($"[CLIENT] Server response: {svReponse}");
                    });

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                if (GUILayout.Button("Send Example Message"))
                {
                    m_Script.m_ExampleClientMessageHandler.ClientSend(new ExampleSendMessage1()
                    {
                        value = "Hello Server",
                    }, svResponse =>
                    {
                        Debug.Log($"Server response message: {svResponse.value}");
                    });
                }
                if (GUILayout.Button("Send Example Message Async"))
                {
                    m_Script.m_ExampleClientMessageHandlerAsync.ClientSend(new ExampleSendMessage2()
                    {
                        value = "Hello Server",
                    }, svResponse =>
                    {
                        Debug.Log($"Server response message: {svResponse.value}");
                    });
                }
                if (GUILayout.Button("Send Multi Example Message"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int index = i;
                        m_Script.m_ExampleClientMessageHandlerAsync.ClientSend(new ExampleSendMessage2()
                        {
                            index = index,
                            value = $"Hello Server ({index})",
                        }, svResponse =>
                        {
                            Debug.Log($"Server response message: {svResponse.value}");
                        });
                    }
                }
            }
        }
#endif
        public struct ExampleSendMessage1 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleResponseMessage1 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleSendMessage2 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleResponseMessage2 : NetworkMessage
        {
            public int index;
            public string value;
        }
#endif
    }
}
