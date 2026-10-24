using System;
using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Editor.Tests {
    // Raises target balloon pops by hand, as the server's popper would.
    public sealed class TargetEventsStub : IFieldTargetEvents {
        private const string KIND_ID = "balloon";
        private const string SHOOTER_ID = "client:shooter";
        private const string WEAPON_ID = "minigun";
        private const float RADIUS = 1f;
        private const int FIELD_NUMBER = 1;
        private const int LOOP_INDEX = 0;

        public event Action<FieldTargetPopped> TargetPopped;

        public void Pop(int index, int reward) {
            FieldTargetDescription target = new(new FieldTargetId(index), KIND_ID, Vector3.zero, RADIUS, reward);
            TargetPopped?.Invoke(new FieldTargetPopped(target, FieldTargetPopCause.Weapon, new PlayerKey(SHOOTER_ID), WEAPON_ID, FIELD_NUMBER, LOOP_INDEX));
        }
    }
}
