using Garapon.Core;
using TMPro;
using UnityEngine;

namespace Garapon.Game
{
    // The bus stop for the hot spring. The gold ball wins a trip ticket, and the player leaves from here
    // when they choose, so earning a little longer for more souvenirs is a decision of their own.
    public sealed class HotSpringBusStop : Interactable
    {
        // Leaving ends the run, so it takes a second press within this many seconds.
        const float ConfirmSeconds = 4f;

        [SerializeField] TMP_Text label;

        float confirmUntil = -1f;

        bool Confirming => Time.unscaledTime < confirmUntil;

        public override string Prompt
        {
            get
            {
                var session = GameSession.Instance;
                if (!Prestige.CanDepart(session.State))
                {
                    return "温泉行きバス停(金玉をひくと温泉旅行券がもらえます)";
                }

                var souvenirs = NumberFormat.Japanese(Prestige.Pending(session.Data, session.State));
                return Confirming
                    ? $"もう一度押すと出発します(おみやげ +{souvenirs}個)"
                    : $"温泉へ出発する(おみやげ +{souvenirs}個)";
            }
        }

        void Update()
        {
            var session = GameSession.Instance;
            label.text = Prestige.CanDepart(session.State)
                ? $"温泉行きバス停\nおみやげ +{NumberFormat.Japanese(Prestige.Pending(session.Data, session.State))}個"
                : "温泉行きバス停\n旅行券が必要";
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            if (!Prestige.CanDepart(session.State))
            {
                session.Post("温泉旅行券がありません。ガラポンで金玉をひきましょう");
                return;
            }

            if (!Confirming)
            {
                confirmUntil = Time.unscaledTime + ConfirmSeconds;
                session.Post("出発するとお金と強化はリセットされます。もう一度 [E] で出発");
                return;
            }

            confirmUntil = -1f;
            session.DepartForHotSpring();
        }
    }
}
