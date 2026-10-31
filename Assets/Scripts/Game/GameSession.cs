using System;
using System.Collections;
using System.Collections.Generic;
using Garapon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Garapon.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        const float AutosaveInterval = 30f;
        const float EndingDelay = 4f;
        const string EndingScene = "Ending";

        [SerializeField] GameDataAsset dataAsset;

        float autosaveTimer;
        Coroutine endingRoutine;

        public static GameSession Instance { get; private set; }

        public GameData Data => dataAsset.Data;
        public GameState State { get; private set; }
        public UpgradeSystem Upgrades { get; private set; }
        public Economy Economy { get; private set; }
        public Lottery Lottery { get; private set; }
        public List<string> Cart { get; } = new List<string>();

        public event Action<string> MessagePosted;

        void Awake()
        {
            Instance = this;
            State = SaveFile.Load();
            Upgrades = new UpgradeSystem(Data, State);
            Economy = new Economy(Data, State, Upgrades);
            SetRandomSource(new SystemRandomSource());

            if (State.Cleared)
            {
                SceneManager.LoadScene(EndingScene);
            }
        }

        void Update()
        {
            State.PlaySeconds += Time.unscaledDeltaTime;
            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveInterval)
            {
                autosaveTimer = 0;
                SaveFile.Save(State);
            }
        }

        void OnApplicationQuit()
        {
            SaveFile.Save(State);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetRandomSource(IRandomSource randomSource)
        {
            Lottery = new Lottery(State, new DrawResolver(Data, randomSource));
        }

        public void Post(string message)
        {
            MessagePosted?.Invoke(message);
        }

        public long CartTotal()
        {
            var total = 0L;
            foreach (var productId in Cart)
            {
                total += Economy.GetPrice(Data.FindProduct(productId));
            }

            return total;
        }

        public void HandlePrize(PrizeTierDef tier)
        {
            if (tier.Color == PrizeColor.Gold)
            {
                Post("金玉!! 温泉旅行が当たりました!");
                SaveFile.Save(State);
                endingRoutine ??= StartCoroutine(GoToEnding());
                return;
            }

            var item = Data.FindItem(tier.ItemId);
            Post($"{PrizeColors.DisplayName(tier.Color)}: {item.DisplayName}");
        }

        IEnumerator GoToEnding()
        {
            yield return new WaitForSecondsRealtime(EndingDelay);
            SceneManager.LoadScene(EndingScene);
        }
    }
}
