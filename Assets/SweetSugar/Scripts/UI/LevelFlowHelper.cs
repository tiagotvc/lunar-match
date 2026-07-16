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
            // TEMP diagnostic logging - remove once the grid-click black-screen bug is found.
            // Compare this log trail between a working JOGAR click and a broken grid click.
            Debug.Log($"[LevelFlowHelper] PlayLevel({level}) start. GUIUtils.THIS={(GUIUtils.THIS != null ? "ok" : "NULL")}, LevelManager.THIS={(LevelManager.THIS != null ? "ok" : "NULL")}, gameStatus BEFORE={LevelManager.GetGameStatus()}, lifes={InitScript.lifes}");

            InitScript.OpenMenuPlay(level);
            Debug.Log($"[LevelFlowHelper] After OpenMenuPlay: gameStatus={LevelManager.GetGameStatus()}, OpenLevel pref={PlayerPrefs.GetInt("OpenLevel")}");

            if (GUIUtils.THIS != null)
            {
                // NOTE: if the player has 0 lives, StartGame() opens the life shop instead of
                // starting the level - same limitation the original MenuPlay flow has.
                GUIUtils.THIS.StartGame();
            }
            Debug.Log($"[LevelFlowHelper] After StartGame: gameStatus={LevelManager.GetGameStatus()}, fieldBoards.Count={LevelManager.THIS?.fieldBoards.Count}, levelLoaded={LevelManager.THIS?.levelLoaded}");

            var menuPlay = ReferenceRestorer.FindMenuPlay();
            if (menuPlay != null)
            {
                menuPlay.SetActive(false);
            }
            Debug.Log($"[LevelFlowHelper] PlayLevel({level}) end. Level active={GameObject.Find("Level")?.activeSelf}, Main Camera pos={Camera.main.transform.position}, orthoSize={Camera.main.orthographicSize}");
        }
    }
}
