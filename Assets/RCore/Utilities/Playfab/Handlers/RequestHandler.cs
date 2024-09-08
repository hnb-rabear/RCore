#if PLAYFAB

using PlayFab;
using PlayFab.AuthenticationModels;

namespace RCore.PlayFab.Handlers
{
    public class PlayerInfo
    {
        public string EntityToken { get; set; }
        public string PlayFabId { get; set; }
        public string SessionTicket { get; set; }
        public EntityKey Entity { get; set; }
    }

    public class RequestHandler
    {
        protected PlayerInfo Player { get; set; }
        protected PlayFabAuthenticationContext PlayFabAuthenticationContext { get; set; }
        public RequestHandler(PlayerInfo player)
        {
            Player = player;
            PlayFabAuthenticationContext = new PlayFabAuthenticationContext
            {
                EntityToken = player.EntityToken
            };
        }
    }
}

#endif