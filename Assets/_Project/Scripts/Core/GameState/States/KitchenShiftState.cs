namespace Zmywak.Core.GameState.States
{
    /// <summary>Active while the player is washing dishes during a shift.</summary>
    public class KitchenShiftState : IGameState
    {
        public const string KitchenShiftSceneName = "Kitchen_Shift";

        public void Enter()
        {
            SceneLoader.Load(KitchenShiftSceneName); // in the background, behind the loading screen
        }

        public void Exit()
        {
        }
    }
}
