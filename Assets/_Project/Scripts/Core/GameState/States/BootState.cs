namespace Zmywak.Core.GameState.States
{
    /// <summary>Runs once at startup, then hands control over to <see cref="MainMenuState"/> - or, when Play was pressed
    /// on another scene in the editor, back to that scene's state (<see cref="StartFromAnyScene"/>).</summary>
    public class BootState : IGameState
    {
        private readonly GameStateMachine _machine;

        public BootState(GameStateMachine machine)
        {
            _machine = machine;
        }

        public void Enter()
        {
            if (StartFromAnyScene.TakeReturn(out System.Type state))
            {
                _machine.ChangeState(state);
                return;
            }

            _machine.ChangeState<MainMenuState>();
        }

        public void Exit()
        {
        }
    }
}
