using System;
using UnityEngine;

namespace RevCore.Samples
{
    // Example data POCO — add fields as needed
    [Serializable]
    public class PlayerData : JObjectData
    {
        public int coins;
        public int level = 1;
        public string playerName = "Player";
    }

    // Example model ScriptableObject
    [CreateAssetMenu(menuName = "RevCore/Samples/PlayerModel")]
    public class PlayerModel : JObjectModel<PlayerData>
    {
        public override void Init() { }
        public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds) { }
        public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
        public override void OnUpdate(float deltaTime) { }
        public override void OnPreSave(int utcNowTimestamp) { }
        public override void OnRemoteConfigFetched() { }

        public void AddCoins(int amount)
        {
            data.coins += amount;
            DispatchEvent(new CoinsChangedEvent { Amount = data.coins });
        }
    }

    public readonly struct CoinsChangedEvent : IEvent
    {
        public readonly int Amount;
        public CoinsChangedEvent(int amount) { Amount = amount; }
    }

    // Example collection aggregator
    [CreateAssetMenu(menuName = "RevCore/Samples/SampleCollection")]
    public class SampleCollection : JObjectModelCollection
    {
        [RevCore.Inspector.CreateScriptableObject, RevCore.Inspector.AutoFill]
        public PlayerModel player;

        public override void Load()
        {
            base.Load(); // creates SessionModel
            CreateModel(player, "PlayerData");
        }
    }

    // Example MonoBehaviour manager
    public class DataSample : JObjectDBManager<SampleCollection>
    {
        private void Start()
        {
            Init();
            Events.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnDestroy()
        {
            Events.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e)
        {
            Log.Info($"Coins changed: {e.Amount}", this);
        }

        private void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.C))
                m_dataCollection.player.AddCoins(10);

            if (Input.GetKeyDown(KeyCode.Return))
                Save(true);
        }
    }
}
