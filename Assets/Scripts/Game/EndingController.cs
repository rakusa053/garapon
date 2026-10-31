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
            var next = state.NextRun();
            var bonus = dataAsset != null ? 1 + next.Clears * dataAsset.Data.ClearBonus : next.Clears + 1;
            resultText.text =
                "温泉旅行に出発!\n\n" +
                $"プレイ時間 {Clock(state.PlaySeconds)}\n" +
                (next.BestClearSeconds > 0 ? $"最速記録 {Clock(next.BestClearSeconds)}\n" : string.Empty) +
                $"抽選回数 {NumberFormat.Japanese(state.TotalDraws)}回\n" +
                $"スタンプ {state.Stamps.Count}/{StampCard.All.Count}\n\n" +
                $"[R] {next.Clears + 1}周目へ(思い出ボーナス 買取×{bonus}、スタンプは持ち越し)\n" +
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
                SaveFile.Save(state.NextRun());
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
