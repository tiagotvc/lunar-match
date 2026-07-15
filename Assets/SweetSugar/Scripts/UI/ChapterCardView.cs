using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.UI
{
    // One card in the world-select carousel (left/center/right slot). Placeholder-art only -
    // chapterImage stays a flat color panel until real chapter art exists.
    public class ChapterCardView : MonoBehaviour
    {
        public Image chapterImage;
        public TextMeshProUGUI chapterLabel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI starsText;
        public RectTransform cardRoot;

        [Header("Highlight tuning")]
        public float centerScale = 1.15f;
        public float sideScale = 0.85f;
        public float centerAlpha = 1f;
        public float sideAlpha = 0.6f;

        public void Bind(WorldSelectController.Chapter chapter, bool isCenter)
        {
            if (titleText != null) titleText.text = chapter.title;
            if (starsText != null) starsText.text = $"{chapter.placeholderStarsEarned}/{chapter.placeholderStarsTotal}";

            var chapterNumber = System.Array.IndexOf(FindController()?.chapters ?? new WorldSelectController.Chapter[0], chapter) + 1;
            if (chapterLabel != null) chapterLabel.text = chapterNumber > 0 ? $"CAPITULO {chapterNumber}" : string.Empty;

            if (cardRoot != null)
            {
                cardRoot.localScale = Vector3.one * (isCenter ? centerScale : sideScale);
            }

            if (chapterImage != null)
            {
                var c = chapterImage.color;
                c.a = isCenter ? centerAlpha : sideAlpha;
                chapterImage.color = c;
            }
        }

        private WorldSelectController FindController()
        {
            return GetComponentInParent<WorldSelectController>();
        }
    }
}
