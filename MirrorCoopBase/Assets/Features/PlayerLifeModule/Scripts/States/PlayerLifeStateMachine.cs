using System;

namespace Features.PlayerLifeModule.Scripts {
    // Server-side life of one player: Alive -> Dead on IDamageable.OnDied, Dead -> Alive only on Revive.
    public sealed class PlayerLifeStateMachine : IPlayerLifeStateMachine {
        private readonly IPlayerLifeActor _actor;
        private readonly IDamageable _damageable;
        private readonly AliveState _alive;
        private readonly DeadState _dead;
        private PlayerLifeStateBase _current;

        public PlayerLifeStateMachine(IPlayerLifeActor actor, IDamageable damageable) {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _damageable = damageable ?? throw new ArgumentNullException(nameof(damageable));
            _alive = new AliveState(actor);
            _dead = new DeadState(actor, damageable);
        }

        public PlayerLifeState State =>
            _current.Id;

        public event Action OnStateChanged;

        // Starts in the stored state, so a player who rejoins dead stays dead.
        public void Start() {
            _damageable.OnDied += OnDied;
            Enter(Resolve(_actor.LifeState));
        }

        // Detaches without a transition: the stored state outlives the player object.
        public void Stop() =>
            _damageable.OnDied -= OnDied;

        public void Revive() {
            if (_current == _dead) {
                Enter(_alive);
                return;
            }

            _damageable.ServerRestore();
        }

        private void OnDied(DamageInfo info) {
            if (_current == _dead)
                return;

            Enter(_dead);
        }

        private void Enter(PlayerLifeStateBase next) {
            if (_current != null)
                _current.Exit();

            _current = next;
            _current.Enter();
            OnStateChanged?.Invoke();
        }

        private PlayerLifeStateBase Resolve(PlayerLifeState state) {
            switch (state) {
                case PlayerLifeState.Alive:
                    return _alive;
                case PlayerLifeState.Dead:
                    return _dead;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
