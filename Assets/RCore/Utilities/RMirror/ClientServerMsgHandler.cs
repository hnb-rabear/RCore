#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.RCM
{
    public interface IMsgSender
    {
        bool SingleMessage { get; }
        bool WaitForResponse { get; }
        bool BlockInputWhileWait { get; }
    }

    public delegate UniTask<ResponseMsgT> HandleClientMessageDelegateTask<SendMsgT, ResponseMsgT>(NetworkConnection conn, SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    public delegate ResponseMsgT HandleClientMessageDelegate<SendMsgT, ResponseMsgT>(NetworkConnection conn, SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    //=============================

    #region Server - Client Message Handler

    /// <summary>
    /// Client send message to server and wait for response
    /// </summary>
    /// <typeparam name="SendMsgT"></typeparam>
    /// <typeparam name="ResponseMsgT"></typeparam>
    public class ClientServerMsgHandler<SendMsgT, ResponseMsgT>
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private ClientMsgSender<ResponseMsgT> m_ClientMsgSender;
        private ServerMsgResponser<SendMsgT, ResponseMsgT> m_ServerMsgResponser;

        public bool WaitForResponse => m_ClientMsgSender.WaitForResponse;
        public float LastTime => m_ClientMsgSender.SendTime;

        public ClientServerMsgHandler(bool pRequireAuthentication = true, bool pBlockInputWhileWait = true, bool pSingleMessage = true)
        {
            m_ClientMsgSender = new ClientMsgSender<ResponseMsgT>(pRequireAuthentication, pBlockInputWhileWait, pSingleMessage);
            m_ServerMsgResponser = new ServerMsgResponser<SendMsgT, ResponseMsgT>(pRequireAuthentication);
        }

        public void ClientSend(SendMsgT pMessage, Action<ResponseMsgT> pOnServerResponse)
        {
            m_ClientMsgSender.Send(pMessage, pOnServerResponse);
        }

        public void ServerRegisterHandlerAsync(HandleClientMessageDelegateTask<SendMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ServerMsgResponser.RegisterHandlerAsync(pClientMessageHandler);
        }

        public void ServerRegisterMultiHandlersAsync(params HandleClientMessageDelegateTask<SendMsgT, ResponseMsgT>[] pClientMessageHandlers)
        {
            m_ServerMsgResponser.RegisterMultiHandlersAsync(pClientMessageHandlers);
        }

        public void ServerRegisterHandler(HandleClientMessageDelegate<SendMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ServerMsgResponser.RegisterHandler(pClientMessageHandler);
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
    }

    #endregion

    //=============================

    #region Client Message Handler

    public class ClientMsgSender<ResponseMsgT> : IMsgSender
        where ResponseMsgT : struct, NetworkMessage
    {
        public bool WaitForResponse { get; private set; }
        public float SendTime { get; private set; }
        public float ResponseTime { get; private set; }
        public bool SingleMessage { get; private set; }
        public bool BlockInputWhileWait { get; private set; }

        private event Action<ResponseMsgT> m_OnServerResponse;

        public ClientMsgSender(bool pRequireAuthentication, bool pBlockInputWhiteWait, bool pSingleMessage)
        {
            SingleMessage = pSingleMessage;
            BlockInputWhileWait = pBlockInputWhiteWait;

            NetworkClient.OnDisconnectedEvent += OnDisconnected;
            NetworkClient.RegisterHandler<ResponseMsgT>(OnHandleServerMessage, pRequireAuthentication);
        }

        public void Send<SendMsgT>(SendMsgT pMessage, Action<ResponseMsgT> pOnServerResponse)
            where SendMsgT : struct, NetworkMessage
        {
            if (SingleMessage && WaitForResponse)
            {
                Debug.LogError("Multi message is not allowed!");
                return;
            }
            SendTime = Time.time;
            WaitForResponse = true;
            m_OnServerResponse = pOnServerResponse;
            RCM.Client.TrackMsgSender(this);
            NetworkClient.Send(pMessage);
        }

        private void OnHandleServerMessage(ResponseMsgT pSvMessage)
        {
            ResponseTime = Time.time;
            WaitForResponse = false;
            m_OnServerResponse.Invoke(pSvMessage);
        }

        public float TimeOut()
        {
            if (WaitForResponse)
                return Time.time - SendTime;
            return 0;
        }

        private void OnDisconnected()
        {
            Cancel();
        }

        public void Cancel()
        {
            WaitForResponse = false;
            m_OnServerResponse = null;
        }
    }

    #endregion

    //=============================

    #region Server Message Handler

    public class ServerMsgResponser<ClientMsgT, ResponseMsgT>
        where ClientMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT> m_eClientMessageAsyncHandler;
        private HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> m_ClientMessageHandler;
        private HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT>[] m_ClientMessageAsyncHandlers;

        public ServerMsgResponser(bool pRequireAuthentication)
        {
            NetworkServer.RegisterHandler<ClientMsgT>(OnHandleClientMessage, pRequireAuthentication);
        }

        public void RegisterHandlerAsync(HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_eClientMessageAsyncHandler = pClientMessageHandler;
        }

        public void RegisterHandler(HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> pClientMessageHandler)
        {
            m_ClientMessageHandler = pClientMessageHandler;
        }

        public void RegisterMultiHandlersAsync(params HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT>[] pClientMessageHandlers)
        {
            m_ClientMessageAsyncHandlers = pClientMessageHandlers;
        }

        private async void OnHandleClientMessage(NetworkConnection conn, ClientMsgT clientMsg)
        {
            if (m_ClientMessageHandler != null)
            {
                var result = m_ClientMessageHandler.Invoke(conn, clientMsg);
                conn.Send(result);
            }
            if (m_eClientMessageAsyncHandler != null)
            {
                var result = await m_eClientMessageAsyncHandler.Invoke(conn, clientMsg);
                conn.Send(result);
            }
            if (m_ClientMessageAsyncHandlers != null)
            {
                foreach (var handler in m_ClientMessageAsyncHandlers)
                {
                    var result = await handler.Invoke(conn, clientMsg);
                    conn.Send(result);
                }
            }
        }
    }

    #endregion
}
#endif