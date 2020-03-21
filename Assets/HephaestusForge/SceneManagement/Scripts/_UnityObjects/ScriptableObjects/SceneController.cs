using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using HephaestusForge.FlaggedEnum;

namespace HephaestusForge.SceneManagement
{
    /// <summary>
    /// The scene manager instance to be used for loading scenes from the scene id enum, scenes are loaded async, dont change the name of this enum as the ZUnityEditor,
    /// uses strings to check for this as an unity event argument in buttons.
    /// </summary>
    public sealed class SceneController : ScriptableObject
    {
        //The scenes that are currently loading
        [SerializeField]
        private Scenes _activeScenes;

        [SerializeField]
        private Scenes _currentlyLoading;

        //Invoked when any scene is loaded
        public event Action<Scenes> _OnAnySceneLoaded;

        //Invoked when any scene is unloaded
        public event Action<Scenes> _OnAnySceneUnloaded;

        //Invoked before any scene is loaded
        public event Action<Scenes> _BeforeAnySceneLoaded;

        //Invoked before any scene is unloaded
        public event Action<Scenes> _BeforeAnySceneUnloaded;

        public float LoadingProcess { get; private set; }

        ///// <summary>
        ///// Getting the ids of the active scenes
        ///// </summary>
        public Scenes[] GetActiveScenes { get { return _activeScenes.ContainedValues().ToArray(); } }

        private void Init()
        {
            _currentlyLoading = 0;
            _activeScenes = (Scenes)(1 << UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Called by unity event
        /// </summary>
        /// <param name="sceneID"></param>
        private void LoadSceneSingle(int sceneID)
        {
            LoadSceneSingle((Scenes)sceneID);
        }

        /// <summary>
        /// Called by unity event
        /// </summary>
        /// <param name="sceneID"></param>
        private void LoadSceneAdditive(int sceneID)
        {
            LoadSceneAdditive((Scenes)sceneID);
        }

        public bool IsSceneActive(Scenes scene)
        {
            return _activeScenes.HasFlag(scene);
        }

        public bool IsSceneBeingLoaded(Scenes scene)
        {
            return _currentlyLoading.HasFlag(scene);
        }

        /// <summary>
        /// Load a scene with the single mode, will unload all other scenes automatically (Except DontDestroyOnLoad)
        /// </summary>
        /// <param name="scene"></param>
        public void LoadSceneSingle(Scenes scene)
        {
            if (!_activeScenes.HasFlag(scene) && !_currentlyLoading.HasFlag(scene))
            {
                if (_BeforeAnySceneUnloaded != null)
                {
                    _activeScenes.ForEachContained(s => _BeforeAnySceneUnloaded.Invoke(s));
                }

                _currentlyLoading |= scene;

                _BeforeAnySceneLoaded?.Invoke(scene);                

                LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// Load a new scene without unloading other scenes, only if isn't already active will it be loaded
        /// </summary>
        /// <param name="scene">The id of the scene to be loaded</param>
        public void LoadSceneAdditive(Scenes scene)
        {
            if (!_activeScenes.HasFlag(scene) && !_currentlyLoading.HasFlag(scene))
            {
                _currentlyLoading |= scene;
                _BeforeAnySceneLoaded?.Invoke(scene);

                LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Unload an active scene, will only work when multiple scenes are active
        /// </summary>
        /// <param name="scene">The scene to be unloaded</param>
        public void UnloadScene(Scenes scene)
        {
            if (_activeScenes.HasFlag(scene))
            {
                _BeforeAnySceneUnloaded?.Invoke(scene);

                UnloadSceneAsync(scene);
            }
        }

        /// <summary>
        /// The coroutine for loading a scene async
        /// </summary>
        /// <param name="scene">The id of the scene</param>
        /// <param name="loadSceneMode">How the scene should be loaded</param>
        /// <returns></returns>
        private void LoadSceneAsync(Scenes scene, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene.GetEnumValueIndex(), loadSceneMode).completed += (obj) =>
            {
                if (loadSceneMode == UnityEngine.SceneManagement.LoadSceneMode.Single && _OnAnySceneUnloaded != null)
                {
                    _activeScenes.ForEachContained(s => _OnAnySceneUnloaded.Invoke(s));

                    _activeScenes = scene;
                }
                
                _currentlyLoading &= ~scene;

                _OnAnySceneLoaded?.Invoke(scene);
            };
        }

        /// <summary>
        /// Coroutine for unloading a scene async
        /// </summary>
        /// <param name="id">The id of the scene</param>
        /// <returns></returns>
        private void UnloadSceneAsync(Scenes id)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(id.GetEnumValueIndex()).completed += (obj) =>
            {
                _activeScenes &= ~id;

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