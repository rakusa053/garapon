using Garapon.Core;
using TMPro;
using UnityEngine;

namespace Garapon.Game
{
    // One counter of the souvenir exchange in town, where souvenirs from the hot-spring trip
    // are traded for perks that last through every later run.
    public sealed class PerkCounter : Interactable
    {
        [SerializeField] string perkId;
        [SerializeField] TMP_Text label;

        PerkDef Perk => GameSession.Instance.Data.FindPerk(perkId);

        // The exchange opens once the first trip has brought souvenirs home.
        public override bool IsAvailable => GameSession.Instance.State.SouvenirsEarned > 0;

        public override string Prompt
        {
            get
            {
                var state = GameSession.Instance.State;
                var perk = Perk;
                return Prestige.IsMaxed(state, perk)
                    ? $"{perk.DisplayName}(交換済み)"
                    : $"{perk.DisplayName} Lv{state.GetPerkLevel(perk.Id) + 1} おみやげ{Prestige.Cost(state, perk)}個と交換({UpgradeText.Effect(perk)})";
            }
        }

        void Update()
        {
            if (!IsAvailable)
            {
                label.text = "おみやげ交換所\n準備中";
                return;
            }

            var state = GameSession.Instance.State;
            var perk = Perk;
            label.text = Prestige.IsMaxed(state, perk)
                ? $"{perk.DisplayName}\n交換済み"
                : $"{perk.DisplayName} Lv{state.GetPerkLevel(perk.Id)}\nおみやげ{Prestige.Cost(state, perk)}個";
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            var perk = Perk;
            if (Prestige.IsMaxed(session.State, perk))
            {
                session.Post("これ以上は交換できません");
                return;
            }

            if (!Prestige.Buy(session.State, perk))
            {
                session.Post($"おみやげが{Prestige.Cost(session.State, perk) - session.State.Souvenirs}個足りません");
                return;
            }

            session.Post($"{perk.DisplayName}と交換しました: {UpgradeText.Effect(perk)}");
            SaveFile.Save(session.State);
        }
    }
}
