/**
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/

using System.Collections.Generic;
using UnityEngine;
#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
#endif
using System;
using Random = UnityEngine.Random;
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
        private List<IMsgSender> m_MsgSenders = new List<IMsgSender>();
        private List<byte> m_ProcessingOperations = new List<byte>();

        private void Start()
        {
#if UNITY_EDITOR
            InitTestHandlers();
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

        //=======================================================================================================

#if UNITY_EDITOR
        #region Examples

        private ClientServerMsgHandler<ExampleSendMsg1, ExampleResponseMsg1> m_ExampleClientMsgHandler;
        private ClientServerMsgHandler<ExampleSendMsg2, ExampleResponseMsg2> m_ExampleClientMsgHandlerAsync;
        private ClientServerMsgHandler<ExampleSendMsg3, ExampleResponseMsg3> m_ExampleClientMsgMultiHandlersAsync;

        private void InitTestHandlers()
        {
            RegisterMessageHandler(103, HandleServerTestMessage103);

            m_ExampleClientMsgHandler = new ClientServerMsgHandler<ExampleSendMsg1, ExampleResponseMsg1>(false, true, true);
            m_ExampleClientMsgHandler.ServerRegisterHandler(HandleExampleClientMsg1);

            m_ExampleClientMsgHandlerAsync = new ClientServerMsgHandler<ExampleSendMsg2, ExampleResponseMsg2>(false, true, true);
            m_ExampleClientMsgHandlerAsync.ServerRegisterHandlerAsync(HandleExampleClientMsg2Async);

            m_ExampleClientMsgMultiHandlersAsync = new ClientServerMsgHandler<ExampleSendMsg3, ExampleResponseMsg3>(false, true, true);
            m_ExampleClientMsgMultiHandlersAsync.ServerRegisterMultiHandlersAsync(HandleExampleClientMsg3Async1, HandleExampleClientMsg3Async2, HandleExampleClientMsg3Async3);
        }

        private void HandleServerTestMessage103(string svMessage)
        {
            Debug.Log($"Server message: {svMessage}");
        }

        private async UniTask<ExampleResponseMsg2> HandleExampleClientMsg2Async(NetworkConnection conn, ExampleSendMsg2 clientMsg)
        {
            await UniTask.DelayFrame(180);
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMsg2
            {
                value = $"{nameof(HandleExampleClientMsg2Async)} ({clientMsg.index})"
            };
            return result;
        }

        private ExampleResponseMsg1 HandleExampleClientMsg1(NetworkConnection conn, ExampleSendMsg1 clientMsg)
        {
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMsg1
            {
                value = $"{nameof(HandleExampleClientMsg1)} ({clientMsg.index})"
            };
            return result;
        }

        private async UniTask<ExampleResponseMsg3> HandleExampleClientMsg3Async1(NetworkConnection conn, ExampleSendMsg3 clientMsg)
        {
            await UniTask.DelayFrame(Random.Range(100, 1000));
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMsg3
            {
                value = $"{nameof(HandleExampleClientMsg3Async1)} ({clientMsg.index})"
            };
            return result;
        }

        private async UniTask<ExampleResponseMsg3> HandleExampleClientMsg3Async2(NetworkConnection conn, ExampleSendMsg3 clientMsg)
        {
            await UniTask.DelayFrame(Random.Range(100, 1000));
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMsg3
            {
                value = $"{nameof(HandleExampleClientMsg3Async2)} ({clientMsg.index})"
            };
            return result;
        }

        private async UniTask<ExampleResponseMsg3> HandleExampleClientMsg3Async3(NetworkConnection conn, ExampleSendMsg3 clientMsg)
        {
            await UniTask.DelayFrame(Random.Range(100, 1000));
            Debug.Log($"Client send message: {clientMsg.value}");
            var result = new ExampleResponseMsg3
            {
                value = $"{nameof(HandleExampleClientMsg3Async3)} ({clientMsg.index})"
            };
            return result;
        }

        public struct ExampleSendMsg1 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleResponseMsg1 : NetworkMessage
        {
            public string value;
        }
        public struct ExampleSendMsg2 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleResponseMsg2 : NetworkMessage
        {
            public string value;
        }
        public struct ExampleSendMsg3 : NetworkMessage
        {
            public int index;
            public string value;
        }
        public struct ExampleResponseMsg3 : NetworkMessage
        {
            public string value;
        }

        #endregion
#endif

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

                EditorGUILayout.LabelField("Operation Message Wrapper Example", GUI.skin.horizontalSlider);

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

                //===================================================================================

                EditorGUILayout.LabelField("Pure Message Wrapper Example", GUI.skin.horizontalSlider);

                if (GUILayout.Button("Send Message"))
                {
                    m_Script.m_ExampleClientMsgHandler.ClientSend(new ExampleSendMsg1()
                    {
                        value = "Hello Server",
                    }, svResponse =>
                    {
                        Debug.Log($"Server response message: {svResponse.value}");
                    });
                }
                if (GUILayout.Button("Send Message Async"))
                {
                    m_Script.m_ExampleClientMsgHandlerAsync.ClientSend(new ExampleSendMsg2()
                    {
                        value = "Hello Server",
                    }, svResponse =>
                    {
                        Debug.Log($"Server response message: {svResponse.value}");
                    });
                }
                if (GUILayout.Button("Send Multi Message"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int index = i;
                        m_Script.m_ExampleClientMsgHandlerAsync.ClientSend(new ExampleSendMsg2()
                        {
                            index = index,
                            value = $"Hello Server ({index})",
                        }, svResponse =>
                        {
                            Debug.Log($"Server response message: {svResponse.value}");
                        });
                    }
                }
                if (GUILayout.Button("Send Message with multi Responses"))
                {
                    m_Script.m_ExampleClientMsgMultiHandlersAsync.ClientSend(new ExampleSendMsg3()
                    {
                        value = "Client is testing multi responses"
                    }, svResponse =>
                    {
                        Debug.Log($"Server response message: {svResponse.value}");
                    });
                }
            }
        }
#endif
#endif
    }
}
