using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Content;
using Features.PointOfInterestModule.Scripts.Network;
using Features.PointOfInterestModule.Scripts.Server;
using NUnit.Framework;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    // The server's rules of a point of interest with the shipped data: the zone starts its content once, the reward
    // container pays the crew once whether it is shot open or rammed, and a hit off the container does nothing.
    public sealed class PointOfInterestServerTests {
        private const int POINT_ID = 1;
        private static readonly Vector3 _point = new(300f, 0f, 400f);

        private PointOfInterestConfiguration _configuration;
        private PointOfInterestContentDefinition _content;
        private IPointOfInterestContentCatalog _catalog;
        private RecordingPointsOfInterest _points;
        private RecordingCrewWallet _wallet;
        private CountingContent _countingContent;
        private PointOfInterestServerModel _serverModel;
        private IPointOfInterestZoneRule _zoneRule;
        private IPointOfInterestOpenService _openService;

        [SetUp]
        public void SetUp() {
            _configuration = AddressableConfigurationLoader.Load<PointOfInterestConfiguration>(Address.Configurations.PointOfInterestConfiguration_Default);
            _content = _configuration.Contents[0];
            _catalog = new PointOfInterestContentCatalog(_configuration);
            _points = new RecordingPointsOfInterest();
            _points.ServerAdd(new PointOfInterestEntry { Id = POINT_ID, ContentId = _content.Id, Position = _point, State = PointOfInterestState.Dormant });
            _wallet = new RecordingCrewWallet();
            _countingContent = new CountingContent(_content.Id);
            _serverModel = new PointOfInterestServerModel();
            _zoneRule = new PointOfInterestZoneRule(_configuration);
            _openService = new PointOfInterestOpenService(_points, _points, _catalog, _wallet);
        }

        [Test]
        public void WhenTheZoneIsEnteredTwice_ThenTheContentStartsOnce() {
            PointOfInterestTriggerService trigger = new(_points, _points, new List<IPointOfInterestContent> { _countingContent });

            Assert.AreEqual(PointOfInterestTriggerResult.Triggered, trigger.ServerTrigger(POINT_ID));
            Assert.AreEqual(PointOfInterestTriggerResult.AlreadyTriggered, trigger.ServerTrigger(POINT_ID));
            Assert.AreEqual(1, _countingContent.Entries);
            Assert.AreEqual(PointOfInterestState.Triggered, _points.Get(0).State);
        }

        [Test]
        public void WhenTheShipIsInsideOrOutsideTheZone_ThenOnlyInsideCounts() {
            Vector3 outward = Vector3.right * _configuration.ZoneRadiusMeters;

            Assert.IsTrue(_zoneRule.IsInZone(_point, _point + outward * 0.9f));
            Assert.IsFalse(_zoneRule.IsInZone(_point, _point + outward * 1.1f));
        }

        [Test]
        public void WhenTheContainerIsOpenedTwice_ThenTheCrewIsPaidOnce() {
            Assert.AreEqual(PointOfInterestOpenResult.Opened, _openService.ServerOpen(POINT_ID));
            Assert.AreEqual(PointOfInterestOpenResult.AlreadyOpened, _openService.ServerOpen(POINT_ID));
            Assert.AreEqual(1, _wallet.Earnings);
            Assert.AreEqual(_content.Reward, _wallet.Balance);
            Assert.AreEqual(PointOfInterestState.Opened, _points.Get(0).State);
        }

        [Test]
        public void WhenHitsReachTheHitPoints_ThenTheContainerOpensAndPaysOnce() {
            PointOfInterestDamageService damage = new(_points, _zoneRule, _catalog, _openService, _serverModel);
            float hit = _content.ContainerHitPoints / 3f;
            Vector3 onContainer = _point + Vector3.up * _content.ContainerRadiusMeters;

            damage.ServerApplyHit(POINT_ID, onContainer, hit);
            damage.ServerApplyHit(POINT_ID, onContainer, hit);
            Assert.IsTrue(damage.IsStanding(POINT_ID), "Two thirds of the hit points opened the container.");
            damage.ServerApplyHit(POINT_ID, onContainer, hit);
            damage.ServerApplyHit(POINT_ID, onContainer, hit);

            Assert.IsFalse(damage.IsStanding(POINT_ID));
            Assert.AreEqual(1, _wallet.Earnings);
            Assert.AreEqual(_content.Reward, _wallet.Balance);
        }

        [Test]
        public void WhenAHitMissesTheContainer_ThenNothingIsDamaged() {
            PointOfInterestDamageService damage = new(_points, _zoneRule, _catalog, _openService, _serverModel);
            Vector3 farOff = _point + Vector3.up * (_content.ContainerRadiusMeters + _configuration.HitToleranceMeters + 1f);

            damage.ServerApplyHit(POINT_ID, farOff, _content.ContainerHitPoints * 2f);

            Assert.IsTrue(damage.IsStanding(POINT_ID));
            Assert.AreEqual(0, _wallet.Earnings);
        }

        [Test]
        public void WhenTheHullReachesTheContainer_ThenItRamsIt() {
            float reach = _content.ContainerRadiusMeters + _configuration.ShipRamReachMeters;

            Assert.IsTrue(_zoneRule.IsRamming(_point, _point + Vector3.forward * (reach - 0.5f), _content.ContainerRadiusMeters));
            Assert.IsFalse(_zoneRule.IsRamming(_point, _point + Vector3.forward * (reach + 0.5f), _content.ContainerRadiusMeters));
        }
    }
}
