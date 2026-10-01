using System;
using Mirror;
using Zenject;

namespace Features.NetworkModelModule.Scripts {
    public abstract class NetworkPlayerModelBridge<TModel> : NetworkBehaviour where TModel : NetworkModelBase {
        private IPlayerIdentityService _identity;
        private TModel _bound;
        private PlayerKey _key;
        private bool _serverBound;
        private bool _callbacksBound;
        private bool _suppress;

        protected TModel Bound =>
            _bound;

        protected bool SuppressModelWrites =>
            _suppress;

        protected abstract string PlayerKeyId { get; set; }

        [Inject]
        private void InjectIdentity(IPlayerIdentityService identity) =>
            _identity = identity;

        public override void OnStartServer() {
            _key = _identity.GetKey(connectionToClient);
            if (_key.IsEmpty)
                throw new InvalidOperationException("Player key is empty.");

            PlayerKeyId = _key.Id;
            _bound = BindServer(_key);
            _serverBound = true;
            _suppress = true;
            PullFromModel();
            _suppress = false;
            BindCallbacksOnce();
            _bound.SetAvailable(true);
        }

        public override void OnStartClient() {
            if (string.IsNullOrEmpty(PlayerKeyId))
                return;

            _key = new PlayerKey(PlayerKeyId);
            _bound = BindClient(_key, isLocalPlayer);
            BindCallbacksOnce();
            PushFullState();
            _bound.SetAvailable(true);
        }

        public override void OnStopServer() {
            if (_serverBound == false)
                return;

            ReleaseCallbacksOnce();
            ReleaseServer(_key);
            if (_bound != null)
                _bound.SetAvailable(false);

            _serverBound = false;
        }

        public override void OnStopClient() {
            ReleaseCallbacksOnce();
            if (_bound != null)
                _bound.SetAvailable(false);

            if (_key.IsEmpty == false)
                ReleaseClient(_key, isLocalPlayer);

            _bound = null;
        }

        protected abstract TModel BindServer(PlayerKey key);

        protected abstract TModel BindClient(PlayerKey key, bool local);

        protected abstract void ReleaseServer(PlayerKey key);

        protected abstract void ReleaseClient(PlayerKey key, bool local);

        protected abstract void PullFromModel();

        protected abstract void PushFullState();

        protected virtual void BindCallbacks() {
        }

        protected virtual void ReleaseCallbacks() {
        }

        private void BindCallbacksOnce() {
            if (_callbacksBound)
                return;

            _callbacksBound = true;
            BindCallbacks();
        }

        private void ReleaseCallbacksOnce() {
            if (_callbacksBound == false)
                return;

            _callbacksBound = false;
            ReleaseCallbacks();
        }
    }
}
