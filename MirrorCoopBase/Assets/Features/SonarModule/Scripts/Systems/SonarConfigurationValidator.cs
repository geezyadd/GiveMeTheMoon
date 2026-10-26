using System;
using System.Collections.Generic;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // The one startup check of the sonar data: every kind listed once by a unique id, the source kinds among them,
    // the balloon tiers rising from reward 0, and the timings and limits above zero.
    public sealed class SonarConfigurationValidator : IInitializable {
        private const string ERROR_FORMAT = "{0}: {1}";

        private readonly SonarConfiguration _sonarConfiguration;

        public SonarConfigurationValidator(SonarConfiguration sonarConfiguration) =>
            _sonarConfiguration = sonarConfiguration;

        public void Initialize() {
            ValidateKinds();
            ValidateKindListed(_sonarConfiguration.StationKind, nameof(SonarConfiguration.StationKind));
            ValidateKindListed(_sonarConfiguration.LargeRockKind, nameof(SonarConfiguration.LargeRockKind));
            ValidateKindListed(_sonarConfiguration.BigRockKind, nameof(SonarConfiguration.BigRockKind));
            ValidateKindListed(_sonarConfiguration.DebrisKind, nameof(SonarConfiguration.DebrisKind));
            ValidateTargetTiers();
            ValidatePositive(_sonarConfiguration.MinRangeMeters, nameof(SonarConfiguration.MinRangeMeters));
            ValidatePositive(_sonarConfiguration.PingCooldownSeconds, nameof(SonarConfiguration.PingCooldownSeconds));
            ValidatePositive(_sonarConfiguration.PingWaveSeconds, nameof(SonarConfiguration.PingWaveSeconds));
            ValidatePositive(_sonarConfiguration.EchoFadeSeconds, nameof(SonarConfiguration.EchoFadeSeconds));
            ValidatePositive(_sonarConfiguration.MarkLimit, nameof(SonarConfiguration.MarkLimit));
            ValidatePositive(_sonarConfiguration.ServerCheckSeconds, nameof(SonarConfiguration.ServerCheckSeconds));
            ValidatePositive(_sonarConfiguration.ConsoleKeepOpenMeters, nameof(SonarConfiguration.ConsoleKeepOpenMeters));
            ValidatePositive(_sonarConfiguration.DebrisCellMeters, nameof(SonarConfiguration.DebrisCellMeters));
            ValidatePositive(_sonarConfiguration.DebrisMinRocks, nameof(SonarConfiguration.DebrisMinRocks));
            ValidatePositive(_sonarConfiguration.BigRockMinDiameterMeters, nameof(SonarConfiguration.BigRockMinDiameterMeters));
        }

        private void ValidateTargetTiers() {
            IReadOnlyList<SonarTargetTier> tiers = _sonarConfiguration.TargetTiers;
            if (tiers.Count == 0 || tiers[0].MinReward != 0)
                Fail($"{nameof(SonarConfiguration.TargetTiers)} must start with a tier from reward 0.");

            for (int i = 0; i < tiers.Count; i++) {
                ValidateKindListed(tiers[i].Kind, $"{nameof(SonarConfiguration.TargetTiers)}[{i}]");
                if (i > 0 && tiers[i].MinReward <= tiers[i - 1].MinReward)
                    Fail($"{nameof(SonarConfiguration.TargetTiers)} must rise: tier {i} starts at {tiers[i].MinReward}.");
            }
        }

        private void ValidateKinds() {
            HashSet<string> ids = new HashSet<string>();
            IReadOnlyList<SonarContactKind> kinds = _sonarConfiguration.Kinds;
            for (int i = 0; i < kinds.Count; i++) {
                SonarContactKind kind = kinds[i];
                if (kind == null || string.IsNullOrEmpty(kind.Id))
                    Fail($"{nameof(SonarConfiguration.Kinds)}[{i}] is empty or has no id.");

                if (ids.Add(kind.Id) == false)
                    Fail($"the kind id '{kind.Id}' is listed twice.");

                if (kind.MarkSeconds <= 0f || kind.IconPixels <= 0f)
                    Fail($"the kind '{kind.Id}' needs a mark time and an icon size above zero.");
            }
        }

        private void ValidateKindListed(SonarContactKind kind, string fieldName) {
            IReadOnlyList<SonarContactKind> kinds = _sonarConfiguration.Kinds;
            for (int i = 0; i < kinds.Count; i++) {
                if (kinds[i] == kind && kind != null)
                    return;
            }

            Fail($"{fieldName} must be set and listed in {nameof(SonarConfiguration.Kinds)}.");
        }

        private void ValidatePositive(float value, string fieldName) {
            if (value <= 0f)
                Fail($"{fieldName} must be above zero, found {value}.");
        }

        private void Fail(string message) =>
            throw new InvalidOperationException(string.Format(ERROR_FORMAT, _sonarConfiguration.name, message));
    }
}
