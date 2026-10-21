using System;
using System.Collections;
using Gustom.References;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Database;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Game.Manager
{
    [DisallowMultipleComponent]
    public class SceneSwitcher : MonoBehaviourSingleton<SceneSwitcher>
    {
        [field: SerializeField, ReadOnly] public bool IsLoading { get; private set; } = false;
        
        private static DataScenes dataScenes => GameDatabase.GetModule<DataScenes>();
        
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
            
            string scenePath = scene.Path;
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError($"Scene not found: {scenePath}");
                onComplete?.Invoke();
                return;
            }
            IsLoading = true;
            StartCoroutine(IERunSceneSwitchWithCleanup());

            IEnumerator IERunSceneSwitchWithCleanup()
            {
                try
                {
                    yield return StartCoroutine(IESwitchSingle());
                }
                finally
                {
                    IsLoading = false;
                    onComplete?.Invoke();
                    Debug.Log("Scene switch lifecycle completed.");
                }
            }
            
            IEnumerator IESwitchSingle()
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
        }


        public bool IsSceneKeepLoaded(string scenePath)
        {
            return scenePath == dataScenes.essentials.Path;
        }
    }
}