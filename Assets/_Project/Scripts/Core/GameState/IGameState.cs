namespace Zmywak.Core.GameState
{
    /// <summary>A single node in the top-level game flow (Boot, MainMenu, KitchenShift, ...).</summary>
    public interface IGameState
    {
        void Enter();
        void Exit();
    }
}
