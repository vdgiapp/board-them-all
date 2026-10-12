using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gustom.Editor
{
    [InitializeOnLoad]
    public static class SceneNavigatorToolbar
    {
        static SceneNavigatorToolbar()
        {
            ToolbarCallback.OnToolbarGUILeft -= OnToolbarGUI;
            ToolbarCallback.OnToolbarGUILeft += OnToolbarGUI;
        }

        private static void OnToolbarGUI()
        {
            string currentScenePath = SceneManager.GetActiveScene().path;

            string displayName = string.IsNullOrEmpty(currentScenePath)
                ? "No Scene Opened"
                : Path.GetFileNameWithoutExtension(currentScenePath);

            GUIStyle flatDropdownStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedWidth = 140,
                fixedHeight = 20,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 15, 0, 0)
            };

            Rect btnRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(140), GUILayout.Height(20));
            if (GUI.Button(btnRect, new GUIContent($"{displayName}"), flatDropdownStyle))
            {
                PopupWindow.Show(btnRect, new SceneNavigatorPopup());
            }

            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle arrowStyle = EditorStyles.toolbarDropDown;
                Rect arrowRect = new Rect(btnRect.x + btnRect.width - 16, btnRect.y, 16, btnRect.height);
                arrowStyle.Draw(arrowRect, GUIContent.none, false, false, false, false);
            }
        }
    }

    public class SceneNavigatorPopup : PopupWindowContent
    {
        private class SceneItem
        {
            public string Name;
            public string Path;
            public bool InBuildSettings;
            public int BuildIndex;
        }

        private List<SceneItem> _allScenes = new List<SceneItem>();
        private ReorderableList _reorderableList;
        private Vector2 _scrollPosition;

        public SceneNavigatorPopup()
        {
            RefreshSceneData();
        }

        private void RefreshSceneData()
        {
            _allScenes.Clear();

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            var buildScenes = EditorBuildSettings.scenes.ToList();

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                int buildIndex = buildScenes.FindIndex(s => s.path == path);
                _allScenes.Add(new SceneItem
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Path = path,
                    InBuildSettings = buildIndex >= 0,
                    BuildIndex = buildIndex
                });
            }
            
            _allScenes = _allScenes
                .OrderByDescending(x => x.InBuildSettings)
                .ThenBy(x => x.InBuildSettings ? x.BuildIndex : int.MaxValue)
                .ThenBy(x => x.Name)
                .ToList();

            _reorderableList = new ReorderableList(_allScenes, typeof(SceneItem), true, false, false, false)
            {
                elementHeight = 22,
                drawElementCallback = DrawSceneElement,
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    ApplyListOrderToBuildSettings();
                }
            };
        }

        private void DrawSceneElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _allScenes.Count) return;
            SceneItem item = _allScenes[index];
            
            // Scene name button
            Rect nameRect = new Rect(rect.x + 0, rect.y + 1, rect.width - 92, rect.height - 2);

            if (nameRect.Contains(Event.current.mousePosition))
            {
                Color hoverColor = EditorGUIUtility.isProSkin
                    ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                    : new Color(0.7f, 0.7f, 0.7f, 0.5f);

                EditorGUI.DrawRect(nameRect, hoverColor);
                editorWindow.Repaint();
            }

            string formattedName = item.InBuildSettings ? $"{item.BuildIndex}. {item.Name}" : item.Name;
            bool isActiveScene = SceneManager.GetActiveScene().path == item.Path;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(6, 6, 0, 0),
                alignment = TextAnchor.MiddleLeft
            };

            if (isActiveScene)
            {
                labelStyle.fontStyle = FontStyle.BoldAndItalic;
                labelStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
            }

            if (GUI.Button(nameRect, formattedName, labelStyle))
            {
                ExecuteSceneSwitch(item.Path);
                editorWindow.Close();
            }

            // Toggle build status button
            Rect toggleRect = new Rect(rect.x + rect.width - 85, rect.y + 3, 85, rect.height - 2);
            string toggleText = item.InBuildSettings ? "Build on" : "Build off";
            Color oldBackground = GUI.backgroundColor;

            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            if (item.InBuildSettings)
            {
                GUI.backgroundColor = new Color(0.25f, 0.6f, 0.25f);
            }

            if (GUI.Button(toggleRect, toggleText, buttonStyle))
            {
                ToggleBuildStatus(item);
            }

            GUI.backgroundColor = oldBackground;
        }

        private void ExecuteSceneSwitch(string scenePath)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot switch scenes while in Play mode!");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private void ToggleBuildStatus(SceneItem item)
        {
            List<EditorBuildSettingsScene> buildScenes = EditorBuildSettings.scenes.ToList();
            if (item.InBuildSettings)
            {
                buildScenes.RemoveAll(x => x.path == item.Path);
            }
            else
            {
                buildScenes.Add(new EditorBuildSettingsScene(item.Path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
            RefreshSceneData();
        }

        private void ApplyListOrderToBuildSettings()
        {
            // Isolate and retrieve only the scenes with "InBuildSettings" enabled, following the new drag-and-drop order.
            var buildItems = _allScenes.Where(x => x.InBuildSettings).ToList();
            
            List<EditorBuildSettingsScene> newBuildSettings = new List<EditorBuildSettingsScene>();
            foreach (SceneItem item in buildItems)
            {
                newBuildSettings.Add(new EditorBuildSettingsScene(item.Path, true));
            }
            
            EditorBuildSettings.scenes = newBuildSettings.ToArray();
            
            // Update the interface and reassign the Build Index value.
            RefreshSceneData();
        }

        public override Vector2 GetWindowSize()
        {
            // Slightly increase the width to prevent the button layout strip from being compressed when the scrollbar appears.
            return new Vector2(340, Mathf.Clamp((_allScenes.Count * 22) + 30, 150, 500));
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(4);
            GUILayout.Label("Drag (≡) to Re-order Build Scenes", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(2);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Wrap the ReorderableList in a fixed Vertical Layout so the mouse correctly detects the drag position when scrolling.
            EditorGUILayout.BeginVertical();
            _reorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
    }
}
