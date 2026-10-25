using System;
using Features.GrabModule.Scripts.Held;
using Features.GrabModule.Scripts.Hold;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // Server side of using an interactable: checks reach and that the use is still allowed, then performs it.
    // A hold target is used only after the server has seen the button held for its whole hold time. A held control
    // (IHeldControl) is held while the button is down and lets go when the player leaves reach or may no longer use it.
    public sealed class UseController : NetworkBehaviour {
        [SerializeField] private GrabController _grab;

        private readonly HoldUseProgress _hold = new HoldUseProgress();

        private InteractionReach _reach;
        private InteractableBase _serverHoldTarget;
        private InteractableBase _requestedHoldTarget;
        private InteractableBase _serverHeldControlTarget;
        private InteractableBase _requestedHeldControl;

        [SyncVar(hook = nameof(OnHoldProgressSynced))]
        private float _holdProgress;

        internal event Action OnHoldProgressChanged;

        public float HoldProgress => _holdProgress;
        internal InteractableBase RequestedHoldTarget => _requestedHoldTarget;

        [Inject]
        private void InjectDependencies(InteractionReach reach) =>
            _reach = reach;

        public override void OnStopServer() {
            ServerEndHold();
            ServerLetGoHeldControl();
        }

        private void Update() {
            if (isServer == false)
                return;

            ServerAdvanceHold(Time.deltaTime);
            ServerCheckHeldControl();
        }

        public bool CanUse(InteractableBase target) =>
            target != null && target.CanUse(netIdentity, _grab);

        public void RequestUse(InteractableBase target) {
            if (CanUse(target) == false)
                return;

            if (target is IHeldControl) {
                _requestedHeldControl = target;
                CmdTakeHeldControl(target);
                return;
            }

            if (target.TryGetHoldSeconds(_grab, out _)) {
                _requestedHoldTarget = target;
                CmdBeginHold(target);
                return;
            }

            CmdUse(target);
        }

        public void RequestEndHold() {
            if (_requestedHoldTarget == null)
                return;

            _requestedHoldTarget = null;
            CmdEndHold();
        }

        public void RequestLetGoHeldControl() {
            if (_requestedHeldControl == null)
                return;

            _requestedHeldControl = null;
            CmdLetGoHeldControl();
        }

        public void SendHeldControlPosition(float position) {
            if (_requestedHeldControl != null)
                CmdMoveHeldControl(position);
        }

        private void ServerAdvanceHold(float deltaTime) {
            if (_hold.IsActive == false)
                return;

            HoldUseStatus status = _hold.Advance(deltaTime, CheckServerHold());
            InteractableBase target = _serverHoldTarget;
            if (status != HoldUseStatus.Holding)
                _serverHoldTarget = null;

            _holdProgress = _hold.Progress;
            if (status == HoldUseStatus.Completed)
                target.ServerUse(netIdentity, _grab);
        }

        // The target can be destroyed or taken away under a running hold: that ends the hold like any other change.
        private HoldUseCheck CheckServerHold() {
            InteractableBase target = _serverHoldTarget;
            if (target == null)
                return new HoldUseCheck(false, false, 0);

            return new HoldUseCheck(target.CanUse(netIdentity, _grab), IsInReach(target), ReadHeldItemNetId());
        }

        // The control can be destroyed, given away or out of reach under a running hold: any of these lets it go.
        private void ServerCheckHeldControl() {
            InteractableBase target = _serverHeldControlTarget;
            if (ReferenceEquals(target, null))
                return;

            if (target == null || target.CanUse(netIdentity, _grab) == false || IsInReach(target) == false)
                ServerLetGoHeldControl();
        }

        private void ServerLetGoHeldControl() {
            InteractableBase target = _serverHeldControlTarget;
            _serverHeldControlTarget = null;
            if (target != null && target is IHeldControl control)
                control.ServerLetGo(netIdentity);
        }

        private void ServerEndHold() {
            _hold.Cancel();
            _serverHoldTarget = null;
            _holdProgress = 0f;
        }

        private bool IsInReach(InteractableBase target) =>
            _reach.Contains(transform, target.transform.position);

        private uint ReadHeldItemNetId() {
            Grabbable held = _grab.Held;
            return held != null ? held.netId : 0;
        }

        [Command]
        private void CmdUse(InteractableBase target) {
            if (target == null || IsInReach(target) == false || target.TryGetHoldSeconds(_grab, out _))
                return;

            if (target.CanUse(netIdentity, _grab))
                target.ServerUse(netIdentity, _grab);
        }

        [Command]
        private void CmdBeginHold(InteractableBase target) {
            if (target == null || IsInReach(target) == false || target.CanUse(netIdentity, _grab) == false)
                return;

            if (target.TryGetHoldSeconds(_grab, out float seconds) == false)
                return;

            _serverHoldTarget = target;
            _hold.Begin(seconds, ReadHeldItemNetId());
            _holdProgress = _hold.Progress;
        }

        [Command]
        private void CmdEndHold() =>
            ServerEndHold();

        [Command]
        private void CmdTakeHeldControl(InteractableBase target) {
            if (target == null || IsInReach(target) == false || target.CanUse(netIdentity, _grab) == false)
                return;

            if (target is not IHeldControl control)
                return;

            if (_serverHeldControlTarget != target)
                ServerLetGoHeldControl();

            if (control.ServerTryTakeHold(netIdentity))
                _serverHeldControlTarget = target;
        }

        [Command]
        private void CmdLetGoHeldControl() =>
            ServerLetGoHeldControl();

        [Command]
        private void CmdMoveHeldControl(float position) {
            InteractableBase target = _serverHeldControlTarget;
            if (target == null || IsInReach(target) == false)
                return;

            if (target is IHeldControl control)
                control.ServerReceivePosition(netIdentity, position);
        }

        private void OnHoldProgressSynced(float previous, float current) =>
            OnHoldProgressChanged?.Invoke();
    }
}
