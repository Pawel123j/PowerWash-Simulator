namespace Zmywak.Core.GameState.States
{
    /// <summary>Loads and owns the main menu screen.</summary>
    public class MainMenuState : IGameState
    {
        public const string MainMenuSceneName = "MainMenu";

        public void Enter()
        {
            SceneLoader.Load(MainMenuSceneName); // in the background, behind the loading screen
        }

        public void Exit()
        {
        }
    }
}
