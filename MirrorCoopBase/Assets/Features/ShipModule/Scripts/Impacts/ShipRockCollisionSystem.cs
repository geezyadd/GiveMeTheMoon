using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Server, every physics step of a flight (takeoff, cruise, landing): the rocks the hull touched since the last
    // step. A small rock breaks, damages the hull and pushes the ship lightly; a large one knocks the ship back once
    // per contact and keeps pushing it out while the contact lasts. Every hit goes to every peer as an impact.
    public sealed class ShipRockCollisionSystem : IFixedTickable {
        private static readonly PlayerKey _noInstigator = new(string.Empty);

        private readonly IShipHullStepSource _shipHullStepSource;
        private readonly IShipHullRockScanner _shipHullRockScanner;
        private readonly IShipRockKnockbackRule _shipRockKnockbackRule;
        private readonly IShipRockDamageRule _shipRockDamageRule;
        private readonly IFieldRockBreaker _fieldRockBreaker;
        private readonly IShipHull _shipHull;
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;
        private readonly List<ShipRockContact> _contacts = new();
        private readonly ShipRockContactCooldowns _cooldowns = new();

        public ShipRockCollisionSystem(
            IShipHullStepSource shipHullStepSource,
            IShipHullRockScanner shipHullRockScanner,
            IShipRockKnockbackRule shipRockKnockbackRule,
            IShipRockDamageRule shipRockDamageRule,
            IFieldRockBreaker fieldRockBreaker,
            IShipHull shipHull,
            ShipRockImpactConfiguration shipRockImpactConfiguration) {
            _shipHullStepSource = shipHullStepSource;
            _shipHullRockScanner = shipHullRockScanner;
            _shipRockKnockbackRule = shipRockKnockbackRule;
            _shipRockDamageRule = shipRockDamageRule;
            _fieldRockBreaker = fieldRockBreaker;
            _shipHull = shipHull;
            _shipRockImpactConfiguration = shipRockImpactConfiguration;
        }

        public void FixedTick() {
            if (!_shipHullStepSource.TryGetStep(out ShipHullStep step)) {
                _cooldowns.Clear();
                return;
            }

            ShipBase ship = step.Ship;
            Vector3 velocity = ship.Knockback.Velocity;
            _shipHullRockScanner.Scan(step.Boxes, step.From, step.To, _shipRockKnockbackRule.CalculateLargeLookAhead(velocity), _contacts);
            foreach (ShipRockContact contact in _contacts)
                HandleContact(ship, contact, velocity);

            _cooldowns.ForgetCooledDown(Time.time, _shipRockImpactConfiguration.LargeHitCooldownSeconds);
        }

        private void HandleContact(ShipBase ship, ShipRockContact contact, Vector3 velocity) {
            Vector3 heading = ReadHeading(ship.transform);
            Vector3 away = _shipRockKnockbackRule.CalculateAwayDirection(ship.transform.position, contact.Rock.Center, heading);
            if (contact.Kind == ShipRockImpactKind.Small)
                HitSmallRock(ship, contact, away, heading, velocity);
            else
                TouchLargeRock(ship, contact, away, heading, velocity);
        }

        private void HitSmallRock(ShipBase ship, ShipRockContact contact, Vector3 away, Vector3 heading, Vector3 velocity) {
            if (_fieldRockBreaker.ServerBreak(contact.Rock.Id, FieldRockDestroyCause.ShipCollision, _noInstigator) != FieldRockBreakResult.Broken)
                return;

            float speed = velocity.magnitude;
            ApplyDamage(_shipRockDamageRule.CalculateSmallRockDamage(contact.Rock.Diameter, speed), contact);
            ApplyKnockback(ship, _shipRockKnockbackRule.CalculateSmallPush(away, heading, velocity));
            ship.ImpactSync.ServerBroadcast(new ShipRockImpact(ShipRockImpactKind.Small, contact.HullPoint, _shipRockDamageRule.CalculateStrength(speed)));
        }

        private void TouchLargeRock(ShipBase ship, ShipRockContact contact, Vector3 away, Vector3 heading, Vector3 velocity) {
            if (!_cooldowns.RecordContact(contact.Rock.Id, Time.time, _shipRockImpactConfiguration.LargeHitCooldownSeconds)) {
                ship.Knockback.SustainKnockback(away, _shipRockImpactConfiguration.ContactPushOutSpeed);
                return;
            }

            ApplyKnockback(ship, _shipRockKnockbackRule.CalculateLargeKnockback(away, heading, velocity));
            float damage = _shipRockDamageRule.CalculateLargeRockDamage();
            if (damage > 0f)
                ApplyDamage(damage, contact);

            ship.ImpactSync.ServerBroadcast(new ShipRockImpact(ShipRockImpactKind.Large, contact.HullPoint, _shipRockDamageRule.CalculateStrength(velocity.magnitude)));
        }

        private void ApplyDamage(float amount, ShipRockContact contact) =>
            _shipHull.ServerApplyDamage(new ShipDamage(amount, ShipDamageCause.Collision, contact.HullPoint, contact.Rock.Id.GetHashCode()));

        private static void ApplyKnockback(ShipBase ship, ShipKnockbackImpulse impulse) =>
            ship.Knockback.ApplyKnockback(impulse.Velocity, impulse.FadeSeconds, impulse.MaxAcceleration);

        private static Vector3 ReadHeading(Transform ship) {
            Vector3 forward = ship.forward;
            forward.y = 0f;
            return forward.normalized;
        }
    }
}
