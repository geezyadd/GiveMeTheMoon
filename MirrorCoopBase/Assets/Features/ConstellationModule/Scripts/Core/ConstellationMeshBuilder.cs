using System;
using System.Collections.Generic;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Core {
    // Builds the highlight mesh of a constellation: a quad per star and a strip per figure segment. Every vertex is a
    // world direction, not a point: ConstellationAtInfinity.shader projects it like the skybox, so the figure stays on
    // the painted stars wherever the camera stands.
    // Vertex streams: UV0 of a star is (shape x, shape y, brightness, twinkle phase), the shape in degrees of a
    // reference-magnitude star; UV0 of a line is (side -1..1, degrees from the line's start, line length in degrees,
    // +1 at the start / -1 at the end) and UV1 the direction of the line's other end, so the shader can widen the line
    // on screen.
    public sealed class ConstellationMeshBuilder {
        public const int STAR_SUBMESH = 0;
        public const int LINE_SUBMESH = 1;

        private const int SUBMESH_COUNT = 2;
        // The vertices lie around the object's origin, but the shader draws them at infinity: bounds this large keep
        // the renderer from being frustum-culled wherever the camera is.
        private const float ALWAYS_VISIBLE_BOUNDS_SIZE = 1000000f;
        private const float NEAR_POLE_HEIGHT = 0.99f;
        // Pogson's ratio: five magnitudes are a hundredfold in light.
        private const float FLUX_PER_MAGNITUDE_EXPONENT = -0.4f;
        // The golden angle spreads the stars' twinkle phases so no two neighbours breathe together.
        private const float TWINKLE_PHASE_STEP_RADIANS = 2.39996323f;
        private const float LINE_START = 1f;
        private const float LINE_END = -1f;

        private static readonly Vector2[] QUAD_CORNERS = { new(-1f, -1f), new(-1f, 1f), new(1f, 1f), new(1f, -1f) };
        private static readonly int[] QUAD_TRIANGLES = { 0, 1, 2, 0, 2, 3 };

        private readonly CelestialSkyFrameConfiguration _celestialSkyFrameConfiguration;
        private readonly ConstellationHighlightConfiguration _constellationHighlightConfiguration;
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector4> _shapes = new();
        private readonly List<Vector3> _otherLineEnds = new();
        private readonly List<int> _starIndices = new();
        private readonly List<int> _lineIndices = new();
        private readonly List<Vector3> _starDirections = new();
        private readonly List<float> _starScales = new();

        public ConstellationMeshBuilder(CelestialSkyFrameConfiguration celestialSkyFrameConfiguration, ConstellationHighlightConfiguration constellationHighlightConfiguration) {
            _celestialSkyFrameConfiguration = celestialSkyFrameConfiguration;
            _constellationHighlightConfiguration = constellationHighlightConfiguration;
        }

        public Mesh Build(ConstellationDefinition constellation) {
            Clear();
            for (int i = 0; i < constellation.Stars.Count; i++)
                AddStar(constellation.Stars[i], i);
            foreach (ConstellationLine line in constellation.Lines)
                AddLine(GetStarIndex(constellation, line.FromStar), GetStarIndex(constellation, line.ToStar));

            Mesh mesh = new() { name = constellation.name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(_vertices);
            mesh.SetUVs(0, _shapes);
            mesh.SetUVs(1, _otherLineEnds);
            mesh.subMeshCount = SUBMESH_COUNT;
            mesh.SetIndices(_starIndices, MeshTopology.Triangles, STAR_SUBMESH, calculateBounds: false);
            mesh.SetIndices(_lineIndices, MeshTopology.Triangles, LINE_SUBMESH, calculateBounds: false);
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

        private static int GetStarIndex(ConstellationDefinition constellation, int index) {
            if (index < 0 || index >= constellation.Stars.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Constellation {constellation.name} has no star {index} for a line.");
            return index;
        }

        private void Clear() {
            _vertices.Clear();
            _shapes.Clear();
            _otherLineEnds.Clear();
            _starIndices.Clear();
            _lineIndices.Clear();
            _starDirections.Clear();
            _starScales.Clear();
        }

        private float GetMagnitudeFactor(float magnitude, float exponent, Vector2 range) {
            float flux = Mathf.Pow(10f, FLUX_PER_MAGNITUDE_EXPONENT * (magnitude - _constellationHighlightConfiguration.ReferenceMagnitude));
            return Mathf.Clamp(Mathf.Pow(flux, exponent), range.x, range.y);
        }

        // The quad is as large as the halo or the spikes, whichever reaches further; its axes are the spikes' axes.
        private void AddStar(ConstellationStar star, int index) {
            Vector3 direction = GetWorldDirection(star);
            float scale = GetMagnitudeFactor(star.Magnitude, _constellationHighlightConfiguration.MagnitudeSizeExponent, _constellationHighlightConfiguration.StarScaleRange);
            float brightness = GetMagnitudeFactor(star.Magnitude, _constellationHighlightConfiguration.MagnitudeBrightnessExponent, _constellationHighlightConfiguration.StarBrightnessRange);
            _starDirections.Add(direction);
            _starScales.Add(scale);

            GetSpikeAxes(direction, out Vector3 spikeRight, out Vector3 spikeUp);
            float extentDegrees = Mathf.Max(_constellationHighlightConfiguration.HaloRadiusDegrees, _constellationHighlightConfiguration.SpikeLengthDegrees);
            float extent = Mathf.Tan(extentDegrees * scale * Mathf.Deg2Rad);
            float twinklePhase = index * TWINKLE_PHASE_STEP_RADIANS;
            int first = _vertices.Count;
            foreach (Vector2 corner in QUAD_CORNERS) {
                _vertices.Add((direction + (spikeRight * corner.x + spikeUp * corner.y) * extent).normalized);
                _shapes.Add(new Vector4(corner.x * extentDegrees, corner.y * extentDegrees, brightness, twinklePhase));
                _otherLineEnds.Add(Vector3.zero);
            }
            foreach (int corner in QUAD_TRIANGLES)
                _starIndices.Add(first + corner);
        }

        // Screen right and up for a camera looking at the direction with the world's up (or forward, near a pole) on
        // top, turned clockwise by the spike angle.
        private void GetSpikeAxes(Vector3 direction, out Vector3 spikeRight, out Vector3 spikeUp) {
            Vector3 helperAxis = Mathf.Abs(direction.y) > NEAR_POLE_HEIGHT ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(helperAxis, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right);
            float angle = _constellationHighlightConfiguration.SpikeAngleDegrees * Mathf.Deg2Rad;
            spikeRight = right * Mathf.Cos(angle) - up * Mathf.Sin(angle);
            spikeUp = up * Mathf.Cos(angle) + right * Mathf.Sin(angle);
        }

        // A line ends short of each star by the gap scaled with that star's drawn size, so close stars (Orion's belt)
        // keep their lines; a line with no length left between the two gaps is skipped.
        private void AddLine(int fromStar, int toStar) {
            Vector3 from = _starDirections[fromStar];
            Vector3 to = _starDirections[toStar];
            float angle = Vector3.Angle(from, to);
            float fromGap = _constellationHighlightConfiguration.LineGapDegrees * _starScales[fromStar];
            float toGap = _constellationHighlightConfiguration.LineGapDegrees * _starScales[toStar];
            float length = angle - fromGap - toGap;
            if (length <= 0f)
                return;

            Vector3 start = Vector3.Slerp(from, to, fromGap / angle);
            Vector3 end = Vector3.Slerp(to, from, toGap / angle);
            int first = _vertices.Count;
            AddLineVertex(start, end, -1f, 0f, length, LINE_START);
            AddLineVertex(start, end, 1f, 0f, length, LINE_START);
            AddLineVertex(end, start, 1f, length, length, LINE_END);
            AddLineVertex(end, start, -1f, length, length, LINE_END);
            foreach (int corner in QUAD_TRIANGLES)
                _lineIndices.Add(first + corner);
        }

        private void AddLineVertex(Vector3 direction, Vector3 otherEnd, float side, float alongDegrees, float lengthDegrees, float lineEnd) {
            _vertices.Add(direction);
            _shapes.Add(new Vector4(side, alongDegrees, lengthDegrees, lineEnd));
            _otherLineEnds.Add(otherEnd);
        }
    }
}
