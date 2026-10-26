using System.Collections.Generic;
using Features.WeaponModule.Scripts.Parts.Delivery;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class RecordingShotOutcomeReceiver : IShotOutcomeReceiver {
        private readonly List<WeaponShotOutcome> _outcomes = new List<WeaponShotOutcome>();

        public IReadOnlyList<WeaponShotOutcome> Outcomes => _outcomes;

        public void ReceiveOutcome(in WeaponShotOutcome outcome) =>
            _outcomes.Add(outcome);
    }
}
