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

        public ClientServerMsgHandler(bool pBlockInputWhileWait = true, bool pSingleMessage = true)
        {
            m_ClientMsgSender = new ClientMsgSender<ResponseMsgT>(pBlockInputWhileWait, pSingleMessage);
            m_ServerMsgResponser = new ServerMsgResponser<SendMsgT, ResponseMsgT>();
        }

        public void ClientSend(SendMsgT pMessage, Action<ResponseMsgT> pOnServerResponse)
        {
            m_ClientMsgSender.Send(pMessage, pOnServerResponse);
        }

        public void ServerRegisterHandlerAsync(HandleClientMessageDelegateTask<SendMsgT, ResponseMsgT> pOnHandleClientMessage)
        {
            m_ServerMsgResponser.RegisterHandlerAsync(pOnHandleClientMessage);
        }

        public void ServerRegisterHandler(HandleClientMessageDelegate<SendMsgT, ResponseMsgT> pOnHandleClientMessage)
        {
            m_ServerMsgResponser.RegisterHandler(pOnHandleClientMessage);
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

        public ClientMsgSender(bool pBlockInputWhiteWait, bool pSingleMessage)
        {
            SingleMessage = pSingleMessage;
            BlockInputWhileWait = pBlockInputWhiteWait;

            NetworkClient.OnDisconnectedEvent += OnDisconnected;
            NetworkClient.RegisterHandler<ResponseMsgT>(OnHandleServerMessage);
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
            WaitForResponse = false;
        }
    }

    #endregion

    //=============================

    #region Server Message Handler

    public class ServerMsgResponser<ClientMsgT, ResponseMsgT>
        where ClientMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT> m_HandleClientMessageAsync;
        private HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> m_HandleClientMessage;

        public ServerMsgResponser()
        {
            NetworkServer.RegisterHandler<ClientMsgT>(OnHandleClientMessage);
        }

        public void RegisterHandlerAsync(HandleClientMessageDelegateTask<ClientMsgT, ResponseMsgT> pOnHandleClientMessage)
        {
            m_HandleClientMessageAsync = pOnHandleClientMessage;
        }

        public void RegisterHandler(HandleClientMessageDelegate<ClientMsgT, ResponseMsgT> pOnHandleClientMessage)
        {
            m_HandleClientMessage = pOnHandleClientMessage;
        }

        private async void OnHandleClientMessage(NetworkConnection conn, ClientMsgT clientMsg)
        {
            if (m_HandleClientMessage != null)
            {
                var result = m_HandleClientMessage.Invoke(conn, clientMsg);
                conn.Send(result);
            }
            if (m_HandleClientMessageAsync != null)
            {
                var result = await m_HandleClientMessageAsync.Invoke(conn, clientMsg);
                conn.Send(result);
            }
        }
    }

    #endregion
}
#endif