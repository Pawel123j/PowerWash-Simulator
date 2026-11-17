using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zmywak.Core.GameState.States;

namespace Zmywak.Core.GameState
{
    /// <summary>A persistent object that carries on through a restart into a save slot (the music, the loading screen).</summary>
    public interface IKeepAcrossRestart
    {
    }

    /// <summary>
    /// Play pressed on a scene other than Boot - the menu, the hub, the kitchen - goes through Boot first, which makes
    /// the persistent services (the state machine, the life between shifts, the story, the quality manager), and then
    /// comes back to that scene (<see cref="BootState"/> asks <see cref="TakeReturn"/>). A player build always starts
    /// at Boot; the test runner starts on a scene of its own, so neither is affected.
    /// </summary>
    public static class StartFromAnyScene
    {
        public const string BootSceneName = "Boot";

        private static Type _returnTo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene()
        {
            _returnTo = StateFor(SceneManager.GetActiveScene().name);
            if (_returnTo != null)
            {
                SceneManager.LoadScene(BootSceneName, LoadSceneMode.Single);
            }
        }

        /// <summary>The state that opens a game scene (null for Boot, and for anything that is not the game's).</summary>
        public static Type StateFor(string sceneName)
        {
            switch (sceneName)
            {
                case MainMenuState.MainMenuSceneName:
                    return typeof(MainMenuState);
                case MetaHubState.MetaHubSceneName:
                    return typeof(MetaHubState);
                case KitchenShiftState.KitchenShiftSceneName:
                    return typeof(KitchenShiftState);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Starts the game over from Boot - the persistent services made afresh, reading the save slot in play - and on
        /// into <paramref name="state"/> instead of the main menu (a save slot picked on the menu goes to the hub).
        /// </summary>
        public static void RestartInto(Type state, MonoBehaviour host)
        {
            host.StartCoroutine(Restart(state));
        }

        private static System.Collections.IEnumerator Restart(Type state)
        {
            // The game's own objects parked in DontDestroyOnLoad: gone before Boot makes them again.
            var probe = new GameObject("DontDestroyOnLoadProbe");
            UnityEngine.Object.DontDestroyOnLoad(probe);
            foreach (GameObject root in probe.scene.GetRootGameObjects())
            {
                if (root == probe || IsGames(root) && root.GetComponentInChildren<IKeepAcrossRestart>(true) == null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            yield return null; // let them unregister from the ServiceLocator
            _returnTo = state;
            SceneManager.LoadScene(BootSceneName, LoadSceneMode.Single);
        }

        private static bool IsGames(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string ns = behaviour != null ? behaviour.GetType().Namespace : null;
                if (ns != null && ns.StartsWith("Zmywak"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The scene's state Boot should go on to instead of the main menu, once (false: the usual start).</summary>
        public static bool TakeReturn(out Type state)
        {
            state = _returnTo;
            _returnTo = null;
            return state != null;
        }
    }
}
