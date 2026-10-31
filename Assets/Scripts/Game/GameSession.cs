using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Garapon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Garapon.Game
{
    // How many groceries one press of [E] puts in the cart, switched with [Q].
    public enum BuyMode
    {
        One,
        Ten,
        Max
    }

    public sealed class GameSession : MonoBehaviour
    {
        const float AutosaveInterval = 30f;
        const float EndingDelay = 4f;
        const string EndingScene = "Ending";
        const float DiscoveryCheckInterval = 0.5f;

        static readonly PrizeColor[] RareFirst =
        {
            PrizeColor.Gold, PrizeColor.Silver, PrizeColor.Blue, PrizeColor.Green, PrizeColor.Red, PrizeColor.White
        };

        [SerializeField] GameDataAsset dataAsset;

        float autosaveTimer;
        float discoveryTimer;
        readonly HashSet<string> stockedProducts = new HashSet<string>();
        readonly SystemRandomSource luckyRandom = new SystemRandomSource();
        readonly SystemRandomSource saleRandom = new SystemRandomSource();
        Coroutine endingRoutine;

        public static GameSession Instance { get; private set; }

        public GameData Data => dataAsset.Data;
        public GameState State { get; private set; }
        public UpgradeSystem Upgrades { get; private set; }
        public Economy Economy { get; private set; }
        public Automation Automation { get; private set; }
        public Progression Progression { get; private set; }
        public Combo Combo { get; private set; }
        public LuckyTrash Lucky { get; private set; }
        public Lottery Lottery { get; private set; }
        public BargainSale Sale { get; private set; }
        public ShoppingCart Cart { get; } = new ShoppingCart();
        public BuyMode BuyMode { get; private set; }

        public event Action<string> MessagePosted;

        void Awake()
        {
            Instance = this;
            State = SaveFile.Load();
            Upgrades = new UpgradeSystem(Data, State);
            Economy = new Economy(Data, State, Upgrades);
            Automation = new Automation(Data, State, Upgrades, Economy);
            Progression = new Progression(Data, State, Upgrades);
            Combo = new Combo(Data, State);
            Lucky = new LuckyTrash(Data, State, Upgrades, Economy);
            Sale = new BargainSale(Data, State, Upgrades, Progression);
            SetRandomSource(new SystemRandomSource());

            // Shelves stocked at load time are not news. Stamps are announced on the first check,
            // so a new run greets the player with the stamps it has just earned.
            Progression.NewlyRevealed(stockedProducts);

            if (State.Cleared)
            {
                SceneManager.LoadScene(EndingScene);
                return;
            }

            ApplyOfflineProgress();
        }

        void Update()
        {
            State.PlaySeconds += Time.unscaledDeltaTime;
            Automation.Tick(Time.deltaTime);
            Lucky.Tick(Time.deltaTime);
            var sale = Sale.Tick(Time.deltaTime, saleRandom);
            if (sale != null)
            {
                AnnounceSale(sale);
            }

            discoveryTimer += Time.unscaledDeltaTime;
            if (discoveryTimer >= DiscoveryCheckInterval)
            {
                discoveryTimer = 0;
                AnnounceDiscoveries();
            }

            autosaveTimer += Time.unscaledDeltaTime;
            if (autosaveTimer >= AutosaveInterval)
            {
                autosaveTimer = 0;
                SaveFile.Save(State);
            }
        }

        void OnApplicationPause(bool paused)
        {
            // Mobile platforms may never call OnApplicationQuit, so save whenever the app is backgrounded.
            if (paused && State != null)
            {
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

        public long CartTotal() => Economy.Total(Cart);

        public void CycleBuyMode()
        {
            BuyMode = BuyMode == BuyMode.Max ? BuyMode.One : BuyMode + 1;
            Post($"まとめ買い: {BuyModeName}");
        }

        public string BuyModeName => BuyMode == BuyMode.One ? "1個ずつ" : BuyMode == BuyMode.Ten ? "10個ずつ" : "買えるだけ";

        // How many of a grocery one press adds in the current buy mode, limited to what can be paid for.
        public long BulkQuantity(string productId)
        {
            var affordable = Economy.MaxAffordable(Cart, productId);
            switch (BuyMode)
            {
                case BuyMode.Ten: return Math.Min(10, Math.Max(1, affordable));
                case BuyMode.Max: return Math.Max(1, affordable);
                default: return 1;
            }
        }

        // Announces upgrades that crossed a milestone between the given levels and now.
        public void AnnounceMilestones(Dictionary<string, int> levelsBefore)
        {
            foreach (var upgrade in Data.Upgrades)
            {
                levelsBefore.TryGetValue(upgrade.Id, out var before);
                var after = State.GetUpgradeLevel(upgrade.Id);
                if (UpgradeSystem.MilestoneFactor(upgrade, after) > UpgradeSystem.MilestoneFactor(upgrade, before))
                {
                    Post($"{upgrade.DisplayName} Lv{after} 達成! 節目ボーナスで効果×{upgrade.MilestoneMultiplier}");
                }
            }
        }

        void AnnounceSale(ProductDef product)
        {
            var seconds = (int)Data.SaleSeconds;
            Post(string.IsNullOrEmpty(product.UpgradeId)
                ? $"タイムセール! {product.DisplayName} 抽選券{Data.SaleTicketMultiplier}倍 ({seconds}秒間)"
                : $"タイムセール! {product.DisplayName} {Data.SaleDiscount * 100:0}%引き ({seconds}秒間)");
        }

        public void HandlePrize(DrawBatchResult result, long drawsBefore)
        {
            if (result.Best.Color == PrizeColor.Gold)
            {
                Post("金玉!! 温泉旅行が当たりました!");
                SaveFile.Save(State);
                endingRoutine ??= StartCoroutine(GoToEnding());
                return;
            }

            var message = new StringBuilder();
            if (result.Draws == 1)
            {
                var item = Data.FindItem(result.Best.ItemId);
                message.Append(PrizeColors.DisplayName(result.Best.Color)).Append(": ").Append(item.DisplayName);
            }
            else
            {
                message.Append(NumberFormat.Japanese(result.Draws)).Append("回抽選 ");
                foreach (var color in RareFirst)
                {
                    var count = result.Count(color);
                    if (count > 0)
                    {
                        message.Append(' ').Append(PrizeColors.DisplayName(color)).Append('×').Append(NumberFormat.Japanese(count));
                    }
                }
            }

            foreach (var milestone in Progression.DrawMilestonesCrossed(drawsBefore, State.TotalDraws))
            {
                message.Append("\n抽選 ").Append(NumberFormat.Japanese(milestone)).Append("回 達成!");
            }

            Post(message.ToString());
        }

        public void CollectLucky()
        {
            var reward = Lucky.Collect(luckyRandom);
            switch (reward.Kind)
            {
                case LuckyKind.Frenzy:
                    Post($"ラッキーごみ! 大漁タイム: {reward.Amount}秒間 ごみの価値×{Data.FrenzyMultiplier}");
                    break;
                case LuckyKind.Windfall:
                    Post($"ラッキーごみ! 臨時収入 {NumberFormat.Yen(reward.Amount)}");
                    break;
                default:
                    Post($"ラッキーごみ! 福引券を{NumberFormat.Japanese(reward.Amount)}枚拾った");
                    break;
            }
        }

        void AnnounceDiscoveries()
        {
            foreach (var product in Progression.NewlyRevealed(stockedProducts))
            {
                Post($"スーパーに新商品入荷: {product.DisplayName}");
            }

            foreach (var stamp in StampCard.Update(State, Upgrades))
            {
                Post($"スタンプ獲得「{stamp.Name}」 買取額 +{Data.StampBonus * 100:0}% ({State.Stamps.Count}/{StampCard.All.Count})");
            }
        }

        void ApplyOfflineProgress()
        {
            var away = Automation.OfflineSeconds(State.LastSavedUnixSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var report = Automation.ApplyOffline(away);
            if (report.TrashCollected <= 0)
            {
                return;
            }

            var span = TimeSpan.FromSeconds(report.Seconds);
            var message = $"おかえりなさい! 留守の{(int)span.TotalHours}時間{span.Minutes}分の間に、ロボが{NumberFormat.Yen(report.TrashCollected)}分のごみを集めました";
            if (report.MoneyEarned > 0)
            {
                message += $"\n出張買取で{NumberFormat.Yen(report.MoneyEarned)}になりました";
            }

            // The HUD subscribes in Start, so post once everything has started.
            StartCoroutine(PostNextFrame(message));
        }

        IEnumerator PostNextFrame(string message)
        {
            yield return null;
            Post(message);
        }

        IEnumerator GoToEnding()
        {
            yield return new WaitForSecondsRealtime(EndingDelay);
            SceneManager.LoadScene(EndingScene);
        }
    }
}
