using Mirror;
using Zenject;

namespace Features.NetworkModelModule.Scripts {
    public abstract class NetworkModelBridge<TModel> : NetworkBehaviour where TModel : NetworkModelBase {
        private TModel _model;
        private bool _callbacksBound;

        protected TModel Model =>
            _model;

        [Inject]
        private void InjectDependencies(TModel model) =>
            _model = model;

        public override void OnStartServer() =>
            BindCallbacksOnce();

        public override void OnStartClient() {
            BindCallbacksOnce();
            // Spawn applies SyncVars without a reliable hook: initial deserialize skips the hook when the
            // value still equals the field default, and SyncList/SyncDictionary/SyncHashSet full snapshots
            // never raise callbacks. Pushing here is the same path for host, client and late join.
            PushFullState();
            _model.SetAvailable(true);
        }

        public override void OnStopClient() {
            ReleaseCallbacksOnce();
            _model.SetAvailable(false);
            ClearLocal();
        }

        protected abstract void PushFullState();

        protected abstract void ClearLocal();

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
