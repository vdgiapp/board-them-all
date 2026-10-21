using System;
using System.Collections;
using Gustom.References;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using Game.Database;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Game.Manager
{
    [DisallowMultipleComponent]
    public class SceneSwitcher : MonoBehaviourSingleton<SceneSwitcher>
    {
        [field: SerializeField, ReadOnly] public bool IsLoading { get; private set; } = false;
        
        private DataScenes dataScenes => GameDatabase.GetModule<DataScenes>();
        
        private void Start()
        {
#if UNITY_EDITOR
            // Not enter play mode from Essentials scene
            if (EditorSceneManager.GetActiveScene().path != dataScenes.essentials)
            {
                return;
            }
#endif
            GoToScene(dataScenes.mainMenu);
        }

        public void GoToScene(SceneReference scene, Action onDoneStartLoading = null, Func<IEnumerator> onSceneLoaded = null, Action onComplete = null)
        {
            if (IsLoading) return;
            StartCoroutine(IEGoToScene());
        
            IEnumerator IEGoToScene()
            {
                yield return IESwitchSingle();
                onComplete?.Invoke();
            }
            
            IEnumerator IESwitchSingle()
            {
                if (IsLoading) yield break;
        
                string scenePath = scene.Path;
                if (!Application.CanStreamedLevelBeLoaded(scenePath))
                {
                    Debug.LogError($"Scene not found: {scenePath}");
                    yield break;
                }
        
                IsLoading = true;
                try
                {
                    SceneReference loadScene = dataScenes.loadScene;
                    Debug.Log($"Loading scene: {scenePath}");
        
                    yield return SceneManager.LoadSceneAsync(loadScene, LoadSceneMode.Additive);
                    yield return CoroutineUtils.WaitUntil(() => UI.LoadingCanvas.Instance);
                    UI.LoadingCanvas loadingCanvas = UI.LoadingCanvas.Instance;
                    loadingCanvas.SetFillAmount(0f);
        
                    yield return loadingCanvas.IEPlayStartLoading();
                    loadingCanvas.SetFillAmount(0.1f);
                    onDoneStartLoading?.Invoke();
        
                    Scene currentScene = SceneManager.GetActiveScene();
                    if (currentScene.IsValid() && currentScene.isLoaded && currentScene.path != loadScene.Path && !IsSceneKeepLoaded(currentScene.path))
                    {
                        yield return SceneManager.UnloadSceneAsync(currentScene);
                    }
                    loadingCanvas.SetFillAmount(0.2f);
        
                    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                    while (loadOperation is { isDone: false })
                    {
                        // Unity progress: 0 -> 0.9, then waits for activation
                        float progress = loadOperation.progress / 0.9f;
                        loadingCanvas.SetFillAmount(Mathf.Lerp(0.2f, 0.9f, progress));
                        yield return null;
                    }
                    loadingCanvas.SetFillAmount(0.9f);
        
                    Scene targetScene = SceneManager.GetSceneByPath(scenePath);
                    if (targetScene.IsValid()) SceneManager.SetActiveScene(targetScene);
        
                    yield return Resources.UnloadUnusedAssets();
                    loadingCanvas.SetFillAmount(0.95f);
                    Debug.Log($"Loaded scene: {scenePath}");
        
                    if (onSceneLoaded != null) yield return StartCoroutine(onSceneLoaded());
                    loadingCanvas.SetFillAmount(1f);
        
                    yield return loadingCanvas.IEPlayEndLoading();
                    
                    Scene loadingScene = SceneManager.GetSceneByPath(loadScene);
                    if (loadingScene.IsValid() && loadingScene.isLoaded)
                    {
                        yield return SceneManager.UnloadSceneAsync(loadingScene);
                    }
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public bool IsSceneKeepLoaded(string scenePath)
        {
            return scenePath == dataScenes.essentials.Path;
        }
    }
}