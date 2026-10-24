using System;
using UnityEngine;

namespace Features.ShipModule.Scripts.Repair {
    // One place on the tool rack: which tool lies there at the start of a run.
    [Serializable]
    public sealed class ShipToolRackSlot {
        public RepairToolDefinition Tool;
        public Transform Point;
    }
}
