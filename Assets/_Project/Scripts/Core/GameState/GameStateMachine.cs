using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.GameState.States;
using Zmywak.Core.Services;

namespace Zmywak.Core.GameState
{
    /// <summary>
    /// Drives the top-level flow of the game (Boot -> MainMenu -> KitchenShift -> ...).
    /// Lives on a persistent bootstrap object and is looked up through <see cref="ServiceLocator"/>.
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();
        private IGameState _currentState;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);

            RegisterState(new BootState(this));
            RegisterState(new MainMenuState());
            RegisterState(new MetaHubState());
            RegisterState(new KitchenShiftState());
        }

        private void Start()
        {
            ChangeState<BootState>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameStateMachine>();
        }

        public void RegisterState(IGameState state)
        {
            _states[state.GetType()] = state;
        }

        public void ChangeState<T>() where T : IGameState
        {
            ChangeState(typeof(T));
        }

        public void ChangeState(Type state)
        {
            if (!_states.TryGetValue(state, out IGameState next))
            {
                throw new InvalidOperationException($"GameState {state.Name} was never registered.");
            }

            _currentState?.Exit();
            _currentState = next;
            _currentState.Enter();
        }
    }
}
