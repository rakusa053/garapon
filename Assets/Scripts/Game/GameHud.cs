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

            statusText.text =
                $"所持金 {state.Money:N0}円\n" +
                $"ごみの査定額 {state.TrashValue:N0}円\n" +
                $"抽選券 {state.Tickets:N0}枚\n" +
                $"カゴ {session.Cart.Count}点 {session.CartTotal():N0}円\n" +
                $"抽選回数 {state.TotalDraws:N0}回";

            builder.Clear();
            foreach (var item in session.Data.Items)
            {
                var count = state.GetItem(item.Id);
                if (count > 0)
                {
                    builder.Append(item.DisplayName).Append(" ").Append(count.ToString("N0")).Append('\n');
                }
            }

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

        void ShowMessage(string message)
        {
            messageText.text = message;
            messageTimer = MessageDuration;
        }
    }
}
