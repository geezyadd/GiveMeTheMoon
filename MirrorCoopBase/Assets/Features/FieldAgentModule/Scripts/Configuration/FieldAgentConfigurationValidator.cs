using System;
using System.Collections.Generic;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Configuration {
    // The one startup check of the field agent data: every kind listed once with a view, the kind count fits the
    // network's byte, and the rates and limits are above zero.
    public sealed class FieldAgentConfigurationValidator : IInitializable {
        private const string ERROR_FORMAT = "{0}: {1}";
        private const int MAX_KINDS = byte.MaxValue + 1;
        private const int MIN_FRAME_BUFFER = 2;

        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public FieldAgentConfigurationValidator(FieldAgentConfiguration fieldAgentConfiguration) =>
            _fieldAgentConfiguration = fieldAgentConfiguration;

        public void Initialize() {
            ValidateProfiles();
            ValidatePositive(_fieldAgentConfiguration.SendRate, nameof(FieldAgentConfiguration.SendRate));
            ValidatePositive(_fieldAgentConfiguration.RockQuerySeconds, nameof(FieldAgentConfiguration.RockQuerySeconds));
            ValidatePositive(_fieldAgentConfiguration.RockQueryMarginMeters, nameof(FieldAgentConfiguration.RockQueryMarginMeters));
            ValidatePositive(_fieldAgentConfiguration.RockQueryIntervalMinShare, nameof(FieldAgentConfiguration.RockQueryIntervalMinShare));
            ValidatePositive(_fieldAgentConfiguration.RockQueryIntervalSpreadSteps, nameof(FieldAgentConfiguration.RockQueryIntervalSpreadSteps));
            ValidatePositive(_fieldAgentConfiguration.RockQueryIntervalStepShare, nameof(FieldAgentConfiguration.RockQueryIntervalStepShare));
            ValidatePositive(_fieldAgentConfiguration.InterpolationDelaySeconds, nameof(FieldAgentConfiguration.InterpolationDelaySeconds));
            ValidatePositive(_fieldAgentConfiguration.DeckSampleMeters, nameof(FieldAgentConfiguration.DeckSampleMeters));
            ValidatePositive(_fieldAgentConfiguration.SpawnTries, nameof(FieldAgentConfiguration.SpawnTries));
            ValidatePositive(_fieldAgentConfiguration.SpawnMaxMeters - _fieldAgentConfiguration.SpawnMinMeters, nameof(FieldAgentConfiguration.SpawnMaxMeters));
            if (_fieldAgentConfiguration.FrameBufferSize < MIN_FRAME_BUFFER)
                Fail($"{nameof(FieldAgentConfiguration.FrameBufferSize)} must keep at least {MIN_FRAME_BUFFER} batches.");
        }

        private void ValidateProfiles() {
            IReadOnlyList<FieldAgentProfile> profiles = _fieldAgentConfiguration.Profiles;
            if (profiles.Count == 0 || profiles.Count > MAX_KINDS)
                Fail($"{nameof(FieldAgentConfiguration.Profiles)} must list 1..{MAX_KINDS} kinds, has {profiles.Count}.");

            HashSet<string> ids = new();
            foreach (FieldAgentProfile profile in profiles)
                ValidateProfile(profile, ids);
        }

        private void ValidateProfile(FieldAgentProfile profile, HashSet<string> ids) {
            if (profile == null || string.IsNullOrEmpty(profile.Id) || ids.Add(profile.Id) == false)
                Fail($"{nameof(FieldAgentConfiguration.Profiles)} has an empty, unnamed or repeated kind.");

            if (profile.ViewPrefab == null)
                Fail($"Kind {profile.Id} has no view prefab.");

            ValidatePositive(profile.MaxSpeed, profile.Id + "." + nameof(FieldAgentProfile.MaxSpeed));
            ValidatePositive(profile.Acceleration, profile.Id + "." + nameof(FieldAgentProfile.Acceleration));
            ValidatePositive(profile.TurnRateDegrees, profile.Id + "." + nameof(FieldAgentProfile.TurnRateDegrees));
            ValidatePositive(profile.DeckWalkSpeed, profile.Id + "." + nameof(FieldAgentProfile.DeckWalkSpeed));
            ValidatePositive(profile.LandingSeconds, profile.Id + "." + nameof(FieldAgentProfile.LandingSeconds));
            ValidatePositive(profile.TakeOffSeconds, profile.Id + "." + nameof(FieldAgentProfile.TakeOffSeconds));
        }

        private void ValidatePositive(float value, string name) {
            if (value <= 0f)
                Fail($"{name} must be above zero, is {value}.");
        }

        private void Fail(string message) =>
            throw new InvalidOperationException(string.Format(ERROR_FORMAT, _fieldAgentConfiguration.name, message));
    }
}
