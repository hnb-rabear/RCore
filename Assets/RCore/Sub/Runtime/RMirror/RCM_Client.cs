/***
 * Author RadBear - nbhung71711@gmail.com - 2021
 **/
//#define TEST_RCM
#if MIRROR
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using System;
using Random = UnityEngine.Random;
using RCore;
#if UNITY_EDITOR
using UnityEditor;
using Debug = RCore.Debug;
#endif

namespace RCore.RCM
{
    public interface IMirrorClientListener
    {
        void OnStartClient();
        void OnStopClient();
        void OnClientConnect();
    }
    public struct WrapMessage : NetworkMessage
    {
        public byte k;
        public string v;
    }

    public class RCM_Client : MonoBehaviour
    {
        private static RCM_Client m_Instance;
        private Dictionary<byte, Action<string>> m_ServerMessageHandlers;
        private List<IMsgSender> m_MsgSenders;
        private List<byte> m_ProcessingOperations;
        private List<IMirrorClientListener> m_Listeners;

        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
#if UNITY_EDITOR && TEST_RCM
            InitTestHandlers();
#endif
        }

#if UNITY_EDITOR && !UNITY_SERVER
        private int m_GuiWidth = 500;
        private int m_GuiHeight = 100;
        private void OnGUI()
        {
            if (NetworkClient.isConnected && Configuration.Instance.EnableLog)
            {
                var boxStyle = new GUIStyle(GUI.skin.box);
                GUILayout.BeginArea(new Rect(0, 0, m_GuiWidth, m_GuiHeight), boxStyle);

                bool waitingForResponse = WaitingResponse();
                var lableStyle = new GUIStyle(GUI.skin.label);
                lableStyle.fontSize = 30 * Screen.height / 1280;
                GUI.color = waitingForResponse ? Color.yellow : Color.green;
                GUILayout.Label($"Waiting for response: {waitingForResponse}", lableStyle);

                GUI.color = BlockingInput() ? Color.red : Color.green;
                GUILayout.Label($"Blocking input: {BlockingInput()}", lableStyle);

                GUILayout.EndArea();
            }
        }
#endif
        public void Init()
        {
            m_ServerMessageHandlers = new Dictionary<byte, Action<string>>();
            m_MsgSenders = new List<IMsgSender>();
            m_ProcessingOperations = new List<byte>();
            m_Listeners = new List<IMirrorClientListener>();
        }

        public void OnStartClient()
        {
            foreach (var listener in m_Listeners)
                listener.OnStartClient();
        }

        public void OnClientConnect()
        {
            m_MsgSenders = new List<IMsgSender>();
            m_ProcessingOperations = new List<byte>();
            NetworkClient.RegisterHandler<ServerMessage>(HandleServerMessage, false);

            foreach (var listener in m_Listeners)
                listener.OnClientConnect();
        }

        public void OnStopClient()
        {
            m_MsgSenders = new List<IMsgSender>();
            m_ProcessingOperations = new List<byte>();

            foreach (var listener in m_Listeners)
                listener.OnStopClient();
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

        public void Register<T>(T pListener) where T : IMirrorClientListener
        {
            if (!m_Listeners.Contains(pListener))
            {
                m_Listeners.Add(pListener);
                if (NetworkClient.active)
                    pListener.OnStartClient();
                if (NetworkClient.isConnected)
                    pListener.OnClientConnect();
            }
        }

        public void UnRegister<T>(T pListener) where T : IMirrorClientListener
        {
            m_Listeners.Remove(pListener);
        }

        //=======================================================================================================

#if UNITY_EDITOR && TEST_RCM
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

#if UNITY_EDITOR && TEST_RCM
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

    }
}
#endif