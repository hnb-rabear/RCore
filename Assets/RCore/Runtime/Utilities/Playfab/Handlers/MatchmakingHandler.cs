#if PLAYFAB

using PlayFab;
using PlayFab.MultiplayerModels;
using System;
using System.Collections;
using UnityEngine;

namespace RCore.PlayFab.Handlers
{
    public class MatchmakingHandler : RequestHandler
    {
        public const string TICKET_SEARCH = "Searching for opponent...";
        public const string TICKET_MATCH = "Opponent found. Starting game...";
        public const string TICKET_CANEL = "Matchmaking stopped";
        public const string TICKET_CANCEL_STARTED = "Stopping matchmaking...";
        public const string TICKET_TIMEDOUT_OR_CANCELLED = "Opponent not found or match was cancelled";
        public const float RETRY_GET_TICKET_STATUS_AFTER_SECONDS = 6;
        public const string Canceled = "Canceled";
        public const string Matched = "Matched";

        public string QuickPlayQueueName { get; set; }
        public string MatchLobbyQueueName { get; set; }
        public int GiveUpAfterSeconds { get; set; }
        public bool IsQuickPlay { get; set; }
        public string QueueName => IsQuickPlay ? QuickPlayQueueName : MatchLobbyQueueName;

        public CreateMatchmakingTicketResult MatchmakingTicketResult { get; set; }

        public GetMatchmakingTicketResult MatchmakingTicketStatus { get; set; }

        public GetMatchResult MatchResult { get; set; }

        public MatchmakingHandler(PlayerInfo pPlayer, string pQuickPlayQueueName, string pMatchLobbyQueueName, int pGiveUpAfterSeconds, bool pIsQuickPlay) : base(pPlayer)
        {
            QuickPlayQueueName = pQuickPlayQueueName;
            MatchLobbyQueueName = pMatchLobbyQueueName;
            GiveUpAfterSeconds = pGiveUpAfterSeconds;
            IsQuickPlay = pIsQuickPlay;
        }

        public void CreateMatchmakingTicket(string attributeValue, Action<CreateMatchmakingTicketResult> pCallback)
        {
            var request = new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Attributes = GetMatchmakingAttribute(attributeValue),
                    Entity = new EntityKey
                    {
                        Id = Player.Entity.Id,
                        Type = Player.Entity.Type,
                    },
                },
                GiveUpAfterSeconds = GiveUpAfterSeconds,
                QueueName = QueueName,
                AuthenticationContext = PlayFabAuthenticationContext
            };
            PlayFabMultiplayerAPI.CreateMatchmakingTicket(request,
                (result) =>
                {
                    MatchmakingTicketResult = result;
                    Debug.Log(result.ToJson());
                    pCallback?.Invoke(result);
                },
                (error) =>
                {
                    var result = $"CreateMatchmakingTicket failed. Message: {error.ErrorMessage}, Code: {error.HttpCode}";
                    Debug.LogError(result);
                    pCallback?.Invoke(null);
                }
            );
        }

        public void GetMatchmakingTicketStatus(bool escapeObject = false, Action<GetMatchmakingTicketResult> pCallback = null)
        {
            var request = new GetMatchmakingTicketRequest
            {
                EscapeObject = escapeObject,
                QueueName = QueueName,
                TicketId = MatchmakingTicketResult.TicketId,
                AuthenticationContext = PlayFabAuthenticationContext
            };
            PlayFabMultiplayerAPI.GetMatchmakingTicket(request,
                (result) =>
                {
                    MatchmakingTicketStatus = result;
                    Debug.Log(result.ToJson());
                    pCallback?.Invoke(result);
                },
                (error) =>
                {
                    var result = $"GetMatchmakingTicketStatus failed. Message: {error.ErrorMessage}, Code: {error.HttpCode}";
                    Debug.LogError(result);
                    pCallback?.Invoke(null);
                }
            );
        }

        public void CancelAllMatchmakingTicketsForPlayer(Action<CancelAllMatchmakingTicketsForPlayerResult> pCallback = null)
        {
            var request = new CancelAllMatchmakingTicketsForPlayerRequest
            {
                QueueName = QueueName,
                Entity = new EntityKey
                {
                    Id = Player.Entity.Id,
                    Type = Player.Entity.Type,
                },
                AuthenticationContext = PlayFabAuthenticationContext
            };
            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(request,
                result =>
                {
                    // set both objects to null as the related ticket was cancelled
                    MatchmakingTicketResult = null;
                    MatchmakingTicketStatus = null;
                    Debug.Log(result.ToJson());
                    pCallback?.Invoke(result);
                },
                error =>
                {
                    var result = $"CancelAllMatchmakingTicketsForPlayer failed. Message: {error.ErrorMessage}, Code: {error.HttpCode}";
                    Debug.LogError(result);
                    pCallback?.Invoke(null);
                }
            );
        }

        public IEnumerator IEEnsureGetMatchmakingTicketStatus()
        {
            while (MatchmakingTicketStatus != null
                && MatchmakingTicketStatus.Status != Matched
                && MatchmakingTicketStatus.Status != Canceled)
            {
                bool wait = true;
                GetMatchmakingTicketStatus(pCallback: result => { wait = false; });
                yield return new WaitUntil(() => !wait);

                Debug.Log($"Matchmaking Ticket Status: { MatchmakingTicketStatus.Status }");

                if (MatchmakingTicketStatus.Status == Canceled)
                    MatchmakingTicketStatus = null;

                if (MatchmakingTicketStatus != null && MatchmakingTicketStatus.Status != Matched)
                    yield return new WaitForSeconds(RETRY_GET_TICKET_STATUS_AFTER_SECONDS);
            }
        }

        public void GetMatchInfo(bool escapeObject = false, bool returnMemberAttributes = false, Action<GetMatchResult> pCallback = null)
        {
            var request = new GetMatchRequest
            {
                EscapeObject = escapeObject,
                MatchId = MatchmakingTicketStatus.MatchId,
                QueueName = QueueName,
                ReturnMemberAttributes = returnMemberAttributes,
                AuthenticationContext = PlayFabAuthenticationContext,
            };
            PlayFabMultiplayerAPI.GetMatch(request,
                (result) =>
                {
                    MatchResult = result;
                    Debug.LogError(result.ToJson());
                    pCallback?.Invoke(result);
                },
                (error) =>
                {
                    var result = $"GetMatchInfo failed. Message: {error.ErrorMessage}, Code: {error.HttpCode}";
                    Debug.LogError(result);
                    pCallback?.Invoke(null);
                }
            );
        }

        private MatchmakingPlayerAttributes GetMatchmakingAttribute(string attributeValue)
        {
            var attributes = new MatchmakingPlayerAttributes();
            attributes.DataObject = IsQuickPlay ? (new { Skill = attributeValue }) : (object)new { LobbyId = attributeValue };
            return attributes;
        }

        public IEnumerator IECreateSinglePlayerMatch(string pAttributeValue)
        {
            bool wait = true;
            bool success = false;

            //1. Cancel current matchmaking
            if (MatchmakingTicketResult != null || MatchmakingTicketStatus != null)
            {
                Debug.Log(TICKET_CANCEL_STARTED);
                CancelAllMatchmakingTicketsForPlayer(result =>
                {
                    wait = false;
                    success = result != null;
                });
                yield return new WaitUntil(() => !wait);
                if (!success)
                    yield break;
                Debug.Log(TICKET_CANEL);
            }

            //2. Create new matchmaking ticket
            wait = true;
            Debug.Log(TICKET_SEARCH);
            CreateMatchmakingTicket(pAttributeValue, result =>
            {
                wait = false;
                success = result != null;
            });
            yield return new WaitUntil(() => !wait);
            if (!success)
                yield break;

            //3. Process ticket
            while (MatchmakingTicketStatus != null
                && MatchmakingTicketStatus.Status != Matched
                && MatchmakingTicketStatus.Status != Canceled)
            {
                wait = true;
                GetMatchmakingTicketStatus(pCallback: result => { wait = false; });
                yield return new WaitUntil(() => !wait);

                Debug.Log($"Matchmaking Ticket Status: { MatchmakingTicketStatus.Status }");

                if (MatchmakingTicketStatus.Status == Canceled)
                    MatchmakingTicketStatus = null;

                if (MatchmakingTicketStatus != null && MatchmakingTicketStatus.Status != Matched)
                    yield return new WaitForSeconds(RETRY_GET_TICKET_STATUS_AFTER_SECONDS);
            }

            //4. Finally, we get the match's info
            var ticketStatus = MatchmakingTicketStatus;
            if (ticketStatus != null && ticketStatus.Status == Matched)
            {
                wait = true;
                GetMatchInfo(pCallback: result =>
                {
                    wait = false;
                    success = result != null;
                });
                yield return new WaitUntil(() => !wait);
                if (success)
                    Debug.Log(TICKET_MATCH);
                else
                    Debug.Log(TICKET_TIMEDOUT_OR_CANCELLED);
            }
            else
                Debug.Log(TICKET_TIMEDOUT_OR_CANCELLED);
        }
    }
}

#endif