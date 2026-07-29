#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace TopsonGames.MeshAnimationSystem.Editor
{
    public class SkinnedMeshMaterialCombiner : EditorWindow
    {
        private GameObject sourceCharacter;
        private string savePath = "Assets/BakedCharacters";
        private int maxAtlasSize = 4096;
        private int padding = 4;

        [Header("Material Settings")]
        private Material targetMaterialTemplate;
        private bool enableAlphaClipping = true;
        private bool bakeMaterialColorTint = true;

        [MenuItem("Tools/Topson Games/Material & Mesh Combiner")]
        public static void ShowWindow()
        {
            GetWindow<SkinnedMeshMaterialCombiner>("Mesh Combiner");
        }

        void OnGUI()
        {
            GUILayout.Label("Character Mesh & Material Combiner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sourceCharacter = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Source Character", "The character GameObject or Prefab containing the SkinnedMeshRenderers to be combined."),
                sourceCharacter, typeof(GameObject), true);

            savePath = EditorGUILayout.TextField(
                new GUIContent("Save Path", "The directory path where the generated Atlas, Material, Meshes, and Prefab will be saved."),
                savePath);

            maxAtlasSize = EditorGUILayout.IntSlider(
                new GUIContent("Max Atlas Size", "The maximum resolution of the generated texture atlas (e.g., 2048, 4096)."),
                maxAtlasSize, 512, 8192);

            padding = EditorGUILayout.IntSlider(
                new GUIContent("Atlas Padding", "The number of empty pixels between packed textures to prevent color bleeding at a distance."),
                padding, 0, 16);

            EditorGUILayout.Space();
            GUILayout.Label("Output Material", EditorStyles.boldLabel);

            targetMaterialTemplate = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Material Template (Optional)", "An optional existing material to use as a base. If left empty, a default Standard or URP Lit material will be created."),
                targetMaterialTemplate, typeof(Material), false);

            enableAlphaClipping = EditorGUILayout.Toggle(
                new GUIContent("Enable Alpha Cutout", "Enable this if your character uses transparent textures (e.g., hair, torn clothes) to activate Alpha Cutout/Clipping."),
                enableAlphaClipping);

            bakeMaterialColorTint = EditorGUILayout.Toggle(
                new GUIContent("Bake Color Tints", "If the original material has a base color tint, this option bakes that color directly into the texture atlas pixels."),
                bakeMaterialColorTint);

            EditorGUILayout.HelpBox("This tool creates a clone of your character with modified UVs and 1 shared material. Use this clone in the Animation Baker!", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Combine Character"))
            {
                if (sourceCharacter == null)
                {
                    Debug.LogError("Combiner: Please assign a Source Character.");
                    return;
                }
                Combine();
            }
        }

        private void Combine()
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            GameObject resultGO = Instantiate(sourceCharacter);
            resultGO.name = sourceCharacter.name + "_UVCombined";

            SkinnedMeshRenderer[] renderers = resultGO.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("Combiner: No SkinnedMeshRenderers found on the target object.");
                DestroyImmediate(resultGO);
                return;
            }

            List<Material> uniqueMaterials = new List<Material>();
            List<Texture2D> textureList = new List<Texture2D>();

            foreach (var smr in renderers)
            {
                foreach (var mat in smr.sharedMaterials)
                {
                    if (mat != null && !uniqueMaterials.Contains(mat))
                    {
                        uniqueMaterials.Add(mat);
                        Texture2D tex = GetReadableTextureFromMaterial(mat);
                        textureList.Add(tex);
                    }
                }
            }

            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Rect[] atlasRects = atlas.PackTextures(textureList.ToArray(), padding, maxAtlasSize);

            string atlasName = $"{sourceCharacter.name}_Atlas";
            string atlasPath = Path.Combine(savePath, atlasName + ".png");
            File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
            AssetDatabase.Refresh();

            Texture2D savedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);

            Material combinedMaterial;
            if (targetMaterialTemplate != null)
            {
                combinedMaterial = new Material(targetMaterialTemplate);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                combinedMaterial = new Material(shader);
            }

            if (combinedMaterial.HasProperty("_BaseMap")) combinedMaterial.SetTexture("_BaseMap", savedAtlas);
            else if (combinedMaterial.HasProperty("_MainTex")) combinedMaterial.SetTexture("_MainTex", savedAtlas);

            combinedMaterial.enableInstancing = true;

            if (enableAlphaClipping)
            {
                if (combinedMaterial.HasProperty("_AlphaClip")) combinedMaterial.SetFloat("_AlphaClip", 1);
                if (combinedMaterial.HasProperty("_Mode")) combinedMaterial.SetFloat("_Mode", 1);
                combinedMaterial.EnableKeyword("_ALPHATEST_ON");
            }

            string matPath = Path.Combine(savePath, $"{sourceCharacter.name}_CombinedMat.mat");
            AssetDatabase.CreateAsset(combinedMaterial, matPath);

            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer smr = renderers[i];
                Mesh originalMesh = smr.sharedMesh;
                if (originalMesh == null) continue;

                Mesh newMesh = Instantiate(originalMesh);
                newMesh.name = $"{sourceCharacter.name}_MeshPart_{i}";
                Vector2[] uvs = newMesh.uv;

                List<int> combinedTriangles = new List<int>();

                for (int submesh = 0; submesh < originalMesh.subMeshCount; submesh++)
                {
                    if (submesh >= smr.sharedMaterials.Length) break;

                    Material mat = smr.sharedMaterials[submesh];
                    int atlasIndex = uniqueMaterials.IndexOf(mat);
                    if (atlasIndex == -1) continue;

                    Rect rect = atlasRects[atlasIndex];
                    int[] tris = originalMesh.GetTriangles(submesh);

                    foreach (int vIndex in tris)
                    {
                        Vector2 originalUV = originalMesh.uv[vIndex];
                        uvs[vIndex] = new Vector2(
                            Mathf.Lerp(rect.xMin, rect.xMax, originalUV.x),
                            Mathf.Lerp(rect.yMin, rect.yMax, originalUV.y)
                        );
                    }
                    combinedTriangles.AddRange(tris);
                }

                newMesh.uv = uvs;

                newMesh.subMeshCount = 1;
                newMesh.SetTriangles(combinedTriangles.ToArray(), 0);

                string meshPath = Path.Combine(savePath, $"{newMesh.name}.asset");
                AssetDatabase.CreateAsset(newMesh, meshPath);

                smr.sharedMesh = newMesh;
                smr.sharedMaterials = new Material[] { combinedMaterial };
            }

            string prefabPath = Path.Combine(savePath, $"{resultGO.name}.prefab");
            PrefabUtility.SaveAsPrefabAsset(resultGO, prefabPath);
            DestroyImmediate(resultGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject finalInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject;
            Selection.activeGameObject = finalInstance;

            Debug.Log($"<color=green>Successfully combined textures & UVs for {sourceCharacter.name}! You can now use this Prefab in the Animation Baker.</color>");
        }

        private Texture2D GetReadableTextureFromMaterial(Material mat)
        {
            Texture2D sourceTex = null;
            if (mat.HasProperty("_BaseMap")) sourceTex = mat.GetTexture("_BaseMap") as Texture2D;
            else if (mat.HasProperty("_MainTex")) sourceTex = mat.GetTexture("_MainTex") as Texture2D;

            Color matColor = Color.white;
            if (mat.HasProperty("_BaseColor")) matColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color")) matColor = mat.GetColor("_Color");

            int width = sourceTex != null ? sourceTex.width : 16;
            int height = sourceTex != null ? sourceTex.height : 16;

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            if (sourceTex != null)
            {
                Graphics.Blit(sourceTex, rt);
            }
            else
            {
                Texture2D tempColorTex = new Texture2D(1, 1);
                tempColorTex.SetPixel(0, 0, Color.white);
                tempColorTex.Apply();
                Graphics.Blit(tempColorTex, rt);
            }

            Texture2D readableTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            if (bakeMaterialColorTint && matColor != Color.white)
            {
                Color[] pixels = readableTex.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(
                        pixels[i].r * matColor.r,
                        pixels[i].g * matColor.g,
                        pixels[i].b * matColor.b,
                        pixels[i].a * matColor.a
                    );
                }
                readableTex.SetPixels(pixels);
                readableTex.Apply();
            }

            return readableTex;
        }
    }
}
#endif