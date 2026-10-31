using System.Text;
using Garapon.Core;
using TMPro;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class GameHud : MonoBehaviour
    {
        const float MessageDuration = 3f;

        [SerializeField] PlayerController player;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text inventoryText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text messageText;

        readonly StringBuilder builder = new StringBuilder();
        float messageTimer;

        void Start()
        {
            GameSession.Instance.MessagePosted += ShowMessage;
            messageText.text = string.Empty;
        }

        void OnDestroy()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.MessagePosted -= ShowMessage;
            }
        }

        void Update()
        {
            var session = GameSession.Instance;
            var state = session.State;

            builder.Clear();
            builder.Append("所持金 ").Append(NumberFormat.Yen(state.Money)).Append('\n');
            builder.Append("ごみの査定額 ").Append(NumberFormat.Yen(state.TrashValue));
            var robotYenPerSecond = (long)(session.Upgrades.AutoCollectPerSecond * session.Data.AverageTrashValue());
            if (robotYenPerSecond > 0)
            {
                builder.Append("(ロボ 毎秒+").Append(NumberFormat.Japanese(robotYenPerSecond)).Append(')');
            }

            builder.Append('\n');
            builder.Append("抽選券 ").Append(NumberFormat.Japanese(state.Tickets)).Append("枚\n");
            builder.Append("カゴ ").Append(session.Cart.Count).Append("点 ").Append(NumberFormat.Yen(session.CartTotal())).Append('\n');
            builder.Append("抽選回数 ").Append(NumberFormat.Japanese(state.TotalDraws)).Append("回\n");
            var untilGuarantee = session.Lottery.DrawsUntilGuarantee;
            if (untilGuarantee > 0)
            {
                builder.Append("金玉確定まで あと").Append(NumberFormat.Japanese(untilGuarantee)).Append("回");
            }

            statusText.text = builder.ToString();

            builder.Clear();
            foreach (var item in session.Data.Items)
            {
                var count = state.GetItem(item.Id);
                if (count > 0)
                {
                    builder.Append(item.DisplayName).Append(' ').Append(NumberFormat.Japanese(count)).Append('\n');
                }
            }

            AppendGoal(session);
            inventoryText.text = builder.ToString();

            var target = player.Target;
            promptText.text = target == null ? string.Empty : $"[E] {target.Prompt}";

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                {
                    messageText.text = string.Empty;
                }
            }
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
                builder.Append("スーパーで買えます!");
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
        }

        void ShowMessage(string message)
        {
            messageText.text = message;
            messageTimer = MessageDuration;
        }
    }
}
