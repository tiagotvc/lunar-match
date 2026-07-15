using UnityEditor;
using UnityEngine;

namespace SweetSugar.Editor
{
    // One-off utility to roll the curved-world material out to every sprite on the map at
    // once, instead of dragging it onto dozens of objects by hand. Run with the gameStatic
    // scene open (Map details must be the active object in the hierarchy for GameObject.Find
    // to see it - it's active by default now).
    public static class ApplyCurvedWorldMaterial
    {
        private const string MaterialPath = "Assets/SweetSugar/Shaders/New Material.mat";

        [MenuItem("Sweet Sugar/Apply Curved World Material To Map")]
        public static void Apply()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"Curved world material not found at {MaterialPath}");
                return;
            }

            var count = 0;

            var mapDetails = GameObject.Find("Map details");
            if (mapDetails == null)
            {
                Debug.LogError("Could not find \"Map details\" in the open scene - is gameStatic.unity open?");
                return;
            }

            foreach (var renderer in mapDetails.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Undo.RecordObject(renderer, "Apply Curved World Material");
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                count++;
            }

            for (var i = 1; i <= 5; i++)
            {
                var background = GameObject.Find($"map_background_0{i}");
                if (background == null)
                {
                    Debug.LogWarning($"Could not find map_background_0{i}");
                    continue;
                }

                var renderer = background.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    continue;
                }

                Undo.RecordObject(renderer, "Apply Curved World Material");
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                count++;
            }

            Debug.Log($"Applied curved world material to {count} sprite renderers. Save the scene (Ctrl+S) to keep it.");
        }
    }
}
