using SweetSugar.Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Editor
{
    // Builds the scrollable level-select grid (Sweet Sugar > Build Level Select Screen) and
    // adds an "Open Level Select" button to the top of the already-built WorldSelect screen.
    // Run "Sweet Sugar > Build World Select Screen" first if WorldSelect doesn't exist yet.
    public static class BuildLevelSelectScreen
    {
        private const string ScreenName = "LevelSelect";
        private static readonly Color PanelBg = new Color(0.08f, 0.06f, 0.18f, 0.98f);
        private static readonly Color AccentGold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color CellUnlocked = new Color(0.28f, 0.18f, 0.5f, 1f);

        [MenuItem("Sweet Sugar/Build Level Select Screen")]
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
            screen.gameObject.SetActive(false); // opened on demand from WorldSelect

            var bg = CreateUIObject("Background", screen);
            StretchFull(bg);
            bg.gameObject.AddComponent<Image>().color = PanelBg;

            var title = CreateText(screen, "Title", "SELECIONE A FASE", 36, AccentGold, FontStyles.Bold);
            AnchorStretch(title.rectTransform, 0f, 1f, 0.9f, 0.98f);

            var backButton = CreateButton(screen, "BackButton", "< Voltar", new Color(1f, 1f, 1f, 0.15f), Color.white, 22);
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.03f, 0.9f);
            backRect.anchorMax = new Vector2(0.28f, 0.98f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            var scrollArea = CreateUIObject("ScrollArea", screen);
            AnchorStretch(scrollArea, 0.04f, 0.96f, 0.04f, 0.88f);
            scrollArea.gameObject.AddComponent<RectMask2D>();
            var scrollRect = scrollArea.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var content = CreateUIObject("Content", scrollArea);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(16, 16);
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = content;
            scrollRect.viewport = scrollArea;

            var cellTemplate = CreateLevelCell(content);
            cellTemplate.gameObject.SetActive(false);

            var controller = screen.gameObject.AddComponent<LevelSelectController>();
            controller.gridParent = content;
            controller.cellTemplate = cellTemplate.gameObject;

            UnityEventTools.AddPersistentListener(backButton.onClick, controller.OnBackPressed);

            EditorUtility.SetDirty(canvasGlobal);
            Debug.Log("Built LevelSelect screen under CanvasGlobal (inactive by default).");

            var worldSelectController = WireOpenButtonOnWorldSelect(canvasGlobalRect, screen.gameObject);
            if (worldSelectController != null)
            {
                controller.worldSelectScreen = worldSelectController.gameObject;
            }

            Selection.activeGameObject = screen.gameObject;
            Debug.Log("Save the scene (Ctrl+S) to keep it.");
        }

        // Returns the found WorldSelectController (or null) so the caller can cross-wire
        // LevelSelectController.worldSelectScreen too - both screens need a direct reference
        // to each other since GameObject.Find can't find whichever one is currently inactive.
        private static WorldSelectController WireOpenButtonOnWorldSelect(RectTransform canvasGlobalRect, GameObject levelSelectScreen)
        {
            var worldSelectTransform = canvasGlobalRect.Find(WorldSelectScreenName());
            if (worldSelectTransform == null)
            {
                Debug.LogWarning("\"WorldSelect\" not found under CanvasGlobal - run Sweet Sugar > Build World Select Screen first, then re-run this to add the open button.");
                return null;
            }

            var worldSelect = worldSelectTransform.GetComponent<RectTransform>();

            var existingOpenButton = worldSelect.Find("OpenLevelSelectButton");
            if (existingOpenButton != null)
            {
                Object.DestroyImmediate(existingOpenButton.gameObject);
            }

            var worldSelectController = worldSelect.GetComponent<WorldSelectController>();
            if (worldSelectController == null)
            {
                Debug.LogWarning("WorldSelect has no WorldSelectController component - can't wire the open button.");
                return null;
            }

            worldSelectController.levelSelectScreen = levelSelectScreen;

            var openButton = CreateButton(worldSelect, "OpenLevelSelectButton", "Fases", new Color(1f, 1f, 1f, 0.18f), Color.white, 22);
            var rect = openButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.36f, 0.96f);
            rect.anchorMax = new Vector2(0.64f, 1.02f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UnityEventTools.AddPersistentListener(openButton.onClick, worldSelectController.OnOpenLevelSelect);

            return worldSelectController;
        }

        private static string WorldSelectScreenName() => "WorldSelect";

        private static RectTransform CreateLevelCell(RectTransform parent)
        {
            var cell = CreateUIObject("LevelCell", parent);
            cell.sizeDelta = new Vector2(130, 130);

            var bg = cell.gameObject.AddComponent<Image>();
            bg.color = CellUnlocked;

            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            var numberText = CreateText(cell, "Number", "0", 32, Color.white, FontStyles.Bold);
            AnchorStretch(numberText.rectTransform, 0f, 1f, 0.3f, 0.85f);

            var starsText = CreateText(cell, "Stars", "", 18, AccentGold, FontStyles.Normal);
            AnchorStretch(starsText.rectTransform, 0f, 1f, 0.05f, 0.28f);

            var lockIcon = CreateText(cell, "LockIcon", "LOCK", 16, new Color(1f, 1f, 1f, 0.6f), FontStyles.Bold);
            AnchorStretch(lockIcon.rectTransform, 0f, 1f, 0.4f, 0.6f);
            lockIcon.gameObject.SetActive(false);

            var view = cell.gameObject.AddComponent<LevelButtonView>();
            view.button = button;
            view.background = bg;
            view.numberText = numberText;
            view.starsText = starsText;
            view.lockIcon = lockIcon.gameObject;

            return cell;
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
