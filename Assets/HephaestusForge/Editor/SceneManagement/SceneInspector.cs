using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace HephaestusForge.SceneManagement
{
    [CustomEditor(typeof(SceneAsset))]
    public class SceneInspector : Editor
    {
        private SceneAsset _castedTarget;

        private void OnEnable()
        {
            _castedTarget = (SceneAsset)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = true;

            if (GUILayout.Button("Add to build settings"))
            {
                var scenes = EditorBuildSettings.scenes;

                Array.Resize(ref scenes, EditorBuildSettings.scenes.Length + 1);
                var assetPath = AssetDatabase.GetAssetPath(_castedTarget);

                if (!EditorBuildSettings.scenes.Any(s => s.path == assetPath) && !EditorBuildSettings.scenes.Any(s => s.path.Contains(_castedTarget.name)))
                {
                    scenes.SetValue(new EditorBuildSettingsScene(assetPath, true), scenes.Length - 1);

                    EditorBuildSettings.scenes = scenes;

                    FindAndWriteSceneIDEnum();

                    Debug.Log($"Added scene: {_castedTarget.name} to buildsettings.");

                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.Log($"Scene: {_castedTarget.name} was already in buildsettings");
                }
            }

            if (GUILayout.Button("Open Build window"))
            {
                EditorBuildSettings.sceneListChanged += OnSceneListChanged;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }

            GUI.enabled = false;
        }

        private void OnSceneListChanged()
        {
            FindAndWriteSceneIDEnum();

            Debug.Log("Changed sceneslist");
        }

        private void FindAndWriteSceneIDEnum()
        {
            var path = GetPath();
            List<string> fileSplit;

            using (StreamReader reader = new StreamReader(path))
            {
                string file = reader.ReadToEnd();

                fileSplit = new List<string>(file.Split(new string[] { "\r\n" }, StringSplitOptions.None));

                fileSplit.RemoveRange(10, fileSplit.Count - 10);
            }

            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var sceneInfo = EditorBuildSettings.scenes[i].path.Split('/');

                fileSplit.Add($"            {sceneInfo[sceneInfo.Length - 1].Split('.')[0]} = 1 << {i},");
            }

            fileSplit.Add("        }");
            fileSplit.Add("    }");
            fileSplit.Add("}");

            using (StreamWriter writer = new StreamWriter(path))
            {
                for (int i = 0; i < fileSplit.Count; i++)
                {
                    writer.WriteLine(fileSplit[i]);
                }
            }
        }

        private string GetPath()
        {
            string path = Application.dataPath;
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();

                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex);
                }

                try
                {
                    var range = Directory.GetFiles(path);

                    for (int i = 0; i < range.Length; i++)
                    {
                        if (!range[i].Contains(".meta"))
                        {
                            if (range[i].Contains("SceneID.cs"))
                            {
                                return range[i].Replace('\\', '/');
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            return string.Empty;
        }
    }
}