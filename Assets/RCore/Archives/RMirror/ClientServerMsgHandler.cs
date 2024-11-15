#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using RCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = RCore.Debug;

namespace RCore.RCM
{
    public interface IMsgSender
    {
        bool SingleMessage { get; }
        bool WaitForResponse { get; }
        bool BlockInputWhileWait { get; }
    }

    public interface IRCM_MsgHandler
    {
        void OnStartClient();
        void OnStartServer();
        void OnStopClient();
        void OnStopServer();
    }

    public delegate UniTask<ResponseMsgT> HandleClientMessageDelegateAsync<SendMsgT, ResponseMsgT>(NetworkConnection conn, SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    public delegate ResponseMsgT HandleClientMessageDelegate<SendMsgT, ResponseMsgT>(NetworkConnection conn, SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    public delegate UniTask ManualHandleClientMessageDelegateAsync<SendMsgT, ResponseMsgT>(NetworkConnection conn, SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    //=============================

    #region Server - Client Message Handler

    /// <summary>
    /// Client send message to server and wait for response
    /// </summary>
    /// <typeparam name="SendMsgT"></typeparam>
    /// <typeparam name="ResponseMsgT"></typeparam>
    public class ClientServerMsgHandler<SendMsgT, ResponseMsgT> : IRCM_MsgHandler
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private ClientMsgSender<ResponseMsgT> m_ClientMsgSender;
        private ServerMsgResponser<SendMsgT, ResponseMsgT> m_ServerMsgResponser;

        public int SendMsgId() => MessagePacking.GetId<SendMsgT>();
        public int ResponseId() => MessagePacking.GetId<ResponseMsgT>();
        public bool WaitForResponse() => m_ClientMsgSender.WaitForResponse;
        public float LastTime() => m_ClientMsgSender.SendTime;

        public ClientServerMsgHandler(bool pRequireAuthentication = true, bool pBlockInputWhileWait = true, bool pSingleMessage = true)
        {
            m_ClientMsgSender = new ClientMsgSender<ResponseMsgT>(pRequireAuthentication, pBlockInputWhileWait, pSingleMessage);
            m_ServerMsgResponser = new ServerMsgResponser<SendMsgT, ResponseMsgT>(pRequireAuthentication);
            if (NetworkServer.active) OnStartServer();
            if (NetworkClient.active) OnStartClient();
        }

        public void ClientSend(SendMsgT pMessage, Action<ResponseMsgT> pOnServerResponse)
        {
            m_ClientMsgSender.Send(pMessage, pOnServerResponse);
        }

        public void ClientRegisterCustomHandler(Action<ResponseMsgT> pServerMsgHandler)
        {
            m_ClientMsgSender.RegisterCustomHandler(pServerMsgHandler);
        }

        public void ServerRegisterHandlerAsync(HandleClientMessageDelegateAsync<SendMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ServerMsgResponser.RegisterHandlerAsync(pClientMessageHandler);
        }

        public void ServerRegisterMultiHandlersAsync(params HandleClientMessageDelegateAsync<SendMsgT, ResponseMsgT>[] pClientMessageHandlers)
        {
            m_ServerMsgResponser.RegisterMultiHandlersAsync(pClientMessageHandlers);
        }

        public void ServerRegisterHandler(HandleClientMessageDelegate<SendMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ServerMsgResponser.RegisterHandler(pClientMessageHandler);
        }

        public void ServerRegisterManualHandlerAsync(ManualHandleClientMessageDelegateAsync<SendMsgT, ResponseMsgT> pManualClientMessageHandler)
        {
            m_ServerMsgResponser.RegisterManualHandlerAsync(pManualClientMessageHandler);
        }

        /// <summary>
        /// Called in Update
        /// </summary>
        public bool AutoCancelByTimeOut(float pMaxTimeOut)
        {
            bool isTimeOut = m_ClientMsgSender.TimeOut() >= pMaxTimeOut;
            if (isTimeOut)
                m_ClientMsgSender.Cancel();
            return isTimeOut;
        }

        public void OnStartClient()
        {
            m_ClientMsgSender.OnStartClient();
        }

        public void OnStopClient()
        {
            m_ClientMsgSender.OnStopClient();
        }

        public void OnStartServer()
        {
            m_ServerMsgResponser.OnStartServer();
        }

        public void OnStopServer()
        {
            m_ServerMsgResponser.OnStopServer();
        }
    }

    #endregion

    //=============================

    #region Client Message Handler

    public class ClientMsgSender<ResponseMsgT> : IMsgSender
        where ResponseMsgT : struct, NetworkMessage
    {
        public bool WaitForResponse => m_OnServerResponse != null;
        public float SendTime { get; private set; }
        public float ResponseTime { get; private set; }
        public bool SingleMessage { get; private set; }
        public bool BlockInputWhileWait { get; private set; }
        public bool requireAuthentication { get; private set; }
        public ushort messageId { get; private set; }

        private event Action<ResponseMsgT> m_OnServerResponse;
        public event Action<ResponseMsgT> m_OnServerResponseCustom;

        public ClientMsgSender(bool pRequireAuthentication, bool pBlockInputWhiteWait, bool pSingleMessage)
        {
            SingleMessage = pSingleMessage;
            BlockInputWhileWait = pBlockInputWhiteWait;
            requireAuthentication = pRequireAuthentication;
        }

        public void OnStartClient()
        {
            NetworkClient.RegisterHandler<ResponseMsgT>(OnHandleServerMessage, requireAuthentication);

            Cancel();

#if UNITY_EDITOR
            Debug.Log($"Register Response: {typeof(ResponseMsgT).Name} ({MessagePacking.GetId<ResponseMsgT>()})", ColorHelper.LightLime);
#endif
        }

        public void OnStopClient()
        {
            NetworkClient.UnregisterHandler<ResponseMsgT>();

            Cancel();

#if UNITY_EDITOR
            Debug.Log($"Unregister Response: {typeof(ResponseMsgT).Name} ({MessagePacking.GetId<ResponseMsgT>()})", ColorHelper.LightLime);
#endif
        }

        public void Send<SendMsgT>(SendMsgT pMessage, Action<ResponseMsgT> pOnServerResponse)
            where SendMsgT : struct, NetworkMessage
        {
            if (SingleMessage && WaitForResponse)
            {
                Debug.LogError($"Multi messages is not allowed! {typeof(SendMsgT).Name} ({MessagePacking.GetId<SendMsgT>()})");
                return;
            }

            if (!NetworkClient.isConnected)
            {
                Debug.LogError($"Disconnected, can't send message {typeof(SendMsgT).Name} ({MessagePacking.GetId<SendMsgT>()})!");
                pOnServerResponse?.Invoke(new ResponseMsgT());
                return;
            }

            messageId = MessagePacking.GetId<SendMsgT>();
            SendTime = Time.time;
            m_OnServerResponse = pOnServerResponse;
            RCM.Client.TrackMsgSender(this);
            NetworkClient.Send(pMessage);
#if UNITY_EDITOR
            Debug.Log($"Send Request: {typeof(SendMsgT).Name} ({MessagePacking.GetId<SendMsgT>()})", JsonUtility.ToJson(pMessage), ColorHelper.LightLime);
#endif
        }

        private void OnHandleServerMessage(ResponseMsgT pSvMessage)
        {
            ResponseTime = Time.time;
            m_OnServerResponse?.Invoke(pSvMessage);
            m_OnServerResponse = null;
            m_OnServerResponseCustom?.Invoke(pSvMessage);
        }

        public float TimeOut()
        {
            if (WaitForResponse)
                return Time.time - SendTime;
            return 0;
        }

        public void Cancel()
        {
            m_OnServerResponse = null;
        }

        public void RegisterCustomHandler(Action<ResponseMsgT> pHandler)
        {
            m_OnServerResponseCustom += pHandler;
        }

        public void UnregisterCustomHandler(Action<ResponseMsgT> pHandler)
        {
            m_OnServerResponseCustom -= pHandler;
        }
    }

    #endregion

    //=============================

    #region Server Message Handler

    public class ServerMsgResponser<ClientMsgT, ResponseMsgT>
        where ClientMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private HandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT> m_ClientMessageAsyncHandler;
        private HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> m_ClientMessageHandler;
        private HandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT>[] m_MultiClientMessageAsyncHandlers;
        private ManualHandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT> m_VoidHandleClientMessageDelegateAsync;
        private bool m_RequireAuthentication;

        public ServerMsgResponser(bool pRequireAuthentication)
        {
            m_RequireAuthentication = pRequireAuthentication;
        }

        public void OnStartServer()
        {
            NetworkServer.RegisterHandler<ClientMsgT>(OnHandleClientMessage, m_RequireAuthentication);
        }

        public void OnStopServer()
        {
            NetworkServer.UnregisterHandler<ClientMsgT>();
        }

        public void RegisterHandlerAsync(HandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ClientMessageAsyncHandler = pClientMessageHandler;
        }

        public void RegisterManualHandlerAsync(ManualHandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT> pManualHandleClientMessageDelegateAsync)
        {
            m_VoidHandleClientMessageDelegateAsync = pManualHandleClientMessageDelegateAsync;
        }

        public void RegisterHandler(HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ClientMessageHandler = pClientMessageHandler;
        }

        public void RegisterMultiHandlersAsync(params HandleClientMessageDelegateAsync<ClientMsgT, ResponseMsgT>[] pClientMessageHandlers)
        {
            m_MultiClientMessageAsyncHandlers = pClientMessageHandlers;
        }

        private async void OnHandleClientMessage(NetworkConnection conn, ClientMsgT clientMsg)
        {
            ResponseMsgT result = default(ResponseMsgT);
            if (m_ClientMessageHandler != null)
            {
                result = m_ClientMessageHandler.Invoke(conn, clientMsg);
                conn.Send(result);
            }
            if (m_ClientMessageAsyncHandler != null)
            {
                result = await m_ClientMessageAsyncHandler.Invoke(conn, clientMsg);
                conn.Send(result);
            }
            if (m_MultiClientMessageAsyncHandlers != null)
            {
                foreach (var handler in m_MultiClientMessageAsyncHandlers)
                {
                    result = await handler.Invoke(conn, clientMsg);
                    conn.Send(result);
                }
            }
            if (m_VoidHandleClientMessageDelegateAsync != null)
            {
                await m_VoidHandleClientMessageDelegateAsync.Invoke(conn, clientMsg);
            }
#if UNITY_EDITOR
            Debug.Log($"Send Response: {typeof(ResponseMsgT).Name} ({MessagePacking.GetId<ResponseMsgT>()})", JsonConvert.SerializeObject(result, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            }), ColorHelper.LightGold);
#endif
        }
    }

    #endregion
}
#endif