using System;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Agents {
    // Server: every field agent as parallel arrays (state, behaviour, deck path, rocks around it) that one system steps at once; no
    // object per agent in the scene. Kept in id order, so the batch a client gets is sorted and two batches can be
    // walked side by side. Bound in DataInstaller.
    public sealed class FieldAgentServerModel {
        private const int START_CAPACITY = 64;
        private const int FIRST_ID = 1;

        private FieldAgentState[] _states = new FieldAgentState[START_CAPACITY];
        private IFieldAgentBehaviour[] _behaviours = new IFieldAgentBehaviour[START_CAPACITY];
        private FieldAgentDeckPath[] _deckPaths = new FieldAgentDeckPath[START_CAPACITY];
        private FieldAgentRockCache[] _rockCaches = new FieldAgentRockCache[START_CAPACITY];
        private int _nextId = FIRST_ID;
        private bool _hasShipSample;

        public int Count { get; private set; }
        public int FrameSequence { get; private set; }
        public Vector3 ShipVelocity { get; private set; }
        public Vector3 LastShipPosition { get; private set; }

        public ref FieldAgentState GetState(int index) =>
            ref _states[index];

        public IFieldAgentBehaviour GetBehaviour(int index) =>
            _behaviours[index];

        public FieldAgentDeckPath GetDeckPath(int index) =>
            _deckPaths[index];

        public FieldAgentRockCache GetRockCache(int index) =>
            _rockCaches[index];

        public int Add(FieldAgentState state, IFieldAgentBehaviour behaviour) {
            EnsureCapacity(Count + 1);
            state.Id = _nextId++;
            _states[Count] = state;
            _behaviours[Count] = behaviour;
            _deckPaths[Count] ??= new FieldAgentDeckPath();
            _deckPaths[Count].Clear();
            _rockCaches[Count] ??= new FieldAgentRockCache();
            _rockCaches[Count].Invalidate();
            Count++;
            return state.Id;
        }

        public bool TryFindIndex(int id, out int index) {
            index = Array.BinarySearch(_states, 0, Count, new FieldAgentState { Id = id }, FieldAgentIdComparer.Instance);
            return index >= 0;
        }

        public void SetBehaviour(int index, IFieldAgentBehaviour behaviour) {
            _behaviours[index] = behaviour;
            _states[index].IsBehaviourFinished = false;
        }

        // Shifts the slots after the removed one down, so the id order holds; the removed deck path is reused.
        public void RemoveAt(int index) {
            FieldAgentDeckPath freedPath = _deckPaths[index];
            FieldAgentRockCache freedCache = _rockCaches[index];
            int moved = Count - index - 1;
            Array.Copy(_states, index + 1, _states, index, moved);
            Array.Copy(_behaviours, index + 1, _behaviours, index, moved);
            Array.Copy(_deckPaths, index + 1, _deckPaths, index, moved);
            Array.Copy(_rockCaches, index + 1, _rockCaches, index, moved);
            Count--;
            _behaviours[Count] = null;
            _deckPaths[Count] = freedPath;
            _rockCaches[Count] = freedCache;
        }

        public void Clear() {
            Array.Clear(_behaviours, 0, Count);
            Count = 0;
            _hasShipSample = false;
            ShipVelocity = Vector3.zero;
        }

        public int TakeFrameSequence() =>
            ++FrameSequence;

        public void RecordShipPosition(Vector3 position, float deltaTime) {
            if (_hasShipSample && deltaTime > 0f)
                ShipVelocity = (position - LastShipPosition) / deltaTime;

            LastShipPosition = position;
            _hasShipSample = true;
        }

        // Flight positions and world-anchored behaviours move with the world; ship-space agents stay where they are.
        public void ShiftWorld(Vector3 delta) {
            LastShipPosition += delta;
            for (int i = 0; i < Count; i++) {
                if (_states[i].IsInShipSpace == false)
                    _states[i].Position += delta;

                _rockCaches[i].Invalidate();

                if (_behaviours[i] is IWorldShiftFollower follower)
                    follower.FollowWorldShift(delta);
            }
        }

        private void EnsureCapacity(int needed) {
            if (needed <= _states.Length)
                return;

            int capacity = _states.Length * 2;
            Array.Resize(ref _states, capacity);
            Array.Resize(ref _behaviours, capacity);
            Array.Resize(ref _deckPaths, capacity);
            Array.Resize(ref _rockCaches, capacity);
        }
    }
}
