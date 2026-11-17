namespace Zmywak.Core.GameState.States
{
    /// <summary>The player's home base between shifts: stats, wallet, actions like calling home.</summary>
    public class MetaHubState : IGameState
    {
        public const string MetaHubSceneName = "MetaHub";

        public void Enter()
        {
            SceneLoader.Load(MetaHubSceneName); // in the background, behind the loading screen
        }

        public void Exit()
        {
        }
    }
}
