using Features.NetworkModelModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.NetworkModelModule.Samples {
    public sealed class DebugCounterSample : NetworkBehaviour {
        [SerializeField] private DebugCounterBridge _bridge;

        private IReadOnlyDebugCounterModel _model;

        public int ObservedChanges { get; private set; }
        public int ObservedCount { get; private set; }

        [Inject]
        private void InjectDependencies(IReadOnlyDebugCounterModel model) =>
            _model = model;

        public override void OnStartClient() {
            _model.OnChanged += OnChanged;
            _model.OnCountChanged += OnCountChanged;
            if (isServer)
                CmdBump();
        }

        public override void OnStopClient() {
            _model.OnChanged -= OnChanged;
            _model.OnCountChanged -= OnCountChanged;
        }

        [Command(requiresAuthority = false)]
        public void CmdBump() =>
            ServerBump();

        [Server]
        private void ServerBump() {
            _bridge.ServerSetCount(4);
            _bridge.ServerSetLabel("bumped");
            _bridge.ServerSetPosition(new Vector3(1f, 2f, 3f));
            _bridge.ServerAddScores(7);
        }

        private void OnChanged() =>
            ObservedChanges++;

        private void OnCountChanged() =>
            ObservedCount = _model.Count;
    }
}
