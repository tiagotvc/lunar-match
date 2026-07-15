using SweetSugar.Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Editor
{
    // Builds the placeholder "choose a chapter, then Play" screen (Sweet Sugar > Build World
    // Select Screen), reusing the WorldSelectController/ChapterCardView runtime scripts.
    // Structure only - no final art (logo, chapter illustrations, avatar) exists in the
    // project yet, so cards render as flat color panels until that art is swapped in.
    public static class BuildWorldSelectScreen
    {
        private const string ScreenName = "WorldSelect";
        private static readonly Color PanelBg = new Color(0.11f, 0.08f, 0.24f, 0.96f);
        private static readonly Color CardBg = new Color(0.28f, 0.18f, 0.5f, 1f);
        private static readonly Color AccentGold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color PlayTeal = new Color(0.15f, 0.78f, 0.75f);

        [MenuItem("Sweet Sugar/Build World Select Screen")]
        public static void Build()
        {
            var canvasGlobal = GameObject.Find("CanvasGlobal");
            if (canvasGlobal == null)
            {
                Debug.LogError("Could not find \"CanvasGlobal\" in the open scene - is gameStatic.unity open?");
                return;
            }

            var canvasGlobalRect = canvasGlobal.GetComponent<RectTransform>();
            if (canvasGlobalRect == null)
            {
                Debug.LogError("\"CanvasGlobal\" has no RectTransform - is it really the UI canvas root?");
                return;
            }

            var existing = GameObject.Find(ScreenName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var screen = CreateUIObject(ScreenName, canvasGlobalRect);
            StretchFull(screen);

            var bg = CreateUIObject("Background", screen);
            StretchFull(bg);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = PanelBg;

            var titleText = CreateText(screen, "Title", "LUNAR MATCH", 64, AccentGold, FontStyles.Bold);
            AnchorStretch(titleText.rectTransform, 0f, 1f, 0.82f, 0.96f);

            var cardArea = CreateUIObject("CardArea", screen);
            AnchorStretch(cardArea, 0f, 1f, 0.34f, 0.74f);

            var leftArrow = CreateArrowButton(cardArea, "LeftArrow", "<", new Vector2(0.06f, 0.5f));
            var rightArrow = CreateArrowButton(cardArea, "RightArrow", ">", new Vector2(0.94f, 0.5f));

            var leftCardView = CreateCard(cardArea, "CardLeft", new Vector2(-280, 0), 200, 300);
            var centerCardView = CreateCard(cardArea, "CardCenter", Vector2.zero, 220, 330);
            var rightCardView = CreateCard(cardArea, "CardRight", new Vector2(280, 0), 200, 300);

            var pageDots = CreateUIObject("PageDots", screen);
            AnchorStretch(pageDots, 0f, 1f, 0.29f, 0.33f);
            var pageDotsLayout = pageDots.gameObject.AddComponent<HorizontalLayoutGroup>();
            pageDotsLayout.childAlignment = TextAnchor.MiddleCenter;
            pageDotsLayout.spacing = 12;
            pageDotsLayout.childForceExpandWidth = false;
            pageDotsLayout.childForceExpandHeight = false;

            var dotTemplate = CreateUIObject("DotTemplate", pageDots);
            dotTemplate.sizeDelta = new Vector2(14, 14);
            var dotImage = dotTemplate.gameObject.AddComponent<Image>();
            dotImage.color = new Color(1f, 1f, 1f, 0.4f);
            dotTemplate.gameObject.SetActive(false);

            var playButton = CreateButton(screen, "PlayButton", "JOGAR", PlayTeal, Color.white, 40);
            var playRect = playButton.GetComponent<RectTransform>();
            AnchorStretch(playRect, 0.28f, 0.72f, 0.16f, 0.24f);

            var bottomNav = CreateUIObject("BottomNav", screen);
            AnchorStretch(bottomNav, 0f, 1f, 0f, 0.09f);
            var navLayout = bottomNav.gameObject.AddComponent<HorizontalLayoutGroup>();
            navLayout.childAlignment = TextAnchor.MiddleCenter;
            navLayout.childForceExpandWidth = true;
            navLayout.childForceExpandHeight = true;
            navLayout.padding = new RectOffset(10, 10, 6, 6);

            CreateNavTab(bottomNav, "Loja");
            CreateNavTab(bottomNav, "Eventos");
            CreateNavTab(bottomNav, "Jornada", true);
            CreateNavTab(bottomNav, "Ranking");
            CreateNavTab(bottomNav, "Opcoes");

            var controller = screen.gameObject.AddComponent<WorldSelectController>();
            controller.leftCard = leftCardView;
            controller.centerCard = centerCardView;
            controller.rightCard = rightCardView;
            controller.pageDotsParent = pageDots;
            controller.pageDotPrefab = dotTemplate.gameObject;
            controller.playButton = playButton;

            UnityEventTools.AddPersistentListener(leftArrow.onClick, controller.OnLeftArrow);
            UnityEventTools.AddPersistentListener(rightArrow.onClick, controller.OnRightArrow);
            UnityEventTools.AddPersistentListener(playButton.onClick, controller.OnPlayPressed);

            screen.gameObject.SetActive(true);
            Selection.activeGameObject = screen.gameObject;
            EditorUtility.SetDirty(canvasGlobal);

            Debug.Log("Built WorldSelect screen under CanvasGlobal. Save the scene (Ctrl+S) to keep it.");
        }

        private static ChapterCardView CreateCard(RectTransform parent, string name, Vector2 anchoredPosition, float width, float height)
        {
            var card = CreateUIObject(name, parent);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = anchoredPosition;
            card.sizeDelta = new Vector2(width, height);

            var image = card.gameObject.AddComponent<Image>();
            image.color = CardBg;

            var label = CreateText(card, "ChapterLabel", "CAPITULO", 18, AccentGold, FontStyles.Bold);
            AnchorStretch(label.rectTransform, 0f, 1f, 0.86f, 0.98f);

            var titleText = CreateText(card, "TitleText", "Chapter", 24, Color.white, FontStyles.Bold);
            AnchorStretch(titleText.rectTransform, 0f, 1f, 0.42f, 0.62f);

            var starsText = CreateText(card, "StarsText", "0/0", 20, AccentGold, FontStyles.Normal);
            AnchorStretch(starsText.rectTransform, 0f, 1f, 0.04f, 0.16f);

            var view = card.gameObject.AddComponent<ChapterCardView>();
            view.cardRoot = card;
            view.chapterImage = image;
            view.chapterLabel = label;
            view.titleText = titleText;
            view.starsText = starsText;

            return view;
        }

        private static Button CreateArrowButton(RectTransform parent, string name, string label, Vector2 anchor)
        {
            var go = CreateUIObject(name, parent);
            go.anchorMin = go.anchorMax = anchor;
            go.pivot = new Vector2(0.5f, 0.5f);
            go.anchoredPosition = Vector2.zero;
            go.sizeDelta = new Vector2(60, 60);

            var image = go.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);

            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(go, "Label", label, 32, Color.white, FontStyles.Bold);
            StretchFull(labelText.rectTransform);

            return button;
        }

        private static void CreateNavTab(RectTransform parent, string label, bool selected = false)
        {
            var go = CreateUIObject(label, parent);
            var image = go.gameObject.AddComponent<Image>();
            image.color = selected ? new Color(0.4f, 0.25f, 0.7f, 1f) : new Color(1f, 1f, 1f, 0.05f);

            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go, "Label", label, 16, selected ? AccentGold : new Color(1f, 1f, 1f, 0.7f), FontStyles.Normal);
            StretchFull(text.rectTransform);
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Color bg, Color textColor, int fontSize)
        {
            var go = CreateUIObject(name, parent);
            var image = go.gameObject.AddComponent<Image>();
            image.color = bg;

            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go, "Label", label, fontSize, textColor, FontStyles.Bold);
            StretchFull(text.rectTransform);

            return button;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, string content, int fontSize, Color color, FontStyles style)
        {
            var rect = CreateUIObject(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static RectTransform CreateUIObject(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AnchorStretch(RectTransform rect, float xMin, float xMax, float yMin, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
