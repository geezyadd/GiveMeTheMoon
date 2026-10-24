using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Configurations;
using Features.SonarModule.Scripts.Core;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Server: puts the navigator's marks into the synced model after the mark rule, and takes a mark out once its
    // time ran out or its object is gone (a broken rock, a dispersed debris patch, the station after landing).
    public sealed class SonarMarkSystem : ISonarMarkSystem, ITickable {
        private const int NO_MARK = -1;

        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly SonarConsoleBindingModel _sonarConsoleBindingModel;
        private readonly ISonarContactService _sonarContactService;
        private readonly ISonarMarkRule _sonarMarkRule;
        private readonly SonarConfiguration _sonarConfiguration;

        private float _nextCheckAt;

        public SonarMarkSystem(
            IPlayerIdentityService playerIdentityService,
            IReadOnlySonarConsoleModel sonarConsoleModel,
            SonarConsoleBindingModel sonarConsoleBindingModel,
            ISonarContactService sonarContactService,
            ISonarMarkRule sonarMarkRule,
            SonarConfiguration sonarConfiguration) {
            _playerIdentityService = playerIdentityService;
            _sonarConsoleModel = sonarConsoleModel;
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
            _sonarContactService = sonarContactService;
            _sonarMarkRule = sonarMarkRule;
            _sonarConfiguration = sonarConfiguration;
        }

        public void ServerMark(ISonarConsoleWriter writer, NetworkConnectionToClient connection, string contactId) {
            bool isNavigator = _playerIdentityService.GetKey(connection).Id == _sonarConsoleModel.NavigatorKey;
            bool isContactFound = _sonarContactService.TryGetContact(contactId, out SonarContact contact);
            int markIndex = FindMark(contactId);
            SonarMarkRequest request = new SonarMarkRequest(isNavigator, isContactFound, isContactFound && contact.Kind.IsMarkable, markIndex != NO_MARK, _sonarConsoleModel.Marks.Count);
            SonarMarkResult result = _sonarMarkRule.Evaluate(request);
            if (result == SonarMarkResult.Marked)
                writer.ServerAddMarks(_sonarMarkRule.CreateMark(contact, NetworkTime.time));
            else if (result == SonarMarkResult.Refreshed)
                writer.ServerSetMarks(markIndex, _sonarMarkRule.CreateMark(contact, NetworkTime.time));
        }

        public void Tick() {
            if (NetworkServer.active == false || _sonarConsoleBindingModel.IsBound == false || _sonarConsoleModel.Marks.Count == 0)
                return;

            if (Time.time < _nextCheckAt)
                return;

            _nextCheckAt = Time.time + _sonarConfiguration.ServerCheckSeconds;
            RemoveGoneMarks(_sonarConsoleBindingModel.Writer);
        }

        private void RemoveGoneMarks(ISonarConsoleWriter writer) {
            IReadOnlyList<SonarMark> marks = _sonarConsoleModel.Marks;
            for (int i = marks.Count - 1; i >= 0; i--) {
                SonarMark mark = marks[i];
                bool isGone = _sonarMarkRule.IsExpired(mark.ExpiresAt, NetworkTime.time) || _sonarContactService.TryGetContact(mark.ContactId, out _) == false;
                if (isGone)
                    writer.ServerRemoveMarksAt(i);
            }
        }

        private int FindMark(string contactId) {
            IReadOnlyList<SonarMark> marks = _sonarConsoleModel.Marks;
            for (int i = 0; i < marks.Count; i++) {
                if (marks[i].ContactId == contactId)
                    return i;
            }

            return NO_MARK;
        }
    }
}
