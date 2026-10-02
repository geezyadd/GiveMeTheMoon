using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipPoseSync : NetworkBehaviour {
        private const double MIN_SAMPLE_SPAN = 0.005d;

        [SerializeField] private Transform _ship;
        [SerializeField] private float _interpolationDelay = 0.06f;

        [SyncVar(hook = nameof(OnFlyingChanged))]
        private bool _flying;

        [SyncVar]
        private Vector3 _latePos;

        [SyncVar]
        private Quaternion _lateRot = Quaternion.identity;

        private readonly List<PoseSample> _samples = new List<PoseSample>(16);
        private float _nextPublish;
        private ShipBase _shipBase;
        private double _lastShiftTime = double.MinValue;
        private Vector3 _lastShiftDelta;

        internal bool IsFlying => _flying;
        internal ShipBase Ship => _shipBase;

        internal void BindShip(ShipBase ship, Transform root) {
            _shipBase = ship;
            _ship = root;
        }

        [Server]
        internal void ServerSetFlying(bool flying) {
            _flying = flying;
            if (flying == false)
                _samples.Clear();
        }

        [Server]
        internal void ServerSnap(Vector3 position, Quaternion rotation) {
            _latePos = position;
            _lateRot = rotation;
            _samples.Clear();
            RpcSnap(position, rotation);
        }

        // Floating-origin shift while flying: clients move their buffered samples instead of snapping, so the
        // interpolated pose (and everything riding it) stays continuous.
        [Server]
        internal void ServerShift(Vector3 delta, Vector3 position, Quaternion rotation) {
            _latePos = position;
            _lateRot = rotation;
            RpcShift(NetworkTime.time, delta);
        }

        [Server]
        internal void ServerPublish(Vector3 position, Quaternion rotation, Vector3 velocity) {
            _latePos = position;
            _lateRot = rotation;
            if (Time.unscaledTime < _nextPublish)
                return;

            _nextPublish = Time.unscaledTime + 0.033f;
            RpcPose(NetworkTime.time, position, rotation, velocity);
        }

        internal void ApplyInterpolated(bool lowLatency) {
            if (_shipBase == null)
                return;

            if (_samples.Count == 0) {
                if (_lateRot.x != 0f || _lateRot.y != 0f || _lateRot.z != 0f || _lateRot.w != 0f)
                    _shipBase.ApplyDisplayPose(_latePos, _lateRot);
                return;
            }

            double delay = lowLatency ? 0d : _interpolationDelay;
            double renderTime = NetworkTime.time - delay;
            PoseSample last = _samples[_samples.Count - 1];
            if (renderTime >= last.Time) {
                float extra = Mathf.Min((float)(renderTime - last.Time), 0.08f);
                _shipBase.ApplyDisplayPose(last.Position + last.Velocity * extra, ExtrapolateRotation(last, extra));
                return;
            }

            PoseSample from = _samples[0];
            PoseSample to = last;
            for (int i = 0; i < _samples.Count - 1; i++) {
                if (_samples[i].Time <= renderTime && _samples[i + 1].Time >= renderTime) {
                    from = _samples[i];
                    to = _samples[i + 1];
                    break;
                }
            }

            double span = to.Time - from.Time;
            float blend = span > 0.0001d ? (float)((renderTime - from.Time) / span) : 1f;
            blend = Mathf.Clamp01(blend);
            _shipBase.ApplyDisplayPose(
                Vector3.Lerp(from.Position, to.Position, blend),
                Quaternion.Slerp(from.Rotation, to.Rotation, blend));
        }

        public override void OnStartServer() {
            _latePos = _ship.position;
            _lateRot = _ship.rotation;
        }

        public override void OnStartClient() {
            if (isServer == false && _shipBase != null)
                _shipBase.ApplyDisplayPose(_latePos, _lateRot);
        }

        private Quaternion ExtrapolateRotation(PoseSample last, float extra) {
            if (_samples.Count < 2)
                return last.Rotation;

            PoseSample previous = _samples[_samples.Count - 2];
            double span = last.Time - previous.Time;
            if (span <= MIN_SAMPLE_SPAN)
                return last.Rotation;

            float factor = 1f + Mathf.Min((float)(extra / span), 1f);
            return Quaternion.SlerpUnclamped(previous.Rotation, last.Rotation, factor);
        }

        private void OnFlyingChanged(bool previous, bool current) {
            if (_shipBase != null)
                _shipBase.OnClientFlightChanged(current);
        }

        [Server]
        internal void ServerAttachDeckItem(uint netId, Vector3 localPosition, Quaternion localRotation) {
            RpcAttachDeckItem(netId, localPosition, localRotation);
        }

        [Server]
        internal void ServerDetachDeckItem(uint netId) {
            RpcDetachDeckItem(netId);
        }

        [ClientRpc]
        private void RpcAttachDeckItem(uint netId, Vector3 localPosition, Quaternion localRotation) {
            if (isServer || _shipBase == null)
                return;

            _shipBase.ClientAttachDeckItem(netId, localPosition, localRotation);
        }

        [ClientRpc]
        private void RpcDetachDeckItem(uint netId) {
            if (isServer || _shipBase == null)
                return;

            _shipBase.ClientDetachDeckItem(netId);
        }

        [ClientRpc]
        private void RpcSnap(Vector3 position, Quaternion rotation) {
            if (isServer)
                return;

            _samples.Clear();
            if (_shipBase == null)
                return;

            // The snap RPC can arrive before the _flying=false SyncVar; riders still bound here would be carried to the berth.
            _shipBase.OnClientFlightChanged(false);
            _shipBase.ApplyDisplayPose(position, rotation);
        }

        [ClientRpc]
        private void RpcShift(double time, Vector3 delta) {
            if (isServer)
                return;

            _lastShiftTime = time;
            _lastShiftDelta = delta;
            for (int i = 0; i < _samples.Count; i++) {
                PoseSample sample = _samples[i];
                if (sample.Time < time)
                    _samples[i] = new PoseSample(sample.Time, sample.Position + delta, sample.Rotation, sample.Velocity);
            }

            if (_shipBase != null && _samples.Count == 0)
                _shipBase.ApplyDisplayPose(_ship.position + delta, _ship.rotation);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcPose(double time, Vector3 position, Quaternion rotation, Vector3 velocity) {
            if (isServer)
                return;

            // An unreliable pose sent before the shift can arrive after the reliable shift RPC.
            if (time < _lastShiftTime)
                position += _lastShiftDelta;

            if (_samples.Count > 0 && time <= _samples[_samples.Count - 1].Time)
                return;

            _samples.Add(new PoseSample(time, position, rotation, velocity));
            while (_samples.Count > 12)
                _samples.RemoveAt(0);
        }

        private readonly struct PoseSample {
            public PoseSample(double time, Vector3 position, Quaternion rotation, Vector3 velocity) {
                Time = time;
                Position = position;
                Rotation = rotation;
                Velocity = velocity;
            }

            public double Time { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Velocity { get; }
        }
    }
}
