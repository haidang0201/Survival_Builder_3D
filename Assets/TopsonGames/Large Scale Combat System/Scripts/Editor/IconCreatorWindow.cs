using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace TopsonGames.LSCSEditor
{
    public class IconCreatorWindow : EditorWindow
    {
        // Render List
        [SerializeField] private List<GameObject> prefabsToRender = new List<GameObject>();

        private string saveFolder = "Assets/Icons";

        // Naming Settings
        private bool usePrefabName = true;
        private string customFileName = "NewIcon";

        // Render Settings
        private int iconSize = 512;
        private bool transparentBackground = true;
        private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        private Vector3 objectRotation = new Vector3(15f, -45f, 0f);
        private float cameraZoom = 1.5f;
        private Vector2 cameraOffset = Vector2.zero;

        // Internal references
        private SerializedObject serializedObject;
        private SerializedProperty prefabsProperty;
        private Texture2D previewTexture;
        private GameObject tempSceneRoot;

        [MenuItem("Tools/Topson Games/IconCreator")]
        public static void ShowWindow()
        {
            IconCreatorWindow window = GetWindow<IconCreatorWindow>("Icon Creator");
            window.minSize = new Vector2(400, 700);
            window.Show();
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            prefabsProperty = serializedObject.FindProperty("prefabsToRender");
            UpdatePreview();
        }

        private void OnDisable()
        {
            CleanUp();
            if (previewTexture != null) DestroyImmediate(previewTexture);
        }

        private void OnGUI()
        {
            GUILayout.Label("Icon Creator - Batch Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prefabsProperty, new GUIContent("Prefabs to Render"), true);

            EditorGUILayout.Space();
            GUILayout.Label("Render Settings", EditorStyles.boldLabel);

            objectRotation = EditorGUILayout.Vector3Field("Rotation", objectRotation);
            cameraZoom = EditorGUILayout.Slider("Zoom", cameraZoom, 0.1f, 10f);
            cameraOffset = EditorGUILayout.Vector2Field("Offset (X/Y)", cameraOffset);
            iconSize = EditorGUILayout.IntSlider("Resolution", iconSize, 64, 2048);

            EditorGUILayout.Space();

            transparentBackground = EditorGUILayout.Toggle("Transparent Background", transparentBackground);
            if (!transparentBackground)
            {
                backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Save Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.Contains(Application.dataPath))
                {
                    saveFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            usePrefabName = EditorGUILayout.Toggle("Use Prefab Name", usePrefabName);
            if (!usePrefabName)
            {
                customFileName = EditorGUILayout.TextField("Custom File Name", customFileName);
                EditorGUILayout.HelpBox("Multiple files will be suffixed with _0, _1, etc.", MessageType.Info);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdatePreview();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            GameObject previewTarget = GetFirstValidPrefab();
            if (previewTarget != null)
            {
                GUILayout.Label($"Live Preview (Showing: {previewTarget.name})", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("Live Preview", EditorStyles.boldLabel);
            }

            Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (previewTexture != null && previewTarget != null)
            {
                if (transparentBackground)
                    EditorGUI.DrawTextureTransparent(previewRect, previewTexture, ScaleMode.ScaleToFit);
                else
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.HelpBox(previewRect, "Assign at least one Prefab to see the preview.", MessageType.Info);
            }

            EditorGUILayout.Space();

            GUI.color = Color.green;
            string buttonText = prefabsToRender.Count > 1 ? $"Batch Save {GetValidPrefabCount()} Icons" : "Save Icon";

            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                SaveIcons();
            }
            GUI.color = Color.white;
        }

        private void UpdatePreview()
        {
            GameObject target = GetFirstValidPrefab();

            if (target == null)
            {
                if (previewTexture != null) DestroyImmediate(previewTexture);
                previewTexture = null;
                return;
            }

            if (previewTexture != null) DestroyImmediate(previewTexture);
            previewTexture = GenerateIcon(target);
        }

        private Texture2D GenerateIcon(GameObject prefab)
        {
            CleanUp(); 

            tempSceneRoot = new GameObject("IconCreator_TempRoot");
            tempSceneRoot.hideFlags = HideFlags.HideAndDontSave;
            tempSceneRoot.transform.position = new Vector3(0, -9999f, 0);

            GameObject instance = Instantiate(prefab, tempSceneRoot.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(objectRotation);

            Bounds bounds = CalculateBounds(instance);
            Vector3 center = bounds.center;
            float extents = bounds.extents.magnitude;
            if (extents == 0) extents = 1f; 

            GameObject camObj = new GameObject("IconCreator_TempCamera");
            camObj.transform.SetParent(tempSceneRoot.transform);
            Camera renderCam = camObj.AddComponent<Camera>();

            renderCam.transform.position = center + new Vector3(cameraOffset.x, cameraOffset.y, -extents * cameraZoom * 2f);
            renderCam.transform.LookAt(center + new Vector3(cameraOffset.x, cameraOffset.y, 0));

            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;
            renderCam.orthographic = true;
            renderCam.orthographicSize = extents * (cameraZoom * 0.5f);

            GameObject lightObj = new GameObject("IconCreator_TempLight");
            lightObj.transform.SetParent(tempSceneRoot.transform);
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            dirLight.intensity = 1.2f;

            GameObject fillLightObj = new GameObject("IconCreator_TempFillLight");
            fillLightObj.transform.SetParent(tempSceneRoot.transform);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fillLight.intensity = 0.5f;

            RenderTexture renderTexture = new RenderTexture(iconSize, iconSize, 24, RenderTextureFormat.ARGB32);
            renderCam.targetTexture = renderTexture;
            renderCam.Render();

            Texture2D resultTexture = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            resultTexture.ReadPixels(new Rect(0, 0, iconSize, iconSize), 0, 0);
            resultTexture.Apply();

            RenderTexture.active = null;
            renderCam.targetTexture = null;
            DestroyImmediate(renderTexture);

            CleanUp();

            return resultTexture;
        }

        private void SaveIcons()
        {
            if (GetValidPrefabCount() == 0)
            {
                Debug.LogWarning("Topsons icon Creator: No prefabs assigned to render.");
                return;
            }

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            int savedCount = 0;
            int fileIndex = 0;

            foreach (GameObject prefab in prefabsToRender)
            {
                if (prefab == null) continue;

                Texture2D tex = GenerateIcon(prefab);

                string currentFileName = usePrefabName ? prefab.name : customFileName;

                if (!usePrefabName && prefabsToRender.Count > 1)
                {
                    currentFileName += $"_{fileIndex}";
                }

                string fullPath = Path.Combine(saveFolder, currentFileName + ".png");

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);
                DestroyImmediate(tex);

                savedCount++;
                fileIndex++;

                AssetDatabase.Refresh();
                TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"<color=green>Topsons icon Creator: Successfully saved {savedCount} icon(s) to {saveFolder}.</color>");

            UpdatePreview(); 
        }

        private GameObject GetFirstValidPrefab()
        {
            foreach (var p in prefabsToRender)
            {
                if (p != null) return p;
            }
            return null;
        }

        private int GetValidPrefabCount()
        {
            int count = 0;
            foreach (var p in prefabsToRender)
            {
                if (p != null) count++;
            }
            return count;
        }

        private Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void CleanUp()
        {
            if (tempSceneRoot != null)
            {
                DestroyImmediate(tempSceneRoot);
                tempSceneRoot = null;
            }
        }
    }
}