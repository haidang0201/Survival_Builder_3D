#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace TopsonGames.MeshAnimationSystem.Editor
{
    public class AnimationBaker : EditorWindow
    {
        private GameObject characterPrefab;

        [SerializeField] private List<AnimationClip> animationClips = new List<AnimationClip>();
        private string saveFolder = "Assets/BakedAnimations";
        [Tooltip("1.0 = full original frame rate; <1.0 = undersampling")]
        private float frameSamplingRate = 1.0f;

        [SerializeField] private List<BoneSelection> availableBones = new List<BoneSelection>();

        private bool createCharacterOnBake = true;

        [Tooltip("Extra scale correction if you still experience issues. Normally leave at 1.")]
        private float scaleCorrectionFactor = 1.0f;

        private Material targetMaterial;
        private Vector2 boneListScrollPos;

        private SerializedObject serializedObject;
        private SerializedProperty clipsProperty;

        [System.Serializable]
        private class BoneSelection
        {
            public string name;
            public bool selected;
        }

        [MenuItem("Tools/Topson Games/Animation Baker")]
        public static void ShowWindow() => GetWindow<AnimationBaker>("Animation Baker");

        void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            clipsProperty = serializedObject.FindProperty("animationClips");
        }

        void OnGUI()
        {
            serializedObject.Update();
            GUILayout.Label("Mesh Animation Baker", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            characterPrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab", characterPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                PopulateBoneList();
            }

            EditorGUILayout.PropertyField(clipsProperty, new GUIContent("Animation Clips"), true);
            EditorGUILayout.Space();
            targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material", targetMaterial, typeof(Material), false);
            EditorGUILayout.HelpBox("Important: The target material must have 'Enable GPU Instancing' checked in its inspector.", MessageType.Info);
            EditorGUILayout.Space();
            saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);
            frameSamplingRate = EditorGUILayout.Slider("Frame Sampling (%)", frameSamplingRate * 100f, 1f, 100f) / 100f;
            EditorGUILayout.Space();
            scaleCorrectionFactor = EditorGUILayout.FloatField(new GUIContent("Scale Correction Factor"), scaleCorrectionFactor);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Bones to Bake (leave empty to skip)", EditorStyles.boldLabel);
            boneListScrollPos = EditorGUILayout.BeginScrollView(boneListScrollPos, GUILayout.Height(150));
            if (availableBones.Count > 0)
            {
                foreach (var bone in availableBones)
                {
                    bone.selected = EditorGUILayout.ToggleLeft(bone.name, bone.selected);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Assign a Character Prefab to see available bones.");
            }
            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();

            createCharacterOnBake = EditorGUILayout.Toggle("Create Character in Scene", createCharacterOnBake);

            if (GUILayout.Button("Bake All Animations"))
            {
                if (characterPrefab == null || animationClips == null || animationClips.Count == 0 || targetMaterial == null)
                {
                    Debug.LogError("Baker: Please specify Prefab, Target Material and at least one Clip.");
                    return;
                }
                var baked = new List<MeshAnimation>();
                foreach (var clip in animationClips.Where(c => c != null))
                {
                    var res = Bake(clip);
                    if (res != null) baked.Add(res);
                }
                if (createCharacterOnBake && baked.Count > 0) CreateCharacterInScene(baked);
            }
        }

        private void PopulateBoneList()
        {
            availableBones.Clear();
            if (characterPrefab == null) return;
            var renderers = characterPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) return;

            var boneSet = new HashSet<Transform>();
            foreach (var renderer in renderers)
            {
                foreach (var bone in renderer.bones) boneSet.Add(bone);
            }

            foreach (var bone in boneSet.OrderBy(b => b.name))
            {
                availableBones.Add(new BoneSelection { name = bone.name, selected = false });
            }
        }

        private MeshAnimation Bake(AnimationClip animationClip)
        {
            var bonesToBakeNames = availableBones.Where(b => b.selected).Select(b => b.name).ToList();

            if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);
            string assetPath = Path.Combine(saveFolder, $"{characterPrefab.name}_{animationClip.name}.asset");

            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (a is Mesh) DestroyImmediate(a, true);

            var newAnimation = ScriptableObject.CreateInstance<MeshAnimation>();
            AssetDatabase.CreateAsset(newAnimation, assetPath);

            var instance = Instantiate(characterPrefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            Animator animator = instance.GetComponentInChildren<Animator>();
            PlayableGraph graph = default;
            AnimationClipPlayable clipPlayable = default;

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create();
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
                clipPlayable = AnimationClipPlayable.Create(graph, animationClip);
                output.SetSourcePlayable(clipPlayable);
                graph.Play();
            }

            var smrArray = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (smrArray.Length == 0)
            {
                Debug.LogError("Baker: Prefab does not have a SkinnedMeshRenderer.");
                if (graph.IsValid()) graph.Destroy();
                DestroyImmediate(instance);
                return null;
            }

            foreach (var smr in smrArray)
            {
                smr.updateWhenOffscreen = true;
            }

            var targetBones = ResolveTargetBones(instance.transform, bonesToBakeNames);
            int totalOriginalFrames = Mathf.Max(1, Mathf.RoundToInt(animationClip.length * animationClip.frameRate));
            int framesToBake = Mathf.Max(1, Mathf.RoundToInt(totalOriginalFrames * frameSamplingRate));
            var bakedFrames = new Mesh[framesToBake];

            var bakedBoneData = new Dictionary<string, List<BoneFrameData>>();
            foreach (var kv in targetBones) bakedBoneData[kv.Key] = new List<BoneFrameData>();

            for (int i = 0; i < framesToBake; i++)
            {
                float t = (framesToBake == 1) ? 0f : (i / (float)(framesToBake - 1)) * animationClip.length;

                if (animator != null && graph.IsValid())
                {
                    clipPlayable.SetTime(t);
                    graph.Evaluate();
                }
                else
                {
                    animationClip.SampleAnimation(instance, t);
                }

                foreach (var kv in targetBones)
                {
                    Transform tr = kv.Value;
                    Vector3 pLocal = instance.transform.InverseTransformPoint(tr.position);
                    Quaternion rLocal = Quaternion.Inverse(instance.transform.rotation) * tr.rotation;

                    bakedBoneData[kv.Key].Add(new BoneFrameData { position = pLocal, rotation = rLocal });
                }

                var combines = new List<CombineInstance>(smrArray.Length);
                foreach (var smr in smrArray)
                {
                    var baked = new Mesh();
                    smr.BakeMesh(baked);
                    var verts = baked.vertices;

                    var M = instance.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                    for (int v = 0; v < verts.Length; v++) verts[v] = M.MultiplyPoint3x4(verts[v]);

                    baked.vertices = verts;
                    combines.Add(new CombineInstance { mesh = baked, transform = Matrix4x4.identity });
                }

                var final = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                final.CombineMeshes(combines.ToArray(), true, false);

                if (!Mathf.Approximately(scaleCorrectionFactor, 1f))
                {
                    var fverts = final.vertices;
                    for (int v = 0; v < fverts.Length; v++) fverts[v] *= scaleCorrectionFactor;
                    final.vertices = fverts;
                    final.RecalculateBounds();
                }

                final.name = $"frame_{i:D04}";
                AssetDatabase.AddObjectToAsset(final, newAnimation);
                bakedFrames[i] = final;
            }

            if (graph.IsValid()) graph.Destroy();
            DestroyImmediate(instance);

            newAnimation.clipName = animationClip.name;
            newAnimation.frames = bakedFrames;
            newAnimation.frameRate = animationClip.frameRate * frameSamplingRate;
            newAnimation.isLooping = animationClip.isLooping;
            newAnimation.bakedScale = scaleCorrectionFactor;

            newAnimation.boneAnimations = new List<BoneAnimation>();
            foreach (var kv in bakedBoneData)
                newAnimation.boneAnimations.Add(new BoneAnimation { boneName = kv.Key, frames = kv.Value.ToArray() });

            EditorUtility.SetDirty(newAnimation);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = newAnimation;
            Debug.Log($"Baker: '{animationClip.name}' → {framesToBake} Frames | Bones: {string.Join(", ", bakedBoneData.Keys)}");
            return newAnimation;
        }

        private Dictionary<string, Transform> ResolveTargetBones(Transform root, List<string> boneNames)
        {
            var dict = new Dictionary<string, Transform>();
            if (boneNames == null || boneNames.Count == 0) return dict;
            foreach (var name in boneNames)
            {
                var t = FindDeepChild(root, name);
                if (t != null)
                {
                    if (!dict.ContainsKey(name)) dict.Add(name, t);
                }
                else Debug.LogWarning($"Baker: Bone '{name}' not found.");
            }
            return dict;
        }

        private void CreateCharacterInScene(List<MeshAnimation> bakedAnims)
        {
            if (bakedAnims == null || bakedAnims.Count == 0) return;
            GameObject go = new GameObject(characterPrefab.name + "_MeshAnimated");

            go.transform.localScale = characterPrefab.transform.localScale;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var ma = go.AddComponent<MeshAnimator>();
            mr.sharedMaterial = targetMaterial;
            if (bakedAnims[0].frames.Length > 0) mf.sharedMesh = bakedAnims[0].frames[0];

            var soMA = new SerializedObject(ma);
            soMA.FindProperty("targetMeshFilter").objectReferenceValue = mf;
            var lodLevelsProp = soMA.FindProperty("lodLevels");
            lodLevelsProp.ClearArray();
            lodLevelsProp.InsertArrayElementAtIndex(0);
            var lod0AnimsProp = lodLevelsProp.GetArrayElementAtIndex(0).FindPropertyRelative("animations");
            lod0AnimsProp.ClearArray();
            for (int i = 0; i < bakedAnims.Count; i++)
            {
                lod0AnimsProp.InsertArrayElementAtIndex(i);
                lod0AnimsProp.GetArrayElementAtIndex(i).objectReferenceValue = bakedAnims[i];
            }
            soMA.ApplyModifiedProperties();

            var bakedBoneNames = bakedAnims.SelectMany(a => a.boneAnimations.Select(b => b.boneName)).Distinct().ToList();
            foreach (var boneName in bakedBoneNames)
            {
                var apGO = new GameObject(boneName + "_Attachment");
                apGO.transform.SetParent(go.transform, false);
                var ap = apGO.AddComponent<AttachmentPoint>();
                ap.targetAnimator = ma;
                ap.boneName = boneName;
            }

            Selection.activeGameObject = go;
            Debug.Log($"Scene: '{go.name}' created.");
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif