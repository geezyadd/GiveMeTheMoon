using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The points of interest of the game's fields: the same seed gives the same points, every point lies off the
    // straight course within the side range, the field has as many as the configuration allows, and no rock of the
    // flight plane stands in a point's clear circle.
    public sealed class FieldPointOfInterestStepTests {
        private const float TOLERANCE_METERS = 0.01f;
        private static readonly int[] _seeds = { 1, 17, 4242, 90001 };
        private static readonly int[] _loops = { 0, 4, 9 };

        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;

        [OneTimeSetUp]
        public void OneTimeSetUp() =>
            _composition = new FlightFieldEditorCompositionFactory().Create();

        [Test]
        public void WhenTheSameRequestIsGeneratedTwice_ThenThePointsAreTheSame() {
            foreach (int seed in _seeds) {
                FlightField first = Generate(seed, 0);
                FlightField second = Generate(seed, 0);

                CollectionAssert.AreEqual(first.PointsOfInterest, second.PointsOfInterest, $"Seed {seed}: the points of interest differ between two generations.");
            }
        }

        [Test]
        public void WhenAFieldIsGenerated_ThenItHasPointsWithinTheConfiguredCount() {
            FlightFieldConfiguration configuration = _composition.Configuration;
            foreach (int seed in _seeds) {
                foreach (int loop in _loops) {
                    int count = Generate(seed, loop).PointsOfInterest.Count;

                    Assert.That(count, Is.InRange(configuration.PointOfInterestMinCount, configuration.PointOfInterestMaxCount), $"Seed {seed} loop {loop}: {count} points of interest.");
                }
            }
        }

        [Test]
        public void WhenAFieldIsGenerated_ThenEveryPointLiesOffTheCourseWithinTheSideRange() {
            FlightFieldConfiguration configuration = _composition.Configuration;
            foreach (int seed in _seeds) {
                foreach (int loop in _loops) {
                    FieldRequest request = _scenario.CreateRequest(_composition.RequestFactory, seed, loop);
                    FlightField field = _composition.Generator.Generate(request);
                    foreach (Vector3 point in WorldPoints(field)) {
                        float side = DistanceToCourse(point, request);

                        Assert.That(side, Is.InRange(configuration.PointOfInterestMinSideMeters - TOLERANCE_METERS, configuration.PointOfInterestMaxSideMeters + TOLERANCE_METERS),
                            $"Seed {seed} loop {loop}: a point of interest {side:0.0} m beside the course.");
                    }
                }
            }
        }

        [Test]
        public void WhenAFieldIsGenerated_ThenNoRockStandsInAPointsClearCircle() {
            float clearRadius = _composition.Configuration.PointOfInterestClearRadiusMeters;
            foreach (int seed in _seeds) {
                foreach (int loop in _loops) {
                    FlightField field = Generate(seed, loop);
                    foreach (Vector3 point in WorldPoints(field))
                        AssertClear(field, point, clearRadius, $"Seed {seed} loop {loop}");
                }
            }
        }

        private FlightField Generate(int seed, int loop) =>
            _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, seed, loop));

        private static List<Vector3> WorldPoints(FlightField field) {
            List<Vector3> points = new();
            foreach (Vector3 fieldPosition in field.PointsOfInterest)
                points.Add(field.Frame.FieldToWorld(fieldPosition));

            return points;
        }

        private static float DistanceToCourse(Vector3 point, FieldRequest request) {
            Vector2 start = FieldPlaneGeometry.ToGround(request.CruiseStart);
            Vector2 end = FieldPlaneGeometry.ToGround(request.StationB);
            Vector2 direction = (end - start).normalized;
            Vector2 offset = FieldPlaneGeometry.ToGround(point) - start;
            return Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
        }

        private static void AssertClear(FlightField field, Vector3 point, float clearRadius, string context) {
            Vector2Int size = field.Frame.Size;
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    Vector2Int cell = new(x, y);
                    if (Vector3.Distance(field.Frame.CellToWorld(cell), point) > clearRadius)
                        continue;

                    FieldCell content = field.Cells[cell];
                    Assert.IsFalse(content.IsOccupied || content.HasDebris, $"{context}: cell {cell} in a point's clear circle holds a rock.");
                }
            }
        }
    }
}
