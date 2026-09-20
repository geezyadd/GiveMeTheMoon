using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipPoseSync : NetworkBehaviour {
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

        internal bool IsFlying => _flying;

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
                _shipBase.ApplyDisplayPose(last.Position + last.Velocity * extra, last.Rotation);
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

        public override void OnStartClient() {
            if (_flying && _shipBase != null)
                _shipBase.ApplyDisplayPose(_latePos, _lateRot);
        }

        private void OnFlyingChanged(bool previous, bool current) {
            if (_shipBase != null)
                _shipBase.OnClientFlightChanged(current);
        }

        [ClientRpc]
        private void RpcSnap(Vector3 position, Quaternion rotation) {
            if (isServer)
                return;

            _samples.Clear();
            if (_shipBase != null)
                _shipBase.ApplyDisplayPose(position, rotation);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcPose(double time, Vector3 position, Quaternion rotation, Vector3 velocity) {
            if (isServer)
                return;

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
