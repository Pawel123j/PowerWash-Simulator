using UnityEngine;
using UnityEngine.SceneManagement;
using Zmywak.UI.Screens;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// What every scene gets without being built with it: the loading screen between scenes, a voice and a feel for every
    /// control on its canvases (<see cref="UiSounds"/>), and in Kuba's room - behind the menu and in the evening - its
    /// sound (<see cref="RoomAmbience"/>).
    /// </summary>
    public static class UiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LoadingScreen.Install();
            bool hasAmbience = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                hasAmbience |= root.GetComponentInChildren<RoomAmbience>(true) != null;
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.isRootCanvas && canvas.GetComponent<UiSounds>() == null)
                    {
                        canvas.gameObject.AddComponent<UiSounds>();
                    }
                }
            }

            bool menu = scene.name == "MainMenu";
            if ((menu || scene.name == "MetaHub") && !hasAmbience)
            {
                var go = new GameObject("RoomAmbience");
                SceneManager.MoveGameObjectToScene(go, scene);
                go.SetActive(false);
                RoomAmbience ambience = go.AddComponent<RoomAmbience>();
                ambience.IsMenu = menu;
                go.SetActive(true);
            }
        }
    }
}
