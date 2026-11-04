using System;
using Garapon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Garapon.Game
{
    public sealed class EndingController : MonoBehaviour
    {
        const string TownScene = "Town";

        [SerializeField] GameDataAsset dataAsset;
        [SerializeField] TMP_Text resultText;

        GameState state;

        void Start()
        {
            state = SaveFile.Load();
            var data = dataAsset.Data;
            var next = Prestige.NextRun(data, state);
            var bonus = 1 + next.Clears * data.ClearBonus;
            resultText.text =
                "温泉旅行に出発!\n\n" +
                (state.TripWonSeconds > 0 ? $"金玉までの時間 {Clock(state.TripWonSeconds)}\n" : string.Empty) +
                $"プレイ時間 {Clock(state.PlaySeconds)}\n" +
                (next.BestClearSeconds > 0 ? $"最速記録 {Clock(next.BestClearSeconds)}\n" : string.Empty) +
                $"抽選回数 {NumberFormat.Japanese(state.TotalDraws)}回\n" +
                $"スタンプ {state.Stamps.Count}/{StampCard.All.Count}\n" +
                $"おみやげ +{NumberFormat.Japanese(next.SouvenirsEarned - state.SouvenirsEarned)}個(手持ち {NumberFormat.Japanese(next.Souvenirs)}個)\n" +
                "町のおみやげ交換所で、ずっと残る特典と交換できます\n\n" +
                $"[R] {next.Clears + 1}周目へ(思い出ボーナス 買取×{bonus}、スタンプ・おみやげ・特典は持ち越し)\n" +
                "[X] 記録も消して最初から";
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                SaveFile.Save(Prestige.NextRun(dataAsset.Data, state));
                SceneManager.LoadScene(TownScene);
            }
            else if (keyboard.xKey.wasPressedThisFrame)
            {
                SaveFile.Delete();
                SceneManager.LoadScene(TownScene);
            }
        }

        static string Clock(double seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{(int)span.TotalHours}時間{span.Minutes}分";
        }
    }
}
