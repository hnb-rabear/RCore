#if MIRROR
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.RCM
{
    public delegate UniTask<ResponseMsgT> HandleServerMessageDelegateTask<SendMsgT, ResponseMsgT>(SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    public delegate ResponseMsgT HandleServerMessageDelegate<SendMsgT, ResponseMsgT>(SendMsgT clientMsg)
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage;

    #region Server Client Message Handler

    /// <summary>
    /// Server send message to client and wait for response
    /// </summary>
    /// <typeparam name="SendMsgT"></typeparam>
    /// <typeparam name="ResponseMsgT"></typeparam>
    public class ServerClientMsgHandler<SendMsgT, ResponseMsgT>
        where SendMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private ServerMsgSender<ResponseMsgT> m_ServerMsgSender;
        private ClientMsgResponser<SendMsgT, ResponseMsgT> m_ClientMsgResponser;

        public ServerClientMsgHandler()
        {
            m_ServerMsgSender = new ServerMsgSender<ResponseMsgT>();
            m_ClientMsgResponser = new ClientMsgResponser<SendMsgT, ResponseMsgT>();
        }

        public void SeverSend(NetworkConnection conn, SendMsgT pMessage, Action<NetworkConnection, ResponseMsgT> pOnClientResponse)
        {
            m_ServerMsgSender.Send(conn, pMessage, pOnClientResponse);
        }

        public void ClientRegisterHandlerAsync(HandleServerMessageDelegateTask<SendMsgT, ResponseMsgT> pOnHandleServerResponse)
        {
            m_ClientMsgResponser.RegisterHandlerAsync(pOnHandleServerResponse);
        }

        public void ClientRegisterHandler(HandleServerMessageDelegate<SendMsgT, ResponseMsgT> pOnHandleServerResponse)
        {
            m_ClientMsgResponser.RegisterHandler(pOnHandleServerResponse);
        }
    }

    #endregion

    //=============================

    #region Server Message Handler

    public class ServerMsgSender<ResponseMsgT>
        where ResponseMsgT : struct, NetworkMessage
    {
        public bool WaitForResponse { get; private set; }
        public float SendTime { get; private set; }
        public float ResponseTime { get; private set; }
        private event Action<NetworkConnection, ResponseMsgT> m_OnClientResponse;

        public ServerMsgSender()
        {
            NetworkServer.RegisterHandler<ResponseMsgT>(OnHandleClientMessage);
        }

        public void Send<SendMsgT>(NetworkConnection conn, SendMsgT pMessage, Action<NetworkConnection, ResponseMsgT> pOnClientResponse)
            where SendMsgT : struct, NetworkMessage
        {
            SendTime = Time.time;
            WaitForResponse = true;
            m_OnClientResponse = pOnClientResponse;
            conn.Send(pMessage);
        }

        private void OnHandleClientMessage(NetworkConnection conn, ResponseMsgT clientMsg)
        {
            ResponseTime = Time.time;
            WaitForResponse = false;
            m_OnClientResponse?.Invoke(conn, clientMsg);
        }
    }

    #endregion

    //=============================

    #region Client Message Handler 

    public class ClientMsgResponser<ServerMsgT, ResponseMsgT>
        where ServerMsgT : struct, NetworkMessage
        where ResponseMsgT : struct, NetworkMessage
    {
        private HandleServerMessageDelegateTask<ServerMsgT, ResponseMsgT> m_HandleServerMessageAsync;
        private HandleServerMessageDelegate<ServerMsgT, ResponseMsgT> m_HandleServerMessage;

        public ClientMsgResponser()
        {
            NetworkClient.RegisterHandler<ServerMsgT>(OnHandleServerMessage);
        }

        public void RegisterHandler(HandleServerMessageDelegate<ServerMsgT, ResponseMsgT> pOnHandleServerMessage)
        {
            m_HandleServerMessage = pOnHandleServerMessage;
        }

        public void RegisterHandlerAsync(HandleServerMessageDelegateTask<ServerMsgT, ResponseMsgT> pOnHandleServerMessage)
        {
            m_HandleServerMessageAsync = pOnHandleServerMessage;
        }

        private async void OnHandleServerMessage(ServerMsgT serverMsg)
        {
            if (m_HandleServerMessage != null)
            {
                var result = m_HandleServerMessage.Invoke(serverMsg);
                NetworkClient.Send(result);
            }
            if (m_HandleServerMessageAsync != null)
            {
                var result = await m_HandleServerMessageAsync.Invoke(serverMsg);
                NetworkClient.Send(result);
            }
        }
    }

    #endregion
}
#endif