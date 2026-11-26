using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipPoseSync : NetworkBehaviour {
        private const double MIN_SAMPLE_SPAN = 0.005d;
        private const float MAX_EXTRAPOLATION = 0.08f;

        [SerializeField] private Transform _ship;
        [SerializeField] private float _interpolationDelay = 0.06f;
        [Tooltip("A client's clock for the ship may run this share faster or slower to meet the server time after a stall (0.05 = 5 %).")]
        [SerializeField, Range(0f, 0.5f)] private float _clockMaxRateChange = 0.05f;
        [Tooltip("How fast that clock closes a gap to the server time: the share of the gap per second, before the limit above.")]
        [SerializeField, Min(0f)] private float _clockCatchUpPerSecond = 1f;
        [Tooltip("A gap to the server time this long, seconds, is closed at once (a long stall).")]
        [SerializeField, Min(0.05f)] private float _clockJumpGap = 0.5f;

        [SyncVar(hook = nameof(OnFlyingChanged))]
        private bool _flying;

        [SyncVar]
        private Vector3 _latePos;

        [SyncVar]
        private Quaternion _lateRot = Quaternion.identity;

        [SyncVar(hook = nameof(OnWorldShiftChanged))]
        private WorldShift _worldShift;

        private readonly SyncList<DeckCargoEntry> _deckCargo = new SyncList<DeckCargoEntry>();
        private readonly List<PoseSample> _samples = new List<PoseSample>(16);
        private float _nextPublish;
        private ShipBase _shipBase;
        // A client steps the ship's pose by this, evenly per physics step and eased toward the network time, so a stalled
        // frame does not make the ship (and its riders) leap.
        private SmoothServerClock _clock;

        internal bool IsFlying => _flying;
        internal ShipBase Ship => _shipBase;
        internal WorldShift WorldShift => _worldShift;

        private void Awake() =>
            _clock = new SmoothServerClock(_clockMaxRateChange, _clockCatchUpPerSecond, _clockJumpGap);

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
            RpcShift(delta);
        }

        // The SyncVar goes out in the shift frame's broadcast (syncInterval 0), on the reliable channel after that
        // frame's RPCs, so clients apply it in the same frame as the ship's shift and snap RPCs.
        [Server]
        internal void ServerRecordWorldShift(Vector3 delta) =>
            _worldShift = _worldShift.Add(delta);

        internal Vector3 ShiftSince(WorldShift placedUnder) =>
            _worldShift.Since(placedUnder);

        [Server]
        internal void ServerPublish(Vector3 position, Quaternion rotation, Vector3 velocity) {
            _latePos = position;
            _lateRot = rotation;
            if (Time.unscaledTime < _nextPublish)
                return;

            _nextPublish = Time.unscaledTime + 0.033f;
            RpcPose(NetworkTime.time, _worldShift, position, rotation, velocity);
        }

        // A client, once per physics step: the ship's pose at this step's show time (the smooth server clock minus the
        // interpolation delay; none for the local pilot), applied to its body.
        internal void ClientStep(float dt, bool lowLatency) {
            if (_shipBase == null || _clock.IsStarted == false)
                return;

            _clock.Step(dt);
            double showTime = _clock.Time - (lowLatency ? 0d : _interpolationDelay);
            if (TrySamplePose(showTime, out Vector3 position, out Quaternion rotation))
                _shipBase.MoveBody(position, rotation);
        }

        private void Update() {
            if (isServer || NetworkClient.active == false)
                return;

            if (_clock.IsStarted == false) {
                _clock.Start(NetworkTime.time);
                return;
            }

            // The clock stands at the last physics step; the frame is a little later.
            double clockNow = _clock.Time + (Time.timeAsDouble - Time.fixedTimeAsDouble);
            _clock.Compare(NetworkTime.time, clockNow);
        }

        // The pose at a time from the buffered samples: between two of them, a little past the last one, or the last
        // synced pose when there are none yet.
        private bool TrySamplePose(double showTime, out Vector3 position, out Quaternion rotation) {
            position = _latePos;
            rotation = _lateRot;
            if (_samples.Count == 0)
                return _lateRot.x != 0f || _lateRot.y != 0f || _lateRot.z != 0f || _lateRot.w != 0f;

            PoseSample last = _samples[_samples.Count - 1];
            if (showTime >= last.Time) {
                float extra = Mathf.Min((float)(showTime - last.Time), MAX_EXTRAPOLATION);
                position = last.Position + last.Velocity * extra;
                rotation = ExtrapolateRotation(last, extra);
                return true;
            }

            PoseSample from = _samples[0];
            PoseSample to = last;
            for (int i = 0; i < _samples.Count - 1; i++) {
                if (_samples[i].Time <= showTime && _samples[i + 1].Time >= showTime) {
                    from = _samples[i];
                    to = _samples[i + 1];
                    break;
                }
            }

            double span = to.Time - from.Time;
            float blend = span > 0.0001d ? (float)((showTime - from.Time) / span) : 1f;
            blend = Mathf.Clamp01(blend);
            position = Vector3.Lerp(from.Position, to.Position, blend);
            rotation = Quaternion.Slerp(from.Rotation, to.Rotation, blend);
            return true;
        }

        public override void OnStartServer() {
            _latePos = _ship.position;
            _lateRot = _ship.rotation;
        }

        public override void OnStartClient() {
            if (isServer || _shipBase == null)
                return;

            _shipBase.ApplyDisplayPose(_latePos, _lateRot);
            // A full spawn does not call the list callbacks: the cargo already on the deck is attached here.
            _deckCargo.OnAdd += OnDeckCargoAdded;
            _deckCargo.OnSet += OnDeckCargoSet;
            _deckCargo.OnRemove += OnDeckCargoRemoved;
            for (int i = 0; i < _deckCargo.Count; i++)
                AttachDeckItem(_deckCargo[i]);
        }

        public override void OnStopClient() {
            if (isServer || _shipBase == null)
                return;

            _deckCargo.OnAdd -= OnDeckCargoAdded;
            _deckCargo.OnSet -= OnDeckCargoSet;
            _deckCargo.OnRemove -= OnDeckCargoRemoved;
            _shipBase.Cargo.DetachAll(false);
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

        // Buffered poses move into the new frame, so the interpolated pose (and everything riding it) stays continuous.
        private void RebaseSamples() {
            for (int i = 0; i < _samples.Count; i++) {
                PoseSample sample = _samples[i];
                if (sample.WorldShift.Count == _worldShift.Count)
                    continue;

                Vector3 position = sample.Position + ShiftSince(sample.WorldShift);
                _samples[i] = new PoseSample(sample.Time, position, sample.Rotation, sample.Velocity, _worldShift);
            }
        }

        private void OnWorldShiftChanged(WorldShift previous, WorldShift current) {
            if (isServer == false)
                RebaseSamples();
        }

        private void OnFlyingChanged(bool previous, bool current) {
            if (_shipBase != null)
                _shipBase.OnClientFlightChanged(current);
        }

        [Server]
        internal void ServerAttachDeckItem(DeckCargoEntry entry) =>
            _deckCargo.Add(entry);

        [Server]
        internal void ServerSettleDeckItem(DeckCargoEntry entry) {
            for (int i = 0; i < _deckCargo.Count; i++) {
                if (_deckCargo[i].NetId == entry.NetId)
                    _deckCargo[i] = entry;
            }
        }

        [Server]
        internal void ServerDetachDeckItem(uint netId) {
            for (int i = _deckCargo.Count - 1; i >= 0; i--) {
                if (_deckCargo[i].NetId == netId)
                    _deckCargo.RemoveAt(i);
            }
        }

        private void OnDeckCargoAdded(int index) =>
            AttachDeckItem(_deckCargo[index]);

        private void OnDeckCargoSet(int index, DeckCargoEntry previous) =>
            AttachDeckItem(_deckCargo[index]);

        private void OnDeckCargoRemoved(int index, DeckCargoEntry removed) =>
            _shipBase.Cargo.ClientDetach(removed.NetId);

        private void AttachDeckItem(DeckCargoEntry entry) =>
            _shipBase.Cargo.ClientAttach(entry);

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
        private void RpcShift(Vector3 delta) {
            if (isServer)
                return;

            if (_shipBase != null && _samples.Count == 0)
                _shipBase.ApplyDisplayPose(_ship.position + delta, _ship.rotation);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcPose(double time, WorldShift placedUnder, Vector3 position, Quaternion rotation, Vector3 velocity) {
            if (isServer)
                return;

            // Unreliable poses and the reliable shift can arrive in either order, and the shift frame's own pose has
            // the shift's time: each pose is moved from the shift it was published under into this peer's current one.
            position += ShiftSince(placedUnder);

            if (_samples.Count > 0 && time <= _samples[_samples.Count - 1].Time)
                return;

            _samples.Add(new PoseSample(time, position, rotation, velocity, _worldShift));
            while (_samples.Count > 12)
                _samples.RemoveAt(0);
        }

        private readonly struct PoseSample {
            public PoseSample(double time, Vector3 position, Quaternion rotation, Vector3 velocity, WorldShift worldShift) {
                Time = time;
                Position = position;
                Rotation = rotation;
                Velocity = velocity;
                WorldShift = worldShift;
            }

            public double Time { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Velocity { get; }
            public WorldShift WorldShift { get; }
        }
    }
}
