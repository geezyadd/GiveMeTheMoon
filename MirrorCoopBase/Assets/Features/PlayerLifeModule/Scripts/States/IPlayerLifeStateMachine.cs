using System;

namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerLifeStateMachine {
        public PlayerLifeState State { get; }

        public event Action OnStateChanged;

        public void Start();
        public void Stop();
        public void Revive();
    }
}
