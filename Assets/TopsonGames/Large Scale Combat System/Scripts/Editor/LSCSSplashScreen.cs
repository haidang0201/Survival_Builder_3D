using UnityEditor;
using UnityEngine;
using System.IO;

namespace TopsonGames.LSCSEditor
{
    [InitializeOnLoad]
    public class LSCSSplashScreen : EditorWindow
    {
        private static string s_WelcomeScreenShownThisSessionKey = "TopsonGames.LSCSSplashScreen.ShownThisSession";
        private static string s_DoNotShowAgainKey = "TopsonGames.LSCSSplashScreen.DoNotShowAgain";
        private static LSCSSplashScreen _window;

        static LSCSSplashScreen()
        {
            SessionState.SetBool(s_WelcomeScreenShownThisSessionKey, false);
            EditorApplication.update += ShowWelcomeScreenOnLoad;
        }

        private static void ShowWelcomeScreenOnLoad()
        {
            EditorApplication.update -= ShowWelcomeScreenOnLoad;

            if (EditorPrefs.GetBool(s_DoNotShowAgainKey, false))
            {
                return;
            }

            if (SessionState.GetBool(s_WelcomeScreenShownThisSessionKey, false) && !EditorApplication.isCompiling)
            {
                return;
            }

            if (!Directory.Exists("Assets/TopsonGames/Large Scale Combat System"))
            {
                return;
            }

            SessionState.SetBool(s_WelcomeScreenShownThisSessionKey, true);
            Init();
        }

        [MenuItem("Tools/Topson Games/Show Welcome Screen", false, 0)]
        public static void Init()
        {
            _window = (LSCSSplashScreen)EditorWindow.GetWindow(typeof(LSCSSplashScreen), true, "Topson Games - Welcome!", true); 
            _window.minSize = new Vector2(500, 350);
            _window.maxSize = new Vector2(500, 350);
            _window.Show();
        }

        void OnGUI()
        {

            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 20;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Large Scale Combat System", titleStyle); 
            EditorGUILayout.Space(20);


            GUIStyle messageStyle = new GUIStyle(EditorStyles.label);
            messageStyle.wordWrap = true;
            messageStyle.alignment = TextAnchor.MiddleLeft;
            messageStyle.fontSize = 12;
            EditorGUILayout.HelpBox(
                "Thank you for choosing our Large Scale Combat System! \n\n" +
                "This system helps you create massive battles and complex formations. " +
                "To ensure a smooth start, please install these required Unity packages via the Package Manager (Unity Registry) if you do not have them in your project already:\n" +
                "- Burst\n" +
                "- Collections\n" +
                "- Jobs\n\n" +
                "We're here to support you in building amazing games!", MessageType.Info);

            EditorGUILayout.Space(30); 

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Documentation", GUILayout.Width(130), GUILayout.Height(35))) 
            {
                Application.OpenURL("http://topsongames.com");
            }

            if (GUILayout.Button("Discord", GUILayout.Width(130), GUILayout.Height(35))) 
            {
                Application.OpenURL("https://discord.gg/uUfQuUWPCR");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(30); 


            EditorGUILayout.BeginHorizontal();


            bool doNotShow = EditorPrefs.GetBool(s_DoNotShowAgainKey, false);
            bool newDoNotShow = EditorGUILayout.ToggleLeft("Don't show this again", doNotShow, GUILayout.Width(150)); 
            if (newDoNotShow != doNotShow)
            {
                EditorPrefs.SetBool(s_DoNotShowAgainKey, newDoNotShow);
            }

            GUILayout.FlexibleSpace();


            if (GUILayout.Button("Close", GUILayout.Width(150), GUILayout.Height(35))) 
            {
                _window.Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
    }
}