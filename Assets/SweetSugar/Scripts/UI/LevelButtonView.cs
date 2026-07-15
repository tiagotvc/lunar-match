using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.UI
{
    // One cell in the level-select grid.
    //
    // NOTE: uses "using System" + unqualified Action<int> rather than "System.Action<int>"
    // inline - this namespace (SweetSugar.Scripts.UI) shares the SweetSugar.Scripts parent
    // with SweetSugar.Scripts.System (Scripts/System/AnimateItems.cs), which shadows the real
    // .NET System namespace for inline "System.X" references (see WorldSelectController.cs
    // for the full explanation - same fix applied there).
    public class LevelButtonView : MonoBehaviour
    {
        public Button button;
        public Image background;
        public TextMeshProUGUI numberText;
        public GameObject lockIcon;
        public TextMeshProUGUI starsText;

        private static readonly Color UnlockedColor = new Color(0.28f, 0.18f, 0.5f, 1f);
        private static readonly Color LockedColor = new Color(0.15f, 0.12f, 0.22f, 1f);

        public void Bind(int level, Action<int> onPressed)
        {
            var unlocked = LevelFlowHelper.IsUnlocked(level);
            var stars = LevelFlowHelper.StarsFor(level);

            if (numberText != null)
            {
                numberText.text = level.ToString();
                numberText.gameObject.SetActive(unlocked);
            }

            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (background != null) background.color = unlocked ? UnlockedColor : LockedColor;

            if (starsText != null)
            {
                starsText.gameObject.SetActive(unlocked && stars > 0);
                starsText.text = new string('*', stars);
            }

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                if (unlocked)
                {
                    button.onClick.AddListener(() => onPressed(level));
                }
            }
        }
    }
}
