using System.Collections.Generic;
using Features.ShipDamageModule.Scripts.Damage;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // ShipDamageModule's impact path without a ship: the real damage rule picks from a fixed set of modules and hull
    // points with a fixed roll, and what it picks is recorded as broken / opened.
    public sealed class RuleImpactDamageService : IShipImpactDamageService {
        private readonly IShipDamageRule _shipDamageRule;
        private readonly List<ShipDamageCandidate> _modules;
        private readonly List<ShipDamageCandidate> _points;
        private readonly float _roll;

        public List<string> BrokenSockets { get; } = new();
        public List<string> OpenedBreaches { get; } = new();
        public List<float> Strengths { get; } = new();

        public RuleImpactDamageService(IShipDamageRule shipDamageRule, List<ShipDamageCandidate> modules, List<ShipDamageCandidate> points, float roll) {
            _shipDamageRule = shipDamageRule;
            _modules = modules;
            _points = points;
            _roll = roll;
        }

        public void ServerApplyImpact(Vector3 point, float strength) {
            Strengths.Add(strength);
            if (_shipDamageRule.TryPickBrokenModule(_modules, point, strength, _roll, out string socketId))
                BrokenSockets.Add(socketId);

            if (_shipDamageRule.TryPickBreach(_points, OpenedBreaches.Count, point, strength, _roll, out string pointId))
                OpenedBreaches.Add(pointId);
        }
    }
}
