using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI;
using SweetSugar.Scripts.GUI.Utils;
using UnityEngine;

namespace SweetSugar.Scripts.UI
{
    // Shared "actually start playing level N" logic, used by both WorldSelectController's
    // Play button and LevelSelectController's grid buttons - see WorldSelectController's
    // original OnPlayPressed for how this sequence was traced from the legacy MenuPlay popup
    // flow (InitScript.OpenMenuPlay + GUIUtils.StartGame, skipping the popup's own second
    // confirmation tap since we want a single tap here).
    public static class LevelFlowHelper
    {
        public const int TotalLevels = 100;

        public static int ReachedLevel => PlayerPrefs.GetInt("ReachedLevel", 1);

        public static int StarsFor(int level)
        {
            return PlayerPrefs.GetInt($"Level.{level:000}.StarsCount", 0);
        }

        public static bool IsUnlocked(int level)
        {
            return level <= ReachedLevel;
        }

        public static void PlayLevel(int level)
        {
            InitScript.OpenMenuPlay(level);

            if (GUIUtils.THIS != null)
            {
                // NOTE: if the player has 0 lives, StartGame() opens the life shop instead of
                // starting the level - same limitation the original MenuPlay flow has.
                GUIUtils.THIS.StartGame();
            }

            var menuPlay = ReferenceRestorer.FindMenuPlay();
            if (menuPlay != null)
            {
                menuPlay.SetActive(false);
            }
        }
    }
}
