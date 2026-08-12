using UnityEngine;
using UnityEditor;

namespace TopsonGames.MeshAnimationSystem.Editor
{
    public class AnimationSamplerDebugger : EditorWindow
    {
        private GameObject characterPrefab;
        private AnimationClip animationClip;
        private string boneName;
        private float sampleTime = 0f;

        [MenuItem("Tools/Topson Games/Animation Sampler Debugger")]
        public static void ShowWindow() => GetWindow<AnimationSamplerDebugger>("Animation Sampler Debugger");

        void OnGUI()
        {
            GUILayout.Label("Animation Sampling Debugger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use this tool to verify that an animation clip correctly animates a bone in your prefab. Move the slider and watch the console for position values.", MessageType.Info);

            characterPrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab", characterPrefab, typeof(GameObject), false);
            animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
            boneName = EditorGUILayout.TextField("Bone Name to Track", boneName);

            if (characterPrefab == null || animationClip == null || string.IsNullOrEmpty(boneName))
            {
                EditorGUILayout.HelpBox("Please assign a Prefab, Clip, and Bone Name.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            sampleTime = EditorGUILayout.Slider("Sample Time", sampleTime, 0f, animationClip.length);
            if (EditorGUI.EndChangeCheck())
            {
                TestSample();
            }
            if (GUILayout.Button("Test Sample at Current Time"))
            {
                TestSample();
            }
        }

        private void TestSample()
        {
            if (characterPrefab == null || animationClip == null || string.IsNullOrEmpty(boneName)) return;

            GameObject instance = null;
            try
            {
                instance = Instantiate(characterPrefab);
                instance.hideFlags = HideFlags.HideAndDontSave;

                Transform bone = FindDeepChild(instance.transform, boneName);
                if (bone == null)
                {
                    Debug.LogError($"[Debugger] Could not find bone '{boneName}' in the prefab hierarchy.");
                    return;
                }

                animationClip.SampleAnimation(instance, sampleTime);
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
#endif


                Debug.Log($"--- Sampling at time: {sampleTime:F3}s ---\n" +
                          $"Bone '{boneName}' -- World Position: {bone.position.ToString("F4")}\n" +
                          $"Bone '{boneName}' -- Local Position (relative to its parent): {bone.localPosition.ToString("F4")}\n" +
                          $"Root '{instance.name}' -- World Position: {instance.transform.position.ToString("F4")}");
            }
            finally
            {
                if (instance != null)
                {
                    DestroyImmediate(instance);
                }
            }
        }

        private Transform FindDeepChild(Transform aParent, string aName)
        {
            if (aParent.name == aName) return aParent;
            var result = aParent.Find(aName);
            if (result != null) return result;
            foreach (Transform child in aParent)
            {
                result = FindDeepChild(child, aName);
                if (result != null) return result;
            }
            return null;
        }
    }
}