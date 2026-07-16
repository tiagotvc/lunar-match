using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.UI
{
    // Drives the "choose a world/chapter, then Play" screen. Structure is built by the
    // editor tool Sweet Sugar > Build World Select Screen; this component just needs its
    // card slot references wired up in the Inspector (the builder does that automatically).
    //
    // V1 scope: chapters are a hardcoded array mapped onto the existing 100-level board in
    // 20-level chunks (matches BackgroundsForStaticMap's existing chunking). Star totals are
    // placeholders (not yet reading real per-level star data) - wire that up once the real
    // chapter/star data model exists.
    //
    // NOTE: this file lives under the SweetSugar.Scripts.UI namespace, which shares the
    // SweetSugar.Scripts parent with an existing SweetSugar.Scripts.System namespace (from
    // Scripts/System/AnimateItems.cs) - that shadows the real .NET System namespace for
    // unqualified "System.X" references here, hence the explicit "using System" instead of
    // writing "[System.Serializable]" inline.
    public class WorldSelectController : MonoBehaviour
    {
        [Serializable]
        public class Chapter
        {
            public string title;
            public int firstLevel;
            public int levelCount;
            public int placeholderStarsEarned;
            public int placeholderStarsTotal;
        }

        [Header("Data (placeholder)")]
        public Chapter[] chapters =
        {
            new Chapter { title = "Vale da Brisa", firstLevel = 1, levelCount = 20, placeholderStarsEarned = 30, placeholderStarsTotal = 30 },
            new Chapter { title = "Luminaris", firstLevel = 21, levelCount = 20, placeholderStarsEarned = 24, placeholderStarsTotal = 30 },
            new Chapter { title = "Pico Estelar", firstLevel = 41, levelCount = 20, placeholderStarsEarned = 8, placeholderStarsTotal = 30 },
            new Chapter { title = "Capitulo 4", firstLevel = 61, levelCount = 20, placeholderStarsEarned = 0, placeholderStarsTotal = 30 },
            new Chapter { title = "Capitulo 5", firstLevel = 81, levelCount = 20, placeholderStarsEarned = 0, placeholderStarsTotal = 30 },
        };

        [Header("Card slots (assigned by the builder tool)")]
        public ChapterCardView leftCard;
        public ChapterCardView centerCard;
        public ChapterCardView rightCard;
        public Transform pageDotsParent;
        public GameObject pageDotPrefab;
        public Button playButton;

        [Header("Level select (assigned by the builder tool)")]
        // GameObject.Find does not find inactive objects, and LevelSelect starts inactive -
        // a direct reference (set by BuildLevelSelectScreen.cs) avoids that gotcha entirely.
        public GameObject levelSelectScreen;

        private int _currentIndex;
        private Image[] _pageDots;

        private void Awake()
        {
            // The old LevelsMap (world-space map sprites + its own CanvasMap) competes with
            // this screen for the view - LevelManager's gameStatus setter calls EnableMap(true)
            // on boot (gameStatus starts as GameState.Map), which repositions Camera.main to
            // frame the map's world-space content. Disabling LevelsMap outright removes it from
            // the scene entirely instead of fighting Canvas sort order / camera framing.
            var levelsMap = GameObject.Find("LevelsMap");
            if (levelsMap != null)
            {
                levelsMap.SetActive(false);
            }
        }

        private void Start()
        {
            BuildPageDots();
            Refresh();
        }

        public void OnLeftArrow()
        {
            _currentIndex = (_currentIndex - 1 + chapters.Length) % chapters.Length;
            Refresh();
        }

        public void OnRightArrow()
        {
            _currentIndex = (_currentIndex + 1) % chapters.Length;
            Refresh();
        }

        public void OnPlayPressed()
        {
            var chapter = chapters[_currentIndex];
            var reached = LevelFlowHelper.ReachedLevel;
            var levelToOpen = reached;
            if (reached < chapter.firstLevel || reached >= chapter.firstLevel + chapter.levelCount)
            {
                // Reached level isn't inside this chapter - open the chapter's first level instead.
                levelToOpen = chapter.firstLevel;
            }

            LevelFlowHelper.PlayLevel(levelToOpen);
            gameObject.SetActive(false);
        }

        public void OnOpenLevelSelect()
        {
            if (levelSelectScreen != null)
            {
                levelSelectScreen.SetActive(true);
            }
        }

        private void Refresh()
        {
            var leftIndex = (_currentIndex - 1 + chapters.Length) % chapters.Length;
            var rightIndex = (_currentIndex + 1) % chapters.Length;

            if (leftCard != null) leftCard.Bind(chapters[leftIndex], false);
            if (centerCard != null) centerCard.Bind(chapters[_currentIndex], true);
            if (rightCard != null) rightCard.Bind(chapters[rightIndex], false);

            for (var i = 0; i < _pageDots.Length; i++)
            {
                _pageDots[i].color = i == _currentIndex ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.4f);
            }
        }

        private void BuildPageDots()
        {
            if (pageDotsParent == null || pageDotPrefab == null)
            {
                _pageDots = new Image[0];
                return;
            }

            _pageDots = new Image[chapters.Length];
            for (var i = 0; i < chapters.Length; i++)
            {
                var dot = Instantiate(pageDotPrefab, pageDotsParent);
                dot.SetActive(true);
                _pageDots[i] = dot.GetComponent<Image>();
            }
        }
    }
}
