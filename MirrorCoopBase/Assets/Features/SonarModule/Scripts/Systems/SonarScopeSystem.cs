using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Configurations;
using Features.SonarModule.Scripts.Core;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // The ping game on the navigator's peer: a ping takes the contacts in range from this peer's own data, the wave
    // runs out from the ship and lights each one as it passes, the echoes fade, and every frame the scope gets the
    // echoes laid out around the ship's nose (the biggest first), its heading and the station's bearing. Nothing of
    // it goes over the network.
    public sealed class SonarScopeSystem : IInitializable, IDisposable, ITickable {
        private const float WAVE_GONE = -1f;
        private const float WAVE_END_SHARE = 1f;

        private readonly SonarScopeModel _sonarScopeModel;
        private readonly SonarConsoleRequests _sonarConsoleRequests;
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly ISonarContactService _sonarContactService;
        private readonly ISonarRangeService _sonarRangeService;
        private readonly ISonarPingRule _sonarPingRule;
        private readonly ISonarScopeLayout _sonarScopeLayout;
        private readonly ISonarBearingService _sonarBearingService;
        private readonly ISonarCondition _sonarCondition;
        private readonly SonarConfiguration _sonarConfiguration;
        private readonly List<SonarContact> _unlitContacts = new();

        public SonarScopeSystem(
            SonarScopeModel sonarScopeModel,
            SonarConsoleRequests sonarConsoleRequests,
            IReadOnlySonarConsoleModel sonarConsoleModel,
            IShipClientBindingModel shipClientBindingModel,
            ISonarContactService sonarContactService,
            ISonarRangeService sonarRangeService,
            ISonarPingRule sonarPingRule,
            ISonarScopeLayout sonarScopeLayout,
            ISonarBearingService sonarBearingService,
            ISonarCondition sonarCondition,
            SonarConfiguration sonarConfiguration) {
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleRequests = sonarConsoleRequests;
            _sonarConsoleModel = sonarConsoleModel;
            _shipClientBindingModel = shipClientBindingModel;
            _sonarContactService = sonarContactService;
            _sonarRangeService = sonarRangeService;
            _sonarPingRule = sonarPingRule;
            _sonarScopeLayout = sonarScopeLayout;
            _sonarBearingService = sonarBearingService;
            _sonarCondition = sonarCondition;
            _sonarConfiguration = sonarConfiguration;
        }

        public void Initialize() {
            _sonarConsoleRequests.OnPingRequested += OnPingRequested;
            _sonarScopeModel.OnOpenChanged += OnScopeOpenChanged;
        }

        public void Dispose() {
            _sonarConsoleRequests.OnPingRequested -= OnPingRequested;
            _sonarScopeModel.OnOpenChanged -= OnScopeOpenChanged;
        }

        public void Tick() {
            ShipBase ship = _shipClientBindingModel.Ship;
            if (_sonarScopeModel.IsOpen == false || ship == null)
                return;

            float now = Time.time;
            float range = _sonarRangeService.GetRangeMeters();
            float waveShare = _sonarPingRule.GetWaveShare(now, _sonarScopeModel.LastPingAt);
            Vector3 shipPosition = ship.transform.position;
            float shipYaw = ship.transform.eulerAngles.y;
            LightReachedContacts(shipPosition, range, waveShare, now);
            RemoveFadedEchoes(now);
            LayOutBlips(shipPosition, shipYaw, range, now);
            bool isWaveRunning = waveShare <= WAVE_END_SHARE;
            _sonarScopeModel.CommitFrame(new SonarScopeFrame(
                range,
                isWaveRunning ? waveShare : WAVE_GONE,
                _sonarPingRule.GetCooldownShare(now, _sonarScopeModel.LastPingAt),
                _sonarCondition.IsWorking,
                _sonarConsoleModel.Marks.Count,
                _sonarConfiguration.MarkLimit,
                _sonarBearingService.GetBearings(shipPosition, shipYaw, range)));
        }

        private void LightReachedContacts(Vector3 shipPosition, float range, float waveShare, float now) {
            for (int i = _unlitContacts.Count - 1; i >= 0; i--) {
                SonarContact contact = _unlitContacts[i];
                float share = Mathf.Min(_sonarScopeLayout.GetDistanceShare(shipPosition, contact.Position, range), WAVE_END_SHARE);
                if (share > waveShare)
                    continue;

                LightEcho(new SonarEcho(contact, now));
                _unlitContacts.RemoveAt(i);
            }

            if (waveShare > WAVE_END_SHARE)
                _unlitContacts.Clear();
        }

        // A contact pinged again replaces its old echo, so it never shows twice.
        private void LightEcho(SonarEcho echo) {
            IReadOnlyList<SonarEcho> echoes = _sonarScopeModel.Echoes;
            for (int i = 0; i < echoes.Count; i++) {
                if (echoes[i].Contact.Id != echo.Contact.Id)
                    continue;

                _sonarScopeModel.ReplaceEchoAt(i, echo);
                return;
            }

            _sonarScopeModel.AddEcho(echo);
        }

        private void RemoveFadedEchoes(float now) {
            IReadOnlyList<SonarEcho> echoes = _sonarScopeModel.Echoes;
            for (int i = echoes.Count - 1; i >= 0; i--) {
                if (_sonarPingRule.GetEchoAlpha(now, echoes[i].LitAt) <= 0f)
                    _sonarScopeModel.RemoveEchoAt(i);
            }
        }

        private void LayOutBlips(Vector3 shipPosition, float shipYaw, float range, float now) {
            _sonarScopeModel.BeginFrame();
            IReadOnlyList<SonarEcho> echoes = _sonarScopeModel.Echoes;
            for (int i = 0; i < echoes.Count; i++) {
                SonarContact contact = echoes[i].Contact;
                if (_sonarScopeLayout.TryPlace(shipPosition, shipYaw, contact.Position, range, contact.Kind.IsShownBeyondRange, out Vector2 scopePosition) == false)
                    continue;

                float alpha = _sonarPingRule.GetEchoAlpha(now, echoes[i].LitAt);
                _sonarScopeModel.AddBlip(new SonarBlip(contact.Id, contact.Kind, scopePosition, contact.Radius / range, alpha, IsMarked(contact.Id)));
            }

            _sonarScopeModel.SortBlipsBySize();
        }

        private bool IsMarked(string contactId) {
            IReadOnlyList<SonarMark> marks = _sonarConsoleModel.Marks;
            for (int i = 0; i < marks.Count; i++) {
                if (marks[i].ContactId == contactId)
                    return true;
            }

            return false;
        }

        // A broken radar still pings and still cools down, but its wave finds nothing.
        private void OnPingRequested() {
            ShipBase ship = _shipClientBindingModel.Ship;
            float now = Time.time;
            bool canPing = _sonarScopeModel.IsOpen && ship != null && _sonarPingRule.CanPing(now, _sonarScopeModel.LastPingAt);
            if (canPing == false)
                return;

            _sonarScopeModel.StartPing(now);
            if (_sonarCondition.IsWorking)
                _sonarContactService.CollectContacts(ship.transform.position, _sonarRangeService.GetRangeMeters(), _unlitContacts);
            else
                _unlitContacts.Clear();
        }

        private void OnScopeOpenChanged() =>
            _unlitContacts.Clear();
    }
}
