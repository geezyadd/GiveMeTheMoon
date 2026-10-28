using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Network;
using Mirror;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // A client's last few agent batches in time order, and the agents between two of them at a render time: position
    // lerped and rotation slerped per agent while its mode (so its space) stays the same, else the newer pose. Before
    // the oldest batch it shows the oldest, after the newest the newest (no extrapolation).
    public sealed class FieldAgentFrameBuffer {
        private readonly FieldAgentBufferedFrame[] _frames;
        private int _start;

        public int Count { get; private set; }

        public FieldAgentFrameBuffer(int capacity) {
            _frames = new FieldAgentBufferedFrame[capacity];
            for (int i = 0; i < capacity; i++)
                _frames[i] = new FieldAgentBufferedFrame();
        }

        public void Add(in FieldAgentFrame frame) {
            if (Count > 0 && IsNotNewer(frame, GetFrame(Count - 1)))
                return;

            if (Count == _frames.Length) {
                _start = (_start + 1) % _frames.Length;
                Count--;
            }

            _frames[(_start + Count) % _frames.Length].CopyFrom(frame);
            Count++;
        }

        public void Clear() {
            _start = 0;
            Count = 0;
        }

        // A floating-origin shift: the flying agents of every kept batch move with the world.
        public void Shift(Vector3 delta) {
            for (int k = 0; k < Count; k++)
                ShiftFrame(GetFrame(k), delta);
        }

        public void Sample(double renderTime, List<FieldAgentPose> results) {
            results.Clear();
            if (Count == 0)
                return;

            FieldAgentBufferedFrame newest = GetFrame(Count - 1);
            if (renderTime >= newest.ServerTime || Count == 1) {
                AddUnchanged(newest, results);
                return;
            }

            FieldAgentBufferedFrame oldest = GetFrame(0);
            if (renderTime <= oldest.ServerTime) {
                AddUnchanged(oldest, results);
                return;
            }

            int later = 1;
            while (GetFrame(later).ServerTime < renderTime)
                later++;

            FieldAgentBufferedFrame from = GetFrame(later - 1);
            FieldAgentBufferedFrame to = GetFrame(later);
            float share = (float)((renderTime - from.ServerTime) / (to.ServerTime - from.ServerTime));
            AddBetween(from, to, share, results);
        }

        private FieldAgentBufferedFrame GetFrame(int chronologicalIndex) =>
            _frames[(_start + chronologicalIndex) % _frames.Length];

        private static void ShiftFrame(FieldAgentBufferedFrame frame, Vector3 delta) {
            for (int i = 0; i < frame.Count; i++) {
                if (frame.Agents[i].Mode == (byte)FieldAgentMode.Flight)
                    frame.Agents[i].Position += delta;
            }
        }

        private static bool IsNotNewer(in FieldAgentFrame frame, FieldAgentBufferedFrame newest) =>
            frame.Sequence <= newest.Sequence || frame.ServerTime <= newest.ServerTime;

        private static void AddUnchanged(FieldAgentBufferedFrame frame, List<FieldAgentPose> results) {
            for (int i = 0; i < frame.Count; i++)
                results.Add(CreatePose(frame.Agents[i]));
        }

        // Both batches are sorted by id: one pass finds every agent of the later batch in the earlier one.
        private static void AddBetween(FieldAgentBufferedFrame from, FieldAgentBufferedFrame to, float share, List<FieldAgentPose> results) {
            int fromIndex = 0;
            for (int i = 0; i < to.Count; i++) {
                FieldAgentSnapshot later = to.Agents[i];
                while (fromIndex < from.Count && from.Agents[fromIndex].Id < later.Id)
                    fromIndex++;

                bool isInBoth = fromIndex < from.Count && from.Agents[fromIndex].Id == later.Id && from.Agents[fromIndex].Mode == later.Mode;
                results.Add(isInBoth ? Interpolate(from.Agents[fromIndex], later, share) : CreatePose(later));
            }
        }

        private static FieldAgentPose Interpolate(FieldAgentSnapshot earlier, FieldAgentSnapshot later, float share) {
            Vector3 position = Vector3.Lerp(earlier.Position, later.Position, share);
            Quaternion rotation = Quaternion.Slerp(Compression.DecompressQuaternion(earlier.Rotation), Compression.DecompressQuaternion(later.Rotation), share);
            return new FieldAgentPose(later.Id, later.KindIndex, (FieldAgentMode)later.Mode, position, rotation);
        }

        private static FieldAgentPose CreatePose(FieldAgentSnapshot snapshot) =>
            new(snapshot.Id, snapshot.KindIndex, (FieldAgentMode)snapshot.Mode, snapshot.Position, Compression.DecompressQuaternion(snapshot.Rotation));
    }
}
