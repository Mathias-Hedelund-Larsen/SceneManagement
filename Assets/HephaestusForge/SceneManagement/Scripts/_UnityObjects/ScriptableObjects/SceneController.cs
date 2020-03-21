using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace HephaestusForge.SceneManagement
{
    /// <summary>
    /// The scene manager instance to be used for loading scenes from the scene id enum, scenes are loaded async, dont change the name of this enum as the ZUnityEditor,
    /// uses strings to check for this as an unity event argument in buttons.
    /// </summary>
    public sealed class SceneController : ScriptableObject
    {
        //The scenes that are currently loading
        [NonSerialized]
        private List<SceneID> _currentlyLoading = new List<SceneID>();

        //The currently active scenes
        [NonSerialized]
        private Dictionary<SceneID, UnityEngine.SceneManagement.Scene> _activeScenes = new Dictionary<SceneID, UnityEngine.SceneManagement.Scene>();

        //Invoked when any scene is loaded

        public event Action<SceneID> _OnAnySceneLoaded;

        //Invoked when any scene is unloaded
        public event Action<SceneID> _OnAnySceneUnloaded;

        //Invoked before any scene is loaded
        public event Action<SceneID> _BeforeAnySceneLoaded;

        //Invoked before any scene is unloaded
        public event Action<SceneID> _BeforeAnySceneUnloaded;

        public float LoadingProcess { get; private set; }

        /// <summary>
        /// Getting the ids of the active scenes
        /// </summary>
        public List<SceneID> GetActiveScenes { get { return _activeScenes.Keys.ToList(); } }

        /// <summary>
        /// Called by unity event
        /// </summary>
        /// <param name="sceneID"></param>
        private void LoadSceneSingle(int sceneID)
        {
            LoadSceneSingle((SceneID)sceneID);
        }

        /// <summary>
        /// Called by unity event
        /// </summary>
        /// <param name="sceneID"></param>
        private void LoadSceneAdditive(int sceneID)
        {
            LoadSceneAdditive((SceneID)sceneID);
        }

        /// <summary>
        /// Load a scene with the single mode, will unload all other scenes automatically (Except DontDestroyOnLoad)
        /// </summary>
        /// <param name="scene"></param>
        public void LoadSceneSingle(SceneID scene)
        {
            if (!_activeScenes.ContainsKey(scene) && !_currentlyLoading.Contains(scene))
            {
                _currentlyLoading.Add(scene);
                _BeforeAnySceneLoaded?.Invoke(scene);

                if (_BeforeAnySceneUnloaded != null)
                {
                    foreach (var pair in _activeScenes)
                    {
                        _BeforeAnySceneUnloaded.Invoke(pair.Key);
                    }
                }

                LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// Load a new scene without unloading other scenes, only if isn't already active will it be loaded
        /// </summary>
        /// <param name="scene">The id of the scene to be loaded</param>
        public void LoadSceneAdditive(SceneID scene)
        {
            if (!_activeScenes.ContainsKey(scene) && !_currentlyLoading.Contains(scene))
            {
                _currentlyLoading.Add(scene);
                _BeforeAnySceneLoaded?.Invoke(scene);

                LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Unload an active scene, will only work when multiple scenes are active
        /// </summary>
        /// <param name="scene">The scene to be unloaded</param>
        public void UnloadScene(SceneID scene)
        {
            if (_activeScenes.ContainsKey(scene))
            {
                _BeforeAnySceneUnloaded?.Invoke(scene);

                UnloadSceneAsync(scene);
            }
        }

        /// <summary>
        /// The coroutine for loading a scene async
        /// </summary>
        /// <param name="id">The id of the scene</param>
        /// <param name="loadSceneMode">How the scene should be loaded</param>
        /// <returns></returns>
        private void LoadSceneAsync(SceneID id, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)id, loadSceneMode).completed += (obj) =>
            {
                if (loadSceneMode == UnityEngine.SceneManagement.LoadSceneMode.Single && _OnAnySceneUnloaded != null)
                {
                    foreach (var pair in _activeScenes)
                    {
                        _OnAnySceneUnloaded.Invoke(pair.Key);
                    }

                    _activeScenes.Clear();
                }

                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).buildIndex == (int)id)
                    {
                        _activeScenes.Add(id, UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
                    }
                }

                    //UnityEditor.EditorBuildSettings.sce
                    _currentlyLoading.Remove(id);

                _OnAnySceneLoaded?.Invoke(id);
            };
        }

        /// <summary>
        /// Coroutine for unloading a scene async
        /// </summary>
        /// <param name="id">The id of the scene</param>
        /// <returns></returns>
        private void UnloadSceneAsync(SceneID id)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync((int)id).completed += (obj) =>
            {
                _activeScenes.Remove(id);

                _OnAnySceneUnloaded?.Invoke(id);
            };
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Assets/Create/FoldergeistAssets/LimitToOne/SceneController", false, 0)]
        private static void CreateInstance()
        {
            if (UnityEditor.AssetDatabase.FindAssets("t:SceneController").Length == 0)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);

                if (path.Length > 0)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(CreateInstance<SceneController>(), path + "/SceneController.asset");
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