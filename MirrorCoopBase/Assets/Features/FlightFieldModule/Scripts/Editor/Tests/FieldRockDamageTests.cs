using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Weapon damage on field rocks, on the server: hit points come from the rock's size in the data, hits add up, a
    // rock breaks once as a Weapon break by its shooter, an unbreakable rock ignores hits, a hit off the rock is
    // refused, the damage goes with the field, and a rock id survives the trip through a weapon target id.
    public sealed class FieldRockDamageTests {
        private const string GIANTS_PROFILE = "obstacles_giants";
        private const string WEAPON_ID = "minigun";
        private const int SEED = 7;
        private const int LOOP = 2;
        private const float PROBE_RADIUS = 120f;
        private const int PROBE_STEP_CELLS = 4;
        private const float DAMAGE_PER_HIT = 1f;
        private const float MINIGUN_HITS_PER_SECOND = 20f;
        private const float SMALL_ROCK_MAX_SECONDS = 0.8f;
        private const float BIG_ROCK_MIN_SECONDS = 4f;
        private const float BIG_ROCK_DIAMETER = 150f;
        private const float OFF_ROCK_METERS = 50f;
        private const int MANY_HITS = 500;
        private const ulong DEBUG_TARGET_ID = 0xDEB0000000000001UL;

        private static readonly PlayerKey _shooter = new("client:gunner");

        private readonly FieldDebugScenario _scenario = new();

        private FlightFieldEditorComposition _composition;
        private FieldRockDurabilityConfiguration _durability;
        private FlightField _field;
        private List<FieldRockDescription> _rocks;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _durability = AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default);
            FieldDifficultyProfile profile = _composition.ProfileLookup.GetRequired(GIANTS_PROFILE);
            _field = _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, SEED, LOOP), profile));
            _rocks = FindRocks(new FieldRockTestRig(_composition, _durability, _field));
        }

        [Test]
        public void WhenRocksOfEverySizeAreRead_ThenTheirHitPointsComeFromTheirDiameter() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription smallest = _rocks.Where(rock => rock.IsBreakable).OrderBy(rock => rock.Diameter).First();
            FieldRockDescription biggest = _rocks.Where(rock => rock.IsBreakable).OrderBy(rock => rock.Diameter).Last();

            float smallSeconds = rig.HitPoints.GetHitPoints(smallest) / MINIGUN_HITS_PER_SECOND;
            float bigHitPoints = _durability.HitPointsByDiameter.Evaluate(BIG_ROCK_DIAMETER);

            Assert.LessOrEqual(smallSeconds, SMALL_ROCK_MAX_SECONDS, $"A {smallest.Diameter:0} m rock takes {smallSeconds:0.00} s of minigun fire.");
            Assert.Greater(rig.HitPoints.GetHitPoints(biggest), rig.HitPoints.GetHitPoints(smallest), "A bigger rock must hold more hit points.");
            Assert.GreaterOrEqual(bigHitPoints / MINIGUN_HITS_PER_SECOND, BIG_ROCK_MIN_SECONDS, "A big rock breaks too fast.");
            Assert.AreEqual(_durability.HitPointsByDiameter.Evaluate(smallest.Diameter), rig.HitPoints.GetHitPoints(smallest), 0.001f);
        }

        [Test]
        public void WhenHitsStayBelowTheHitPoints_ThenTheDamageAddsUpAndTheRockStands() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription rock = FindBigBreakableRock();

            FieldRockDamageResult first = rig.DamageService.ServerApplyHit(rock.Id, CreateHit(rock.Center));
            FieldRockDamageResult second = rig.DamageService.ServerApplyHit(rock.Id, CreateHit(rock.Center));

            Assert.AreEqual(FieldRockDamageResult.Damaged, first);
            Assert.AreEqual(FieldRockDamageResult.Damaged, second);
            Assert.AreEqual(2f * DAMAGE_PER_HIT, rig.DamageModel.GetDamageTaken(rock.Id), 0.001f);
            Assert.IsFalse(rig.BrokenRocks.IsBroken(rock.Id));
        }

        [Test]
        public void WhenTheDamageReachesTheHitPoints_ThenTheRockBreaksOnceAsAWeaponBreakByTheShooter() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription rock = FindBigBreakableRock();
            List<FieldRockDestroyed> events = new();
            rig.Breaker.RockDestroyed += events.Add;
            int hitsToBreak = Mathf.CeilToInt(rig.HitPoints.GetHitPoints(rock) / DAMAGE_PER_HIT);

            List<FieldRockDamageResult> results = Enumerable.Range(0, hitsToBreak + 2).Select(_ => rig.DamageService.ServerApplyHit(rock.Id, CreateHit(rock.Center))).ToList();

            Assert.AreEqual(FieldRockDamageResult.Broken, results[hitsToBreak - 1], "The rock must break on the hit that reaches its hit points.");
            Assert.IsTrue(results.Take(hitsToBreak - 1).All(result => result == FieldRockDamageResult.Damaged));
            Assert.IsTrue(results.Skip(hitsToBreak).All(result => result == FieldRockDamageResult.NotFound), "A broken rock still takes hits.");
            Assert.AreEqual(1, events.Count, "RockDestroyed must fire once per rock.");
            Assert.AreEqual(rock.Id, events[0].Id);
            Assert.AreEqual(FieldRockDestroyCause.Weapon, events[0].Cause);
            Assert.AreEqual(_shooter, events[0].Instigator);
            Assert.AreEqual(WEAPON_ID, events[0].WeaponId);
            Assert.AreEqual(0, rig.DamageModel.DamagedCount, "A broken rock keeps its damage entry.");
        }

        [Test]
        public void WhenAnUnbreakableRockIsHit_ThenItIgnoresTheHits() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription giant = _rocks.First(rock => !rock.IsBreakable);
            List<FieldRockDestroyed> events = new();
            rig.Breaker.RockDestroyed += events.Add;

            List<FieldRockDamageResult> results = Enumerable.Range(0, MANY_HITS).Select(_ => rig.DamageService.ServerApplyHit(giant.Id, CreateHit(giant.Center))).ToList();

            Assert.IsTrue(results.All(result => result == FieldRockDamageResult.Unbreakable));
            Assert.IsEmpty(events);
            Assert.AreEqual(0, rig.DamageModel.DamagedCount);
        }

        [Test]
        public void WhenTheReportedPointIsOffTheRock_ThenTheHitIsRefused() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription rock = FindBigBreakableRock();
            Vector3 offRock = rock.Center + Vector3.up * (rock.Radius + _durability.HitPointToleranceMeters + OFF_ROCK_METERS);

            FieldRockDamageResult result = rig.DamageService.ServerApplyHit(rock.Id, CreateHit(offRock));

            Assert.AreEqual(FieldRockDamageResult.TooFar, result);
            Assert.AreEqual(0, rig.DamageModel.DamagedCount);
        }

        [Test]
        public void WhenTheFieldIsReset_ThenTheDamageTakenIsForgotten() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription rock = FindBigBreakableRock();
            rig.DamageService.ServerApplyHit(rock.Id, CreateHit(rock.Center));

            rig.DamageService.ServerReset();

            Assert.AreEqual(0, rig.DamageModel.DamagedCount);
            Assert.AreEqual(0f, rig.DamageModel.GetDamageTaken(rock.Id));
        }

        [Test]
        public void WhenTheFieldIsTakenDown_ThenAHitFindsNoRock() {
            FieldRockTestRig rig = CreateRig();
            FieldRockDescription rock = FindBigBreakableRock();

            rig.ServerModel.ClearField();

            Assert.AreEqual(FieldRockDamageResult.NotFound, rig.DamageService.ServerApplyHit(rock.Id, CreateHit(rock.Center)));
        }

        [Test]
        public void WhenARockIdBecomesATargetId_ThenItComesBackTheSameAndOtherIdsAreNotRocks() {
            FieldRockTargetIds targetIds = new();
            FieldRockId cluster = _rocks.First(rock => rock.Id.Source == FieldRockSource.Cluster).Id;
            FieldRockId debris = _rocks.First(rock => rock.Id.Source == FieldRockSource.Debris).Id;

            Assert.IsTrue(targetIds.TryGetRockId(targetIds.GetTargetId(cluster), out FieldRockId clusterBack));
            Assert.IsTrue(targetIds.TryGetRockId(targetIds.GetTargetId(debris), out FieldRockId debrisBack));
            Assert.AreEqual(cluster, clusterBack);
            Assert.AreEqual(debris, debrisBack);
            Assert.AreNotEqual(targetIds.GetTargetId(cluster), targetIds.GetTargetId(debris));
            Assert.IsFalse(targetIds.TryGetRockId(DEBUG_TARGET_ID, out _), "The debug dummy's id must not read as a rock.");
        }

        [Test]
        public void WhenTheWeaponHitPathAsksForARock_ThenItIsAliveUntilItBreaks() {
            FieldRockTestRig rig = CreateRig();
            FieldRockTargetIds targetIds = new();
            RecordingWeaponTargetRegistry registry = new();
            FieldRockWeaponTargetSystem targets = new(registry, targetIds, new FieldRockWeaponTarget(rig.Query, rig.DamageService));
            targets.Initialize();
            FieldRockDescription rock = _rocks.Where(other => other.IsBreakable).OrderBy(other => other.Diameter).First();

            Assert.AreEqual(1, registry.Sources.Count, "The rocks must register as one source.");
            Assert.IsEmpty(registry.Targets, "The rocks must not register one by one.");
            Assert.IsTrue(targets.TryGetTarget(targetIds.GetTargetId(rock.Id), out IWeaponTarget target));
            Assert.IsTrue(target.IsAlive);
            for (int i = 0; i < Mathf.CeilToInt(rig.HitPoints.GetHitPoints(rock)); i++)
                target.ServerReceiveHit(CreateHit(rock.Center));

            Assert.IsTrue(rig.BrokenRocks.IsBroken(rock.Id));
            Assert.IsFalse(target.IsAlive, "A broken rock is still a live target.");
            Assert.IsFalse(targets.TryGetTarget(DEBUG_TARGET_ID, out _));
        }

        private FieldRockTestRig CreateRig() =>
            new(_composition, _durability, _field);

        private FieldRockDescription FindBigBreakableRock() =>
            _rocks.Where(rock => rock.IsBreakable).OrderByDescending(rock => rock.Diameter).First();

        private static WeaponHit CreateHit(Vector3 point) =>
            new(0UL, point, Vector3.up, DAMAGE_PER_HIT, _shooter, WEAPON_ID);

        // Rocks on the flight plane along the middle of the field: debris, clusters and giants.
        private List<FieldRockDescription> FindRocks(FieldRockTestRig rig) {
            Dictionary<FieldRockId, FieldRockDescription> rocks = new();
            List<FieldRockDescription> found = new();
            FieldFrame frame = _field.Frame;
            for (int along = 0; along < frame.LengthCells; along += PROBE_STEP_CELLS) {
                for (int across = PROBE_STEP_CELLS; across < frame.WidthCells; across += PROBE_STEP_CELLS) {
                    rig.Query.FindInSphere(frame.CellToWorld(new Vector2Int(along, across)), PROBE_RADIUS, found);
                    foreach (FieldRockDescription rock in found)
                        rocks[rock.Id] = rock;
                }
            }

            return rocks.Values.ToList();
        }
    }
}
