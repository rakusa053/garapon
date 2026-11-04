using System.Collections.Generic;
using System.Text;
using Garapon.Core;
using TMPro;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class GameHud : MonoBehaviour
    {
        const float MessageDuration = 3f;
        const float BusyMessageDuration = 1.5f;
        const int MaxQueuedMessages = 6;

        [SerializeField] PlayerController player;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text inventoryText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text messageText;

        readonly StringBuilder builder = new StringBuilder();
        readonly Queue<string> messages = new Queue<string>();
        float messageTimer;

        void Start()
        {
            GameSession.Instance.MessagePosted += QueueMessage;
            messageText.text = string.Empty;
        }

        void OnDestroy()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.MessagePosted -= QueueMessage;
            }
        }

        void Update()
        {
            var session = GameSession.Instance;
            UpdateStatus(session);
            UpdateInventory(session);

            var target = player.Target;
            promptText.text = target == null ? string.Empty : $"[E] {target.Prompt}";

            UpdateMessages();
        }

        void UpdateStatus(GameSession session)
        {
            var state = session.State;
            builder.Clear();
            if (state.Clears > 0)
            {
                builder.Append(state.Clears + 1).Append("周目(思い出ボーナス 買取×").Append(1 + state.Clears * session.Data.ClearBonus).Append(")\n");
            }

            builder.Append("所持金 ").Append(NumberFormat.Yen(state.Money)).Append('\n');
            builder.Append("ごみの査定額 ").Append(NumberFormat.Yen(state.TrashValue));
            var robotYenPerSecond = (long)(session.Upgrades.AutoCollectPerSecond * session.Data.AverageTrashValue() * session.Economy.FrenzyMultiplier);
            if (robotYenPerSecond > 0)
            {
                builder.Append("(ロボ 毎秒+").Append(NumberFormat.Japanese(robotYenPerSecond)).Append(')');
            }

            builder.Append('\n');

            if (state.FrenzySecondsLeft > 0)
            {
                builder.Append("<color=#FFD040>大漁タイム! ごみ×").Append(session.Data.FrenzyMultiplier)
                    .Append(" 残り").Append(Mathf.CeilToInt((float)state.FrenzySecondsLeft)).Append("秒</color>\n");
            }

            if (session.Combo.IsActive(Time.time) && session.Combo.Count > 1)
            {
                builder.Append("<color=#7FE0FF>").Append(session.Combo.Count).Append("コンボ ×")
                    .Append(session.Combo.Multiplier.ToString("0.00")).Append("</color>\n");
            }

            var sale = session.Sale.Current;
            if (sale != null)
            {
                builder.Append("<color=#FF6060>タイムセール ").Append(sale.DisplayName)
                    .Append(string.IsNullOrEmpty(sale.UpgradeId) ? " 抽選券" + session.Data.SaleTicketMultiplier + "倍" : $" {session.Data.SaleDiscount * 100:0}%引き")
                    .Append(" 残り").Append(Mathf.CeilToInt((float)session.Sale.SecondsLeft)).Append("秒</color>\n");
            }

            if (state.HasTripTicket)
            {
                builder.Append("<color=#FFD040>温泉旅行券あり! バス停からいつでも出発できます</color>\n");
            }

            builder.Append("抽選券 ").Append(NumberFormat.Japanese(state.Tickets)).Append("枚\n");
            builder.Append("カゴ ").Append(NumberFormat.Japanese(session.Cart.Count)).Append("点 ").Append(NumberFormat.Yen(session.CartTotal()))
                .Append("  [Q] ").Append(session.BuyModeName).Append('\n');
            builder.Append("抽選回数 ").Append(NumberFormat.Japanese(state.TotalDraws)).Append("回\n");
            var untilGuarantee = session.Lottery.DrawsUntilGuarantee;
            if (untilGuarantee > 0)
            {
                builder.Append("金玉確定まで あと").Append(NumberFormat.Japanese(untilGuarantee)).Append("回");
            }

            statusText.text = builder.ToString();
        }

        void UpdateInventory(GameSession session)
        {
            var state = session.State;
            builder.Clear();
            if (state.SouvenirsEarned > 0 || state.Clears > 0)
            {
                builder.Append("おみやげ ").Append(NumberFormat.Japanese(state.Souvenirs)).Append("個");
                var pending = Prestige.Pending(session.Data, state);
                builder.Append("(この旅で +").Append(NumberFormat.Japanese(pending)).Append(")\n");
            }
            else
            {
                builder.Append("温泉のおみやげ 見込み ").Append(NumberFormat.Japanese(Prestige.Pending(session.Data, state))).Append("個\n");
            }

            if (state.HasTripTicket)
            {
                // While holding the ticket, the question is whether one more souvenir is worth waiting for.
                builder.Append("あと").Append(NumberFormat.Yen(Prestige.EarningsForNext(session.Data, state))).Append("稼ぐと おみやげ+1\n");
            }

            builder.Append("スタンプ ").Append(state.Stamps.Count).Append('/').Append(StampCard.All.Count)
                .Append("(買取 +").Append((state.Stamps.Count * session.Data.StampBonus * 100).ToString("0")).Append("%)\n");
            foreach (var item in session.Data.Items)
            {
                var count = state.GetItem(item.Id);
                if (count > 0)
                {
                    builder.Append(item.DisplayName).Append(' ').Append(NumberFormat.Japanese(count)).Append('\n');
                }
            }

            AppendGoal(session);
            AppendNextReveal(session);
            inventoryText.text = builder.ToString();
        }

        // Shows the next upgrade to aim for, so there is always a visible goal.
        void AppendGoal(GameSession session)
        {
            var goal = session.Progression.NextGoal();
            if (goal == null)
            {
                return;
            }

            builder.Append("\n次の目標: ").Append(goal.Upgrade.DisplayName).Append(' ').Append(NumberFormat.Yen(goal.Cost)).Append('\n');
            if (goal.Affordable)
            {
                builder.Append("スーパーで買えます!\n");
                return;
            }

            if (goal.MoneyShort > 0)
            {
                builder.Append("あと").Append(NumberFormat.Yen(goal.MoneyShort)).Append(' ');
            }

            if (goal.MissingItemCount > 0)
            {
                builder.Append(goal.MissingItem.DisplayName).Append("があと").Append(goal.MissingItemCount).Append("個");
            }

            builder.Append('\n');
        }

        // Teases the next product without naming it, so there is something to wonder about.
        void AppendNextReveal(GameSession session)
        {
            var next = session.Progression.NextReveal();
            if (next != null)
            {
                builder.Append("\n次の入荷: ???\n").Append(session.Progression.RevealHint(next));
            }
        }

        void QueueMessage(string message)
        {
            if (messages.Count >= MaxQueuedMessages)
            {
                messages.Dequeue();
            }

            messages.Enqueue(message);

            // Let the current message give way sooner when others are waiting.
            messageTimer = Mathf.Min(messageTimer, BusyMessageDuration);
        }

        void UpdateMessages()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer > 0f)
                {
                    return;
                }
            }

            if (messages.Count == 0)
            {
                messageText.text = string.Empty;
                return;
            }

            // Show messages one after another, faster when several are waiting.
            messageText.text = messages.Dequeue();
            messageTimer = messages.Count > 0 ? BusyMessageDuration : MessageDuration;
        }
    }
}
