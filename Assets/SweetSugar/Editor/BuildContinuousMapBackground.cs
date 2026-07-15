using UnityEditor;
using UnityEngine;

namespace SweetSugar.Editor
{
    // Replaces the 5 separate map_background_0N sprite tiles (each a plain 4-vertex quad,
    // which is why the curved-world shader broke seams between them - see the
    // unity-2d-curved-world-map skill) with one continuous, subdivided mesh using the same
    // texture (all 5 tiles reference the identical Map_1.png, just centered 20.48 units
    // apart with a center pivot - so they cover y = -10.24 .. 92.16 with 50% overlap
    // between consecutive tiles, not edge-to-edge as the spacing alone would suggest).
    public static class BuildContinuousMapBackground
    {
        private const string TileTextureGuid = "dd52cc4c490864872ba9f27e23b0894e"; // Map_1.png
        private const float TileWidth = 15.36f;
        private const float TileHeight = 20.48f;
        private const int TileCount = 5;
        private const int SubdivisionsPerTile = 8;
        private const string ShaderName = "Custom/CurvedContinuousMesh";
        private const string MaterialPath = "Assets/SweetSugar/Shaders/CurvedContinuousMesh.mat";
        private const string MeshAssetPath = "Assets/SweetSugar/Shaders/ContinuousBackground.asset";
        private const string GeneratedObjectName = "ContinuousBackground";

        [MenuItem("Sweet Sugar/Build Continuous Map Background")]
        public static void Build()
        {
            var levelsMap = GameObject.Find("LevelsMap");
            if (levelsMap == null)
            {
                Debug.LogError("Could not find \"LevelsMap\" in the open scene - is gameStatic.unity open?");
                return;
            }

            var texturePath = AssetDatabase.GUIDToAssetPath(TileTextureGuid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogError($"Could not load background texture (guid {TileTextureGuid}, resolved path \"{texturePath}\")");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogError($"Could not find shader {ShaderName} - has it compiled without errors?");
                    return;
                }

                material = new Material(shader) { name = "CurvedContinuousMesh" };
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Curvature", 0.005f);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var mesh = BuildMesh();
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshAssetPath);
            if (existingMesh != null)
            {
                EditorUtility.CopySerialized(mesh, existingMesh);
                mesh = existingMesh;
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, MeshAssetPath);
            }

            var go = GameObject.Find(GeneratedObjectName);
            if (go == null)
            {
                go = new GameObject(GeneratedObjectName);
            }

            go.transform.SetParent(levelsMap.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = go.AddComponent<MeshFilter>();
            }
            meshFilter.sharedMesh = mesh;

            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
            }
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = -1;

            var disabled = 0;
            for (var i = 1; i <= TileCount; i++)
            {
                var tile = GameObject.Find($"map_background_0{i}");
                if (tile == null)
                {
                    continue;
                }

                Undo.RecordObject(tile, "Disable old background tile");
                tile.SetActive(false);
                disabled++;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(go);

            Debug.Log($"Built continuous background mesh ({mesh.vertexCount} vertices, y = {-TileHeight / 2f} .. {TileCount * TileHeight - TileHeight / 2f}), disabled {disabled} old map_background_0N tiles. Save the scene (Ctrl+S) to keep it.");
        }

        private static Mesh BuildMesh()
        {
            var rows = TileCount * SubdivisionsPerTile + 1;
            var vertices = new Vector3[rows * 2];
            var uvs = new Vector2[rows * 2];
            var triangles = new int[(rows - 1) * 6];

            var halfWidth = TileWidth * 0.5f;
            var totalHeight = TileCount * TileHeight;
            var startY = -TileHeight * 0.5f; // tiles are center-pivoted, so coverage starts half a tile below y=0

            for (var row = 0; row < rows; row++)
            {
                var t = (float)row / (rows - 1);
                var y = startY + t * totalHeight;
                var v = (y - startY) / TileHeight;

                var leftIndex = row * 2;
                var rightIndex = row * 2 + 1;

                vertices[leftIndex] = new Vector3(-halfWidth, y, 0);
                vertices[rightIndex] = new Vector3(halfWidth, y, 0);

                uvs[leftIndex] = new Vector2(0, v);
                uvs[rightIndex] = new Vector2(1, v);
            }

            var triIndex = 0;
            for (var row = 0; row < rows - 1; row++)
            {
                var bl = row * 2;
                var br = row * 2 + 1;
                var tl = (row + 1) * 2;
                var tr = (row + 1) * 2 + 1;

                triangles[triIndex++] = bl;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = br;

                triangles[triIndex++] = br;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = tr;
            }

            var mesh = new Mesh { name = "ContinuousBackground" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
