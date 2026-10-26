#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Hull;
using UnityEngine;
using UnityEngine.Rendering;

namespace Features.ShipImpactModule.Scripts.Debug {
    // Debug only, on this peer: draws over the game view what the server's rock check uses: the hull boxes around the
    // ship and, for the field rocks near it, the contact sphere the hull must reach (small: orange, large: red) inside
    // the rock's data sphere (grey). Lines are drawn after every game camera, through everything.
    public sealed class ShipImpactVolumeDebugDrawer : IShipImpactVolumeDebugView, IDisposable {
        private const string LINE_SHADER = "Hidden/Internal-Colored";
        private const string Z_TEST_PROPERTY = "_ZTest";
        private const string Z_WRITE_PROPERTY = "_ZWrite";
        private const string CULL_PROPERTY = "_Cull";
        private const float ROCK_SEARCH_RADIUS = 150f;
        private const int CIRCLE_SEGMENTS = 32;
        private static readonly Color _hullColor = new(0.2f, 1f, 0.4f, 1f);
        private static readonly Color _smallColor = new(1f, 0.6f, 0.1f, 1f);
        private static readonly Color _largeColor = new(1f, 0.2f, 0.2f, 1f);
        private static readonly Color _dataColor = new(0.75f, 0.75f, 0.75f, 0.7f);
        private static readonly Vector3[] _boxCornerSigns = {
            new(-1f, -1f, -1f), new(1f, -1f, -1f), new(1f, -1f, 1f), new(-1f, -1f, 1f),
            new(-1f, 1f, -1f), new(1f, 1f, -1f), new(1f, 1f, 1f), new(-1f, 1f, 1f)
        };
        // Corner index pairs: the bottom ring, the top ring, the four uprights.
        private static readonly int[] _boxEdges = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IShipRockImpactClassifier _shipRockImpactClassifier;
        private readonly IShipRockContactRule _shipRockContactRule;
        private readonly List<FieldRockDescription> _rocks = new();

        private Material _lineMaterial;

        public bool IsShown { get; private set; }

        public ShipImpactVolumeDebugDrawer(
            IShipRunBindingModel shipRunBindingModel,
            IFieldRockQuery fieldRockQuery,
            IShipRockImpactClassifier shipRockImpactClassifier,
            IShipRockContactRule shipRockContactRule) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldRockQuery = fieldRockQuery;
            _shipRockImpactClassifier = shipRockImpactClassifier;
            _shipRockContactRule = shipRockContactRule;
        }

        public void SetShown(bool isShown) {
            if (isShown == IsShown)
                return;

            IsShown = isShown;
            if (isShown)
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            else
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        public void Dispose() {
            SetShown(false);
            if (_lineMaterial != null)
                UnityEngine.Object.Destroy(_lineMaterial);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) {
            ShipBase ship = _shipRunBindingModel.Ship;
            bool isGameView = camera.cameraType == CameraType.Game && camera.targetTexture == null;
            if (ship == null || isGameView == false)
                return;

            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            ReadLineMaterial().SetPass(0);
            GL.Begin(GL.LINES);
            DrawHull(ship);
            DrawRocks(ship.transform.position);
            GL.End();
            GL.PopMatrix();
        }

        private static void DrawHull(ShipBase ship) {
            Matrix4x4 shipToWorld = ship.transform.localToWorldMatrix;
            GL.Color(_hullColor);
            foreach (ShipHullBox box in ship.HullVolume.Boxes) {
                for (int i = 0; i < _boxEdges.Length; i += 2) {
                    GL.Vertex(shipToWorld.MultiplyPoint3x4(box.Center + Vector3.Scale(box.HalfExtents, _boxCornerSigns[_boxEdges[i]])));
                    GL.Vertex(shipToWorld.MultiplyPoint3x4(box.Center + Vector3.Scale(box.HalfExtents, _boxCornerSigns[_boxEdges[i + 1]])));
                }
            }
        }

        private void DrawRocks(Vector3 shipPosition) {
            _fieldRockQuery.FindInSphere(shipPosition, ROCK_SEARCH_RADIUS, _rocks);
            foreach (FieldRockDescription rock in _rocks) {
                ShipRockImpactKind kind = _shipRockImpactClassifier.Classify(rock);
                float contactRadius = _shipRockContactRule.CalculateContactRadius(kind, rock.Radius);
                GL.Color(_dataColor);
                DrawCircle(rock.Center, rock.Radius, Vector3.right, Vector3.forward);
                GL.Color(kind == ShipRockImpactKind.Small ? _smallColor : _largeColor);
                DrawCircle(rock.Center, contactRadius, Vector3.right, Vector3.forward);
                DrawCircle(rock.Center, contactRadius, Vector3.right, Vector3.up);
                DrawCircle(rock.Center, contactRadius, Vector3.up, Vector3.forward);
            }
        }

        private static void DrawCircle(Vector3 center, float radius, Vector3 axisA, Vector3 axisB) {
            Vector3 previous = center + axisA * radius;
            for (int i = 1; i <= CIRCLE_SEGMENTS; i++) {
                float angle = i * Mathf.PI * 2f / CIRCLE_SEGMENTS;
                Vector3 next = center + (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius;
                GL.Vertex(previous);
                GL.Vertex(next);
                previous = next;
            }
        }

        private Material ReadLineMaterial() {
            if (_lineMaterial != null)
                return _lineMaterial;

            _lineMaterial = new Material(Shader.Find(LINE_SHADER)) { hideFlags = HideFlags.HideAndDontSave };
            _lineMaterial.SetInt(Z_TEST_PROPERTY, (int)CompareFunction.Always);
            _lineMaterial.SetInt(Z_WRITE_PROPERTY, 0);
            _lineMaterial.SetInt(CULL_PROPERTY, (int)CullMode.Off);
            return _lineMaterial;
        }
    }
}
#endif
