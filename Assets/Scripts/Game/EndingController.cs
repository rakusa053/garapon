using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Garapon.Game
{
    public sealed class EndingController : MonoBehaviour
    {
        const string TownScene = "Town";

        [SerializeField] TMP_Text resultText;

        void Start()
        {
            var state = SaveFile.Load();
            var playTime = TimeSpan.FromSeconds(state.PlaySeconds);
            resultText.text =
                "温泉旅行に出発!\n\n" +
                $"プレイ時間 {(int)playTime.TotalHours}時間{playTime.Minutes}分\n" +
                $"抽選回数 {state.TotalDraws:N0}回\n\n" +
                "[R] 最初からやり直す";
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                SaveFile.Delete();
                SceneManager.LoadScene(TownScene);
            }
        }
    }
}
