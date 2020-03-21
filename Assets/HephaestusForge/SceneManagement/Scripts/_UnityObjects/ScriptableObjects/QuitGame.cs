using UnityEngine;

namespace HephaestusForge.SceneManagement
{
    public sealed class QuitGame : ScriptableObject
    {
        /// <summary>
        /// Referenced in UnityEvent in editor
        /// </summary>
        public void Execute()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Assets/Create/FoldergeistAssets/LimitToOne/QuitGame", false, 0)]
        private static void CreateInstance()
        {
            if (UnityEditor.AssetDatabase.FindAssets("t:QuitGame").Length == 0)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);

                if (path.Length > 0)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(CreateInstance<QuitGame>(), path + "/QuitGame.asset");
                    }
                }
            }
            else
            {
                Debug.LogWarning("An instance of SceneController already exists");
            }
        }

#endif
    }
}
