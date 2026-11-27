using System;
using System.Collections.Generic;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    // Builds the highlight mesh of a constellation: a glow quad per star and a line per figure segment. Every vertex is
    // a world direction, not a point: ConstellationAtInfinity.shader projects it like the skybox, so the figure stays
    // on the painted stars wherever the camera stands.
    public sealed class ConstellationMeshBuilder {
        public const int STAR_SUBMESH = 0;
        public const int LINE_SUBMESH = 1;

        private const int SUBMESH_COUNT = 2;
        // The vertices lie around the object's origin, but the shader draws them at infinity: bounds this large keep
        // the renderer from being frustum-culled wherever the camera is.
        private const float ALWAYS_VISIBLE_BOUNDS_SIZE = 1000000f;
        private const float NEAR_POLE_HEIGHT = 0.99f;
        private const float MAX_LINE_GAP_SHARE = 0.5f;

        private static readonly Vector2[] STAR_CORNERS = { new(-1f, -1f), new(-1f, 1f), new(1f, 1f), new(1f, -1f) };
        private static readonly int[] STAR_TRIANGLES = { 0, 1, 2, 0, 2, 3 };

        private readonly CelestialSkyFrameConfiguration _celestialSkyFrameConfiguration;
        private readonly ConstellationHighlightConfiguration _constellationHighlightConfiguration;
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector2> _corners = new();
        private readonly List<int> _starIndices = new();
        private readonly List<int> _lineIndices = new();

        public ConstellationMeshBuilder(CelestialSkyFrameConfiguration celestialSkyFrameConfiguration, ConstellationHighlightConfiguration constellationHighlightConfiguration) {
            _celestialSkyFrameConfiguration = celestialSkyFrameConfiguration;
            _constellationHighlightConfiguration = constellationHighlightConfiguration;
        }

        public Mesh Build(ConstellationDefinition constellation) {
            Clear();
            foreach (ConstellationStar star in constellation.Stars)
                AddStar(GetWorldDirection(star));
            foreach (ConstellationLine line in constellation.Lines)
                AddLine(GetWorldDirection(GetStar(constellation, line.FromStar)), GetWorldDirection(GetStar(constellation, line.ToStar)));

            Mesh mesh = new() { name = constellation.name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _corners);
            mesh.subMeshCount = SUBMESH_COUNT;
            mesh.SetIndices(_starIndices, MeshTopology.Triangles, STAR_SUBMESH, calculateBounds: false);
            mesh.SetIndices(_lineIndices, MeshTopology.Lines, LINE_SUBMESH, calculateBounds: false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * ALWAYS_VISIBLE_BOUNDS_SIZE);
            return mesh;
        }

        private Vector3 GetWorldDirection(ConstellationStar star) {
            double rightAscension = star.RightAscensionDegrees * Math.PI / 180.0;
            double declination = star.DeclinationDegrees * Math.PI / 180.0;
            Vector3 celestial = new(
                (float)(Math.Cos(declination) * Math.Cos(rightAscension)),
                (float)Math.Sin(declination),
                (float)(Math.Cos(declination) * Math.Sin(rightAscension)));
            return _celestialSkyFrameConfiguration.CelestialToWorld * celestial;
        }

        private static ConstellationStar GetStar(ConstellationDefinition constellation, int index) {
            if (index < 0 || index >= constellation.Stars.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Constellation {constellation.name} has no star {index} for a line.");
            return constellation.Stars[index];
        }

        private void Clear() {
            _vertices.Clear();
            _corners.Clear();
            _starIndices.Clear();
            _lineIndices.Clear();
        }

        private void AddStar(Vector3 direction) {
            Vector3 helperAxis = Mathf.Abs(direction.y) > NEAR_POLE_HEIGHT ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(direction, helperAxis).normalized;
            Vector3 up = Vector3.Cross(right, direction);
            float radius = Mathf.Tan(_constellationHighlightConfiguration.StarRadiusDegrees * Mathf.Deg2Rad);
            int first = _vertices.Count;
            foreach (Vector2 corner in STAR_CORNERS) {
                _vertices.Add((direction + (right * corner.x + up * corner.y) * radius).normalized);
                _corners.Add(corner);
            }
            foreach (int corner in STAR_TRIANGLES)
                _starIndices.Add(first + corner);
        }

        // A line ends short of its stars by the gap; a line shorter than two gaps would vanish into the glows and is skipped.
        private void AddLine(Vector3 from, Vector3 to) {
            float gapShare = _constellationHighlightConfiguration.LineGapDegrees / Vector3.Angle(from, to);
            if (gapShare >= MAX_LINE_GAP_SHARE)
                return;

            AddLineEnd(Vector3.Slerp(from, to, gapShare));
            AddLineEnd(Vector3.Slerp(to, from, gapShare));
        }

        private void AddLineEnd(Vector3 direction) {
            _lineIndices.Add(_vertices.Count);
            _vertices.Add(direction);
            _corners.Add(Vector2.zero);
        }
    }
}
