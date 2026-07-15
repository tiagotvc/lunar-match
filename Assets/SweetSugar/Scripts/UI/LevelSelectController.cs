using UnityEngine;

namespace SweetSugar.Scripts.UI
{
    // Scrollable grid of level buttons. Structure built by
    // Sweet Sugar > Build Level Select Screen (Assets/SweetSugar/Editor/
    // BuildLevelSelectScreen.cs). Opened via WorldSelectController.OnOpenLevelSelect().
    public class LevelSelectController : MonoBehaviour
    {
        public Transform gridParent;
        public GameObject cellTemplate;

        private bool _built;

        private void OnEnable()
        {
            if (!_built)
            {
                BuildGrid();
                _built = true;
            }
            else
            {
                RefreshGrid();
            }
        }

        public void OnBackPressed()
        {
            gameObject.SetActive(false);
        }

        private void BuildGrid()
        {
            if (gridParent == null || cellTemplate == null)
            {
                return;
            }

            for (var level = 1; level <= LevelFlowHelper.TotalLevels; level++)
            {
                var cell = Instantiate(cellTemplate, gridParent);
                cell.SetActive(true);
                var view = cell.GetComponent<LevelButtonView>();
                if (view != null)
                {
                    view.Bind(level, OnLevelPressed);
                }
            }
        }

        private void RefreshGrid()
        {
            var index = 0;
            foreach (Transform child in gridParent)
            {
                index++;
                var view = child.GetComponent<LevelButtonView>();
                if (view != null)
                {
                    view.Bind(index, OnLevelPressed);
                }
            }
        }

        private void OnLevelPressed(int level)
        {
            LevelFlowHelper.PlayLevel(level);
            gameObject.SetActive(false);

            var worldSelect = GameObject.Find("WorldSelect");
            if (worldSelect != null)
            {
                worldSelect.SetActive(false);
            }
        }
    }
}
