using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.GameState
{
    /// <summary>What shows over the game while a scene loads (the UI's loading screen registers itself).</summary>
    public interface ILoadingView
    {
        /// <summary>Starts covering the screen; done covering once <see cref="IsCovering"/> says so.</summary>
        void Show(string nextScene);

        bool IsCovering { get; }

        void SetProgress(float progress01);

        /// <summary>Uncovers the new scene.</summary>
        void Hide();
    }

    /// <summary>
    /// Moves between the game's big scenes without freezing the phone: the loading screen fades in over the old scene,
    /// the next one loads in the background (LoadSceneAsync), and it is let in once ready and the screen has been up a
    /// moment - long enough to read a line, never longer than that. Without a view (a test, the editor's own scene) it
    /// still loads asynchronously, just uncovered. A load asked for while one is under way waits its turn.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour, IKeepAcrossRestart
    {
        /// <summary>The shortest time the loading screen stays up, so it does not flash.</summary>
        public const float MinimumShownSeconds = 0.6f;

        private static SceneLoader _instance;
        private string _queued;

        /// <summary>The loading screen (set by the UI at start-up).</summary>
        public static ILoadingView View { get; set; }

        /// <summary>A load is under way.</summary>
        public static bool IsLoading { get; private set; }

        /// <summary>A scene finished loading through the loader (after it became the active one).</summary>
        public static event Action<string> Loaded;

        private static SceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneLoader");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<SceneLoader>();
                }

                return _instance;
            }
        }

        /// <summary>Loads a scene in place of the current one, behind the loading screen.</summary>
        public static void Load(string sceneName)
        {
            if (!Application.isPlaying)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                return;
            }

            SceneLoader loader = Instance;
            if (IsLoading)
            {
                // A second tap on the same way out (two quick taps on "go to work") is the same request, not another load:
                // the kitchen loaded twice would lose the evening's plan on the second.
                if (sceneName != loader._loading)
                {
                    loader._queued = sceneName;
                }

                return;
            }

            StartupTimeline.Open(sceneName); // from here the pieces of the scene's start are measured (see StartupTimeline)
            loader.StartCoroutine(loader.LoadRoutine(sceneName));
        }

        private string _loading;

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                IsLoading = false;
            }
        }

        /// <summary>The view, unless it has gone (a test clearing the persistent objects mid-load).</summary>
        private static ILoadingView Live(ILoadingView view) => view is UnityEngine.Object unityObject && unityObject == null ? null : view;

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            _loading = sceneName;
            float shownAt = Time.realtimeSinceStartup;
            ILoadingView view = Live(View);
            long phase = StartupTimeline.Now;
            if (view != null)
            {
                view.Show(sceneName);
                float coverDeadline = Time.realtimeSinceStartup + 1f;
                while (Live(view) != null && !view.IsCovering && Time.realtimeSinceStartup < coverDeadline)
                {
                    yield return null;
                }
            }

            StartupTimeline.Mark("scene: loading screen comes up", phase);
            phase = StartupTimeline.Now;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            StartupTimeline.Mark("scene: LoadSceneAsync call", phase);
            phase = StartupTimeline.Now;
            if (load == null)
            {
                Debug.LogError($"SceneLoader: no scene called {sceneName} in the build.");
                Live(view)?.Hide();
                IsLoading = false;
                _loading = null;
                yield break;
            }

            load.allowSceneActivation = false;
            while (load.progress < 0.9f)
            {
                Live(view)?.SetProgress(load.progress / 0.9f);
                yield return null;
            }

            StartupTimeline.Mark("scene: loading in the background (to 90%)", phase);
            phase = StartupTimeline.Now;
            Live(view)?.SetProgress(1f);
            while (Live(view) != null && Time.realtimeSinceStartup - shownAt < MinimumShownSeconds)
            {
                yield return null;
            }

            StartupTimeline.Mark("scene: waiting for the loading screen's minimum", phase);
            phase = StartupTimeline.Now;
            load.allowSceneActivation = true;
            while (!load.isDone)
            {
                yield return null;
            }

            StartupTimeline.Mark("scene: activation (the new scene's Awake and OnEnable)", phase);
            phase = StartupTimeline.Now;
            yield return null; // the new scene's Start has run before it is uncovered
            StartupTimeline.Mark("scene: first frame (Start of the new scene)", phase);
            Live(view)?.Hide();
            IsLoading = false;
            _loading = null;
            Loaded?.Invoke(sceneName);

            if (_queued != null)
            {
                string next = _queued;
                _queued = null;
                StartCoroutine(LoadRoutine(next));
            }
        }
    }
}
